namespace PredelNews.Core.Interfaces;

public interface IAuditLogRepository
{
    Task LogAsync(string eventType, int? userId, string? username,
                  string? entityType, int? entityId,
                  string? previousValue, string? newValue, string? notes = null);
}
