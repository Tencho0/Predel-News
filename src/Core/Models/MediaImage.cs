namespace PredelNews.Core.Models;

public class MediaImage
{
    public int Id { get; set; }
    public required string Url { get; set; }
    public string? AltText { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? FocalPoint { get; set; }

    public string GetCropUrl(int width, int height)
    {
        return $"{Url}?width={width}&height={height}&mode=crop";
    }

    public string GetResizeUrl(int maxWidth)
    {
        return $"{Url}?width={maxWidth}&mode=max";
    }
}
