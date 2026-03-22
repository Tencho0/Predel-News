namespace PredelNews.Core.Interfaces;

public interface IContactFormRepository
{
    Task InsertAsync(string name, string email, string subject, string message, string? ipAddress);
}
