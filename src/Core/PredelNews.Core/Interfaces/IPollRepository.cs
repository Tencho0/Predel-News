using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface IPollRepository
{
    // Public-facing
    Task<Poll?> GetActivePollWithOptionsAsync();
    Task IncrementVoteAsync(int pollId, int optionId);
    Task<IEnumerable<PollOptionResult>> GetResultsAsync(int pollId);

    // Backoffice
    Task<int> CreatePollAsync(string question, int createdByUserId, DateTime? opensAt, DateTime? closesAt);
    Task AddOptionAsync(int pollId, string optionText, int sortOrder);
    Task ActivatePollAsync(int pollId);
    Task DeactivatePollAsync(int pollId);
    Task<IEnumerable<Poll>> GetAllPollsAsync();
    Task<Poll?> GetPollWithOptionsAsync(int pollId);
    Task DeletePollAsync(int pollId);
    Task<bool> HasVotesAsync(int pollId);
}
