using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Web.Controllers;

public sealed class HomeController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;

        const string message = "Something went wrong. Please try again or contact your administrator.";
        return View("Problem", message);
    }

    public IActionResult Status(int code)
    {
        Response.StatusCode = code;

        var message = "This request could not be completed. Return to products and try again.";

        if (code == StatusCodes.Status404NotFound)
        {
            message = "We couldn't find that product or page.";
        }

        return View("Problem", message);
    }
}
