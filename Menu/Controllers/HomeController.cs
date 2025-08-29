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

        //        public IActionResult LiveSearch(string term)
        //        {
        //            var items = new List<string> {
        //                "Grilled Chicken", "Beef Steak", "Spaghetti Carbonara", "Cheeseburger", "Fish and Chips",
        //                "Mac and Cheese", "Chicken Alfredo", "Garlic Bread", "Tuna Sandwich"
        //            };

        //            var results = items
        //                .Where(x => x.Contains(term, StringComparison.OrdinalIgnoreCase))
        //                .Select(x => $"<div style='padding:6px 12px; border-bottom:1px solid #eee;'>{x}</div>");

        //            return Content(string.Join("", results));
        //        }
    }
}
