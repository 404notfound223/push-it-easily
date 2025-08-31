using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Net.Mail;
using System.Net;

public class LoginController : Controller
{
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

    [HttpPost]
    public IActionResult Register(string fName, string lName, string email, string password)
    {
        // 1. Generate code
        var code = new Random().Next(100000, 999999).ToString();

        // 2. Store code/email in session
        HttpContext.Session.SetString("VerificationCode", code);
        HttpContext.Session.SetString("VerificationEmail", email);

        // 3. Send code to email
        SendVerificationEmail(email, code);

        // 4. Redirect to verification page
        return RedirectToAction("VerifyCode");
    }

    [HttpGet]
    public IActionResult VerifyCode()
    {
        return View();
    }

    [HttpPost]
    public IActionResult VerifyCode(string code)
    {
        var expectedCode = HttpContext.Session.GetString("VerificationCode");
        if (code == expectedCode)
        {
            ViewBag.Success = "Email verified! Registration complete.";
            // Save user to database here
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
        // Replace with your SMTP settings
        var smtpClient = new SmtpClient("smtp.yourserver.com")
        {
            Port = 587,
            Credentials = new NetworkCredential("your@email.com", "yourpassword"),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress("your@email.com"),
            Subject = "Your Verification Code",
            Body = $"Your verification code is: {code}",
            IsBodyHtml = false,
        };
        mailMessage.To.Add(toEmail);

        smtpClient.Send(mailMessage);
    }
}