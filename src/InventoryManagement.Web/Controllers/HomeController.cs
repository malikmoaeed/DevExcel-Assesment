using Microsoft.AspNetCore.Mvc;
namespace InventoryManagement.Web.Controllers;
public sealed class HomeController : Controller {
 [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
 public IActionResult Error() { Response.StatusCode = 500; return View("Problem", "Something went wrong. Please try again or contact your administrator."); }
 public IActionResult Status(int code) { Response.StatusCode = code; return View("Problem", code == 404 ? "We couldn't find that product or page." : "This request could not be completed. Return to products and try again."); }
}
