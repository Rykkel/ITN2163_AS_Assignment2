using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ITN2163_AS_Assignment2.Model;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using ITN2163_AS_Assignment2.Services;

namespace ITN2163_AS_Assignment2.Pages
{
	public class ResetPasswordModel : PageModel
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly PasswordHistoryService _passwordHistoryService;

		public ResetPasswordModel(
			UserManager<ApplicationUser> userManager,
			PasswordHistoryService passwordHistoryService)
		{
			_userManager = userManager;
			_passwordHistoryService = passwordHistoryService;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public class InputModel
		{
			public string UserId { get; set; }
			public string Token { get; set; }

			[Required]
			[DataType(DataType.Password)]
			[Display(Name = "New Password")]
			public string NewPassword { get; set; }

			[Required]
			[DataType(DataType.Password)]
			[Display(Name = "Confirm New Password")]
			[Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
			public string ConfirmPassword { get; set; }
		}

		// Populate the hidden fields on GET
		public void OnGet(string userId, string token)
		{
			Input = new InputModel
			{
				UserId = userId,
				Token = token
			};
		}

		public async Task<IActionResult> OnPostAsync()
		{
			if (!ModelState.IsValid)
			{
				return Page();
			}

			var user = await _userManager.FindByIdAsync(Input.UserId);
			if (user == null)
			{
				// Redirect to confirmation to avoid revealing user info
				return RedirectToPage("ResetPasswordConfirmation");
			}

			// Check if the new password is in the last two passwords
			bool isInHistory = await _passwordHistoryService.IsPasswordInHistory(user, Input.NewPassword);

			if (isInHistory)
			{
				ModelState.AddModelError("Input.NewPassword", "You cannot reuse any of your last two passwords.");
				return Page();
			}

			var result = await _userManager.ResetPasswordAsync(user, Input.Token, Input.NewPassword);
			if (result.Succeeded)
			{
				// Update password history after reset
				await _passwordHistoryService.UpdatePasswordHistory(user, Input.NewPassword);

				return RedirectToPage("ResetPasswordConfirmation");
			}

			// Add errors to ModelState if the reset fails (e.g., token expired)
			foreach (var error in result.Errors)
			{
				ModelState.AddModelError(string.Empty, error.Description);
			}
			return Page();
		}
	}

}
