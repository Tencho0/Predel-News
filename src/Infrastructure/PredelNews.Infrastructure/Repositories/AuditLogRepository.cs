using PredelNews.Core.Interfaces;
using Umbraco.Cms.Infrastructure.Scoping;

namespace PredelNews.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IScopeProvider _scopeProvider;

    public AuditLogRepository(IScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public async Task LogAsync(string eventType, int? userId, string? username,
                         string? entityType, int? entityId,
                         string? previousValue, string? newValue, string? notes = null)
    {
        using var scope = _scopeProvider.CreateScope();

        await scope.Database.ExecuteAsync(
            new NPoco.Sql(
                @"INSERT INTO [pn_audit_log] ([event_type], [acting_user_id], [acting_username],
                  [entity_type], [entity_id], [previous_value], [new_value], [notes])
                  VALUES (@0, @1, @2, @3, @4, @5, @6, @7)",
                eventType, userId, username, entityType, entityId,
                previousValue, newValue, notes));

        scope.Complete();
    }
}
