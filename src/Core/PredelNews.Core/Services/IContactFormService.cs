namespace PredelNews.Core.Services;

public interface IContactFormService
{
    Task<(bool Success, string Message)> SubmitAsync(string name, string email, string subject, string message, string? ipAddress);
}
