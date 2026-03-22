using PredelNews.Core.Models;
using PredelNews.Core.ViewModels;

namespace PredelNews.Core.Services;

public interface IPollService
{
    Task<PollWidgetViewModel?> GetActivePollForDisplayAsync();
    Task<(bool Success, string Message, IEnumerable<PollOptionResult>? Results)> VoteAsync(int pollId, int optionId);
    Task<int> CreatePollAsync(string question, IEnumerable<string> options, int createdByUserId, DateTime? opensAt, DateTime? closesAt);
    Task ActivatePollAsync(int pollId);
    Task DeactivatePollAsync(int pollId);
    Task<IEnumerable<PollSummaryDto>> GetAllPollsAsync();
    Task<Poll?> GetPollWithOptionsAsync(int pollId);
    Task<(bool Success, string Message)> DeletePollAsync(int pollId);
    Task<IEnumerable<PollOptionResult>> GetResultsAsync(int pollId);
}
