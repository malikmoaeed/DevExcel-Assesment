using InventoryManagement.Infrastructure;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Infrastructure.Persistence.Seed;
using InventoryManagement.Web.Filters;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(o => { o.Filters.Add<NotFoundFilter>(); o.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()); });
builder.Services.AddInventory(builder.Configuration.GetConnectionString("Inventory") ?? throw new InvalidOperationException("Missing Inventory connection string."));
var app = builder.Build();
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles(); app.UseRouting(); app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Products}/{action=Index}/{id?}");
if (app.Configuration.GetValue<bool>("Database:Initialize")) {
 using var scope = app.Services.CreateScope();
 var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
 await db.Database.MigrateAsync(); await DatabaseSeeder.SeedAsync(db);
}
app.Run();
public partial class Program;
