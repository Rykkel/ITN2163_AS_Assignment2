using ITN2163_AS_Assignment2.Model;
using ITN2163_AS_Assignment2.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace ITN2163_AS_Assignment2.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IAuditLogService auditLogService;

        public LogoutModel(
            SignInManager<ApplicationUser> signInManager,
            IAuditLogService auditLogService)
        {
            this.signInManager = signInManager;
            this.auditLogService = auditLogService;
        }

        public async Task<IActionResult> OnGet(bool sessionTimeout = false)
        {
            if (sessionTimeout)
            {
                // Log session timeout Logout
                var userId = User.Identity.IsAuthenticated ? User.FindFirstValue(
                    ClaimTypes.NameIdentifier) : "Anonymous";
                await auditLogService.LogActivityAsync(userId, "Session timeout logout", HttpContext.Connection.RemoteIpAddress?.ToString());

                await signInManager.SignOutAsync();
                HttpContext.Session.Clear();
                return RedirectToPage("Login");
            }

            return Page(); // Show logout confirmation page for manual logout
        }

        public async Task<IActionResult> OnPostLogoutAsync()
		{
            // Log manual Logout before signing out
            var userId = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : "Anonymous";
            await auditLogService.LogActivityAsync(userId, "User logged out", HttpContext.Connection.RemoteIpAddress?.ToString());

			await signInManager.SignOutAsync();

            // Clear the session
            HttpContext.Session.Clear();

            return RedirectToPage("Login");
		}

		public async Task<IActionResult> OnPostDontLogoutAsync()
		{
			return RedirectToPage("Index");
		}

	}
}
