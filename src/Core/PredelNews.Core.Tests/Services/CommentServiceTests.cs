using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PredelNews.Core.Models;
using PredelNews.Core.Repositories;
using PredelNews.Core.Services;
using PredelNews.Core.ViewModels;

namespace PredelNews.Core.Tests.Services;

public class CommentServiceTests
{
    private readonly ICommentRepository _repository = Substitute.For<ICommentRepository>();
    private readonly ISiteSettingsService _siteSettings = Substitute.For<ISiteSettingsService>();
    private readonly ILogger<CommentService> _logger = Substitute.For<ILogger<CommentService>>();
    private readonly CommentService _sut;

    public CommentServiceTests()
    {
        _siteSettings.GetSiteSettings().Returns(new SiteSettingsViewModel { BannedWordsList = null });
        _sut = new CommentService(_repository, _siteSettings, _logger);
    }

    private static CommentSubmissionRequest MakeRequest(
        string displayName = "Мария",
        string commentText = "Страхотна статия!",
        string? honeypot = null) =>
        new(ArticleId: 1, DisplayName: displayName, CommentText: commentText,
            IpAddress: "127.0.0.1", HoneypotField: honeypot);

    [Fact]
    public async Task SubmitComment_HoneypotFilled_ReturnsHoneypotTripped()
    {
        var request = MakeRequest(honeypot: "spam-bot-value");

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.HoneypotTripped);
        await _repository.DidNotReceive().InsertCommentAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task SubmitComment_EmptyDisplayName_ReturnsInvalid()
    {
        var request = MakeRequest(displayName: "");

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Invalid);
        result.ValidationErrors.Should().ContainKey("displayName");
    }

    [Fact]
    public async Task SubmitComment_WhitespaceDisplayName_ReturnsInvalid()
    {
        var request = MakeRequest(displayName: "   ");

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Invalid);
        result.ValidationErrors.Should().ContainKey("displayName");
    }

    [Fact]
    public async Task SubmitComment_DisplayNameTooLong_ReturnsInvalid()
    {
        var request = MakeRequest(displayName: new string('А', 201));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Invalid);
        result.ValidationErrors.Should().ContainKey("displayName");
    }

    [Fact]
    public async Task SubmitComment_EmptyCommentText_ReturnsInvalid()
    {
        var request = MakeRequest(commentText: "");

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Invalid);
        result.ValidationErrors.Should().ContainKey("commentText");
    }

    [Fact]
    public async Task SubmitComment_CommentTextTooLong_ReturnsInvalid()
    {
        var request = MakeRequest(commentText: new string('Б', 2001));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Invalid);
        result.ValidationErrors.Should().ContainKey("commentText");
    }

    [Fact]
    public async Task SubmitComment_TwoOrMoreUrls_ReturnsHeld()
    {
        var request = MakeRequest(commentText: "Вижте https://example.com и https://spam.com за повече.");
        _repository.InsertCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), true, "link_count")
            .Returns(new CommentDto(1, "Мария", request.CommentText, DateTime.UtcNow));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Held);
        result.UserMessage.Should().Contain("прегледан");
        await _repository.Received(1).InsertCommentAsync(
            1, "Мария", request.CommentText, "127.0.0.1", true, "link_count");
    }

    [Fact]
    public async Task SubmitComment_SingleUrl_NotHeld()
    {
        var request = MakeRequest(commentText: "Вижте https://example.com за повече.");
        _repository.InsertCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(new CommentDto(1, "Мария", request.CommentText, DateTime.UtcNow));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Accepted);
    }

    [Fact]
    public async Task SubmitComment_BannedWord_ReturnsHeld()
    {
        _siteSettings.GetSiteSettings().Returns(new SiteSettingsViewModel
        {
            BannedWordsList = "спам,обида"
        });
        var request = MakeRequest(commentText: "Това е обида за всички.");
        _repository.InsertCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), true, "banned_word:обида")
            .Returns(new CommentDto(1, "Мария", request.CommentText, DateTime.UtcNow));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Held);
        await _repository.Received(1).InsertCommentAsync(
            1, "Мария", request.CommentText, "127.0.0.1", true, "banned_word:обида");
    }

    [Fact]
    public async Task SubmitComment_BannedWordCaseInsensitive_ReturnsHeld()
    {
        _siteSettings.GetSiteSettings().Returns(new SiteSettingsViewModel
        {
            BannedWordsList = "СПАМ"
        });
        var request = MakeRequest(commentText: "Това е спам коментар.");
        _repository.InsertCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), true, "banned_word:СПАМ")
            .Returns(new CommentDto(1, "Мария", request.CommentText, DateTime.UtcNow));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Held);
    }

    [Fact]
    public async Task SubmitComment_NoBannedWords_Accepted()
    {
        _siteSettings.GetSiteSettings().Returns(new SiteSettingsViewModel
        {
            BannedWordsList = "спам,обида"
        });
        var request = MakeRequest(commentText: "Чудесна статия, благодаря!");
        _repository.InsertCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(new CommentDto(1, "Мария", request.CommentText, DateTime.UtcNow));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Accepted);
        result.Comment.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitComment_CleanInput_ReturnsAccepted()
    {
        var request = MakeRequest();
        _repository.InsertCommentAsync(1, "Мария", "Страхотна статия!", "127.0.0.1", false, null)
            .Returns(new CommentDto(1, "Мария", "Страхотна статия!", DateTime.UtcNow));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Accepted);
        result.Comment.Should().NotBeNull();
        result.Comment!.DisplayName.Should().Be("Мария");
    }

    [Fact]
    public async Task SubmitComment_HtmlInDisplayName_StrippedBeforeStorage()
    {
        var request = MakeRequest(displayName: "<b>Иван</b>");
        _repository.InsertCommentAsync(1, "Иван", "Страхотна статия!", "127.0.0.1", false, null)
            .Returns(new CommentDto(1, "Иван", "Страхотна статия!", DateTime.UtcNow));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Accepted);
        await _repository.Received(1).InsertCommentAsync(
            1, "Иван", "Страхотна статия!", "127.0.0.1", false, null);
    }

    [Fact]
    public async Task SubmitComment_EmptyBannedWordsList_Accepted()
    {
        _siteSettings.GetSiteSettings().Returns(new SiteSettingsViewModel
        {
            BannedWordsList = ""
        });
        var request = MakeRequest();
        _repository.InsertCommentAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string?>())
            .Returns(new CommentDto(1, "Мария", request.CommentText, DateTime.UtcNow));

        var result = await _sut.SubmitCommentAsync(request);

        result.Status.Should().Be(CommentSubmissionStatus.Accepted);
    }

    [Fact]
    public async Task DeleteComment_ExistingComment_SoftDeletesAndAudits()
    {
        var existing = new CommentDto(42, "Иван", "Лош коментар", DateTime.UtcNow);
        _repository.GetByIdAsync(42).Returns(existing);

        await _sut.DeleteCommentAsync(42, 1, "admin@predel.bg");

        await _repository.Received(1).SoftDeleteAsync(42);
        await _repository.Received(1).InsertAuditLogAsync(
            42, "deleted", 1, "admin@predel.bg", "Лош коментар");
    }

    [Fact]
    public async Task DeleteComment_NonExistentComment_DoesNothing()
    {
        _repository.GetByIdAsync(99).Returns((CommentDto?)null);

        await _sut.DeleteCommentAsync(99, 1, "admin@predel.bg");

        await _repository.DidNotReceive().SoftDeleteAsync(Arg.Any<int>());
        await _repository.DidNotReceive().InsertAuditLogAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<int?>(),
            Arg.Any<string?>(), Arg.Any<string?>());
    }
}
