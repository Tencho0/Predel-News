namespace PredelNews.Core.Interfaces;

public interface IEmailService
{
    Task<bool> SendAsync(string toAddress, string subject, string body);
}
