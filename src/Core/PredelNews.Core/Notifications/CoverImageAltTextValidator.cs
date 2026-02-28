using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace PredelNews.Core.Notifications;

public class CoverImageAltTextValidator : INotificationAsyncHandler<ContentSavingNotification>
{
    private readonly ILogger<CoverImageAltTextValidator> _logger;

    public CoverImageAltTextValidator(ILogger<CoverImageAltTextValidator> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken)
    {
        foreach (var entity in notification.SavedEntities)
        {
            if (entity.ContentType.Alias != DocumentTypes.Article)
                continue;

            var coverImageValue = entity.GetValue<string>(PropertyAliases.CoverImage);
            if (string.IsNullOrWhiteSpace(coverImageValue))
                continue;

            // Umbraco MediaPicker3 stores values as JSON containing mediaKey and optional altText.
            // If the JSON has a media reference but no altText (or empty altText), block the save.
            if (!HasAltText(coverImageValue))
            {
                notification.CancelOperation(new EventMessage("Грешка",
                    "Моля, добавете алтернативен текст за основната снимка.",
                    EventMessageType.Error));
                _logger.LogWarning("Article {Id}: Cover image save blocked — missing alt text", entity.Id);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    internal static bool HasAltText(string mediaPickerJson)
    {
        if (string.IsNullOrWhiteSpace(mediaPickerJson))
            return true;

        // MediaPicker3 stores JSON like: [{"mediaKey":"...","altText":"some text",...}]
        // Check that if a mediaKey exists, an altText with non-empty value is also present.
        if (!mediaPickerJson.Contains("\"mediaKey\"", StringComparison.OrdinalIgnoreCase))
            return true;

        // Look for "altText" with a non-empty value
        // Pattern: "altText":"<non-empty>"
        var altTextIndex = mediaPickerJson.IndexOf("\"altText\"", StringComparison.OrdinalIgnoreCase);
        if (altTextIndex < 0)
            return false;

        // Find the value after "altText":
        var colonIndex = mediaPickerJson.IndexOf(':', altTextIndex + 9);
        if (colonIndex < 0)
            return false;

        // Skip whitespace after colon
        var valueStart = colonIndex + 1;
        while (valueStart < mediaPickerJson.Length && char.IsWhiteSpace(mediaPickerJson[valueStart]))
            valueStart++;

        if (valueStart >= mediaPickerJson.Length)
            return false;

        // Check for "null" or empty string '""'
        if (mediaPickerJson[valueStart] == 'n') // null
            return false;

        if (mediaPickerJson[valueStart] == '"')
        {
            // Check if next char is closing quote (empty string)
            return valueStart + 1 < mediaPickerJson.Length && mediaPickerJson[valueStart + 1] != '"';
        }

        return true;
    }
}
