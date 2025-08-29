//using Microsoft.AspNetCore.Mvc;
//using Menu.Models;
//using System.Linq;

//namespace Demo.Controllers
//{
//    public class TopSellerController : Controller
//    {
//        private readonly DB db;

//        public TopSellerController(DB db)
//        {
//            this.db = db;
//        }

//        public IActionResult Index()
//        {
//            var topItemsPerCategory = db.Products
//                .GroupBy(p => p.Category)
//                .Select(g => g.OrderByDescending(p => p.Sold).FirstOrDefault())
//                .Where(p => p != null)
//                .ToList();

//            return View(topItemsPerCategory);
//        }
//    }
//}
