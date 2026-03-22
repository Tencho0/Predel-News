using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using PredelNews.Core.Interfaces;

namespace PredelNews.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string toAddress, string subject, string body)
    {
        try
        {
            var host = _configuration["PredelNews:Email:SmtpHost"] ?? "localhost";
            var port = int.TryParse(_configuration["PredelNews:Email:SmtpPort"], out var p) ? p : 587;
            var user = _configuration["PredelNews:Email:SmtpUser"] ?? "";
            var password = _configuration["PredelNews:Email:SmtpPassword"] ?? "";
            var fromAddress = _configuration["PredelNews:Email:FromAddress"] ?? "noreply@predelnews.com";
            var useSsl = bool.TryParse(_configuration["PredelNews:Email:UseSsl"], out var ssl) && ssl;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Предел Нюз", fromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

            if (!string.IsNullOrEmpty(user))
                await client.AuthenticateAsync(user, password);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToAddress} with subject '{Subject}'", toAddress, subject);
            return false;
        }
    }
}
