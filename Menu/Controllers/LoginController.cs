using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

public class LoginController : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string email, string password)
    {
        // Replace with your admin credentials check
        if (email == "admin@example.com" && password == "adminpassword")
        {
            HttpContext.Session.SetString("IsAdmin", "true");
            return RedirectToAction("Configure", "Menu");
        }
        else
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }
    }
}