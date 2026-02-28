using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PredelNews.Core.Constants;
using PredelNews.Core.Notifications;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

namespace PredelNews.Core.Tests.Notifications;

public class SponsoredContentGuardHandlerTests
{
    private readonly IBackOfficeSecurityAccessor _securityAccessor = Substitute.For<IBackOfficeSecurityAccessor>();
    private readonly ILogger<SponsoredContentGuardHandler> _logger = Substitute.For<ILogger<SponsoredContentGuardHandler>>();
    private readonly SponsoredContentGuardHandler _sut;

    public SponsoredContentGuardHandlerTests()
    {
        _sut = new SponsoredContentGuardHandler(_securityAccessor, _logger);
    }

    private static IContent CreateArticle(bool isSponsored, string? sponsorName = null)
    {
        var content = Substitute.For<IContent>();
        var contentType = Substitute.For<ISimpleContentType>();
        contentType.Alias.Returns(DocumentTypes.Article);
        content.ContentType.Returns(contentType);
        content.GetValue<bool>(PropertyAliases.IsSponsored).Returns(isSponsored);
        content.GetValue<string>(PropertyAliases.SponsorName).Returns(sponsorName);
        return content;
    }

    private static IContent CreateNonArticle()
    {
        var content = Substitute.For<IContent>();
        var contentType = Substitute.For<ISimpleContentType>();
        contentType.Alias.Returns("staticPage");
        content.ContentType.Returns(contentType);
        return content;
    }

    private void SetupUser(bool isAdmin)
    {
        var backOfficeSecurity = Substitute.For<IBackOfficeSecurity>();
        var user = Substitute.For<IUser>();
        var group = Substitute.For<IReadOnlyUserGroup>();
        group.Alias.Returns(isAdmin ? "admin" : "writer");
        user.Groups.Returns(new[] { group });
        backOfficeSecurity.CurrentUser.Returns(user);
        _securityAccessor.BackOfficeSecurity.Returns(backOfficeSecurity);
    }

    [Fact]
    public async Task NonAdmin_WithIsSponsored_CancelsOperation()
    {
        SetupUser(isAdmin: false);
        var article = CreateArticle(isSponsored: true, sponsorName: "TestSponsor");
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_WithIsSponsored_AndSponsorName_Passes()
    {
        SetupUser(isAdmin: true);
        var article = CreateArticle(isSponsored: true, sponsorName: "TestSponsor");
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }

    [Fact]
    public async Task Admin_WithIsSponsored_EmptySponsorName_CancelsOperation()
    {
        SetupUser(isAdmin: true);
        var article = CreateArticle(isSponsored: true, sponsorName: "");
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeTrue();
    }

    [Fact]
    public async Task IsSponsoredFalse_PassesRegardlessOfRole()
    {
        SetupUser(isAdmin: false);
        var article = CreateArticle(isSponsored: false);
        var notification = new ContentSavingNotification(article, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }

    [Fact]
    public async Task NonArticleContent_IsNoOp()
    {
        var content = CreateNonArticle();
        var notification = new ContentSavingNotification(content, new EventMessages());

        await _sut.HandleAsync(notification, CancellationToken.None);

        notification.Cancel.Should().BeFalse();
    }
}
