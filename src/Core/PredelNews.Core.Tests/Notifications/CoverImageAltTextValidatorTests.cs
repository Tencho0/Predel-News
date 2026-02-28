using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PredelNews.Core.Constants;
using PredelNews.Core.Notifications;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace PredelNews.Core.Tests.Notifications;

public class CoverImageAltTextValidatorTests
{
    private readonly ILogger<CoverImageAltTextValidator> _logger = Substitute.For<ILogger<CoverImageAltTextValidator>>();
    private readonly CoverImageAltTextValidator _sut;

    public CoverImageAltTextValidatorTests()
    {
        _sut = new CoverImageAltTextValidator(_logger);
    }

    private static IContent CreateArticle(string? coverImageJson)
    {
        var content = Substitute.For<IContent>();
        var contentType = Substitute.For<ISimpleContentType>();
        contentType.Alias.Returns(DocumentTypes.Article);
        content.ContentType.Returns(contentType);
        content.GetValue<string>(PropertyAliases.CoverImage).Returns(coverImageJson);
        return content;
    }

    [Fact]
    public async Task Article_WithAltText_Passes()
    {
        var json = "[{\"mediaKey\":\"abc-123\",\"altText\":\"Описание на снимка\"}]";
        var article = CreateArticle(json);
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }

    [Fact]
    public async Task Article_WithEmptyAltText_CancelsOperation()
    {
        var json = "[{\"mediaKey\":\"abc-123\",\"altText\":\"\"}]";
        var article = CreateArticle(json);
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeTrue();
    }

    [Fact]
    public async Task Article_WithMissingAltText_CancelsOperation()
    {
        var json = "[{\"mediaKey\":\"abc-123\"}]";
        var article = CreateArticle(json);
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeTrue();
    }

    [Fact]
    public async Task Article_WithNullAltText_CancelsOperation()
    {
        var json = "[{\"mediaKey\":\"abc-123\",\"altText\":null}]";
        var article = CreateArticle(json);
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeTrue();
    }

    [Fact]
    public async Task Article_WithNoCoverImage_Passes()
    {
        var article = CreateArticle(null);
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }

    [Fact]
    public async Task NonArticle_IsNoOp()
    {
        var content = Substitute.For<IContent>();
        var contentType = Substitute.For<ISimpleContentType>();
        contentType.Alias.Returns("staticPage");
        content.ContentType.Returns(contentType);
        var notification = new ContentSavingNotification(content, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }

    [Theory]
    [InlineData("[{\"mediaKey\":\"abc\",\"altText\":\"text\"}]", true)]
    [InlineData("[{\"mediaKey\":\"abc\",\"altText\":\"\"}]", false)]
    [InlineData("[{\"mediaKey\":\"abc\"}]", false)]
    [InlineData("[{\"mediaKey\":\"abc\",\"altText\":null}]", false)]
    [InlineData("", true)]
    [InlineData("plain-text-no-media", true)]
    public void HasAltText_ReturnsExpected(string json, bool expected)
    {
        CoverImageAltTextValidator.HasAltText(json).Should().Be(expected);
    }
}
