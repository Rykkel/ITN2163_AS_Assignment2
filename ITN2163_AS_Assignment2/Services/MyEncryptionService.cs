using System;
using System.Security.Cryptography;
using System.Text;

namespace ITN2163_AS_Assignment2.Services
{
	public class MyEncryptionService
	{
		private readonly string _publicKey;
		private readonly string _privateKey;
		private readonly ILogger<MyEncryptionService> _logger;

		// Encrypt small data using RSA
		public MyEncryptionService(RsaKeyManager rsaKeyManager, ILogger<MyEncryptionService> logger)
		{
			_logger = logger;
			var keys = rsaKeyManager.GetOrCreateKeys();
			_publicKey = keys.PublicKey;
			_privateKey = keys.PrivateKey;
			_logger.LogInformation("RSA keys loaded successfully.");
		}

		public string EncryptWithRsa(string plainText)
		{
			using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
			{
				rsa.FromXmlString(_publicKey); // Load public key
				byte[] dataToEncrypt = Encoding.UTF8.GetBytes(plainText);
				byte[] encryptedData = rsa.Encrypt(dataToEncrypt, false);
				_logger.LogInformation("Data encrypted with RSA.");
				return Convert.ToBase64String(encryptedData); // Return Base64-encoded string
			}
		}

		// Decrypt small data using RSA
		public string DecryptWithRsa(string cipherText)
		{
			using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
			{
				rsa.FromXmlString(_privateKey); // load the private key
				byte[] dataToDecrypt = Convert.FromBase64String(cipherText);
				byte[] decryptedData = rsa.Decrypt(dataToDecrypt, false);
				_logger.LogInformation("Data decrypted with RSA.");
				return Encoding.UTF8.GetString(decryptedData); // Return plain text
			}
		}
	}
}
