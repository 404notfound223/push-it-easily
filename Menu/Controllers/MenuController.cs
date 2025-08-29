using Microsoft.AspNetCore.Mvc;
using Menu.Models;
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
            var products = await _db.Products.ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Specials()
        {
            var products = await _db.Products
                .Where(p => EF.Functions.Like(p.Category, "%special%"))
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Pizza()
        {
            var products = await _db.Products
                .Where(p => p.Category == "Pizza")
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Pasta()
        {
            var products = await _db.Products
                .Where(p => p.Category == "Pasta")
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Seafood()
        {
            var products = await _db.Products
                .Where(p => p.Category == "Seafood")
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Burgers()
        {
            var products = await _db.Products
                .Where(p => p.Category == "Burger" || p.Category == "Burgers")
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Beverages()
        {
            var products = await _db.Products
                .Where(p => p.Category == "Beverages")
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Desserts()
        {
            var products = await _db.Products
                .Where(p => p.Category == "Desserts")
                .ToListAsync();
            return View(products);
        }
    }
}
