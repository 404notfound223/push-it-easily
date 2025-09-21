using Menu.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Menu.Controllers
{
    public class MenuController : Controller
    {
        private readonly DB _db;

        public MenuController(DB db)
        {
            _db = db;
        }

        public async Task<IActionResult> All()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var products = await _db.Products.ToListAsync();

            // For members, filter out disabled products
            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        public async Task<IActionResult> Specials()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var products = await _db.Products
                .Where(p => EF.Functions.Like(p.Category, "%special%"))
                .ToListAsync();

            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        public async Task<IActionResult> Pizza()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var products = await _db.Products
                .Where(p => p.Category == "Pizza")
                .ToListAsync();

            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        public async Task<IActionResult> Pasta()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var products = await _db.Products
                .Where(p => p.Category == "Pasta")
                .ToListAsync();

            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        public async Task<IActionResult> Seafood()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var products = await _db.Products
                .Where(p => p.Category == "Seafood")
                .ToListAsync();

            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        public async Task<IActionResult> Burgers()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var products = await _db.Products
                .Where(p => p.Category == "Burger" || p.Category == "Burgers")
                .ToListAsync();

            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        public async Task<IActionResult> Beverages()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var products = await _db.Products
                .Where(p => p.Category == "Beverages")
                .ToListAsync();

            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        public async Task<IActionResult> Desserts()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            var products = await _db.Products
                .Where(p => p.Category == "Desserts")
                .ToListAsync();

            if (userRole != "admin" && userRole != "staff")
            {
                products = products.Where(p => !p.IsDisabled).ToList();
            }

            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest request)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id.ToString() == request.Id.ToString());
            if (product == null)
                return NotFound();

            product.IsDisabled = request.Disable;
            await _db.SaveChangesAsync();

            return Json(new { success = true, isDisabled = product.IsDisabled });
        }

    }

}
