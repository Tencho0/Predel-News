using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;
using PredelNews.Core.ViewModels;

namespace PredelNews.Core.Services;

public class PollService : IPollService
{
    private readonly IPollRepository _repository;
    private readonly ILogger<PollService> _logger;

    public PollService(IPollRepository repository, ILogger<PollService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PollWidgetViewModel?> GetActivePollForDisplayAsync()
    {
        var poll = await _repository.GetActivePollWithOptionsAsync();
        if (poll == null) return null;

        var totalVotes = poll.Options.Sum(o => o.VoteCount);
        var isClosed = poll.ClosesAt.HasValue && poll.ClosesAt.Value <= DateTime.UtcNow;

        return new PollWidgetViewModel
        {
            PollId = poll.Id,
            Question = poll.Question,
            IsClosed = isClosed,
            TotalVotes = totalVotes,
            Options = poll.Options.Select(o => new PollOptionDisplay
            {
                OptionId = o.Id,
                Text = o.OptionText,
                VoteCount = o.VoteCount,
                Percentage = totalVotes > 0 ? Math.Round((decimal)o.VoteCount / totalVotes * 100, 1) : 0m
            }).ToList()
        };
    }

    public async Task<(bool Success, string Message, IEnumerable<PollOptionResult>? Results)> VoteAsync(int pollId, int optionId)
    {
        var poll = await _repository.GetActivePollWithOptionsAsync();

        if (poll == null || poll.Id != pollId)
            return (false, "\u0410\u043d\u043a\u0435\u0442\u0430\u0442\u0430 \u043d\u0435 \u0435 \u043d\u0430\u043c\u0435\u0440\u0435\u043d\u0430 \u0438\u043b\u0438 \u043d\u0435 \u0435 \u0430\u043a\u0442\u0438\u0432\u043d\u0430.", null);

        if (poll.ClosesAt.HasValue && poll.ClosesAt.Value <= DateTime.UtcNow)
            return (false, "\u0410\u043d\u043a\u0435\u0442\u0430\u0442\u0430 \u0435 \u043f\u0440\u0438\u043a\u043b\u044e\u0447\u0438\u043b\u0430.", null);

        if (poll.OpensAt.HasValue && poll.OpensAt.Value > DateTime.UtcNow)
            return (false, "\u0410\u043d\u043a\u0435\u0442\u0430\u0442\u0430 \u0432\u0441\u0435 \u043e\u0449\u0435 \u043d\u0435 \u0435 \u043e\u0442\u0432\u043e\u0440\u0435\u043d\u0430.", null);

        if (!poll.Options.Any(o => o.Id == optionId))
            return (false, "\u041d\u0435\u0432\u0430\u043b\u0438\u0434\u043d\u0430 \u043e\u043f\u0446\u0438\u044f.", null);

        await _repository.IncrementVoteAsync(pollId, optionId);
        var results = await _repository.GetResultsAsync(pollId);

        return (true, "\u0413\u043b\u0430\u0441\u044a\u0442 \u0432\u0438 \u0431\u0435\u0448\u0435 \u0437\u0430\u043f\u0438\u0441\u0430\u043d.", results);
    }

    public async Task<int> CreatePollAsync(string question, IEnumerable<string> options, int createdByUserId, DateTime? opensAt, DateTime? closesAt)
    {
        var optionList = options.ToList();

        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("\u0412\u044a\u043f\u0440\u043e\u0441\u044a\u0442 \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u0435\u043d.");

        if (optionList.Count < 2 || optionList.Count > 4)
            throw new ArgumentException("\u0410\u043d\u043a\u0435\u0442\u0430\u0442\u0430 \u0442\u0440\u044f\u0431\u0432\u0430 \u0434\u0430 \u0438\u043c\u0430 \u043c\u0435\u0436\u0434\u0443 2 \u0438 4 \u043e\u043f\u0446\u0438\u0438.");

        var pollId = await _repository.CreatePollAsync(question.Trim(), createdByUserId, opensAt, closesAt);

        for (var i = 0; i < optionList.Count; i++)
            await _repository.AddOptionAsync(pollId, optionList[i].Trim(), i);

        return pollId;
    }

    public async Task ActivatePollAsync(int pollId)
    {
        await _repository.ActivatePollAsync(pollId);
    }

    public async Task DeactivatePollAsync(int pollId)
    {
        await _repository.DeactivatePollAsync(pollId);
    }

    public async Task<IEnumerable<PollSummaryDto>> GetAllPollsAsync()
    {
        var polls = await _repository.GetAllPollsAsync();
        return polls.Select(p => new PollSummaryDto
        {
            Id = p.Id,
            Question = p.Question,
            IsActive = p.IsActive,
            OpensAt = p.OpensAt,
            ClosesAt = p.ClosesAt,
            CreatedAt = p.CreatedAt,
            TotalVotes = p.Options.Sum(o => o.VoteCount),
            OptionCount = p.Options.Count
        });
    }

    public async Task<Poll?> GetPollWithOptionsAsync(int pollId)
    {
        return await _repository.GetPollWithOptionsAsync(pollId);
    }

    public async Task<(bool Success, string Message)> DeletePollAsync(int pollId)
    {
        if (await _repository.HasVotesAsync(pollId))
            return (false, "\u041d\u0435 \u043c\u043e\u0436\u0435 \u0434\u0430 \u0438\u0437\u0442\u0440\u0438\u0435\u0442\u0435 \u0430\u043d\u043a\u0435\u0442\u0430 \u0441 \u0433\u043b\u0430\u0441\u043e\u0432\u0435.");

        await _repository.DeletePollAsync(pollId);
        return (true, "\u0410\u043d\u043a\u0435\u0442\u0430\u0442\u0430 \u0431\u0435\u0448\u0435 \u0438\u0437\u0442\u0440\u0438\u0442\u0430.");
    }

    public async Task<IEnumerable<PollOptionResult>> GetResultsAsync(int pollId)
    {
        return await _repository.GetResultsAsync(pollId);
    }
}
