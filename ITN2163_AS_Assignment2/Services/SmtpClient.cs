using System.Net;
using System.Net.Mail;
using ITN2163_AS_Assignment2.Model;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ITN2163_AS_Assignment2.Services
{
	public class SmtpEmailSender : IEmailSender
	{
		private readonly SmtpSettings _smtpSettings;
		
		public SmtpEmailSender(IOptions<SmtpSettings> smtpSettings)
		{
			_smtpSettings = smtpSettings.Value;
		}

		public async Task SendEmailAsync(string email, string subject, string htmlMessage)
		{
			using (var client = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
			{
				client.EnableSsl = _smtpSettings.EnableSsl;
				client.Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password);

				var mailMessage = new MailMessage
				{
					From = new MailAddress(_smtpSettings.UserName),
					Subject = subject,
					Body = htmlMessage,
					IsBodyHtml = true,
				};

				mailMessage.To.Add(email);

				// Send the email asynchronously
				await client.SendMailAsync(mailMessage);
			}
		}
	}

}
