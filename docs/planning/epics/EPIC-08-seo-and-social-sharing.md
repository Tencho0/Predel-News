FILE: docs/planning/epics/EPIC-08-seo-and-social-sharing.md

# EPIC-08 — SEO & Social Sharing

## Goal / Outcome

Implement all SEO foundations (XML sitemap, structured data, canonical URLs, robots.txt, meta tags) and social sharing metadata (OG tags, Twitter Cards) so that PredelNews content is discoverable in search engines and produces rich, professional previews when shared on social platforms.

## In Scope (MVP)

- XML sitemap at `/sitemap.xml` (auto-updated within 60 minutes of publish)
- `robots.txt` (allow public content, disallow `/umbraco/`, reference sitemap)
- Canonical URLs on all pages
- JSON-LD structured data: `NewsArticle` on article pages, `BreadcrumbList` on all pages
- OG meta tags (title, description, image, type, URL) on all pages
- Twitter Card meta tags on all pages
- SEO meta title/description templates per page type (with fallback logic)
- Favicon and web app manifest
- Google Analytics 4 integration (configurable tracking ID in CMS Site Settings)
- Cookie consent banner (basic Bulgarian-language banner, non-intrusive)

## Out of Scope (MVP)

- Full GDPR consent management / CMP (Phase 2)
- RSS feeds (Phase 2)
- AMP pages (not planned)
- Google News sitemap (post-launch consideration)

## Dependencies

- EPIC-02 (Content Model) — SeoComposition, SiteSettings with analytics ID
- EPIC-03 (Public Site Core) — all page templates where SEO tags are rendered
- EPIC-04 (Editorial Workflow) — publish/unpublish triggers sitemap updates

## High-Level Acceptance Criteria

- [ ] `/sitemap.xml` is valid, includes all published content, and updates within 60 minutes of new publish
- [ ] Google Rich Results Test passes with zero errors for a sample article page
- [ ] Facebook Sharing Debugger shows correct OG title, description, and image
- [ ] Canonical URLs are present and correct on all public pages
- [ ] `robots.txt` disallows `/umbraco/` and references the sitemap
- [ ] GA4 tracking fires on every public page when the tracking ID is configured

---

## User Stories

### US-08.01 — XML Sitemap Generation

**As a** search engine bot, **I want** an automatically generated XML sitemap at `/sitemap.xml`, **so that** I can discover and crawl all published content efficiently.

**Acceptance Criteria:**
- Sitemap includes all published articles, category archives, region archives, tag archives, author pages, and static pages
- New or updated content appears in the sitemap within 60 minutes of publication
- Unpublished, draft, or scheduled (not-yet-live) content is excluded
- Sitemap is valid per the Sitemaps.org protocol (schema validation)
- `<lastmod>` dates accurately reflect the most recent modification of each URL
- The sitemap URL is referenced in `robots.txt`

---

### US-08.02 — robots.txt Configuration

**As a** developer, **I want** a properly configured `robots.txt`, **so that** search engines crawl public content and avoid admin paths.

**Acceptance Criteria:**
- `robots.txt` is accessible at `/robots.txt`
- Public content paths are allowed for all user agents
- `/umbraco/` and any preview/admin paths are disallowed
- The file includes a `Sitemap:` directive pointing to `https://predelnews.com/sitemap.xml`

---

### US-08.03 — Canonical URLs

**As a** developer, **I want** every public page to include a correct `<link rel="canonical">` tag, **so that** search engines consolidate ranking signals to the preferred URL.

**Acceptance Criteria:**
- Every public page has a canonical URL pointing to its preferred version (HTTPS, consistent trailing slash convention)
- Paginated pages have self-referencing canonicals (page 2's canonical points to page 2)
- Canonical URLs do not point to non-existent or redirected pages
- The canonical tag is rendered in the `<head>` via the `_SeoMeta.cshtml` partial

---

### US-08.04 — JSON-LD Structured Data (NewsArticle)

**As a** search engine bot, **I want** JSON-LD `NewsArticle` structured data on article pages, **so that** the article is eligible for rich results in Google Search.

**Acceptance Criteria:**
- Article pages include a `<script type="application/ld+json">` block with `NewsArticle` schema
- The structured data includes: headline, datePublished, dateModified (if updated), author (name), publisher (name, logo), image, description, mainEntityOfPage
- Google Rich Results Test returns zero errors for the structured data
- The JSON-LD is rendered server-side (not injected by JavaScript)

---

### US-08.05 — JSON-LD Structured Data (BreadcrumbList)

**As a** search engine bot, **I want** JSON-LD `BreadcrumbList` structured data on all pages with breadcrumbs, **so that** breadcrumb trails appear in search results.

**Acceptance Criteria:**
- All pages with a breadcrumb component also include `BreadcrumbList` JSON-LD
- Each breadcrumb item includes: name, item (URL), and position
- The structured data matches the visible breadcrumb rendered on the page
- Google Rich Results Test validates the BreadcrumbList without errors

---

### US-08.06 — OG & Twitter Card Meta Tags

**As a** visitor, **I want** articles shared on Facebook, Viber, and Twitter to display a rich preview with title, description, and image, **so that** shared links look professional and attract clicks.

**Acceptance Criteria:**
- All public pages include Open Graph tags: `og:title`, `og:description`, `og:image`, `og:type`, `og:url`
- Article pages set `og:type` to `article` with `article:published_time`, `article:section` (category)
- Twitter Card meta tags are present: `twitter:card` (summary_large_image for articles), `twitter:title`, `twitter:description`, `twitter:image`
- OG image falls back: OG Image field → Cover Image → site default
- OG description falls back: SEO Description → Subtitle → first 160 chars of body
- Facebook Sharing Debugger and Twitter Card Validator confirm correct rendering

---

### US-08.07 — SEO Meta Title & Description Templates

**As a** developer, **I want** templated SEO meta titles and descriptions for each page type with fallback logic, **so that** every page has meaningful metadata for search engines.

**Acceptance Criteria:**
- Article: title = `{Headline} | {SiteName}`, description = SEO Description → Subtitle → first 160 chars of body
- Category archive: title = `{CategoryName} — Новини | {SiteName}`
- Region archive: title = `{RegionName} — Новини | {SiteName}`
- Tag archive: title = `{TagName} | {SiteName}`
- Author archive: title = `Статии от {AuthorName} | {SiteName}`
- Homepage: title = SEO Title (from CMS) or `{SiteName} — Новини от Югозападна България`
- Custom SEO title/description fields override the template when provided

---

### US-08.08 — Favicon & Web App Manifest

**As a** visitor, **I want** the site to have a favicon and web app manifest, **so that** the site looks professional in browser tabs, bookmarks, and mobile home screens.

**Acceptance Criteria:**
- Favicon renders in browser tabs (16x16, 32x32, and 180x180 for Apple Touch)
- A `manifest.json` / `site.webmanifest` file exists with site name, theme color, and icons
- The manifest is linked from the `<head>` of all pages

---

### US-08.09 — Google Analytics 4 Integration

**As an** admin, **I want** to configure a GA4 tracking ID in CMS Site Settings so that traffic analytics are collected on every public page, **so that** I can measure site performance and audience behavior.

**Acceptance Criteria:**
- The GA4/GTM Measurement ID is configurable in CMS Site Settings (`analyticsTrackingId`)
- When the ID is present, the GA4 script loads on all public pages (async, non-blocking)
- When the ID is empty, no analytics script loads (no errors)
- The script does not block the main thread for > 100 ms
- Analytics tracking works on all page types (homepage, article, archive, static, search)

---

### US-08.10 — Cookie Consent Banner

**As a** visitor, **I want** to see a non-intrusive cookie consent banner on my first visit, **so that** I'm informed about cookie usage.

**Acceptance Criteria:**
- A banner appears at the bottom of the page on first visit with a Bulgarian message (e.g., "Този сайт използва бисквитки. Продължавайки да разглеждате, вие се съгласявате с употребата им.")
- An "Приемам" (Accept) button dismisses the banner
- A cookie is set to remember the dismissal; the banner does not reappear on subsequent visits
- The banner does not overlay the main content area (positioned at bottom, non-blocking)
- The banner does not qualify as an intrusive interstitial (per Google guidelines)

**Notes:** This is a basic notice banner for MVP. Full consent management with granular opt-in/opt-out is Phase 2.
