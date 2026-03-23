namespace PredelNews.Core.Models;

public class AdSlot
{
    public int Id { get; set; }
    public string SlotId { get; set; } = string.Empty;
    public string SlotName { get; set; } = string.Empty;
    public string Mode { get; set; } = "adsense";
    public string? AdsenseCode { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? BannerDestUrl { get; set; }
    public string? BannerAltText { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsDirectActive =>
        Mode == "direct" &&
        (StartDate == null || StartDate <= DateTime.UtcNow) &&
        (EndDate == null || EndDate > DateTime.UtcNow);
}
