using Menu.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

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
        ViewBag.ClearOrders = true;
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
        var hashedPassword = HashPassword(password);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == hashedPassword);

        if (user != null)
        {
            HttpContext.Session.SetString("UserId", user.UserId);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.Name);

            // Redirect based on role
            switch (user.Role.ToLower())
            {
                case "admin":
                case "staff":
                    return RedirectToAction("Dashboard", "Staff");
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
    public async Task<IActionResult> Register(string name, string email, string password, string verificationCode)
    {
        // Check verification code
        var expectedCode = HttpContext.Session.GetString("VerificationCode");
        var verificationEmail = HttpContext.Session.GetString("VerificationEmail");

        if (string.IsNullOrEmpty(verificationCode))
        {
            ViewBag.Error = "Please verify your email first.";
            return View();
        }

        if (verificationCode != expectedCode || email != verificationEmail)
        {
            ViewBag.Error = "Invalid verification code.";
            return View();
        }

        try
        {
            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                ViewBag.Error = "A user with this email already exists.";
                return View();
            }

            var user = new User
            {
                UserId = await GenerateMemberId(),
                Name = name,
                Email = email,
                Password = HashPassword(password),
                Role = "member"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Clear session data
            HttpContext.Session.Remove("VerificationCode");
            HttpContext.Session.Remove("VerificationEmail");

            TempData["RegistrationSuccess"] = "Registration successful! You can now login with your new account.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ViewBag.Error = "Registration failed: " + ex.Message;
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
            var name = HttpContext.Session.GetString("PendingName");
            var email = HttpContext.Session.GetString("PendingEmail");
            var password = HttpContext.Session.GetString("PendingPassword");

            var user = new User
            {
                UserId = await GenerateMemberId(),
                Name = name,
                Email = email,
                Password = HashPassword(password),
                Role = "member"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Clear session data
            HttpContext.Session.Clear();

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
            var smtpUsername = _configuration["EmailSettings:Username"];
            var smtpPassword = _configuration["EmailSettings:Password"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
            {
                throw new Exception("Email configuration is incomplete. Please configure SMTP settings in appsettings.json");
            }

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
                Body = $@"<h2>Welcome to The Secret Restaurant!</h2>
                    <p>Your verification code is: <strong>{code}</strong></p>
                    <p>Please enter this code to complete your registration.</p>
                    <p>This code will expire in 10 minutes.</p>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            smtpClient.Send(mailMessage);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to send email: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> SendVerificationCode([FromBody] VerificationRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Email))
            return Json(new { success = false, error = "Invalid email." });

        try
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return Json(new { success = false, error = "A user with this email already exists." });
            }

            string email = request.Email;
            var code = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("VerificationCode", code);
            HttpContext.Session.SetString("VerificationEmail", email);

            SendVerificationEmail(email, code);
            return Json(new { success = true, message = "Verification code sent to " + email });
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

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login");
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(string name, string email)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, error = "User not logged in" });
        }

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, error = "User not found" });
            }

            // Check if email is already taken by another user
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.UserId != userId);
            if (existingUser != null)
            {
                return Json(new { success = false, error = "Email is already taken by another user" });
            }

            user.Name = name;
            user.Email = email;
            await _context.SaveChangesAsync();

            // Update session with new name
            HttpContext.Session.SetString("UserName", user.Name);

            return Json(new { success = true, message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, error = "User not logged in" });
        }

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, error = "User not found" });
            }

            // Verify current password
            var hashedCurrentPassword = HashPassword(currentPassword);
            if (user.Password != hashedCurrentPassword)
            {
                return Json(new { success = false, error = "Current password is incorrect" });
            }

            // Update password
            user.Password = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequest request)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, error = "User not logged in" });
        }

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, error = "User not found" });
            }

            user.Name = request.NewUsername;
            await _context.SaveChangesAsync();

            // Update session with new name
            HttpContext.Session.SetString("UserName", user.Name);

            return Json(new { success = true, message = "Username updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendPasswordResetCode([FromBody] PasswordResetRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Email))
            return Json(new { success = false, error = "Invalid email." });

        try
        {
            // Check if user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return Json(new { success = false, error = "No account found with this email address." });
            }

            // Generate reset code
            var resetCode = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("PasswordResetCode", resetCode);
            HttpContext.Session.SetString("PasswordResetEmail", request.Email);

            // Send reset code email
            SendPasswordResetEmail(request.Email, resetCode);
            return Json(new { success = true, message = "Password reset code sent to " + request.Email });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = "Failed to send reset code: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(string email, string resetCode, string newPassword)
    {
        try
        {
            // Verify reset code
            var expectedCode = HttpContext.Session.GetString("PasswordResetCode");
            var resetEmail = HttpContext.Session.GetString("PasswordResetEmail");

            if (string.IsNullOrEmpty(resetCode) || resetCode != expectedCode || email != resetEmail)
            {
                ViewBag.Error = "Invalid or expired reset code.";
                return View("ForgotPassword");
            }

            // Find user and update password
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View("ForgotPassword");
            }

            // Update password
            user.Password = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            // Clear session data
            HttpContext.Session.Remove("PasswordResetCode");
            HttpContext.Session.Remove("PasswordResetEmail");

            // Redirect to login with success message
            TempData["PasswordResetSuccess"] = "Password changed successfully! You can now login with your new password.";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ViewBag.Error = "Password reset failed: " + ex.Message;
            return View("ForgotPassword");
        }
    }

    private void SendPasswordResetEmail(string toEmail, string resetCode)
    {
        try
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:Username"];
            var smtpPassword = _configuration["EmailSettings:Password"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(fromEmail))
            {
                throw new Exception("Email configuration is incomplete. Please configure SMTP settings in appsettings.json");
            }

            using var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true,
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "The Secret Restaurant"),
                Subject = "Password Reset Code - The Secret Restaurant",
                Body = $@"<h2>Password Reset Request</h2>
                    <p>You have requested to reset your password for The Secret Restaurant.</p>
                    <p>Your password reset code is: <strong>{resetCode}</strong></p>
                    <p>Please enter this code to reset your password.</p>
                    <p>This code will expire in 10 minutes.</p>
                    <p>If you did not request this password reset, please ignore this email.</p>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            smtpClient.Send(mailMessage);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to send password reset email: {ex.Message}");
        }
    }

    private string HashPassword(string password)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    private async Task<string> GenerateMemberId()
    {
        var lastUser = await _context.Users
            .OrderByDescending(u => u.UserId)
            .FirstOrDefaultAsync();

        if (lastUser == null || string.IsNullOrEmpty(lastUser.UserId))
            return "M001";

        int lastNumber = int.Parse(lastUser.UserId.Substring(1));
        return "M" + (lastNumber + 1).ToString("D3");
    }
}

public class UpdateUsernameRequest
{
    public string NewUsername { get; set; } = string.Empty;
}

public class PasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}

public class VerificationRequest
{
    public string Email { get; set; } = string.Empty;
}
