namespace PredelNews.Core.ViewModels;

public class PollWidgetViewModel
{
    public int PollId { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public List<PollOptionDisplay> Options { get; set; } = [];
    public int TotalVotes { get; set; }
}

public class PollOptionDisplay
{
    public int OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public decimal Percentage { get; set; }
}
