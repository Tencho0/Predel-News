using Examine;
using Examine.Lucene;
using Examine.Lucene.Providers;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using PredelNews.Core.Constants;
using PredelNews.Core.Services;
using PredelNews.Web.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Examine;

namespace PredelNews.Web.Search;

public class ArticleExamineComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        var fieldDefinitions = new FieldDefinitionCollection(
            new FieldDefinition(PropertyAliases.Headline, FieldDefinitionTypes.FullTextSortable),
            new FieldDefinition(PropertyAliases.Subtitle, FieldDefinitionTypes.FullText),
            new FieldDefinition("bodyText", FieldDefinitionTypes.FullText),
            new FieldDefinition(PropertyAliases.Tags, FieldDefinitionTypes.FullText),
            new FieldDefinition(PropertyAliases.CategoryName, FieldDefinitionTypes.FullText),
            new FieldDefinition(PropertyAliases.RegionName, FieldDefinitionTypes.FullText),
            new FieldDefinition("authorName", FieldDefinitionTypes.FullText),
            new FieldDefinition(PropertyAliases.PublishDate, FieldDefinitionTypes.DateTime),
            new FieldDefinition(PropertyAliases.IsSponsored, FieldDefinitionTypes.Raw),
            new FieldDefinition("articleUrl", FieldDefinitionTypes.Raw)
        );

        builder.Services.AddExamineLuceneIndex<LuceneIndex, ConfigurationEnabledDirectoryFactory>(
            SearchConstants.ArticleIndexName,
            fieldDefinitions,
            new StandardAnalyzer(LuceneVersion.LUCENE_48),
            null,
            null);

        builder.Services.AddScoped<ArticleValueSetBuilder>();
        builder.Services.AddScoped<ISearchService, ExamineSearchService>();

        builder.AddNotificationAsyncHandler<ContentPublishedNotification, ArticlePublishedIndexHandler>();
        builder.AddNotificationAsyncHandler<ContentUnpublishedNotification, ArticleUnpublishedIndexHandler>();
    }
}
