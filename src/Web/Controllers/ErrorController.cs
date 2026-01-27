using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Interfaces;
using PredelNews.Core.ViewModels;

namespace PredelNews.Web.Controllers;

public class ErrorController : Controller
{
    private readonly ICategoryService _categoryService;

    public ErrorController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [Route("error/{statusCode:int}")]
    public async Task<IActionResult> HandleError(int statusCode)
    {
        var navigation = await _categoryService.GetMainNavigationAsync();

        var model = new ErrorViewModel
        {
            StatusCode = statusCode,
            PageTitle = statusCode == 404 ? "Page Not Found" : "Error",
            NavigationCategories = navigation,
            Breadcrumbs = new List<BreadcrumbItem>
            {
                new() { Title = "Error", IsCurrentPage = true }
            }
        };

        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature?.Error != null)
        {
            model.ErrorMessage = "An unexpected error occurred. Please try again later.";
        }

        return statusCode == 404
            ? View("~/Views/Error/NotFound.cshtml", model)
            : View("~/Views/Error/Error.cshtml", model);
    }

    [Route("error")]
    public async Task<IActionResult> HandleException()
    {
        var navigation = await _categoryService.GetMainNavigationAsync();

        var model = new ErrorViewModel
        {
            StatusCode = 500,
            PageTitle = "Error",
            ErrorMessage = "An unexpected error occurred. Please try again later.",
            NavigationCategories = navigation,
            Breadcrumbs = new List<BreadcrumbItem>
            {
                new() { Title = "Error", IsCurrentPage = true }
            }
        };

        return View("~/Views/Error/Error.cshtml", model);
    }
}
