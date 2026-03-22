## Task: EPIC-08 Session 1 — Core SEO Infrastructure

You are implementing EPIC-08 (SEO & Social Sharing) for PredelNews. Read CLAUDE.md and docs/planning/epics/EPIC-08-seo-and-social-sharing.md before starting.

### 1. XML Sitemap Controller (US-08.01)

Create `src/Web/PredelNews.Web/Controllers/SitemapController.cs`:
- Standard ASP.NET Core controller (NOT an Umbraco RenderController)
- Route: `[Route("sitemap.xml")]` returning `Content(..., "application/xml")`
- Query ALL published content via UmbracoHelper.ContentAtRoot(): articles, categories, regions, tags, authors, static pages, homepage, contact page, allNews page
- Generate valid Sitemaps.org XML with `<urlset>`, `<url>`, `<loc>`, `<lastmod>`
- `<loc>` must be absolute URLs (https://{Request.Host}{content.Url()})
- `<lastmod>` = content.UpdateDate in W3C datetime format (yyyy-MM-dd)
- Use 1-hour output cache: `[OutputCache(PolicyName = "PublicPage", Tags = ["sitemap"])]`
- Exclude any content under /umbraco/

Create a notification handler `src/Web/PredelNews.Web/NotificationHandlers/SitemapCacheInvalidator.cs`:
- Implement `INotificationAsyncHandler<ContentPublishedNotification>`
- Inject `IOutputCacheStore` and call `EvictByTagAsync("sitemap")` on publish
- Register in a composer or via builder.Services

### 2. robots.txt (US-08.02)

Create a static file `src/Web/PredelNews.Web/wwwroot/robots.txt`:
```
User-agent: *
Allow: /
Disallow: /umbraco/

Sitemap: https://predelnews.bg/sitemap.xml
```

### 3. Canonical URL on ALL Pages (US-08.03)

Update ALL public-facing controllers to set `ViewBag.CanonicalUrl`. The canonical URL should be: `$"{Request.Scheme}://{Request.Host}{CurrentPage!.Url()}"`.

Controllers to update (they currently only set Title, SeoTitle, SeoDescription):
- HomePageController — add CanonicalUrl
- CategoryController — add CanonicalUrl
- RegionController — add CanonicalUrl
- NewsTagController — add CanonicalUrl
- AuthorController — add CanonicalUrl
- AllNewsPageController — add CanonicalUrl
- StaticPageController — add CanonicalUrl
- ContactPageController — add CanonicalUrl
- SearchPageController — add CanonicalUrl (self-referencing with query params for pagination)
- ArticleController — already has it ✓

### 4. OG Image Fallback Chain on ALL Pages

Update ALL controllers to set `ViewBag.OgImageUrl` with this fallback chain:
1. Content's `ogImage` property (PropertyAliases.OgImage) → GetCropUrl(width:1200)
2. For articles: CoverImage (already done) ✓
3. For archive pages with no ogImage: use SiteSettingsService.DefaultOgImageUrl

Controllers to update:
- HomePageController
- CategoryController
- RegionController
- NewsTagController
- AuthorController (can use author photo as secondary fallback)
- AllNewsPageController
- StaticPageController
- ContactPageController
- SearchPageController (use default OG image)

Each of these controllers needs `SiteSettingsService` injected. Check if it's already injected; if not, add it to the constructor.

### 5. Complete _SeoMeta.cshtml (US-08.06)

Update `src/Web/PredelNews.Web/Views/Shared/_SeoMeta.cshtml`:
- Add `og:type` logic: render `"article"` when ViewBag.PublishedTime is set, otherwise `"website"` (currently hardcoded to "website")
- Ensure og:locale, og:site_name are present (already there ✓)

### 6. SEO Title Templates (US-08.07)

Verify and fix the SEO title pattern per page type. The `<title>` tag in _Layout.cshtml is `@ViewBag.Title | PredelNews`. Each controller should set ViewBag.Title correctly:
- Article: `{Headline}` (ViewBag.Title, appended by layout) ✓
- Category: `{CategoryName} — Новини`
- Region: `{RegionName} — Новини`
- Tag: `{TagName}`
- Author: `Статии от {AuthorName}`
- Homepage: custom SeoTitle or `Начало`
- AllNews: `Всички новини`
- StaticPage: page title
- ContactPage: `Контакти`
- SearchPage: `Търсене: {query}`

Read each controller to check what they currently set and fix if different.

### Verification Checklist (run after implementation)

1. `dotnet build PredelNews.slnx` — must compile with zero errors
2. `dotnet test PredelNews.slnx` — all existing tests pass
3. Grep for `ViewBag.CanonicalUrl` — should appear in ALL 10 controllers
4. Grep for `ViewBag.OgImageUrl` — should appear in ALL 10 controllers
5. Verify robots.txt exists at wwwroot/robots.txt with correct content
6. Verify SitemapController exists and has [OutputCache] + [Route("sitemap.xml")]
7. Verify SitemapCacheInvalidator implements INotificationAsyncHandler<ContentPublishedNotification>
8. Verify _SeoMeta.cshtml has og:type switching between "article" and "website"
