# EPIC-09 Monetization — Design Spec

**Date:** 2026-03-23
**Epic:** EPIC-09 — Ads & Sponsored Content
**Status:** Approved

---

## Context

PredelNews needs revenue from Day 1. EPIC-09 delivers 7 configurable ad slots (AdSense + direct-sold banners), sponsored article labeling ("Платена публикация"), and admin tooling to manage placements without developer help.

### What already exists

- `pn_ad_slots` table created and seeded with 6 rows (V1 migration)
- `AdsensePublisherId` / `AdsenseScriptTag` on `SiteSettingsViewModel`
- `IsSponsored` / `SponsorName` on `ArticleDetailViewModel` and `ArticleSummaryViewModel`
- `_SponsoredBanner.cshtml` partial (bare-bones, no sponsor name)
- `Article.cshtml` calls `_SponsoredBanner` at top/bottom when `IsSponsored = true`
- `_ArticleCard.cshtml` shows "Платена" badge on both card variants
- Ad slot CSS classes (`pn-ad-slot`, `pn-ad-leaderboard`, `pn-ad-sidebar`) with `::before` emitting "Реклама"
- Static placeholder `<div class="pn-ad-slot ...">` in `Article.cshtml` and `HomePage.cshtml`

---

## Key Decisions

| Question | Decision | Rationale |
|---|---|---|
| Ad slot count | 7 (6 from EPIC + `mobile-sticky`) | Seed already has `mobile-sticky`; `article-bottom` added via V2 migration |
| Ad slot data delivery | `AdSlotViewComponent` | Matches existing `Navigation` component pattern; independently cacheable |
| Banner image storage | External URL only | Advertiser-provided assets; no file upload infrastructure at MVP |
| Sponsored link rewriting | Render-time `HtmlHelper` extension | Reversible; no stored HTML mutation |
| Ad slot caching | `IMemoryCache` 60s absolute expiry | Single-server VPS; matches EPIC "within 60 seconds" requirement |
| Architecture | Full layered (Repository → Service → ViewComponent) | Consistent with comments, polls, email signup |

---

## Ad Slot Inventory

| `slot_id` | Description | Dimensions | Pages |
|---|---|---|---|
| `header-leaderboard` | Below header, above content | 728×90 (desktop) / 320×100 (mobile) | All pages |
| `sidebar-top` | Right sidebar, top | 300×250 | Homepage, Article |
| `sidebar-bottom` | Right sidebar, mid | 300×250 | Homepage, Article |
| `article-inline` | After 3rd paragraph | 728×90 | Article only |
| `article-bottom` | Below body, above comments | 728×90 | Article only |
| `footer-banner` | Above footer | 728×90 | All pages |
| `mobile-sticky` | Fixed bottom of viewport | 320×50 | Mobile only |

---

## Section 1: Data Layer

### Domain Model

**`PredelNews.Core/Models/AdSlot.cs`**

```csharp
public class AdSlot
{
    public int Id { get; set; }
    public string SlotId { get; set; } = string.Empty;
    public string SlotName { get; set; } = string.Empty;
    public string Mode { get; set; } = "adsense";   // "adsense" | "direct"
    public string? AdsenseCode { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? BannerDestUrl { get; set; }
    public string? BannerAltText { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Computed — true when direct mode is active within date range
    public bool IsDirectActive =>
        Mode == "direct" &&
        (StartDate == null || StartDate <= DateTime.UtcNow) &&
        (EndDate == null || EndDate > DateTime.UtcNow);
}
```

### Repository

**`PredelNews.Core/Interfaces/IAdSlotRepository.cs`**
```csharp
public interface IAdSlotRepository
{
    Task<IReadOnlyList<AdSlot>> GetAllAsync();
    Task<AdSlot?> GetBySlotIdAsync(string slotId);
    Task UpdateAsync(AdSlot slot);
}
```

**`PredelNews.Infrastructure/Repositories/AdSlotRepository.cs`** — Dapper via `IScopeProvider`, same pattern as `PollRepository`.

### Migration

**`AlterAdSlotsV2Migration`** in `PredelNews.Infrastructure/Migrations/`:
- Seeds the missing `article-bottom` row: `("article-bottom", "Article Bottom (728x90)", "adsense")`
- Added as a new state step in `PredelNewsMigrationPlan`

### Service

**`PredelNews.Core/Interfaces/IAdSlotService.cs`** / **`PredelNews.Core/Services/AdSlotService.cs`**

- `GetAllAsync()` — cache key `"AdSlots:All"`, 60s absolute expiry via `IMemoryCache`
- `GetBySlotIdAsync(string slotId)` — reads from `GetAllAsync()` (no separate DB call)
- `UpdateSlotAsync(AdSlot slot)` — calls `IAdSlotRepository.UpdateAsync`, evicts `"AdSlots:All"`

---

## Section 2: Rendering Layer

### ViewComponent

**`PredelNews.Web/ViewComponents/AdSlotViewComponent.cs`**
- Takes `string slotId` parameter
- Calls `IAdSlotService.GetBySlotIdAsync(slotId)`
- Falls back to AdSense mode if slot not found
- Returns `View("_AdSlot", adSlot)`

### Partial View

**`Views/Shared/Components/AdSlot/_AdSlot.cshtml`**
```html
<div class="pn-ad-slot pn-ad-@Model.SlotId">
  @if (Model.IsDirectActive)
  {
    <a href="@Model.BannerDestUrl" rel="noopener" target="_blank">
      <img src="@Model.BannerImageUrl" alt="@(Model.BannerAltText ?? "Реклама")"
           class="img-fluid">
    </a>
  }
  else if (!string.IsNullOrEmpty(Model.AdsenseCode))
  {
    @Html.Raw(Model.AdsenseCode)
  }
  {{/* "Реклама" label emitted by CSS ::before — no extra markup needed */}}
</div>
```

### Layout Changes (`_Layout.cshtml`)

- `<head>`: render `@Html.Raw(siteSettings.AdsenseScriptTag)` when non-empty
- Above `</footer>`: `@await Component.InvokeAsync("AdSlot", new { slotId = "footer-banner" })`
- Bottom of `<body>`: `@await Component.InvokeAsync("AdSlot", new { slotId = "mobile-sticky" })`

### Page Template Changes

**`HomePage.cshtml`:**
- Replace static leaderboard div → `@await Component.InvokeAsync("AdSlot", new { slotId = "header-leaderboard" })`
- Replace static sidebar div → `sidebar-top` + `sidebar-bottom`

**`Article.cshtml`:**
- Replace static leaderboard div → `header-leaderboard`
- Replace static sidebar div → `sidebar-top` + `sidebar-bottom`
- Replace static article-bottom div → `article-bottom`
- Inject `article-inline` after 3rd paragraph via `HtmlHelper` extension
- `@Html.Raw(Model.Body)` → `@Html.RenderArticleBody(Model.Body, Model.IsSponsored, "article-inline")`

**`_SponsoredBanner.cshtml`:**
- Accept `ViewData["SponsorName"]` and display when non-empty

### Sponsored Link Rewriting

**`PredelNews.Web/Helpers/SponsoredLinkRewriter.cs`** — static `HtmlHelper` extension:

```csharp
public static IHtmlContent RewriteSponsoredLinks(
    this IHtmlHelper html, string body, bool isSponsored)
```

- Uses `HtmlAgilityPack` to parse `body`
- When `isSponsored == true`: for each `<a>` where `href` starts with `http` and does not contain `predelnews.com`, set `rel="sponsored noopener"`
- Returns `IHtmlContent` (safe for `@Html.RewriteSponsoredLinks(...)`)

A combined helper `RenderArticleBody` handles both inline ad injection (after 3rd `<p>`) and sponsored link rewriting in a single parse pass.

### CSS Additions (`site.css`)

```css
/* Sidebar slots hidden below 1024px */
@media (max-width: 1023px) {
  .pn-ad-sidebar { display: none; }
}

/* Mobile sticky slot */
.pn-ad-mobile-sticky {
  position: fixed; bottom: 0; left: 0; right: 0;
  height: 50px; z-index: 1000; background: #fff;
}
@media (min-width: 1024px) {
  .pn-ad-mobile-sticky { display: none; }
}

/* CLS prevention — min-height per slot */
.pn-ad-leaderboard { min-height: 90px; }
.pn-ad-article-inline { min-height: 90px; }
.pn-ad-article-bottom { min-height: 90px; }
.pn-ad-footer-banner { min-height: 90px; }
```

---

## Section 3: CMS Admin

**`PredelNews.BackofficeExtensions/Controllers/AdManagementApiController.cs`**

- Route: `[VersionedApiBackOfficeRoute("ads")]`
- Auth: `[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]` + admin-group check via `IUserService` for mutating endpoints
- Pattern: identical to `EngagementApiController`

### Endpoints

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/predelnews/ads` | Backoffice | List all 7 slots |
| `GET` | `/predelnews/ads/{slotId}` | Backoffice | Single slot |
| `PUT` | `/predelnews/ads/{slotId}` | Admin only | Update slot |
| `POST` | `/predelnews/ads/{slotId}/reset` | Admin only | Switch back to AdSense |

### Request Model

```csharp
public record UpdateAdSlotRequest(
    string Mode,
    string? AdsenseCode,
    string? BannerImageUrl,
    string? BannerDestUrl,
    string? BannerAltText,
    DateTime? StartDate,
    DateTime? EndDate
);
```

### Validation

- `Mode == "direct"` → `BannerImageUrl` and `BannerDestUrl` required
- If both `StartDate` and `EndDate` provided → `StartDate` must be before `EndDate`
- Returns `400 BadRequest` with `{ status, message }` on failure

---

## Section 4: Testing

**`PredelNews.Core.Tests/Services/AdSlotServiceTests.cs`** (xUnit + NSubstitute + FluentAssertions):

- `GetAllAsync_ReturnsCachedResult_OnSecondCall` — repository called once; second call hits cache
- `GetAllAsync_RefreshesCache_AfterExpiry` — repository called again after cache expiry
- `GetBySlotIdAsync_ReturnsCorrectSlot` — correct slot returned from cached list
- `UpdateSlotAsync_EvictsCache` — cache key evicted after update
- `AdSlot_IsDirectActive_ReturnsFalse_WhenEndDatePassed` — computed property reverts to AdSense

**`PredelNews.Core.Tests/Helpers/SponsoredLinkRewriterTests.cs`**:

- `RewriteSponsoredLinks_AddsRelAttribute_ToExternalLinks`
- `RewriteSponsoredLinks_SkipsInternalLinks` (predelnews.com hrefs untouched)
- `RewriteSponsoredLinks_DoesNothing_WhenNotSponsored`

---

## Files to Create

| File | Project |
|---|---|
| `Core/Models/AdSlot.cs` | Core |
| `Core/Interfaces/IAdSlotRepository.cs` | Core |
| `Core/Services/IAdSlotService.cs` | Core |
| `Core/Services/AdSlotService.cs` | Core |
| `Infrastructure/Repositories/AdSlotRepository.cs` | Infrastructure |
| `Infrastructure/Migrations/AlterAdSlotsV2Migration.cs` | Infrastructure |
| `Web/ViewComponents/AdSlotViewComponent.cs` | Web |
| `Web/Views/Shared/Components/AdSlot/_AdSlot.cshtml` | Web |
| `Web/Helpers/SponsoredLinkRewriter.cs` | Web |
| `Core.Tests/Services/AdSlotServiceTests.cs` | Tests |
| `Core.Tests/Helpers/SponsoredLinkRewriterTests.cs` | Tests |

## Files to Modify

| File | Change |
|---|---|
| `_Layout.cshtml` | AdSense script, footer banner, mobile sticky slots |
| `HomePage.cshtml` | Replace static ad divs with ViewComponent calls |
| `Article.cshtml` | Replace static ad divs; use `RenderArticleBody` helper |
| `_SponsoredBanner.cshtml` | Add sponsor name from `ViewData` |
| `site.css` | Responsive sidebar hiding, mobile sticky, CLS min-heights |
| `Core/Extensions/ServiceCollectionExtensions.cs` | Register `IAdSlotService` → `AdSlotService` |
| `Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Register `IAdSlotRepository` → `AdSlotRepository` |
| `Infrastructure/Migrations/PredelNewsMigrationPlan.cs` | Add V2 migration step |
