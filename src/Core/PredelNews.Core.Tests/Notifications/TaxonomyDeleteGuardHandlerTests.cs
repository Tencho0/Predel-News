using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PredelNews.Core.Constants;
using PredelNews.Core.Notifications;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace PredelNews.Core.Tests.Notifications;

public class TaxonomyDeleteGuardHandlerTests
{
    private readonly IContentService _contentService = Substitute.For<IContentService>();
    private readonly IContentTypeService _contentTypeService = Substitute.For<IContentTypeService>();
    private readonly ILogger<TaxonomyDeleteGuardHandler> _logger = Substitute.For<ILogger<TaxonomyDeleteGuardHandler>>();
    private readonly TaxonomyDeleteGuardHandler _sut;

    public TaxonomyDeleteGuardHandlerTests()
    {
        _sut = new TaxonomyDeleteGuardHandler(_contentService, _contentTypeService, _logger);
    }

    private static IContent CreateTaxonomyNode(string typeAlias, Guid key)
    {
        var content = Substitute.For<IContent>();
        var contentType = Substitute.For<ISimpleContentType>();
        contentType.Alias.Returns(typeAlias);
        content.ContentType.Returns(contentType);
        content.Key.Returns(key);
        content.Id.Returns(1);
        return content;
    }

    private IContentType SetupArticleType()
    {
        var articleType = Substitute.For<IContentType>();
        articleType.Id.Returns(100);
        _contentTypeService.Get(DocumentTypes.Article).Returns(articleType);
        return articleType;
    }

    private void SetupArticlesReferencing(int articleTypeId, string propertyAlias, Guid taxonomyKey, int count)
    {
        var articles = new List<IContent>();
        for (int i = 0; i < count; i++)
        {
            var article = Substitute.For<IContent>();
            article.GetValue<string>(propertyAlias).Returns($"umb://document/{taxonomyKey:D}");
            articles.Add(article);
        }

        long total = count;
        _contentService.GetPagedOfTypes(
            Arg.Any<int[]>(),
            Arg.Any<long>(), Arg.Any<int>(), out Arg.Any<long>(),
            Arg.Any<Umbraco.Cms.Core.Persistence.Querying.IQuery<IContent>?>(),
            Arg.Any<Umbraco.Cms.Core.Services.Ordering?>())
            .Returns(x =>
            {
                x[3] = total;
                return articles;
            });
    }

    [Fact]
    public async Task Category_WithReferencingArticles_CancelsOperation()
    {
        var categoryKey = Guid.NewGuid();
        var category = CreateTaxonomyNode(DocumentTypes.Category, categoryKey);
        var articleType = SetupArticleType();
        SetupArticlesReferencing(articleType.Id, PropertyAliases.Category, categoryKey, 3);

        var notification = new ContentDeletingNotification(category, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeTrue();
    }

    [Fact]
    public async Task Category_WithNoReferencingArticles_Passes()
    {
        var categoryKey = Guid.NewGuid();
        var category = CreateTaxonomyNode(DocumentTypes.Category, categoryKey);
        var articleType = SetupArticleType();
        SetupArticlesReferencing(articleType.Id, PropertyAliases.Category, categoryKey, 0);

        var notification = new ContentDeletingNotification(category, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }

    [Fact]
    public async Task Region_WithReferencingArticles_CancelsOperation()
    {
        var regionKey = Guid.NewGuid();
        var region = CreateTaxonomyNode(DocumentTypes.Region, regionKey);
        var articleType = SetupArticleType();
        SetupArticlesReferencing(articleType.Id, PropertyAliases.Region, regionKey, 1);

        var notification = new ContentDeletingNotification(region, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeTrue();
    }

    [Fact]
    public async Task NonTaxonomyContent_IsNoOp()
    {
        var content = CreateTaxonomyNode("staticPage", Guid.NewGuid());
        var notification = new ContentDeletingNotification(content, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }
}
