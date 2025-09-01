using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Mail;
using System.Net;
using Menu.Models;
using Microsoft.EntityFrameworkCore;

public class LoginController : Controller
{
    private readonly DB _context;
    private readonly IConfiguration _configuration;

    public LoginController(DB context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
        
        if (user != null)
        {
            HttpContext.Session.SetString("UserId", user.Id);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.Name);
            
            // Redirect based on role
            switch (user.Role.ToLower())
            {
                case "admin":
                    return RedirectToAction("Configure", "Menu");
                case "staff":
                    return RedirectToAction("StaffDashboard", "Staff");
                case "member":
                    return RedirectToAction("All", "Menu");
                default:
                    return RedirectToAction("All", "Menu");
            }
        }
        else
        {
            ViewBag.Error = "Invalid email or password.";
            return View();
        }
    }

    [HttpPost]
    public IActionResult Register(string fName, string lName, string email, string password)
    {
        HttpContext.Session.SetString("PendingFirstName", fName);
        HttpContext.Session.SetString("PendingLastName", lName);
        HttpContext.Session.SetString("PendingEmail", email);
        HttpContext.Session.SetString("PendingPassword", password);

        // Generate verification code
        var code = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("VerificationCode", code);
        HttpContext.Session.SetString("VerificationEmail", email);

        try
        {
            SendVerificationEmail(email, code);
            return RedirectToAction("VerifyCode");
        }
        catch (Exception)
        {
            ViewBag.Error = "Failed to send verification email. Please try again.";
            return View();
        }
    }

    [HttpGet]
    public IActionResult VerifyCode()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> VerifyCode(string code)
    {
        var expectedCode = HttpContext.Session.GetString("VerificationCode");
        if (code == expectedCode)
        {
            var firstName = HttpContext.Session.GetString("PendingFirstName");
            var lastName = HttpContext.Session.GetString("PendingLastName");
            var email = HttpContext.Session.GetString("PendingEmail");
            var password = HttpContext.Session.GetString("PendingPassword");

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{firstName} {lastName}",
                Email = email,
                Password = password, // In production, hash this password
                Role = "member" // Default role for new registrations
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Clear session data
            HttpContext.Session.Remove("PendingFirstName");
            HttpContext.Session.Remove("PendingLastName");
            HttpContext.Session.Remove("PendingEmail");
            HttpContext.Session.Remove("PendingPassword");
            HttpContext.Session.Remove("VerificationCode");
            HttpContext.Session.Remove("VerificationEmail");

            ViewBag.Success = "Email verified! Registration complete. You can now login.";
            return View();
        }
        else
        {
            ViewBag.Error = "Invalid code. Please try again.";
            return View();
        }
    }

    private void SendVerificationEmail(string toEmail, string code)
    {
        try
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:Username"] ?? "";
            var smtpPassword = _configuration["EmailSettings:Password"] ?? "";
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;

            using var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true,
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "The Secret Restaurant"),
                Subject = "Your Verification Code - The Secret Restaurant",
                Body = $@"
                    <h2>Welcome to The Secret Restaurant!</h2>
                    <p>Your verification code is: <strong>{code}</strong></p>
                    <p>Please enter this code to complete your registration.</p>
                    <p>This code will expire in 10 minutes.</p>
                ",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            smtpClient.Send(mailMessage);
        }
        catch (Exception ex)
        {
            // Log the error (in production, use proper logging)
            throw new Exception($"Failed to send email: {ex.Message}");
        }
    }

    [HttpPost]
    public IActionResult SendVerificationCode([FromBody] VerificationRequest request)
    {
        string email = request.Email;
        var code = new Random().Next(100000, 999999).ToString();
        HttpContext.Session.SetString("VerificationCode", code);
        HttpContext.Session.SetString("VerificationEmail", email);

        try
        {
            SendVerificationEmail(email, code);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "Failed to send email: " + ex.Message });
        }
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
