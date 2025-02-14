using ITN2163_AS_Assignment2.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ITN2163_AS_Assignment2.ViewModels;
using ITN2163_AS_Assignment2.Services;
using System.Text.RegularExpressions;

namespace ITN2163_AS_Assignment2.Pages
{
    [Authorize] // Only logged-in users should access this page
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;
        private readonly AuthDbContext _context;
		private readonly PasswordHistoryService _passwordHistoryService;

		// Bind the view model so the form input is available on POST
		[BindProperty]
        public ChangePassword Input { get; set; }

        public ChangePasswordModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<ChangePasswordModel> logger,
			AuthDbContext context,
			PasswordHistoryService passwordHistoryService
			)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
			_context = context;
			_passwordHistoryService = passwordHistoryService;
		}

        public async Task<IActionResult> OnGetAsync()
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // If no user is found, redirect to login
                return RedirectToPage("/Login");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validate form input
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Login");
            }

            // Minimum password age check (e.g., cannot change password within 5 minutes)
            var minPasswordAge = TimeSpan.FromMinutes(5);
            if (DateTime.UtcNow - user.PasswordChangedDate < minPasswordAge)
            {
                ModelState.AddModelError(string.Empty, "You must wait at least 5 minutes between password changes.");
                return Page();
            }

            // Maximum password age check (e.g., must change password after 30 days)
            var maxPasswordAge = TimeSpan.FromDays(30);
            if (DateTime.UtcNow - user.PasswordChangedDate > maxPasswordAge)
            {
                ModelState.AddModelError(string.Empty, "Your password has expired. Please change your password.");
                return Page();
            }

            // Check if the new password is in the last two passwords
            bool isInHistory = await _passwordHistoryService.IsPasswordInHistory(user, Input.NewPassword);

			if (isInHistory)
			{
				ModelState.AddModelError("Input.NewPassword", "You cannot reuse any of your last two passwords.");
				return Page();
			}

			// Perform server-side password complexity check
			int score = CheckPasswordComplexity(Input.NewPassword);
            if (score < 5)
            {
                ModelState.AddModelError("Input.NewPassword",
                    "New Password must be at least 12 characters long and include at least one lowercase letter, one uppercase letter, one number, and one special character.");
                return Page();
            }

            // Attempt to change the password using the ASP.NET Identity API
            var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
            if (!result.Succeeded)
            {
                // Add errors to the model state so they can be displayed
                foreach (var error in result.Errors)
                {
                    // Check for incorrect current password error and replace it with a custom message
                    if (error.Code == "PasswordMismatch") // ASP.NET Identity uses this code for wrong current password
                    {
                        ModelState.AddModelError(string.Empty, "The current password you entered is incorrect. Please try again.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                return Page();
            }

			// Update password history
			await _passwordHistoryService.UpdatePasswordHistory(user, Input.NewPassword);

			// If successful, refresh sign-in (so the security stamp is updated) and set a status message
			await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");
   
            // Optionally, redirect to the index or another page
            return RedirectToPage("Index");
        }

        // Server-side password complexity check
        private int CheckPasswordComplexity(string password)
        {
            int score = 0;
            if (password == null)
                return score;

            // Check minimum length
            if (password.Length >= 12)
                score++;

            // Check for lowercase letters
            if (Regex.IsMatch(password, "[a-z]"))
                score++;

            // Check for uppercase letters
            if (Regex.IsMatch(password, "[A-Z]"))
                score++;

            // Check for digits
            if (Regex.IsMatch(password, "[0-9]"))
                score++;

            // Check for special characters (non-alphanumeric)
            if (Regex.IsMatch(password, @"[\W_]"))
                score++;

            return score;
        }
    }
}
