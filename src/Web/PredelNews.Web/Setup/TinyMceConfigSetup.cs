using Microsoft.Extensions.Logging;
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
    public const string ArticleRteDataTypeName = "Article Body RTE";

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
        var existing = await _dataTypeService.GetAsync(ArticleRteKey);
        if (existing != null)
        {
            _logger.LogInformation("PredelNews: Article Body RTE data type already exists — skipping");
            return;
        }

        var richTextEditor = _propertyEditors
            .FirstOrDefault(e => e.Alias == Constants.PropertyEditors.Aliases.RichText)
            ?? throw new InvalidOperationException("RichText property editor not found");

        var dataType = new DataType(richTextEditor, _serializer, parentId: -1)
        {
            Key = ArticleRteKey,
            Name = ArticleRteDataTypeName,
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
        _logger.LogInformation("PredelNews: Created custom Article Body RTE data type");
    }
}
