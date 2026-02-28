namespace PredelNews.Core.Services;

public interface ISlugGenerator
{
    string GenerateSlug(string input);
    Task<string> GenerateUniqueSlugAsync(string input, Func<string, Task<bool>> slugExistsChecker);
}
