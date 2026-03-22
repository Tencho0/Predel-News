## Task: EPIC-08 Session 3 — Final Polish & Comprehensive Verification

This is the final session for EPIC-08. Your job is to review everything implemented in Sessions 1-2, fix any issues, and run comprehensive verification.

Read CLAUDE.md and docs/planning/epics/EPIC-08-seo-and-social-sharing.md.

### 1. Full Build & Test

Run:
```bash
dotnet build PredelNews.slnx
dotnet test PredelNews.slnx
```
Fix any compilation errors or test failures.

### 2. Verify Sitemap Controller

Read `src/Web/PredelNews.Web/Controllers/SitemapController.cs` and verify:
- [ ] Route is `[Route("sitemap.xml")]`
- [ ] Returns valid XML with `<?xml version="1.0" encoding="UTF-8"?>` declaration
- [ ] Has `<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">`
- [ ] Iterates ALL published document types: article, category, region, newsTag, author, homePage, staticPage, contactPage, allNewsPage
- [ ] Each `<url>` has `<loc>` with absolute HTTPS URL and `<lastmod>` in yyyy-MM-dd
- [ ] Has output cache with "sitemap" tag
- [ ] Does NOT include unpublished content or /umbraco/ paths

### 3. Verify Sitemap Cache Invalidation

Read the notification handler and verify:
- [ ] Implements `INotificationAsyncHandler<ContentPublishedNotification>`
- [ ] Evicts "sitemap" cache tag on content publish
- [ ] Is registered (either via composer or AddNotificationAsyncHandler)

### 4. Verify robots.txt

Read `src/Web/PredelNews.Web/wwwroot/robots.txt`:
- [ ] Has `User-agent: *`
- [ ] Has `Disallow: /umbraco/`
- [ ] Has `Sitemap: https://predelnews.bg/sitemap.xml`

### 5. Verify ALL Controllers Set Full SEO ViewBag

Read EVERY public controller and verify each sets ALL of these:
- `ViewBag.Title`
- `ViewBag.SeoTitle`
- `ViewBag.SeoDescription`
- `ViewBag.CanonicalUrl`
- `ViewBag.OgImageUrl` (with fallback to SiteSettingsService.DefaultOgImageUrl)
- `ViewBag.Breadcrumbs` (List<BreadcrumbItem>)

Controllers to check:
1. HomePageController
2. ArticleController
3. CategoryController
4. RegionController
5. NewsTagController
6. AuthorController
7. AllNewsPageController
8. StaticPageController
9. ContactPageController
10. SearchPageController

For ArticleController additionally verify:
- `ViewBag.PublishedTime` and `ViewBag.ModifiedTime` are set

### 6. Verify _SeoMeta.cshtml

Read and verify:
- [ ] `<meta name="description">` rendered when seoDescription is set
- [ ] `og:title`, `og:description`, `og:image` with fallbacks
- [ ] `og:type` = "article" when PublishedTime exists, "website" otherwise
- [ ] `og:url` = canonical URL
- [ ] `og:site_name` = "PredelNews"
- [ ] `og:locale` = "bg_BG"
- [ ] `twitter:card` = "summary_large_image" when image exists, "summary" otherwise
- [ ] `twitter:title`, `twitter:description`, `twitter:image`
- [ ] `<link rel="canonical">` present
- [ ] `article:published_time` and `article:modified_time` when applicable

### 7. Verify _JsonLd.cshtml (NewsArticle)

Read and verify:
- [ ] Valid JSON-LD structure for NewsArticle
- [ ] Fields: headline, datePublished, dateModified, author, publisher, image, description, url, isAccessibleForFree, sponsor (conditional)
- [ ] Publisher is "PredelNews" with NewsMediaOrganization type
- [ ] Proper JavaScript encoding for strings

### 8. Verify _BreadcrumbJsonLd.cshtml

Read and verify:
- [ ] Valid JSON-LD BreadcrumbList schema
- [ ] Reads from ViewBag.Breadcrumbs
- [ ] Each item has position, name, item (absolute URL)
- [ ] Only renders when breadcrumbs exist
- [ ] Included in _Layout.cshtml

### 9. Verify _GoogleAnalytics.cshtml

Read and verify:
- [ ] Conditionally renders based on non-empty measurement ID
- [ ] Uses async script loading
- [ ] Standard gtag.js pattern
- [ ] Included in _Layout.cshtml `<head>`

### 10. Verify _CookieConsent.cshtml

Read and verify:
- [ ] Fixed bottom banner, non-intrusive
- [ ] Bulgarian text with "Приемам" button
- [ ] Sets cookie `pn_cookie_consent` on accept
- [ ] Checks cookie on load, hides if already accepted
- [ ] Included in _Layout.cshtml before `</body>`

### 11. Verify site.webmanifest

Read and verify:
- [ ] Valid JSON with name, short_name, icons, theme_color
- [ ] Linked from _Layout.cshtml `<head>`

### 12. Verify _Layout.cshtml Integration

Read _Layout.cshtml and verify the complete `<head>` includes:
- [ ] `<title>` with ViewBag.Title
- [ ] `_SeoMeta` partial
- [ ] `_BreadcrumbJsonLd` partial
- [ ] `_GoogleAnalytics` partial
- [ ] Favicon links (ico, svg, apple-touch-icon)
- [ ] Manifest link
- [ ] Theme-color meta

And before `</body>`:
- [ ] `_CookieConsent` partial

### 13. SEO Title Format Verification

Grep across all controllers and verify these exact title patterns:
- Article: `{Headline}` (layout appends " | PredelNews")
- Category: `{CategoryName} — Новини`
- Region: `{RegionName} — Новини`
- Tag: `{TagName}`
- Author: `Статии от {AuthorName}`
- Homepage: custom or `Начало`
- AllNews: `Всички новини`
- Contact: `Контакти`
- Search: `Търсене: {query}` or `Търсене`

### 14. Final Build & Test

Run again:
```bash
dotnet build PredelNews.slnx
dotnet test PredelNews.slnx
```

Report a final summary of:
- Total files created
- Total files modified
- Any issues found and fixed
- Any items that could NOT be completed and why
