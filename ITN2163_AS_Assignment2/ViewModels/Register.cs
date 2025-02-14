using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ITN2163_AS_Assignment2.ViewModels
{
	public class Register
	{
		[Required]
		public string FirstName { get; set; }

		[Required]
		public string LastName { get; set; }

		[Required]
		public string Gender { get; set; }

		[Required]
		public string NRIC { get; set; } // Will be encrypted before saving

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		[DataType(DataType.Password)]
		public string Password { get; set; }

		[Required]
		[DataType(DataType.Password)]
		[Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
		public string ConfirmPassword { get; set; }

		[Required]
		public DateOnly DateOfBirth { get; set; }

		public IFormFile? Resume { get; set; }

		public string? WhoAmI { get; set; }
	}
}
