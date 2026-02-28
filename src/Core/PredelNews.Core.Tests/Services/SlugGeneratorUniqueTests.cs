using FluentAssertions;
using PredelNews.Core.Services;

namespace PredelNews.Core.Tests.Services;

public class SlugGeneratorUniqueTests
{
    private readonly SlugGenerator _sut = new();

    [Fact]
    public async Task GenerateUniqueSlugAsync_BaseSlugAvailable_ReturnsBaseSlug()
    {
        var result = await _sut.GenerateUniqueSlugAsync("Тест", _ => Task.FromResult(false));
        result.Should().Be("test");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_BaseExists_ReturnsDash2()
    {
        var existingSlugs = new HashSet<string> { "test" };

        var result = await _sut.GenerateUniqueSlugAsync("Тест",
            slug => Task.FromResult(existingSlugs.Contains(slug)));

        result.Should().Be("test-2");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_BaseAndDash2Exist_ReturnsDash3()
    {
        var existingSlugs = new HashSet<string> { "test", "test-2" };

        var result = await _sut.GenerateUniqueSlugAsync("Тест",
            slug => Task.FromResult(existingSlugs.Contains(slug)));

        result.Should().Be("test-3");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_EmptyInput_ReturnsEmpty()
    {
        var result = await _sut.GenerateUniqueSlugAsync("", _ => Task.FromResult(false));
        result.Should().BeEmpty();
    }
}
