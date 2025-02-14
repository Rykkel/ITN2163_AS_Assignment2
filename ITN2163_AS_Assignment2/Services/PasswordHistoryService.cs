using ITN2163_AS_Assignment2.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ITN2163_AS_Assignment2.Services
{
	public class PasswordHistoryService
	{
		private readonly AuthDbContext _context;
		private readonly PasswordHasher<ApplicationUser> _passwordHasher;

		public PasswordHistoryService(AuthDbContext context)
		{
			_context = context;
			_passwordHasher = new PasswordHasher<ApplicationUser>();
		}

		//// Check if the new password matches any of the last two passwords in the history
		//public async Task<bool> CheckPasswordHistory(ApplicationUser user, string newPassword)
		//{
		//	var passwordHistory = await _context.UserPasswordHistories
		//		.Where(ph => ph.UserId == user.Id)
		//		.OrderByDescending(ph => ph.DateChanged)
		//		.Take(3)
		//		.ToListAsync();

		//	foreach (var history in passwordHistory)
		//	{
		//		var result = _passwordHasher.VerifyHashedPassword(user, history.HashedPassword, newPassword);
		//		if (result == PasswordVerificationResult.Success)
		//		{
		//			return false;  // New password matches a previous one
		//		}
		//	}

		//	return true;  // No match found, it’s safe to proceed
		//}

		// Check if the new password exists in the last 2 passwords
		public async Task<bool> IsPasswordInHistory(ApplicationUser user, string newPassword)
		{
			// Get the last two password histories for the user
			var passwordHistory = await _context.UserPasswordHistories
				.Where(ph => ph.UserId == user.Id)
				.OrderByDescending(ph => ph.DateChanged)  // Order by most recent first
				.Take(3)  // Only take the last 2 passwords
				.ToListAsync();

			// Check if the new password matches any of the last two passwords
			foreach (var history in passwordHistory)
			{
				if (_passwordHasher.VerifyHashedPassword(user, history.HashedPassword, newPassword) == PasswordVerificationResult.Success)
				{
					return true;  // The password is in the history, return true to indicate a match
				}
			}

			return false;  // No match found, return false
		}

		// Update password history after a successful change or reset
		public async Task UpdatePasswordHistory(ApplicationUser user, string newPassword)
		{
			var hashedPassword = _passwordHasher.HashPassword(user, newPassword);

			var passwordHistory = new UserPasswordHistory
			{
				UserId = user.Id,
				HashedPassword = hashedPassword,
				DateChanged = System.DateTime.UtcNow
			};

			_context.UserPasswordHistories.Add(passwordHistory);

			// Remove the oldest password history if more than two entries exist
			var historyCount = await _context.UserPasswordHistories
				.Where(ph => ph.UserId == user.Id)
				.CountAsync();

			if (historyCount > 2)
			{
				var oldestHistory = await _context.UserPasswordHistories
					.Where(ph => ph.UserId == user.Id)
					.OrderBy(ph => ph.DateChanged)
					.FirstOrDefaultAsync();

				_context.UserPasswordHistories.Remove(oldestHistory);
			}

			await _context.SaveChangesAsync();
		}
	}
}
