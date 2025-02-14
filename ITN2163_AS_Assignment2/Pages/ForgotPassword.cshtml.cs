using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ITN2163_AS_Assignment2.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ITN2163_AS_Assignment2.Pages
{
	public class ForgotPasswordModel : PageModel
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IEmailSender _emailSender;

		public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
		{
			_userManager = userManager;
			_emailSender = emailSender;
		}

		[BindProperty]
		public string Email { get; set; }

		public void OnGet() { }

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var user = await _userManager.FindByEmailAsync(Email);
			if (user != null)
			{
				// Generate a password reset token
				var token = await _userManager.GeneratePasswordResetTokenAsync(user);

				// Generate a reset link (adjust the route as needed)
				var resetLink = Url.Page(
					"/ResetPassword",
					pageHandler: null,
					values: new { userId = user.Id, token = token },
					protocol: Request.Scheme);

				// Send the email (using your preferred email service)
				await _emailSender.SendEmailAsync(
					Email,
					"Reset Your Password",
					$"Please reset your password by clicking here: <a href='{resetLink}'>Reset Password</a>");
			}

			// Redirect to confirmation regardless of whether the user exists
			return RedirectToPage("ForgotPasswordConfirmation");
		}
	}

}
