using InventoryManagement.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace InventoryManagement.Web.Filters;
public sealed class NotFoundFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not NotFoundException) return;
        context.Result = new NotFoundResult(); context.ExceptionHandled = true;
    }
}
