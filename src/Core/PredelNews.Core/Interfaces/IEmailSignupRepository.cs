using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface IEmailSignupRepository
{
    Task<bool> InsertIfNotExistsAsync(string email);
    Task<IEnumerable<EmailSubscriber>> GetAllAsync();
    Task<int> GetCountAsync();
}
