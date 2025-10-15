using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using DmsProjeckt.Data;

namespace DmsProjeckt.Service
{
    public class EmailService
    {

        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body, List<(byte[] Data, string FileName, string MimeType)>? attachments = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
                throw new ArgumentNullException(nameof(_settings.SenderEmail), "SenderEmail fehlt in den Einstellungen.");

            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentNullException(nameof(to), "Empfänger-E-Mail darf nicht leer sein.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };

            // Anhänge einfügen
            if (attachments != null)
            {
                foreach (var att in attachments)
                {
                    builder.Attachments.Add(att.FileName, att.Data, ContentType.Parse(att.MimeType));
                }
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpServer, _settings.Port, false);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
