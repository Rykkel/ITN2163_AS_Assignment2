using ITN2163_AS_Assignment2.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ITN2163_AS_Assignment2.Model;
using System.Net;
using System.Text.Json;
using ITN2163_AS_Assignment2.Services;

namespace ITN2163_AS_Assignment2.Pages
{
    public class LoginModel : PageModel
    {
		[BindProperty]
		public Login LModel { get; set; }

		// Property for the reCAPTCHA token from the form
		[BindProperty]
		public string RecaptchaToken { get; set; }

		private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> userManager;
		private readonly IConfiguration _configuration;
		private readonly IAuditLogService auditLogService;

		public LoginModel(
			SignInManager<ApplicationUser> signInManager,
			IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
			IConfiguration configuration,
			IAuditLogService auditLogService)
		{
			this.signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            this.userManager = userManager;
			_configuration = configuration;
			this.auditLogService = auditLogService;
		}

		public async Task<IActionResult> OnPostAsync()
		{
			// 1. Verify the reCAPTCHA token first
			var recaptchaValid = await VerifyRecaptcha(RecaptchaToken);
			if (!recaptchaValid)
			{
				ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed. Please try again.");
				return Page();
			}

			if (!ModelState.IsValid)
			{
				return Page();
			}

			var user = await userManager.FindByEmailAsync(LModel.Email);
			if (user == null)
			{
				// Log failed login attempt with email (user not found)
				await auditLogService.LogActivityAsync(LModel.Email, "Failed login attempt - user not found", HttpContext.Connection.RemoteIpAddress?.ToString());
				ModelState.AddModelError(string.Empty, "Invalid email or password.");
				return Page();
			}

            // Check for password expiry (e.g., max age is 30 days)
            var maxPasswordAge = TimeSpan.FromDays(30);
            if (DateTime.UtcNow - user.PasswordChangedDate > maxPasswordAge)
            {
                // Instead of redirecting immediately, sign in the user so they can access the Change Password page.
                await signInManager.SignInAsync(user, isPersistent: LModel.RememberMe);
                // Optionally, you can log this event
                await auditLogService.LogActivityAsync(user.Id, "Password expired. Forced password change on login.", HttpContext.Connection.RemoteIpAddress?.ToString());

                // Redirect the now-signed-in user to the ChangePassword page.
                return RedirectToPage("ChangePassword");
            }

            // Check if the account is locked
            if (await userManager.IsLockedOutAsync(user))
			{
				await auditLogService.LogActivityAsync(user.Id, "Login attempt while account is locked", HttpContext.Connection.RemoteIpAddress?.ToString());
				ModelState.AddModelError(string.Empty, "Your account is locked. Please try again later.");
				return Page();
			}

			var identityResult = await signInManager.PasswordSignInAsync(user.UserName, LModel.Password, LModel.RememberMe, false);

			if (identityResult.Succeeded)
			{
				// Reset the failed login count if successful
				await userManager.ResetAccessFailedCountAsync(user);

				// Set session values
				HttpContext.Session.SetString("UserName", user.FirstName + " " + user.LastName);
				HttpContext.Session.SetString("UserEmail", user.Email);

				// Log successful login
				await auditLogService.LogActivityAsync(user.Id, "Successful login", HttpContext.Connection.RemoteIpAddress?.ToString());

				return RedirectToPage("Index");
			}
			else if (identityResult.IsLockedOut)
			{
				await auditLogService.LogActivityAsync(user.Id, "Account locked due to multiple failed login attempts", HttpContext.Connection.RemoteIpAddress?.ToString());
                // Account is locked after 3 failed attempts
                ModelState.AddModelError(string.Empty, "Your account is locked due to multiple failed login attempts. Please try again later.");
			}
			else if (identityResult.IsNotAllowed)
			{
                await auditLogService.LogActivityAsync(user.Id, "Login attempt not allowed", HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError(string.Empty, "Your account is not allowed to sign in. Please verify your email or contact support.");
			}
			else
			{
				// Increment the failed login attempts
				await userManager.AccessFailedAsync(user);
                await auditLogService.LogActivityAsync(user.Id, "Failed login attempt", HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError(string.Empty, "Invalid email or password."); // Ensure this error is displayed on incorrect password
			}
			
			return Page();
		}

		// Method to verify the reCAPTCHA token with Google
		private async Task<bool> VerifyRecaptcha(string token)
		{
			// Get your secret key from configuration
			var secretKey = _configuration["GoogleReCaptcha:SecretKey"];
			using var client = new HttpClient();

			// Make a POST request to the Google reCAPTCHA verification endpoint
			var response = await client.PostAsync(
				$"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
				null);

			if (response.StatusCode != HttpStatusCode.OK)
			{
				Console.WriteLine("Google recaptcha endpoint did not return OK");
				return false;
			}

			var jsonString = await response.Content.ReadAsStringAsync();

			// Add case-insensitive options for JSON deserialization.
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			var recaptchaResponse = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString, options);

			// Console write the Google response for debugging purposes.
			Console.WriteLine("Google reCAPTCHA response:");
			Console.WriteLine($"Success: {recaptchaResponse?.Success}");
			Console.WriteLine($"Score: {recaptchaResponse?.Score}");
			Console.WriteLine($"Action: {recaptchaResponse?.Action}");
			if (recaptchaResponse?.ErrorCodes != null && recaptchaResponse.ErrorCodes.Any())
			{
				Console.WriteLine("Error Codes: " + string.Join(", ", recaptchaResponse.ErrorCodes));
			}

			// Check that the verification was successful, the score meets your threshold (e.g., 0.5),
			// and that the action matches the one you set in the JS ("login")
			return recaptchaResponse != null &&
				   recaptchaResponse.Success &&
				   recaptchaResponse.Score >= 0.5 &&
				   recaptchaResponse.Action == "login";
		}

		// Class to deserialize the response from Google
		private class RecaptchaResponse
		{
			public bool Success { get; set; }
			public float Score { get; set; }
			public string Action { get; set; }
			public DateTime Challenge_ts { get; set; }
			public string Hostname { get; set; }
			public List<string> ErrorCodes { get; set; }
		}

	}
}
