using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;

namespace FutureTech_StudentManagement.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            // If user is already authenticated, redirect to home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            properties.Items["LoginProvider"] = provider;
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/", string remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["Error"] = $"Error from external provider: {remoteError}";
                return RedirectToAction("Login");
            }

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result?.Principal == null)
            {
                return RedirectToAction("Login");
            }

            // ============================================================
            // ⭐ RBAC: CHECK IF USER'S EMAIL IS IN ADMIN LIST ⭐
            // ============================================================
            // Get the user's email from their Google/GitHub account
            var userEmail = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            // List of allowed admin emails - PUT YOUR ACTUAL EMAILS HERE
            var adminEmails = new List<string>
            {
                "nandishandu51@gmail.com",       
                "Gugukhanyiswa@gmail.com",
                "angelndaba83@gmail.com",
                "nkanyisomabanga0@gmail.com",
                "Sfundozuma114@gmail.com",
                "snepromise0607@gmail.com",
                "thembelihlecele502@gmail.com",
                "mqwelzanoxolo09@gmail.com",
                "njabulomazibuko86@gmail.com",
                "Luyandafortune17@gmail.com"

            };

            // Check if the user's email is authorized
            if (string.IsNullOrEmpty(userEmail) || !adminEmails.Contains(userEmail))
            {
                // Not authorized - sign them out and show error
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["Error"] = "Access Denied: You are not authorized to use this system. Only approved administrators can log in.";
                return RedirectToAction("Login");
            }
            // ============================================================

            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}