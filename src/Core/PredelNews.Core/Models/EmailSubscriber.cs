namespace PredelNews.Core.Models;

public class EmailSubscriber
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime SignedUpAt { get; set; }
    public bool ConsentFlag { get; set; }
}
