using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PredelNews.Core.Constants;
using PredelNews.Core.Notifications;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace PredelNews.Core.Tests.Notifications;

public class ArticleTagCountValidatorTests
{
    private readonly ILogger<ArticleTagCountValidator> _logger = Substitute.For<ILogger<ArticleTagCountValidator>>();
    private readonly ArticleTagCountValidator _sut;

    public ArticleTagCountValidatorTests()
    {
        _sut = new ArticleTagCountValidator(_logger);
    }

    private static IContent CreateArticleWithTags(int tagCount)
    {
        var content = Substitute.For<IContent>();
        var contentType = Substitute.For<ISimpleContentType>();
        contentType.Alias.Returns(DocumentTypes.Article);
        content.ContentType.Returns(contentType);

        if (tagCount == 0)
        {
            content.GetValue<string>(PropertyAliases.Tags).Returns((string?)null);
        }
        else
        {
            var udis = Enumerable.Range(1, tagCount)
                .Select(i => $"umb://document/{Guid.NewGuid():N}");
            content.GetValue<string>(PropertyAliases.Tags).Returns(string.Join(",", udis));
        }

        return content;
    }

    [Fact]
    public async Task Article_WithNoTags_Passes()
    {
        var article = CreateArticleWithTags(0);
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }

    [Fact]
    public async Task Article_With10Tags_Passes()
    {
        var article = CreateArticleWithTags(10);
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }

    [Fact]
    public async Task Article_With11Tags_CancelsOperation()
    {
        var article = CreateArticleWithTags(11);
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeTrue();
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
    [InlineData("", 0)]
    [InlineData("umb://document/abc", 1)]
    [InlineData("umb://document/abc,umb://document/def", 2)]
    [InlineData("a,b,c,d,e,f,g,h,i,j", 10)]
    [InlineData("a,b,c,d,e,f,g,h,i,j,k", 11)]
    public void CountTags_ReturnsExpected(string input, int expected)
    {
        ArticleTagCountValidator.CountTags(input).Should().Be(expected);
    }
}
