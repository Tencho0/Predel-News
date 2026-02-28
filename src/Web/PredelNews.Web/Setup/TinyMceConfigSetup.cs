using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;

namespace PredelNews.Web.Setup;

public class TinyMceConfigSetup : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IDataTypeService _dataTypeService;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _serializer;
    private readonly ILogger<TinyMceConfigSetup> _logger;

    public static readonly Guid ArticleRteKey = Guid.Parse("a1b2c3d4-e5f6-4000-8000-000000000001");
    public static readonly Guid ArticleTagsPickerKey = Guid.Parse("a1b2c3d4-e5f6-4000-8000-000000000002");

    public TinyMceConfigSetup(
        IDataTypeService dataTypeService,
        PropertyEditorCollection propertyEditors,
        IConfigurationEditorJsonSerializer serializer,
        ILogger<TinyMceConfigSetup> logger)
    {
        _dataTypeService = dataTypeService;
        _propertyEditors = propertyEditors;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        await EnsureArticleRteAsync();
        await EnsureArticleTagsPickerAsync();
    }

    private async Task EnsureArticleRteAsync()
    {
        if (await _dataTypeService.GetAsync(ArticleRteKey) != null)
            return;

        var richTextEditor = _propertyEditors
            .FirstOrDefault(e => e.Alias == Constants.PropertyEditors.Aliases.RichText)
            ?? throw new InvalidOperationException("RichText property editor not found");

        var dataType = new DataType(richTextEditor, _serializer, parentId: -1)
        {
            Key = ArticleRteKey,
            Name = "Article Body RTE",
            EditorUiAlias = "Umb.PropertyEditorUi.Tiptap",
            DatabaseType = ValueStorageType.Ntext,
        };

        dataType.ConfigurationData = new Dictionary<string, object>
        {
            ["extensions"] = new[]
            {
                "Umb.Tiptap.RichTextEssentials",
                "Umb.Tiptap.Bold",
                "Umb.Tiptap.Italic",
                "Umb.Tiptap.Underline",
                "Umb.Tiptap.Heading",
                "Umb.Tiptap.BulletList",
                "Umb.Tiptap.OrderedList",
                "Umb.Tiptap.Link",
                "Umb.Tiptap.Image",
                "Umb.Tiptap.Embed",
                "Umb.Tiptap.Blockquote",
                "Umb.Tiptap.Block",
                "Umb.Tiptap.Figure",
                "Umb.Tiptap.MediaUpload",
                "Umb.Tiptap.TrailingNode",
            },
            ["toolbar"] = new object[]
            {
                new object[]
                {
                    new[] { "Umb.Tiptap.Toolbar.Bold", "Umb.Tiptap.Toolbar.Italic", "Umb.Tiptap.Toolbar.Underline" },
                    new[] { "Umb.Tiptap.Toolbar.Heading2", "Umb.Tiptap.Toolbar.Heading3" },
                    new[] { "Umb.Tiptap.Toolbar.BulletList", "Umb.Tiptap.Toolbar.OrderedList" },
                    new[] { "Umb.Tiptap.Toolbar.Blockquote" },
                    new[] { "Umb.Tiptap.Toolbar.Link", "Umb.Tiptap.Toolbar.Unlink" },
                    new[] { "Umb.Tiptap.Toolbar.MediaPicker", "Umb.Tiptap.Toolbar.EmbeddedMedia" },
                }
            },
            ["maxImageSize"] = 500,
            ["overlaySize"] = "medium",
        };

        await _dataTypeService.CreateAsync(dataType, Constants.Security.SuperUserKey);
        _logger.LogInformation("PredelNews: Created Article Body RTE data type");
    }

    private async Task EnsureArticleTagsPickerAsync()
    {
        if (await _dataTypeService.GetAsync(ArticleTagsPickerKey) != null)
            return;

        var mntpEditor = _propertyEditors
            .FirstOrDefault(e => e.Alias == Constants.PropertyEditors.Aliases.MultiNodeTreePicker)
            ?? throw new InvalidOperationException("MultiNodeTreePicker property editor not found");

        var dataType = new DataType(mntpEditor, _serializer, parentId: -1)
        {
            Key = ArticleTagsPickerKey,
            Name = "Article Tags Picker",
            EditorUiAlias = "Umb.PropertyEditorUi.ContentPicker",
            DatabaseType = ValueStorageType.Ntext,
        };

        dataType.ConfigurationData = new Dictionary<string, object>
        {
            ["minNumber"] = 0,
            ["maxNumber"] = 10,
            ["showOpenButton"] = false,
            ["filter"] = DocumentTypes.NewsTag,
            ["ignoreUserStartNodes"] = false,
        };

        await _dataTypeService.CreateAsync(dataType, Constants.Security.SuperUserKey);
        _logger.LogInformation("PredelNews: Created Article Tags Picker data type");
    }
}
