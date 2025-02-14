using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ITN2163_AS_Assignment2.Model
{
	public class ApplicationUser : IdentityUser
	{
		[Required]
		[DataType(DataType.Text)]
		public string FirstName { get; set; }

		[Required]
		[DataType(DataType.Text)]
		public string LastName { get; set; }

		[Required]
		[DataType(DataType.Text)]
		public string Gender { get; set; }

		[Required]
		public string NRIC { get; set; } // Store encrypted NRIC

		[Required]
		public DateOnly DateOfBirth { get; set; }

		public string? Resume { get; set; }

		[DataType(DataType.MultilineText)]
		public string? WhoAmI { get; set; }

        public DateTime PasswordChangedDate { get; set; }
    }
}

