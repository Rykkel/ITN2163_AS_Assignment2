using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ITN2163_AS_Assignment2.Services
{
	public class RsaKeyManager
	{
		private readonly IConfiguration _configuration;
		private readonly ILogger<RsaKeyManager> _logger;


		public RsaKeyManager(IConfiguration configuration, ILogger<RsaKeyManager> logger)
		{
			_configuration = configuration;
			_logger = logger;
		}

		public (string PublicKey, string PrivateKey) GetOrCreateKeys()
		{
			try
			{
				// Try to load keys from appsettings.json
				string publicKey = _configuration["RsaKeys:PublicKey"];
				string privateKey = _configuration["RsaKeys:PrivateKey"];

				if (!string.IsNullOrEmpty(publicKey) && !string.IsNullOrEmpty(privateKey))
				{
					_logger.LogInformation("RSA keys loaded from configuration.");
					return (publicKey, privateKey);
				}

				_logger.LogInformation("RSA keys not found in configuration. Generating new keys.");

				// Generate new keys
				using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
				{
					publicKey = rsa.ToXmlString(false);
					privateKey = rsa.ToXmlString(true);

					// Save the keys back to appsettings.json
					UpdateAppSettings(publicKey, privateKey);

					_logger.LogInformation("New RSA keys generated and saved.");
					return (publicKey, privateKey);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in GetOrCreateKeys");
				throw; // Re-throw the exception after logging
			}
		}

		private void UpdateAppSettings(string publicKey, string privateKey)
		{
			var configFilePath = "appsettings.json";

			try {
			// Load the existing JSON content
			var json = System.IO.File.ReadAllText(configFilePath);
			dynamic jsonObj = JsonConvert.DeserializeObject(json);

			// Update the keys
			jsonObj["RsaKeys"]["PublicKey"] = publicKey;
			jsonObj["RsaKeys"]["PrivateKey"] = privateKey;

			// Write the updated JSON back to the file
			string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
			System.IO.File.WriteAllText(configFilePath, output);

			_logger.LogInformation("AppSettings updated with new RSA keys.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update appsettings.json with RSA keys.");
				throw;
			}
		}
	}
}