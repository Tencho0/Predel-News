namespace PredelNews.Core.ViewModels;

public class ContactPageViewModel : BasePageViewModel
{
    public string? IntroText { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DisplayEmail { get; set; }
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = [];
}
