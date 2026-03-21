using FluentAssertions;
using PredelNews.Core.Services;

namespace PredelNews.Core.Tests.Services;

public class SearchQuerySanitizerTests
{
    [Fact]
    public void Sanitize_NullInput_ReturnsNull()
    {
        SearchQuerySanitizer.Sanitize(null).Should().BeNull();
    }

    [Fact]
    public void Sanitize_EmptyString_ReturnsNull()
    {
        SearchQuerySanitizer.Sanitize("").Should().BeNull();
    }

    [Fact]
    public void Sanitize_WhitespaceOnly_ReturnsNull()
    {
        SearchQuerySanitizer.Sanitize("   ").Should().BeNull();
    }

    [Fact]
    public void Sanitize_HtmlTags_StripsTagsKeepsText()
    {
        var result = SearchQuerySanitizer.Sanitize("<script>alert('x')</script>пожар");

        result.Should().NotBeNull();
        result.Should().Contain("пожар");
        result.Should().NotContain("<script>");
        result.Should().NotContain("</script>");
    }

    [Fact]
    public void Sanitize_LuceneSpecialChars_EscapesThem()
    {
        var result = SearchQuerySanitizer.Sanitize("test+query");
        result.Should().Contain(@"\+");

        result = SearchQuerySanitizer.Sanitize("test-query");
        result.Should().Contain(@"\-");

        result = SearchQuerySanitizer.Sanitize("test(query)");
        result.Should().Contain(@"\(");
        result.Should().Contain(@"\)");

        result = SearchQuerySanitizer.Sanitize("test\"query");
        result.Should().Contain(@"\""");

        result = SearchQuerySanitizer.Sanitize("test*query");
        result.Should().Contain(@"\*");

        result = SearchQuerySanitizer.Sanitize("test?query");
        result.Should().Contain(@"\?");
    }

    [Fact]
    public void Sanitize_DoubleOperators_EscapesThem()
    {
        var result = SearchQuerySanitizer.Sanitize("test&&query");
        result.Should().Contain(@"\&&");

        result = SearchQuerySanitizer.Sanitize("test||query");
        result.Should().Contain(@"\||");
    }

    [Fact]
    public void Sanitize_LongQuery_TruncatesTo200()
    {
        var longInput = new string('а', 300);
        var result = SearchQuerySanitizer.Sanitize(longInput);

        result.Should().NotBeNull();
        result!.Length.Should().BeLessThanOrEqualTo(200);
    }

    [Fact]
    public void Sanitize_NormalCyrillicText_ReturnsUnchanged()
    {
        var input = "Пожар в Благоевград";
        var result = SearchQuerySanitizer.Sanitize(input);

        result.Should().Be(input);
    }

    [Fact]
    public void Sanitize_MixedHtmlAndSpecialChars_HandlesCorrectly()
    {
        var result = SearchQuerySanitizer.Sanitize("<b>test</b>+query");

        result.Should().NotBeNull();
        result.Should().NotContain("<b>");
        result.Should().Contain(@"\+");
        result.Should().Contain("test");
        result.Should().Contain("query");
    }

    [Fact]
    public void Sanitize_OnlyHtmlTags_ReturnsNull()
    {
        SearchQuerySanitizer.Sanitize("<br><hr>").Should().BeNull();
    }
}
