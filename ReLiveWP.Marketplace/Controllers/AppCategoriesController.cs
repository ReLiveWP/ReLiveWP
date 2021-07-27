using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ReLiveWP.Marketplace.Controllers
{
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
}
