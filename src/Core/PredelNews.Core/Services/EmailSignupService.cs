using System.Net.Mail;
using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;

namespace PredelNews.Core.Services;

public class EmailSignupService : IEmailSignupService
{
    private readonly IEmailSignupRepository _repository;
    private readonly ILogger<EmailSignupService> _logger;

    public EmailSignupService(IEmailSignupRepository repository, ILogger<EmailSignupService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> SignupAsync(string email, bool consentChecked)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0418\u043c\u0435\u0439\u043b\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.");

        if (!MailAddress.TryCreate(email.Trim(), out _))
            return (false, "\u041d\u0435\u0432\u0430\u043b\u0438\u0434\u0435\u043d \u0438\u043c\u0435\u0439\u043b \u0430\u0434\u0440\u0435\u0441.");

        if (!consentChecked)
            return (false, "\u041c\u043e\u043b\u044f, \u043e\u0442\u0431\u0435\u043b\u0435\u0436\u0435\u0442\u0435 \u0441\u044a\u0433\u043b\u0430\u0441\u0438\u0435\u0442\u043e \u0437\u0430 \u043f\u043e\u043b\u0443\u0447\u0430\u0432\u0430\u043d\u0435 \u043d\u0430 \u0438\u043c\u0435\u0439\u043b\u0438.");

        await _repository.InsertIfNotExistsAsync(email.Trim().ToLowerInvariant());

        return (true, "\u0411\u043b\u0430\u0433\u043e\u0434\u0430\u0440\u0438\u043c! \u0418\u043c\u0435\u0439\u043b\u044a\u0442 \u0432\u0438 \u0431\u0435\u0448\u0435 \u0437\u0430\u043f\u0438\u0441\u0430\u043d.");
    }
}
