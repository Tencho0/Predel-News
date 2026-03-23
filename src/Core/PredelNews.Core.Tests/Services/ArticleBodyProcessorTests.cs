using FluentAssertions;
using PredelNews.Core.Services;

namespace PredelNews.Core.Tests.Services;

public class ArticleBodyProcessorTests
{
    [Fact]
    public void Process_WhenSponsored_AddsRelToExternalLinks()
    {
        var html = "<p>See <a href=\"https://external.com\">this</a></p>";

        var result = ArticleBodyProcessor.Process(html, isSponsored: true);

        result.Should().Contain("rel=\"sponsored noopener\"");
    }

    [Fact]
    public void Process_WhenSponsored_SkipsInternalLinks()
    {
        var html = "<p>See <a href=\"https://predelnews.com/page\">internal</a></p>";

        var result = ArticleBodyProcessor.Process(html, isSponsored: true);

        result.Should().NotContain("sponsored");
    }

    [Fact]
    public void Process_WhenSponsored_SkipsRelativeLinks()
    {
        var html = "<p>See <a href=\"/page\">relative</a></p>";

        var result = ArticleBodyProcessor.Process(html, isSponsored: true);

        result.Should().NotContain("sponsored");
    }

    [Fact]
    public void Process_WhenNotSponsored_DoesNotModifyLinks()
    {
        var html = "<p>See <a href=\"https://external.com\">this</a></p>";

        var result = ArticleBodyProcessor.Process(html, isSponsored: false);

        result.Should().NotContain("sponsored");
        result.Should().Contain("href=\"https://external.com\"");
    }

    [Fact]
    public void Process_EmptyBody_ReturnsEmpty()
    {
        var result = ArticleBodyProcessor.Process(string.Empty, isSponsored: true);
        result.Should().BeEmpty();
    }

    [Fact]
    public void SplitAtParagraph_WhenEnoughParagraphs_SplitsAfterNth()
    {
        var html = "<p>One</p><p>Two</p><p>Three</p><p>Four</p>";

        var (before, after) = ArticleBodyProcessor.SplitAtParagraph(html, 3);

        before.Should().Be("<p>One</p><p>Two</p><p>Three</p>");
        after.Should().Be("<p>Four</p>");
    }

    [Fact]
    public void SplitAtParagraph_WhenFewerThanRequestedParagraphs_ReturnsFull()
    {
        var html = "<p>One</p><p>Two</p>";

        var (before, after) = ArticleBodyProcessor.SplitAtParagraph(html, 3);

        before.Should().Be("<p>One</p><p>Two</p>");
        after.Should().BeEmpty();
    }

    [Fact]
    public void SplitAtParagraph_WhenExactlyNParagraphs_AfterIsEmpty()
    {
        var html = "<p>One</p><p>Two</p><p>Three</p>";

        var (before, after) = ArticleBodyProcessor.SplitAtParagraph(html, 3);

        before.Should().Be("<p>One</p><p>Two</p><p>Three</p>");
        after.Should().BeEmpty();
    }
}
