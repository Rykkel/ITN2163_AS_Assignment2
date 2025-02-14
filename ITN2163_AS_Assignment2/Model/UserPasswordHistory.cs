namespace ITN2163_AS_Assignment2.Model
{
	public class UserPasswordHistory
	{
		public int Id { get; set; }
		public string UserId { get; set; }  // Foreign Key to ApplicationUser
		public string HashedPassword { get; set; }  // Store hashed passwords
		public DateTime DateChanged { get; set; }
	}
}
