using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ITN2163_AS_Assignment2.Services;
using ITN2163_AS_Assignment2.Model;
using Microsoft.AspNetCore.Http;

namespace ITN2163_AS_Assignment2.Pages
{
    [Authorize]
    public class IndexModel : PageModel
    {
		private readonly ILogger<IndexModel> _logger;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly MyEncryptionService _encryptionService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationUser CurrentUser { get; set; }
		public string DecryptedNRIC { get; set; }
		public string DecryptedDateOfBirth { get; set; }

		public IndexModel(
			ILogger<IndexModel> logger, 
			UserManager<ApplicationUser> userManager, 
			MyEncryptionService encryptionService, 
			IHttpContextAccessor httpContextAccessor)
		{
			_logger = logger;
			_userManager = userManager;
			_encryptionService = encryptionService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> OnGet()
        {
            // Use the authentication cookie to verify if the user is logged in.
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("Login");
            }

            //// Check if session exists
            //if (HttpContext.Session.GetString("UserName") == null )
            //{
            //    return RedirectToPage("Login"); // Redirect if session expired
            //}

            // Retrieve session data
            //string UserName = HttpContext.Session.GetString("UserName");
            //string UserEmail = HttpContext.Session.GetString("UserEmail");

            // Retrieve the current logged-in user
            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser == null)
            {
                // If no user is found, redirect to login page (session might be expired)
                return RedirectToPage("Login");
            }

            // If session values are missing (they may be lost when the browser is closed),
            // repopulate them from the user object.
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserName")))
            {
                HttpContext.Session.SetString("UserName", CurrentUser.FirstName + " " + CurrentUser.LastName);
                HttpContext.Session.SetString("UserEmail", CurrentUser.Email);
            }


            // Decrypt the encrypted fields (ensure CurrentUser is not null before accessing properties)
            DecryptedNRIC = _encryptionService.DecryptWithRsa(CurrentUser.NRIC);

            return Page();
        }
    }
}
