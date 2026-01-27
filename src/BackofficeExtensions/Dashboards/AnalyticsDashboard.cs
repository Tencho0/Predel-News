using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Dashboards;

namespace PredelNews.BackofficeExtensions.Dashboards;

[Weight(10)]
public class AnalyticsDashboard : IDashboard
{
    public string Alias => "preDelNewsAnalytics";
    public string View => "/App_Plugins/PredelNews/dashboards/analytics.html";

    public string[] Sections => new[]
    {
        Umbraco.Cms.Core.Constants.Applications.Content
    };

    public IAccessRule[] AccessRules => Array.Empty<IAccessRule>();
}
