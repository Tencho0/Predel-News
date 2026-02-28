using FluentAssertions;
using PredelNews.Core.Services;

namespace PredelNews.Core.Tests.Services;

public class SlugGeneratorTests
{
    private readonly SlugGenerator _sut = new();

    [Theory]
    [InlineData("Пожар в Благоевград", "pozhar-v-blagoevgrad")]
    [InlineData("ПОЖАР В БЛАГОЕВГРАД", "pozhar-v-blagoevgrad")]
    [InlineData("Общество", "obshtestvo")]
    [InlineData("Кюстендил", "kyustendil")]
    [InlineData("щъркел", "shtarkel")]
    [InlineData("Нощен живот!!!", "noshten-zhivot")]
    [InlineData("Две  думи", "dve-dumi")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("Hello World", "hello-world")]
    [InlineData("Микс latin Кирилица", "miks-latin-kirilitsa")]
    [InlineData("-leading-trailing-", "leading-trailing")]
    [InlineData("a---b", "a-b")]
    public void GenerateSlug_ReturnsExpectedResult(string input, string expected)
    {
        var result = _sut.GenerateSlug(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void GenerateSlug_NullInput_ReturnsEmpty()
    {
        var result = _sut.GenerateSlug(null!);
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSlug_AllSpecialChars_ReturnsEmpty()
    {
        var result = _sut.GenerateSlug("!@#$%^&*()");
        result.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSlug_MixedCyrillicLatinNumbers_Works()
    {
        var result = _sut.GenerateSlug("Новина 123 news");
        result.Should().Be("novina-123-news");
    }
}
