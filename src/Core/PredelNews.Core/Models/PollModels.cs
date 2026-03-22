namespace PredelNews.Core.Models;

public class Poll
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? OpensAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PollOption> Options { get; set; } = [];
}

public class PollOption
{
    public int Id { get; set; }
    public int PollId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public int SortOrder { get; set; }
}

public class PollOptionResult
{
    public int OptionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public decimal Percentage { get; set; }
}

public class PollSummaryDto
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? OpensAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalVotes { get; set; }
    public int OptionCount { get; set; }
}
