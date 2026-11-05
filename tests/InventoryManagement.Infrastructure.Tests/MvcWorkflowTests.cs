using System.Net;
using System.Text.RegularExpressions;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace InventoryManagement.Infrastructure.Tests;

public sealed class MvcWorkflowTests : IAsyncLifetime
{
    private readonly string connection = $"Server=(localdb)\\MSSQLLocalDB;Database=InventoryTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
    private WebApplicationFactory<Program> factory = null!;
    public Task InitializeAsync()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Inventory"] = connection, ["Database:Initialize"] = "true" }));
            b.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connection));
            });
        }); return Task.CompletedTask;
    }
    public async Task DisposeAsync() { await factory.DisposeAsync(); await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options); await db.Database.EnsureDeletedAsync(); }
    private static string Value(string html, string name)
    {
        var match = Regex.Match(html, "name=\"" + Regex.Escape(name) + "\"[^>]*value=\"([^\"]*)\"");
        Assert.True(match.Success, $"Missing form input: {name}"); return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    private static async Task<HttpResponseMessage> Post(HttpClient client, string url, string page, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = Value(page, "__RequestVerificationToken");
        return await client.PostAsync(url, new FormUrlEncodedContent(fields));
    }
    [Fact]
    public async Task ProductAndStockFormsWorkEndToEnd()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var list = await client.GetStringAsync("/Products"); Assert.Contains("Low stock", list); Assert.Contains("iPhone 15 Pro", list);
        var create = await client.GetStringAsync("/Products/Create"); Assert.DoesNotContain("name=\"CurrentStock\"", create);
        var response = await Post(client, "/Products/Create", create, new() { ["Sku"] = "WEB-TEST", ["Name"] = "Web product", ["ReorderLevel"] = "2" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); var detailUrl = response.Headers.Location!.ToString(); var id = detailUrl.Split('/').Last();
        var details = await client.GetStringAsync(detailUrl); Assert.Contains("No movements yet", details);
        async Task<HttpResponseMessage> Move(string type, string quantity, string note)
        {
            var page = await client.GetStringAsync(detailUrl);
            return await Post(client, $"/Products/RecordMovement/{id}", page, new() { ["Movement.Type"] = type, ["Movement.Quantity"] = quantity, ["Movement.Note"] = note, ["Movement.RequestId"] = Value(page, "Movement.RequestId") });
        }
        Assert.Equal(HttpStatusCode.Redirect, (await Move("1", "10", "<script>alert(1)</script>")).StatusCode);
        var rejected = await Move("2", "15", "Too many"); Assert.Equal(HttpStatusCode.OK, rejected.StatusCode); Assert.Contains("only 10 units", await rejected.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect, (await Move("2", "8", "Customer order")).StatusCode);
        details = await client.GetStringAsync(detailUrl); Assert.Contains("Low stock", details); Assert.Contains("Customer order", details); Assert.Contains("&lt;script&gt;", details); Assert.DoesNotContain("<script>alert", details);
        Assert.Contains("Quantity must be greater than zero", await (await Move("1", "0", "")).Content.ReadAsStringAsync());
        var invalidType = await Move("99", "1", "");
        Assert.Equal(HttpStatusCode.OK, invalidType.StatusCode);
        Assert.Contains("validation-summary-errors", await invalidType.Content.ReadAsStringAsync());
        create = await client.GetStringAsync("/Products/Create");
        var duplicate = await Post(client, "/Products/Create", create, new() { ["Sku"] = " web-test ", ["Name"] = "Duplicate", ["ReorderLevel"] = "0" }); Assert.Contains("SKU already exists", await duplicate.Content.ReadAsStringAsync());
        var activePage = await client.GetStringAsync(detailUrl);
        var edit = await client.GetStringAsync($"/Products/Edit/{id}");
        response = await Post(client, $"/Products/Edit/{id}", edit, new() { ["Id"] = id, ["Sku"] = "WEB-TEST", ["Name"] = "Updated product", ["ReorderLevel"] = "3", ["IsActive"] = "false" }); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        details = await client.GetStringAsync(detailUrl); Assert.Contains("Updated product", details); Assert.Contains("This product is inactive", details);
        var inactive = await Post(client, $"/Products/RecordMovement/{id}", activePage, new() { ["Movement.Type"] = "1", ["Movement.Quantity"] = "1", ["Movement.RequestId"] = Value(activePage, "Movement.RequestId") });
        Assert.Contains("Inactive products cannot", await inactive.Content.ReadAsStringAsync());
        Assert.Contains("Updated product", await client.GetStringAsync("/Products?search=WEB-TEST"));
        Assert.Contains("No products found", await client.GetStringAsync("/Products?search=not-a-product"));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Products/Details/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Products/Create", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Contains("InventoryTests_", database.Database.GetDbConnection().Database);
        Assert.Equal(2, await database.StockMovements.CountAsync(m => m.ProductId == int.Parse(id)));
    }
}
