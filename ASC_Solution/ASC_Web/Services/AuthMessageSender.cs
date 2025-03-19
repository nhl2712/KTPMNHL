﻿using Microsoft.Extensions.Options;
/*using System.Net.Mail;*/
using MimeKit;
using MailKit;
using MailKit.Net.Smtp;
using ASC_Web.Configuration;
using ASC_Web.Services;
namespace ASC_Web.Services
{
    public class AuthMessageSender : IEmailSender, ISmsSender
    {
        private IOptions<ApplicationSetting> _settings;

        public AuthMessageSender(IOptions<ApplicationSetting> settings)
        {
            _settings = settings;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("admin", _settings.Value.SMTPAccount));
            emailMessage.To.Add(new MailboxAddress("user", email));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("plain") { Text = message };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(_settings.Value.SMTPServer, _settings.Value.SMTPPort, false);
                await client.AuthenticateAsync(_settings.Value.SMTPAccount, _settings.Value.SMTPPassword);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }


        public Task SendSmsAsync(string number, string message)
        {
            return Task.FromResult(0);
        }

    }
}