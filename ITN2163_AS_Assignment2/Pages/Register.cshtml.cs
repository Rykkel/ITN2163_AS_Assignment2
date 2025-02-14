using ITN2163_AS_Assignment2.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Security.Cryptography;
using ITN2163_AS_Assignment2.Model;
using System.Text.RegularExpressions;
using System.Drawing;
using ITN2163_AS_Assignment2.Services;
using Microsoft.SqlServer.Server;

namespace ITN2163_AS_Assignment2.Pages
{
    public class RegisterModel : PageModel
    {
        private UserManager<ApplicationUser> userManager { get; }
        private SignInManager<ApplicationUser> signInManager { get; }
        private IWebHostEnvironment _webHostEnvironment; // for file storage
        private readonly MyEncryptionService _encryptionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IAuditLogService auditLogService;
		private readonly PasswordHistoryService _passwordHistoryService;

		[BindProperty]
        public Register RModel { get; set; }

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment,
            MyEncryptionService encryptionService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<RegisterModel> logger,
            IAuditLogService auditLogService,
			PasswordHistoryService passwordHistoryService)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
            _encryptionService = encryptionService; // Initialize encryption service
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            this.auditLogService = auditLogService;
            _passwordHistoryService = passwordHistoryService;

		}
        public void OnGet()
        {
			_logger.LogInformation("Register page loaded");
        }

        // Save data into the database
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var existingUser = await userManager.FindByEmailAsync(RModel.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("RModel.Email", "This email address is already in use");
                    return Page();
                };

				// Perform server-side password complexity check
				int score = CheckPasswordComplexity(RModel.Password);
				if (score < 5)
				{
					ModelState.AddModelError("RModel.Password",
						"Password must be at least 12 characters long and include at least one lowercase letter, one uppercase letter, one number, and one special character.");
					return Page();
				}

				string encryptedNRIC = _encryptionService.EncryptWithRsa(RModel.NRIC);
				_logger.LogInformation("Encrypted NRIC for email: {Email}", RModel.Email);
               
                // Call the resume upload function and await the result
                string resumePath = await UploadResumeAsync(RModel.Resume);
                if (resumePath == null)
                {
                    ModelState.AddModelError("RModel.Resume", "Invalid file type. Only PDF and DOCX files are allowed.");
                    return Page();
                }

                // Create user object with additional fields
                var user = new ApplicationUser()
                {
					UserName = RModel.Email,
					FirstName = RModel.FirstName,
                    LastName = RModel.LastName,
                    Gender = RModel.Gender,
                    NRIC = encryptedNRIC,
                    Email = RModel.Email,
                    DateOfBirth = RModel.DateOfBirth,
                    Resume = resumePath,
					WhoAmI = RModel.WhoAmI,
                    PasswordChangedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(user, RModel.Password);
                if (result.Succeeded)
                {
					// Add the initial password to the UserPasswordHistory
					await _passwordHistoryService.UpdatePasswordHistory(user, RModel.Password);

					await signInManager.SignInAsync(user, false);

                    // Set session values
                    HttpContext.Session.SetString("UserName", user.FirstName + " " + user.LastName);
                    HttpContext.Session.SetString("UserEmail", user.Email);

                    // Log successful login
                    await auditLogService.LogActivityAsync(user.Id, "User registered and successful login", HttpContext.Connection.RemoteIpAddress?.ToString());

                    return RedirectToPage("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return Page();
        }

        // Method to upload resume file with validation
        private async Task<string> UploadResumeAsync(IFormFile resume)
        {
            if (resume == null || resume.Length == 0)
                return null;

            // Validate file type (Only allow PDF and DOCX)
            var allowedExtensions = new List<string> { ".pdf", ".docx" };
            var fileExtension = Path.GetExtension(resume.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return null; // Invalid file type

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder); // Ensure folder exists

            string fileName = $"{Guid.NewGuid()}{fileExtension}"; // Avoid filename conflicts
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await resume.CopyToAsync(fileStream);
            }

            return "/uploads/" + fileName; // Return relative path
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
