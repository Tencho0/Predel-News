using PredelNews.Core.Models;

namespace PredelNews.Core.ViewModels;

public class CommentFormViewModel
{
    public int ArticleId { get; set; }
    public string PrefillName { get; set; } = string.Empty;
}

public class CommentListViewModel
{
    public IReadOnlyList<CommentDto> Comments { get; set; } = [];
    public int CommentCount { get; set; }
    public bool CanModerate { get; set; }
}
