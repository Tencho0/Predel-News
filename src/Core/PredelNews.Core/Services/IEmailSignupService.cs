namespace PredelNews.Core.Services;

public interface IEmailSignupService
{
    Task<(bool Success, string Message)> SignupAsync(string email, bool consentChecked);
}
