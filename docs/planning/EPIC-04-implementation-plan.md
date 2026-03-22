# EPIC-04 — Editorial Workflow Implementation Plan

## Context

PredelNews needs a full newsroom workflow inside Umbraco so that Writers can submit articles for editorial review and Editors can publish, schedule, or reject them. Currently the content model has no concept of "In Review" state, no writer lock, no audit trail for article transitions, no output cache, and no editorial dashboard. This epic wires all of that up.

User decisions:
- Article states via a custom `articleStatus` dropdown + `updatedDate` DateTime on Article doc type
- Umbraco 17 Web Component dashboard registered via `umbraco-package.manifest`
- Full scope: all US-04.0x user stories

---

## Files to Create

| File | Purpose |
|------|---------|
| `src/Core/PredelNews.Core/Interfaces/IAuditLogRepository.cs` | Interface for writing to `pn_audit_log` |
| `src/Infrastructure/PredelNews.Infrastructure/Repositories/AuditLogRepository.cs` | Dapper impl of `IAuditLogRepository` |
| `src/Core/PredelNews.Core/Notifications/ArticleWorkflowGuardHandler.cs` | `ContentSavingNotification` — enforces role-based status transitions & writer lock |
| `src/Core/PredelNews.Core/Notifications/ArticlePublishedHandler.cs` | `ContentPublishingNotification` — auto-sets `updatedDate` on re-publish; `ContentPublishedNotification` — evicts output cache |
| `src/Core/PredelNews.Core/Notifications/ArticleUnpublishedHandler.cs` | `ContentUnpublishedNotification` — evicts output cache, audit logs |
| `src/BackofficeExtensions/PredelNews.BackofficeExtensions/Controllers/EditorialDashboardApiController.cs` | Management API endpoint returning dashboard JSON |
| `src/Web/PredelNews.Web/App_Plugins/PredelNews.Backoffice/umbraco-package.manifest` | Registers dashboard extension in Umbraco backoffice |
| `src/Web/PredelNews.Web/App_Plugins/PredelNews.Backoffice/editorial-dashboard.element.js` | Lit web component rendering the editorial dashboard |

## Files to Modify

| File | Change |
|------|--------|
| `src/Core/PredelNews.Core/Constants/PropertyAliases.cs` | Add `ArticleStatus`, `UpdatedDate` |
| `src/Web/PredelNews.Web/Setup/ContentTypeSetup.cs` | Add "Workflow" group to Article with `articleStatus` dropdown + `updatedDate` DateTime |
| `src/Web/PredelNews.Web/Composers/ContentSetupComposer.cs` | Register 3 new notification handlers |
| `src/Core/PredelNews.Core/Extensions/ServiceCollectionExtensions.cs` | Register `IAuditLogRepository` |
| `src/Infrastructure/PredelNews.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Register `AuditLogRepository` |
| `src/BackofficeExtensions/PredelNews.BackofficeExtensions/Extensions/ServiceCollectionExtensions.cs` | Register backoffice API dependencies |
| `src/Web/PredelNews.Web/Program.cs` | Add `AddOutputCache` + `UseOutputCache()` |
| `src/Web/PredelNews.Web/Controllers/ArticleController.cs` | Map `updatedDate` property into `ArticleDetailViewModel` |
| `src/Web/PredelNews.Web/Views/Article.cshtml` | Render "Обновена:" dateline when `Model.UpdatedDate` has value |
| All 9 page controllers | Add `[OutputCache(PolicyName = "PublicPage")]` with cache tags |

---

## Detailed Implementation

### 1. Constants — `PropertyAliases.cs`
```csharp
public const string ArticleStatus = "articleStatus";
public const string UpdatedDate   = "updatedDate";
```

### 2. Content Type — `ContentTypeSetup.cs`
Add a new **"Workflow"** group to the Article content type (after the existing Settings group):
- `articleStatus` — `DropDown` data type, values: `"Draft"`, `"In Review"`. Default: `"Draft"`. Visible to all; enforced server-side.
- `updatedDate` — `DateTime` property. Populated automatically by `ArticlePublishedHandler`; read-only hint in description.

The dropdown data type must be created first via `IDataTypeService` if it doesn't exist (pattern already used in ContentTypeSetup for other data types). Use the alias `"pnArticleStatusDropdown"` to avoid collisions.

### 3. Audit Log — `IAuditLogRepository` + `AuditLogRepository`
Thin Dapper repo matching the existing `pn_audit_log` schema:
```csharp
Task LogAsync(string eventType, int? userId, string? username,
              string? entityType, int? entityId,
              string? previousValue, string? newValue, string? notes = null);
```
Uses `IUmbracoDatabase` or `IDbConnection` (follow pattern from existing Infrastructure repos).

Register: `IAuditLogRepository` → `AuditLogRepository` (scoped).

### 4. `ArticleWorkflowGuardHandler` (ContentSavingNotification)

Injected: `IBackOfficeSecurityAccessor`, `IContentService`, `IAuditLogRepository`

Logic per saving Article:
```
1. Skip if not Article doc type
2. Get current user via IBackOfficeSecurityAccessor
3. Determine isWriter = user groups contain "writer" AND NOT "editor"/"admin"
4. Get the NEW status value from notification.SavedEntities (content being saved)
5. Get the EXISTING status from IContentService.GetById(content.Id) (null for new articles)
6. Writer lock: if isWriter AND existingStatus == "In Review" → CancelWithMessage("Статията е заключена за редакция.")
7. Transition guard: if isWriter AND newStatus is anything other than "Draft"/"In Review" → cancel
8. Log to pn_audit_log: eventType="article.status.changed", previousValue=existingStatus, newValue=newStatus
```

Follows exact same pattern as `SponsoredContentGuardHandler` for `notification.CancelWithNotify(...)`.

### 5. `ArticlePublishedHandler`

Handles TWO notifications:

**`ContentPublishingNotification`** (fires before publish, can modify content):
```
1. Skip if not Article
2. Check IContentService.GetById(id).Published == true  → this is a re-publish (correction)
3. If re-publish: set content.SetValue(PropertyAliases.UpdatedDate, DateTime.UtcNow)
   (modifying the entity here is safe — it's the same object being published)
4. Log to audit log: eventType="article.republished"
```

**`ContentPublishedNotification`** (fires after publish, for cache eviction):
```
1. Skip if not Article
2. Read categoryId, regionId from published content properties
3. Evict tags: "home", "allnews", "article:{id}", "category:{categoryId}", "region:{regionId}"
   via IOutputCacheStore.EvictByTagAsync(...)
4. Log to audit log: eventType="article.published"
```

### 6. `ArticleUnpublishedHandler` (ContentUnpublishedNotification)
Same cache eviction as above + audit log entry `"article.unpublished"`.

### 7. Output Cache — `Program.cs`

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("PublicPage", policy =>
        policy.Expire(TimeSpan.FromSeconds(60)));
});
// ...
app.UseOutputCache();   // before app.UseUmbraco()
```

Each controller gets:
```csharp
[OutputCache(PolicyName = "PublicPage", Tags = new[] { "home" })]          // HomePageController
[OutputCache(PolicyName = "PublicPage", Tags = new[] { "article:{id}" })]  // ArticleController (dynamic tag set in action)
// etc.
```

For dynamic tags (article ID, category ID), use `HttpContext.Response.Headers` or `IOutputCacheFeature` to add tags at request time inside the controller action (pattern: `HttpContext.Features.Get<IOutputCacheFeature>()?.Tags.Add("article:{id}")`).

### 8. UpdatedDate Rendering — `Article.cshtml`

Add after the publish date display:
```cshtml
@if (Model.UpdatedDate.HasValue)
{
  <span class="small text-meta">
    Обновена: @Model.UpdatedDate.Value.ToString("dd.MM.yyyy, HH:mm")
  </span>
}
```

Update `ArticleController.cs` to map the property:
```csharp
var updatedRaw = content.GetProperty(PropertyAliases.UpdatedDate)?.GetValue();
model.UpdatedDate = updatedRaw is DateTime dt && dt > model.PublishDate ? dt : null;
```

### 9. Editorial Dashboard API — `EditorialDashboardApiController`

Controller base: `ManagementApiControllerBase` (from `Umbraco.Cms.Api.Management`)
Route: `GET /umbraco/management/api/v1/predelnews/editorial`
Authorization: `[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]`

Response shape:
```json
{
  "inReviewCount": 3,
  "inReviewArticles": [
    { "id": 123, "headline": "...", "authorName": "...", "modifiedAt": "2026-03-03T14:22:00Z" }
  ],
  "publishedTodayCount": 5,
  "publishedThisWeekCount": 12,
  "heldCommentsCount": 2,
  "emailSignupsCount": 47
}
```

Data sources:
- `inReview*` → `IContentService.GetPagedOfType(articleTypeId, ...)` filtered by `articleStatus == "In Review"`
- `publishedToday/Week` → same service filtered by `PublishDate >= today/week`
- `heldCommentsCount` → Dapper: `SELECT COUNT(*) FROM pn_comments WHERE is_held=1 AND is_deleted=0`
- `emailSignupsCount` → Dapper: `SELECT COUNT(*) FROM pn_email_subscribers`

### 10. Web Component — `editorial-dashboard.element.js`

ES module placed in `App_Plugins/PredelNews.Backoffice/`. Imports from Umbraco's runtime module map:
```javascript
import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
```

On `connectedCallback`, fetches `/umbraco/management/api/v1/predelnews/editorial` using `tryExecute` pattern with the Umbraco auth context.

Renders:
- Row of 4 stat boxes: In Review / Published Today / Published This Week / Held Comments
- Table of In Review articles: Headline | Author | Last Modified | (link to `/umbraco#/content/content/edit/{id}`)
- Email signups count

Uses `<uui-box>`, `<uui-loader-bar>`, `<uui-table>` from Umbraco UI library (available via module map at runtime).

### 11. `umbraco-package.manifest`

```json
{
  "$schema": "../../umbraco-package-schema.json",
  "name": "PredelNews.Backoffice",
  "version": "1.0.0",
  "extensions": [
    {
      "type": "dashboard",
      "alias": "PredelNews.Dashboard.Editorial",
      "name": "Editorial Dashboard",
      "element": "/App_Plugins/PredelNews.Backoffice/editorial-dashboard.element.js",
      "weight": 100,
      "meta": { "label": "Editorial", "pathname": "editorial" },
      "conditions": [
        { "alias": "Umb.Condition.SectionAlias", "match": "Umb.Section.Content" }
      ]
    }
  ]
}
```

### 12. Article Preview (US-04.07)
Umbraco's built-in preview (`/umbraco/preview/{id}`) renders the public template in preview mode with unpublished content. Because all views use `@inherits UmbracoViewPage<T>`, preview works automatically — Umbraco's `IPublishedContent` in preview mode returns draft values. Preview access is restricted to authenticated backoffice users by Umbraco's auth middleware.

**No code changes required.** Verify manually after restarting.

---

## Execution Order

1. Constants → `PropertyAliases.cs`
2. Data type + properties → `ContentTypeSetup.cs`
3. `IAuditLogRepository` interface + `AuditLogRepository` Dapper impl
4. Register repos in DI (`Core` + `Infrastructure` extensions)
5. `ArticleWorkflowGuardHandler`
6. `ArticlePublishedHandler` (both publishing + published notifications)
7. `ArticleUnpublishedHandler`
8. Register all 3 handlers in `ContentSetupComposer.cs`
9. Output cache in `Program.cs` + add `[OutputCache]` to all 9 controllers
10. `ArticleController` maps `updatedDate`; `Article.cshtml` renders dateline
11. `EditorialDashboardApiController`
12. `editorial-dashboard.element.js` + `umbraco-package.manifest`
13. Register backoffice services in `ServiceCollectionExtensions.cs`
14. `dotnet build` → 0 errors

---

## Verification

1. `dotnet build PredelNews.slnx` → 0 errors
2. Run app → Umbraco startup logs show "Content type setup complete" without errors
3. Open Umbraco backoffice → Article editor shows new "Workflow" group with `articleStatus` dropdown
4. As Writer: create article, set status to "In Review", save → confirm locked (can't save again as Writer)
5. As Editor: open In Review article, publish → appears on public site
6. Edit the published article as Editor and republish → "Обновена" dateline renders on article page
7. Check `SELECT * FROM pn_audit_log ORDER BY created_at DESC` in SQL → status change rows present
8. Unpublish article → confirm article page returns 404 within 60 seconds
9. Backoffice Content section → "Editorial" tab appears in dashboard area
10. Dashboard shows In Review count, published counts, comment/signup stats
