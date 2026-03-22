using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;
using PredelNews.Core.Services;
using PredelNews.Core.ViewModels;

namespace PredelNews.Core.Tests.Services;

public class PollServiceTests
{
    private readonly IPollRepository _repository = Substitute.For<IPollRepository>();
    private readonly ILogger<PollService> _logger = Substitute.For<ILogger<PollService>>();
    private readonly PollService _sut;

    public PollServiceTests()
    {
        _sut = new PollService(_repository, _logger);
    }

    [Fact]
    public async Task GetActivePollForDisplayAsync_ActivePollExists_ReturnsViewModel()
    {
        _repository.GetActivePollWithOptionsAsync().Returns(new Poll
        {
            Id = 1, Question = "\u0422\u0435\u0441\u0442\u043e\u0432\u0430 \u0430\u043d\u043a\u0435\u0442\u0430?", IsActive = true,
            Options = [
                new PollOption { Id = 10, OptionText = "\u0414\u0430", VoteCount = 3, SortOrder = 0 },
                new PollOption { Id = 11, OptionText = "\u041d\u0435", VoteCount = 7, SortOrder = 1 }
            ]
        });

        var result = await _sut.GetActivePollForDisplayAsync();

        result.Should().NotBeNull();
        result!.PollId.Should().Be(1);
        result.Question.Should().Be("\u0422\u0435\u0441\u0442\u043e\u0432\u0430 \u0430\u043d\u043a\u0435\u0442\u0430?");
        result.TotalVotes.Should().Be(10);
        result.Options.Should().HaveCount(2);
        result.Options[0].Percentage.Should().Be(30.0m);
        result.Options[1].Percentage.Should().Be(70.0m);
    }

    [Fact]
    public async Task GetActivePollForDisplayAsync_NoPoll_ReturnsNull()
    {
        _repository.GetActivePollWithOptionsAsync().Returns((Poll?)null);

        var result = await _sut.GetActivePollForDisplayAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActivePollForDisplayAsync_ClosedPoll_SetsIsClosedTrue()
    {
        _repository.GetActivePollWithOptionsAsync().Returns(new Poll
        {
            Id = 1, Question = "\u0421\u0442\u0430\u0440\u0430 \u0430\u043d\u043a\u0435\u0442\u0430?", IsActive = true,
            ClosesAt = DateTime.UtcNow.AddDays(-1),
            Options = [ new PollOption { Id = 10, OptionText = "\u0414\u0430", VoteCount = 0, SortOrder = 0 } ]
        });

        var result = await _sut.GetActivePollForDisplayAsync();

        result.Should().NotBeNull();
        result!.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task VoteAsync_ValidVote_IncrementsAndReturnsResults()
    {
        _repository.GetActivePollWithOptionsAsync().Returns(new Poll
        {
            Id = 1, Question = "Test?", IsActive = true,
            Options = [
                new PollOption { Id = 10, PollId = 1, OptionText = "A", VoteCount = 0, SortOrder = 0 },
                new PollOption { Id = 11, PollId = 1, OptionText = "B", VoteCount = 0, SortOrder = 1 }
            ]
        });
        _repository.GetResultsAsync(1).Returns([
            new PollOptionResult { OptionId = 10, OptionText = "A", VoteCount = 1, Percentage = 100.0m },
        ]);

        var (success, message, results) = await _sut.VoteAsync(1, 10);

        success.Should().BeTrue();
        results.Should().NotBeNull();
        await _repository.Received(1).IncrementVoteAsync(1, 10);
    }

    [Fact]
    public async Task VoteAsync_ClosedPoll_ReturnsError()
    {
        _repository.GetActivePollWithOptionsAsync().Returns(new Poll
        {
            Id = 1, Question = "Old?", IsActive = true,
            ClosesAt = DateTime.UtcNow.AddDays(-1),
            Options = [ new PollOption { Id = 10, PollId = 1, SortOrder = 0 } ]
        });

        var (success, message, _) = await _sut.VoteAsync(1, 10);

        success.Should().BeFalse();
        message.Should().Contain("\u043f\u0440\u0438\u043a\u043b\u044e\u0447\u0438\u043b\u0430");
    }

    [Fact]
    public async Task VoteAsync_InactivePoll_ReturnsError()
    {
        _repository.GetActivePollWithOptionsAsync().Returns((Poll?)null);

        var (success, message, _) = await _sut.VoteAsync(1, 10);

        success.Should().BeFalse();
    }

    [Fact]
    public async Task VoteAsync_OptionNotBelongingToPoll_ReturnsError()
    {
        _repository.GetActivePollWithOptionsAsync().Returns(new Poll
        {
            Id = 1, Question = "Test?", IsActive = true,
            Options = [ new PollOption { Id = 10, PollId = 1, SortOrder = 0 } ]
        });

        var (success, message, _) = await _sut.VoteAsync(1, 999);

        success.Should().BeFalse();
        await _repository.DidNotReceive().IncrementVoteAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task CreatePollAsync_TooFewOptions_ReturnsError()
    {
        var act = () => _sut.CreatePollAsync("Q?", ["Only one"], 1, null, null);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*2*4*");
    }

    [Fact]
    public async Task CreatePollAsync_TooManyOptions_ReturnsError()
    {
        var act = () => _sut.CreatePollAsync("Q?", ["A", "B", "C", "D", "E"], 1, null, null);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*2*4*");
    }

    [Fact]
    public async Task DeletePollAsync_PollWithVotes_ReturnsError()
    {
        _repository.HasVotesAsync(1).Returns(true);

        var (success, message) = await _sut.DeletePollAsync(1);

        success.Should().BeFalse();
        message.Should().Contain("\u0433\u043b\u0430\u0441\u043e\u0432\u0435");
        await _repository.DidNotReceive().DeletePollAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetActivePollForDisplayAsync_ZeroVotes_AllPercentagesZero()
    {
        _repository.GetActivePollWithOptionsAsync().Returns(new Poll
        {
            Id = 1, Question = "Empty?", IsActive = true,
            Options = [
                new PollOption { Id = 10, OptionText = "A", VoteCount = 0, SortOrder = 0 },
                new PollOption { Id = 11, OptionText = "B", VoteCount = 0, SortOrder = 1 }
            ]
        });

        var result = await _sut.GetActivePollForDisplayAsync();

        result!.Options.Should().AllSatisfy(o => o.Percentage.Should().Be(0m));
        result.TotalVotes.Should().Be(0);
    }
}
