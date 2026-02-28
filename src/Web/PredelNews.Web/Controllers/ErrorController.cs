using Microsoft.AspNetCore.Mvc;

namespace PredelNews.Web.Controllers;

[Route("error")]
public class ErrorController : Controller
{
    [Route("{statusCode}")]
    public IActionResult Index(int statusCode)
    {
        ViewBag.Title = statusCode switch
        {
            404 => "Страницата не е намерена",
            _ => "Сървърна грешка",
        };

        return statusCode switch
        {
            404 => View("Error404"),
            _ => View("Error500"),
        };
    }
}
