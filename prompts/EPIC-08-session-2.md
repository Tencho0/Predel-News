## Task: EPIC-08 Session 2 — Structured Data, GA4, Cookie Banner, Manifest

Continue EPIC-08 implementation. Read CLAUDE.md and docs/planning/epics/EPIC-08-seo-and-social-sharing.md for full context.

### 1. BreadcrumbList JSON-LD (US-08.05)

Create `src/Web/PredelNews.Web/Views/Shared/_BreadcrumbJsonLd.cshtml`:
- Reads breadcrumb data from ViewBag.Breadcrumbs (List<BreadcrumbItem>)
- Renders a `<script type="application/ld+json">` block with BreadcrumbList schema
- Format:
  ```json
  {
    "@context": "https://schema.org",
    "@type": "BreadcrumbList",
    "itemListElement": [
      { "@type": "ListItem", "position": 1, "name": "Начало", "item": "https://predelnews.bg/" },
      { "@type": "ListItem", "position": 2, "name": "{name}", "item": "{absoluteUrl}" }
    ]
  }
  ```
- Use JavaScriptEncoder for safe JSON output (same pattern as _JsonLd.cshtml)
- Only render if ViewBag.Breadcrumbs is not null/empty

Update _Layout.cshtml `<head>` section to include `@await Html.PartialAsync("_BreadcrumbJsonLd")` right after `@await Html.PartialAsync("_SeoMeta")`.

Ensure ALL controllers that have breadcrumb data set `ViewBag.Breadcrumbs`. Check which controllers already populate breadcrumbs in their ViewModel and add `ViewBag.Breadcrumbs = model.Breadcrumbs;` where missing. Controllers to check:
- ArticleController (has model.Breadcrumbs ✓, needs ViewBag assignment)
- CategoryController
- RegionController
- NewsTagController
- AuthorController
- AllNewsPageController
- StaticPageController
- ContactPageController

Each should have breadcrumbs like:
- Category: Начало → {CategoryName}
- Region: Начало → {RegionName}
- Tag: Начало → {TagName}
- Author: Начало → Статии от {AuthorName}
- AllNews: Начало → Всички новини
- Static: Начало → {PageTitle}
- Contact: Начало → Контакти

### 2. Extend _JsonLd.cshtml for Article Pages (US-08.04)

Update existing `_JsonLd.cshtml` to also include:
- `"description"` field (from model.SeoDescription ?? model.Subtitle)
- `"url"` field (from model.CanonicalUrl ?? model.ShareUrl)
- `"isAccessibleForFree": true`
- `"isPartOf"` with WebSite schema

Verify the existing NewsArticle JSON-LD still renders correctly on article pages.

### 3. GA4 Integration (US-08.09)

Create `src/Web/PredelNews.Web/Views/Shared/_GoogleAnalytics.cshtml`:
- Inject SiteSettingsService (or read from ViewBag.AnalyticsTrackingId)
- If the GA4 Measurement ID is not empty, render the standard GA4 gtag.js snippet:
  ```html
  <script async src="https://www.googletagmanager.com/gtag/js?id={measurementId}"></script>
  <script>
    window.dataLayer = window.dataLayer || [];
    function gtag(){dataLayer.push(arguments);}
    gtag('js', new Date());
    gtag('config', '{measurementId}');
  </script>
  ```
- If measurement ID is empty/null, render nothing (no script, no errors)
- Script must load async (non-blocking)

Update _Layout.cshtml to include this partial in `<head>` after the other meta tags.

Ensure SiteSettingsService exposes the GA4 measurement ID. Check PropertyAliases for `analyticsTrackingId` or `AnalyticsTrackingId` — add if missing. The SiteSettings document type should already have this property from EPIC-02.

### 4. Cookie Consent Banner (US-08.10)

Create `src/Web/PredelNews.Web/Views/Shared/_CookieConsent.cshtml`:
- Bottom-of-page fixed banner (not a modal)
- Bulgarian text: "Този сайт използва бисквитки за подобряване на потребителското изживяване. Продължавайки да разглеждате, вие се съгласявате с употребата им."
- "Приемам" (Accept) button
- Inline CSS for the banner (or add to site.css):
  - position: fixed; bottom: 0; width: 100%; z-index: 9999
  - Dark background (matches site footer style), white text
  - Dismiss animation
- JavaScript (inline or in a small JS file):
  - On "Приемам" click: set cookie `pn_cookie_consent=1` with 365-day expiry, hide banner
  - On page load: if cookie exists, don't show the banner
  - Use vanilla JS, no dependencies

Update _Layout.cshtml to include this partial just before `</body>`.

### 5. Web App Manifest & Favicon Links (US-08.08)

Create `src/Web/PredelNews.Web/wwwroot/site.webmanifest`:
```json
{
  "name": "PredelNews",
  "short_name": "PredelNews",
  "description": "Регионални новини от Югозападна България",
  "start_url": "/",
  "display": "browser",
  "background_color": "#ffffff",
  "theme_color": "#1a1a2e",
  "icons": [
    { "src": "/images/favicon.svg", "sizes": "any", "type": "image/svg+xml" },
    { "src": "/favicon.ico", "sizes": "16x16 32x32", "type": "image/x-icon" }
  ]
}
```

Update _Layout.cshtml `<head>` to add:
```html
<link rel="icon" href="~/favicon.ico" sizes="any">
<link rel="icon" href="~/images/favicon.svg" type="image/svg+xml">
<link rel="apple-touch-icon" href="~/images/apple-touch-icon.png">
<link rel="manifest" href="~/site.webmanifest">
<meta name="theme-color" content="#1a1a2e">
```
(Replace the existing single `<link rel="icon" href="~/favicon.ico">` line)

### Verification Checklist

1. `dotnet build PredelNews.slnx` — zero errors
2. `dotnet test PredelNews.slnx` — all tests pass
3. Verify _BreadcrumbJsonLd.cshtml exists and is included in _Layout.cshtml
4. Grep for `ViewBag.Breadcrumbs` — should be set in all archive controllers
5. Verify _GoogleAnalytics.cshtml renders conditionally (check for null/empty ID guard)
6. Verify _CookieConsent.cshtml has cookie check logic and "Приемам" button
7. Verify site.webmanifest exists in wwwroot/
8. Verify _Layout.cshtml head has: manifest link, theme-color meta, apple-touch-icon
9. Verify _JsonLd.cshtml includes description, url, isAccessibleForFree fields
10. Open _Layout.cshtml and verify the order: _SeoMeta → _BreadcrumbJsonLd → _GoogleAnalytics → (content) → _CookieConsent → scripts
