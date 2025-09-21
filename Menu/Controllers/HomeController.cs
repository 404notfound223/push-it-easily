using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Menu.Models;

namespace Menu.Controllers
{
    public class HomeController : Controller
    {
        public readonly DB db;

        public HomeController(DB db)
        {
            this.db = db;
        }

        public IActionResult Index()
        {
            ViewBag.PageTitle = "The Secret Restaurant Menu";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LiveSearch(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new { success = false, results = new List<object>() });
            }

            try
            {
                var products = await db.Products
                    .Where(p => EF.Functions.Like(p.Name, $"%{term}%") ||
                               EF.Functions.Like(p.Description, $"%{term}%") ||
                               EF.Functions.Like(p.Category, $"%{term}%"))
                    .Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        description = p.Description,
                        price = p.Price,
                        category = p.Category,
                        imageUrl = p.ImagePath
                    })
                    .Take(10) // Limit results to 10 items
                    .ToListAsync();

                return Json(new { success = true, results = products });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message, results = new List<object>() });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("All", "Menu");
            }

            try
            {
                var products = await db.Products
                    .Where(p => EF.Functions.Like(p.Name, $"%{query}%"))
                    .ToListAsync();

                ViewBag.SearchQuery = query;
                ViewBag.ResultCount = products.Count;
                return View("SearchResult", products);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("SearchResult", new List<Product>());
            }
        }
    }
}
