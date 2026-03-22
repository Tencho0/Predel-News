using System.Net.Mail;
using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;

namespace PredelNews.Core.Services;

public class ContactFormService : IContactFormService
{
    private readonly IContactFormRepository _repository;
    private readonly IEmailService _emailService;
    private readonly ISiteSettingsService _siteSettings;
    private readonly ILogger<ContactFormService> _logger;

    public ContactFormService(
        IContactFormRepository repository,
        IEmailService emailService,
        ISiteSettingsService siteSettings,
        ILogger<ContactFormService> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _siteSettings = siteSettings;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SubmitAsync(
        string name, string email, string subject, string message, string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0418\u043c\u0435\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.");
        if (string.IsNullOrWhiteSpace(email))
            return (false, "\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0418\u043c\u0435\u0439\u043b\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.");
        if (!MailAddress.TryCreate(email, out _))
            return (false, "\u041d\u0435\u0432\u0430\u043b\u0438\u0434\u0435\u043d \u0438\u043c\u0435\u0439\u043b \u0430\u0434\u0440\u0435\u0441.");
        if (string.IsNullOrWhiteSpace(subject))
            return (false, "\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0422\u0435\u043c\u0430\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.");
        if (string.IsNullOrWhiteSpace(message))
            return (false, "\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0421\u044a\u043e\u0431\u0449\u0435\u043d\u0438\u0435\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.");

        // DB first — graceful degradation if email fails
        await _repository.InsertAsync(name.Trim(), email.Trim(), subject.Trim(), message.Trim(), ipAddress);

        // Attempt email notification
        var settings = _siteSettings.GetSiteSettings();
        var recipientEmail = settings.ContactRecipientEmail;

        if (!string.IsNullOrWhiteSpace(recipientEmail))
        {
            try
            {
                var body = string.Join(System.Environment.NewLine,
                    "\u041d\u043e\u0432\u043e \u0437\u0430\u043f\u0438\u0442\u0432\u0430\u043d\u0435 \u043e\u0442 \u043a\u043e\u043d\u0442\u0430\u043a\u0442\u043d\u0430\u0442\u0430 \u0444\u043e\u0440\u043c\u0430:",
                    string.Empty,
                    $"\u0418\u043c\u0435: {name.Trim()}",
                    $"\u0418\u043c\u0435\u0439\u043b: {email.Trim()}",
                    $"\u0422\u0435\u043c\u0430: {subject.Trim()}",
                    string.Empty,
                    "\u0421\u044a\u043e\u0431\u0449\u0435\u043d\u0438\u0435:",
                    message.Trim(),
                    string.Empty,
                    "---",
                    $"IP \u0430\u0434\u0440\u0435\u0441: {ipAddress}");

                var emailSubject = $"\u041a\u043e\u043d\u0442\u0430\u043a\u0442\u043d\u0430 \u0444\u043e\u0440\u043c\u0430: {subject.Trim()}";
                await _emailService.SendAsync(recipientEmail, emailSubject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact form email for submission from {Email}", email);
            }
        }

        return (true, "\u0421\u044a\u043e\u0431\u0449\u0435\u043d\u0438\u0435\u0442\u043e \u0432\u0438 \u0431\u0435\u0448\u0435 \u0438\u0437\u043f\u0440\u0430\u0442\u0435\u043d\u043e \u0443\u0441\u043f\u0435\u0448\u043d\u043e.");
    }
}
