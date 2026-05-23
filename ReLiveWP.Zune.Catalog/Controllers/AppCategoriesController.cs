using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Zune.Catalog.Controllers;

[Route("/{version}/{language}/appCategories/{action=Index}")]
[Route("/{version}/{language}/appCategories/{category}/{action}")]
public class AppCategoriesController : Controller
{
    public IActionResult Index()
    {
        return File("appcategories.xml", "application/atom+xml");
    }

    public IActionResult Apps(string category)
    {
        return NotFound();
    }
}
