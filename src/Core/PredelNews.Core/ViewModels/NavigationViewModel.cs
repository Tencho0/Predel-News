namespace PredelNews.Core.ViewModels;

public class NavigationViewModel
{
    public List<NavItem> Categories { get; set; } = [];
    public List<NavItem> Regions { get; set; } = [];
}

public class NavItem
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
