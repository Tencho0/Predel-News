# Technical Specification — PredelNews MVP (Phase 1)

**Product:** PredelNews — Regional News Website for Southwest Bulgaria
**Domain:** predelnews.com
**Platform:** Umbraco 17 LTS (.NET 10) on Windows VPS (IIS)
**Document owner:** Solutions Architect / Tech Lead
**Status:** Draft v1.0
**Last updated:** 2026-02-24

> **Repository path:** `docs/technical/technical-specification.md`
> Parent documents: `docs/business/prd.md` · `docs/business/functional-specification.md` · `docs/business/non-functional-requirements.md` · `docs/technical/architecture.md`
> Related: `docs/technical/database-schema.md` · `docs/technical/deployment.md` · `docs/technical/observability.md`

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Technology Stack Overview](#2-technology-stack-overview)
3. [Solution Architecture (Concrete View)](#3-solution-architecture-concrete-view)
4. [Content Models & Umbraco Document Types](#4-content-models--umbraco-document-types)
5. [Custom Modules — Implementation Details](#5-custom-modules--implementation-details)
   - [5.1 Comments Module](#51-comments-module)
   - [5.2 Poll Module](#52-poll-module)
   - [5.3 Ads & Sponsored Content Module](#53-ads--sponsored-content-module)
   - [5.4 Email Signup Module](#54-email-signup-module)
   - [5.5 Contact Form Module](#55-contact-form-module)
6. [Search Implementation](#6-search-implementation)
7. [Routing, URLs, and SEO Plumbing](#7-routing-urls-and-seo-plumbing)
8. [Security & Compliance Implementation](#8-security--compliance-implementation)
9. [Performance & Caching Details](#9-performance--caching-details)
10. [Environment & Deployment Details](#10-environment--deployment-details)
11. [Logging, Monitoring & Observability Hooks](#11-logging-monitoring--observability-hooks)
12. [Risks, Constraints & Technical Decisions](#12-risks-constraints--technical-decisions)
13. [Open Questions / TBDs](#13-open-questions--tbds)

---

## 1. Introduction

### 1.1 Purpose

This document translates the PredelNews Product Requirements Document (PRD), Functional Specification (FS), Non-Functional Requirements (NFRs), and Architecture into **concrete implementation instructions** for the MVP (Phase 1). It defines *how* the system is built — data models, service interfaces, API contracts, configuration parameters, and deployment details.

The intended readers are:

- **Backend and frontend developers** implementing the features
- **DevOps** provisioning and configuring the Windows VPS
- **QA engineers** writing test cases and verifying acceptance criteria
- **Product Manager / Editor-in-Chief** reviewing technical decisions that affect editorial workflow or content policy

This document does not duplicate functional requirements; instead it references them by ID (e.g., `FR-CM-001`) and specifies the implementation approach.

### 1.2 Relationship to Other Documents

| Document | Role relative to this spec |
|---|---|
| `docs/business/prd.md` | Source of truth for goals, scope, personas, and success metrics |
| `docs/business/functional-specification.md` | Defines *what* the system must do; this spec defines *how* |
| `docs/business/non-functional-requirements.md` | Performance, security, reliability, privacy constraints |
| `docs/technical/architecture.md` | Logical architecture and container diagram; this spec refines it into code-level decisions |
| `docs/technical/database-schema.md` | To be authored; will contain the full DDL extracted from §5 of this document |
| `docs/technical/deployment.md` | To be authored; will elaborate §10 of this document |
| `docs/technical/observability.md` | To be authored; will elaborate §11 of this document |

### 1.3 In-Scope vs. Out-of-Scope (MVP)

**In scope:** All Phase 1 features as listed in PRD §8.1 (F1–F13) and FS sections 3–12.

**Out of scope — not implemented in MVP** (design hooks noted where relevant):

| Feature | Phase | Notes |
|---|---|---|
| Newsletter sending / campaign management | 2 | `pn_email_subscribers` table ready for Phase 2 ESP integration |
| Comment threading / replies | 2 | `parent_comment_id` column reserved (NULL) in `pn_comments` schema |
| reCAPTCHA | 2 | `IAntiSpamPipeline` interface allows plugging in new steps |
| MFA for CMS | 2 | Umbraco 2FA can be enabled per-user group without code changes |
| CDN / WAF (Cloudflare) | 2 | Static asset cache headers are CDN-compatible; no code changes needed |
| Full GDPR consent management | 2 | Cookie banner in place; GA4 script loading hook exists for gating |
| RSS feeds | 2 | Taxonomy and pagination infrastructure already in place |
| Push notifications | 2+ | No service worker infrastructure at MVP |
| Reader accounts / paywall | 3+ | Umbraco Members subsystem is available but unused |
| Dark mode | Never | `color-scheme: light only` enforced in CSS |

---

## 2. Technology Stack Overview

### 2.1 Core Platform

| Component | Technology | Version / Notes |
|---|---|---|
| CMS | Umbraco | 17 LTS |
| Runtime | .NET | 10 (LTS) |
| Web framework | ASP.NET Core | 10 (included with .NET 10) |
| Web server | IIS | Windows IIS with ASP.NET Core hosting bundle |
| Database | SQL Server Express | Latest stable (2022 recommended) |
| ORM / Data access | Umbraco's built-in ORM + parameterized ADO.NET for custom tables | Dapper for lightweight custom queries |
| Search | Umbraco Examine | Lucene-based, in-process |
| Image processing | Umbraco ImageSharp | Built into Umbraco 13+ |
| SSL | Let's Encrypt (via win-acme) | Auto-renewed |
| Logging | Serilog | File sink, daily rotation |
| DI container | Microsoft.Extensions.DependencyInjection | Built-in ASP.NET Core |

### 2.2 Frontend Stack

| Concern | Approach |
|---|---|
| Rendering | Server-side Razor templates (`.cshtml`). No SPA framework. |
| CSS | Custom CSS with BEM methodology. No CSS framework dependency (or a lightweight utility-first framework such as a minimal custom build). `color-scheme: light only` enforced globally. |
| JavaScript | Progressive enhancement only. Vanilla JS for: hamburger nav, share button interactions, poll voting AJAX, comment form AJAX, cookie banner. Target: ≤ 50 KB total JS (compressed). No bundler at MVP; cache-busted via query string version (`?v=...`). |
| HTML structure | Semantic HTML5 (`<header>`, `<nav>`, `<main>`, `<article>`, `<aside>`, `<footer>`, `<section>`). One `<main>` per page. Skip-to-content link on every page. |
| Templating | Umbraco Razor views + view components for reusable UI blocks. |

### 2.3 External Services

| Service | Purpose | Configuration |
|---|---|---|
| Google AdSense | Display advertising | JS snippet configured per-slot in CMS Site Settings |
| Google Analytics 4 (or GTM) | Traffic analytics | Measurement ID configurable in CMS Site Settings |
| SMTP / Transactional Email | Contact form delivery, editorial notifications | Configurable via environment variables |
| Let's Encrypt (win-acme) | TLS certificate | Auto-renewed on VPS |
| YouTube | Video embeds only | Responsive `<iframe>` in article body; no YouTube API |
| Facebook / Viber | Share button targets only | Plain URL-based share links; no SDK |

ASSUMPTION: The transactional email provider will be one of: SendGrid (free tier), Mailgun, or the hosting provider's SMTP relay. The choice does not affect application code — only the SMTP host/port/credentials in configuration.

---

## 3. Solution Architecture (Concrete View)

### 3.1 Application Layers

The application is a **single ASP.NET Core project** (Umbraco web application) organized into logical layers. No microservices or separate projects at MVP.

```
PredelNews.Web/
├── Controllers/
│   ├── Api/                         # JSON API endpoints (comments, polls, email, contact)
│   │   ├── CommentsApiController.cs
│   │   ├── PollApiController.cs
│   │   ├── EmailSignupApiController.cs
│   │   └── ContactApiController.cs
│   └── Surface/
│       └── SearchSurfaceController.cs
├── Models/
│   ├── Content/                     # Typed Umbraco content models (generated or hand-coded)
│   ├── ViewModels/                  # View-specific models
│   └── Dto/                         # API request/response DTOs
├── Services/
│   ├── Comments/
│   ├── Polls/
│   ├── AdSlots/
│   ├── EmailSignup/
│   ├── ContactForm/
│   ├── RateLimit/
│   ├── Audit/
│   └── Seo/
├── Data/
│   ├── Migrations/                  # Umbraco IMigrationPlan implementations
│   └── Repositories/                # Custom data access (Dapper over ADO.NET)
├── Notifications/                   # Umbraco notification handlers (ContentPublished, etc.)
├── Infrastructure/
│   ├── Middleware/                  # SecurityHeadersMiddleware, etc.
│   └── Extensions/                  # IServiceCollection extension methods
├── Backoffice/
│   ├── Dashboards/                  # Custom Umbraco backoffice dashboards
│   └── Sections/                    # Custom backoffice sections
├── Views/
│   ├── Homepage.cshtml
│   ├── Article.cshtml
│   ├── Category.cshtml
│   ├── Region.cshtml
│   ├── Tag.cshtml
│   ├── Author.cshtml
│   ├── AllNews.cshtml
│   ├── StaticPage.cshtml
│   ├── ContactPage.cshtml
│   ├── Search.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       ├── _ArticleCard.cshtml      # Reusable card component
│       ├── _AdSlot.cshtml           # Ad slot partial
│       ├── _CommentForm.cshtml
│       ├── _CommentList.cshtml
│       ├── _PollWidget.cshtml
│       ├── _SponsoredBanner.cshtml  # "Платена публикация" banner
│       ├── _Pagination.cshtml
│       ├── _SeoMeta.cshtml          # OG/Twitter/canonical head tags
│       ├── _JsonLd.cshtml           # JSON-LD structured data
│       ├── _Breadcrumb.cshtml
│       └── _CookieBanner.cshtml
├── wwwroot/
│   ├── css/
│   │   └── site.css
│   ├── js/
│   │   ├── nav.js                   # Hamburger nav
│   │   ├── comments.js              # Comment form AJAX
│   │   ├── poll.js                  # Poll voting AJAX
│   │   ├── share.js                 # Share button interactions
│   │   └── cookie-banner.js
│   └── images/                      # Static assets (favicon, logo placeholder)
└── Program.cs                       # Startup / service registration
```

### 3.2 How Umbraco Is Used

| Umbraco Feature | Usage |
|---|---|
| **Content tree** | Stores all publishable content: articles, categories, regions, tags, authors, static pages, homepage, site settings |
| **Document types** | Define the schema for each content type (see §4) |
| **Media library** | Stores uploaded images and PDFs |
| **IPublishedContentCache** | In-memory content cache; eliminates per-request SQL reads for published content |
| **ContentPublishedNotification** | Triggers output cache invalidation and Examine re-index on publish |
| **IMigrationPlan / PackageMigration** | Creates and migrates all custom `pn_*` tables on app startup |
| **BackOffice UI** | Extended with custom dashboards and sections for comments, polls, ads, email subscribers, audit logs |
| **Umbraco Users / User Groups** | Implements the Writer / Editor / Admin role model |
| **Examine (Lucene)** | Full-text search index for published articles |
| **ImageSharp** | On-demand WebP image resizing and `srcset` generation |
| **Umbraco Scheduler** | Fires scheduled article publishing (polls at 1-min intervals) |

### 3.3 Custom Module Integration

All custom modules (comments, polls, ads, email signup, contact form) follow this pattern:

1. **SQL tables** (prefixed `pn_`) created via `IMigrationPlan`.
2. **Repository** class handles data access via Dapper (parameterized queries only).
3. **Service** class contains business logic; registered in DI as scoped/singleton as appropriate.
4. **API Controller** (or Surface Controller) receives HTTP requests, applies CSRF validation, calls the service, returns results.
5. **Razor partials** render the relevant UI, consuming view models populated from services.
6. **Backoffice section/dashboard** allows editorial management.

---

## 4. Content Models & Umbraco Document Types

### 4.1 Compositions (Shared Property Groups)

These compositions are re-used across multiple document types to keep property definitions DRY.

#### `SeoComposition`

| Alias | Label (BG) | Umbraco Data Type | Required |
|---|---|---|---|
| `seoTitle` | SEO заглавие | Textstring | No |
| `seoDescription` | SEO описание | Textarea (max 320) | No |
| `ogImage` | OG изображение | Media Picker | No |

#### `PageMetaComposition`

| Alias | Label (BG) | Umbraco Data Type | Required |
|---|---|---|---|
| `navHide` | Скрий от навигация | True/False | No |

### 4.2 `Article` Document Type

Alias: `article` | Allowed parents: `newsRoot`

| Alias | Label (BG) | Umbraco Data Type | Required | Notes |
|---|---|---|---|---|
| `headline` | Заглавие | Textstring | ✅ | CMS shows character counter; warning > 120 chars |
| `subtitle` | Подзаглавие | Textstring | ❌ | Used for cards and as SEO fallback |
| `body` | Съдържание | Rich Text Editor (TinyMCE) | ✅ | See §4.2.1 for toolbar config |
| `coverImage` | Основна снимка | Media Picker (single image) | ✅ | Alt text sub-field required (enforced in CMS validator) |
| `category` | Категория | Content Picker (Category) | ✅ | Limited to `category` doc type |
| `region` | Регион | Content Picker (Region) | ✅ | Limited to `region` doc type |
| `tags` | Тагове | Multi-Node Content Picker | ❌ | Limited to `newsTag` doc type; max 10 |
| `author` | Автор | Content Picker (Author) | ✅ | Limited to `author` doc type |
| `isSponsored` | Спонсорирана | True/False | ✅ (default false) | Property group: "Sponsored" — visible to Admin role only |
| `sponsorName` | Спонсор | Textstring | Conditional | Required when `isSponsored = true`; validated in `ContentSavingNotification` |
| `relatedArticlesOverride` | Свързани статии (ръчно) | Multi-Node Content Picker | ❌ | Limited to `article`; max 6; overrides algorithm |
| `articleWorkflowStatus` | Редакционен статус | Dropdown (Draft, In Review) | Auto | Set by service layer; drives "In Review" lock logic |
| + `SeoComposition` | | | | SEO title, description, OG image |

**Slug generation:** Auto-generated from `headline` via Latin transliteration (see §7.3). Editable by Editor/Admin only (`FR-CT-001 AC3`).

**`isSponsored` field restriction:** The `isSponsored` property lives in a dedicated property group `"Sponsored"`. The Admin user group has Read/Write access. The Editor and Writer user groups have no access to this property group, enforced via Umbraco user group property sensitivity configuration AND a `ContentSavingNotification` handler that rejects attempts to set `isSponsored = true` by non-Admin users.

#### 4.2.1 Article Body Rich Text Editor Configuration

The TinyMCE toolbar for the `body` property is configured to include:

- Bold, Italic, Underline
- H2, H3 (H1 is reserved for the article headline and excluded)
- Ordered list, Unordered list
- Hyperlink (with `rel` attribute option; defaults to `noopener` for external links)
- Insert image (opens media library; alt text required)
- Blockquote
- YouTube embed — custom TinyMCE plugin: writer pastes a YouTube URL; the plugin converts it to a responsive `<iframe>` with `<div class="video-embed">` wrapper, `title` attribute, `loading="lazy"`, and aspect-ratio CSS

**Phase 2 body candidates:** Facebook/X embeds, tables, custom info boxes (via Block List).

### 4.3 `Category` Document Type

Alias: `category` | Allowed parents: `categoryRoot`

| Alias | Label (BG) | Umbraco Data Type | Required |
|---|---|---|---|
| `categoryName` | Назва | Textstring | ✅ |
| `seoDescription` | SEO описание | Textarea | ❌ |

**Slug:** Auto-generated from `categoryName` via Latin transliteration; editable by Editor/Admin. URL pattern: `/kategoriya/{slug}/`.

**MVP seed categories** (created during `PredelNewsMigrationPlan` or initial CMS setup):

| Display Name (BG) | Proposed Slug |
|---|---|
| Общество | `obshtestvo` |
| Политика | `politika` |
| Криминално | `kriminalno` |
| Икономика / Бизнес | `ikonomika` |
| Спорт | `sport` |
| Култура | `kultura` |
| Любопитно | `lyubopitno` |
| Хайлайф | `haylayf` |

### 4.4 `Region` Document Type

Alias: `region` | Allowed parents: `regionRoot`

| Alias | Label (BG) | Umbraco Data Type | Required |
|---|---|---|---|
| `regionName` | Назва | Textstring | ✅ |
| `seoDescription` | SEO описание | Textarea | ❌ |

**Slug:** Auto-generated; URL pattern: `/region/{slug}/`.

**MVP seed regions:**

| Display Name (BG) | Proposed Slug |
|---|---|
| Благоевград | `blagoevgrad` |
| Кюстендил | `kyustendil` |
| Перник | `pernik` |
| София | `sofiya` |
| България | `balgariya` |

### 4.5 `Tag` (newsTag) Document Type

Alias: `newsTag` | Allowed parents: `tagRoot`

ASSUMPTION: We use a custom `newsTag` document type rather than Umbraco's built-in tag system, because each tag needs its own archive page URL (`/tag/{slug}/`) with SEO metadata.

| Alias | Label (BG) | Umbraco Data Type | Required |
|---|---|---|---|
| `tagName` | Тяг | Textstring | ✅ |
| `seoDescription` | SEO описание | Textarea | ❌ |

**Slug:** Auto-generated; URL pattern: `/tag/{slug}/`. Tags may be created inline by writers in the article editor (type-ahead, creates new `newsTag` node on save).

### 4.6 `Author` Document Type

Alias: `author` | Allowed parents: `authorRoot`

| Alias | Label (BG) | Umbraco Data Type | Required |
|---|---|---|---|
| `fullName` | Пълно Име | Textstring | ✅ |
| `bio` | Биография | Textarea | ❌ |
| `photo` | Снимка | Media Picker | ❌ |
| `email` | Email (вътрешен) | Email | ❌ |

**Author email** is marked as internal-only and never rendered on public pages (`FR-CT-006 AC4`). It is in a separate "Internal" property group visible only to Editor/Admin.

**Slug:** Auto-generated from `fullName`; URL pattern: `/avtor/{slug}/`.

### 4.7 `HomePage` Document Type

Alias: `homePage` | Allowed parents: Root

| Alias | Label (BG) | Umbraco Data Type | Required | Notes |
|---|---|---|---|---|
| `featuredArticles` | Горещи Новини (курация) | Multi-Node Content Picker (sortable) | ❌ | Max 6 items; limited to `article` doc type. Index 0 = featured; indexes 1–5 = headline links. Falls back to 6 most-recent if empty. |
| `nationalHeadlinesOverride` | Национални Заглавия (ръчно) | Multi-Node Content Picker | ❌ | Optional override for the "National & World" block |
| + `SeoComposition` | | | | Homepage SEO fields |

### 4.8 `SiteSettings` Document Type

Alias: `siteSettings` | Singleton — one instance in content tree; not publicly accessible (`navHide = true`)

| Alias | Label (BG) | Umbraco Data Type | Required |
|---|---|---|---|
| `siteName` | Името на сайта | Textstring | ✅ |
| `siteLogo` | Лого | Media Picker | ✅ |
| `analyticsTrackingId` | GA4/GTM ID | Textstring | ❌ |
| `adSenseSiteScript` | AdSense сайт-скрипт | Textarea | ❌ |
| `contactRecipientEmail` | Email за контакти | Email | ✅ |
| `socialFacebook` | Facebook URL | Textstring | ❌ |
| `socialInstagram` | Instagram URL | Textstring | ❌ |
| `defaultSeoDescription` | Стандартно SEO описание | Textarea (max 320) | ❌ |
| `bannedWordsList` | Забранени думи | Textarea (comma-separated) | ❌ |
| `maintenanceMode` | Режим на поддръжка | True/False | ❌ |

ASSUMPTION: `SiteSettings` is accessed via a typed model helper that reads the singleton node from Umbraco's content cache. This is fast (in-memory) and requires no DB round-trip per request.

### 4.9 Static Page Document Types

#### `StaticPage` — Alias: `staticPage`

Used for: За нас, Реклама, Рекламна оферта, Политика за поверителност.

| Alias | Label (BG) | Umbraco Data Type | Required |
|---|---|---|---|
| `pageTitle` | Заглавие | Textstring | ✅ |
| `body` | Съдържание | Rich Text Editor | ✅ |
| `mediaKitPdf` | Медия кит (PDF) | Media Picker (PDF) | ❌ |
| + `SeoComposition` | | | |

#### `ContactPage` — Alias: `contactPage`

| Alias | Label (BG) | Umbraco Data Type | Required |
|---|---|---|---|
| `pageTitle` | Заглавие | Textstring | ✅ |
| `introText` | Въвеждащ текст | Textarea | ❌ |
| `phoneNumber` | Телефон | Textstring | ❌ |
| `displayEmail` | Email (публичен) | Email | ❌ |
| + `SeoComposition` | | | |

#### `AllNewsPage` — Alias: `allNewsPage`

No content fields beyond SEO. Renders a paginated feed of all published articles.

### 4.10 Content Tree Structure

```
Root
└── predelnews.com [homePage]              → /
    ├── novini [newsRoot]                  → /novini/ (redirects → /vsichki-novini/)
    │   └── {article-slug} [article]      → /novini/{slug}/
    ├── kategoriya [categoryRoot]          → /kategoriya/ (redirects → homepage)
    │   └── {category-slug} [category]    → /kategoriya/{slug}/
    ├── region [regionRoot]                → /region/ (redirects → homepage)
    │   └── {region-slug} [region]        → /region/{slug}/
    ├── tag [tagRoot]                      → /tag/ (redirects → homepage)
    │   └── {tag-slug} [newsTag]           → /tag/{slug}/
    ├── avtor [authorRoot]                 → /avtor/ (redirects → homepage)
    │   └── {author-slug} [author]        → /avtor/{slug}/
    ├── vsichki-novini [allNewsPage]       → /vsichki-novini/
    ├── za-nas [staticPage]               → /za-nas/
    ├── reklama [staticPage]              → /reklama/
    ├── reklamna-oferta [staticPage]      → /reklamna-oferta/
    ├── kontakti [contactPage]            → /kontakti/
    ├── politika-za-poveritelnost [staticPage] → /politika-za-poveritelnost/
    └── _settings [siteSettings]          → not publicly accessible
```

**Article URL decision:** ASSUMPTION: Articles are placed at `/novini/{slug}/` (not at root `/`). This namespaces them cleanly, prevents slug conflicts with reserved root paths (e.g., `/tag/`, `/avtor/`), and is still SEO-friendly. This resolves PRD OQ1.

---

## 5. Custom Modules — Implementation Details

All custom database tables use the `pn_` prefix and are created via Umbraco's `IMigrationPlan` / `PackageMigration` mechanism, ensuring migrations run automatically on app startup in all environments.

```csharp
// PredelNewsMigrationPlan.cs
public class PredelNewsMigrationPlan : PackageMigrationPlan
{
    public PredelNewsMigrationPlan() : base("PredelNews") { }

    protected override void DefinePlan()
    {
        From(string.Empty)
            .To<CreateCustomTablesV1Migration>("v1.0.0");
    }
}
```

### 5.1 Comments Module

Implements `FR-CM-001` through `FR-CM-008` and `FR-AB-008`.

#### 5.1.1 Database Schema

```sql
-- pn_comments: stores all comments (including soft-deleted and held)
CREATE TABLE pn_comments (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    article_id      INT NOT NULL,              -- Umbraco content node ID
    display_name    NVARCHAR(50) NOT NULL,
    comment_text    NVARCHAR(2000) NOT NULL,
    ip_address      NVARCHAR(45) NOT NULL,     -- IPv4 or IPv6
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    is_deleted      BIT NOT NULL DEFAULT 0,
    is_held         BIT NOT NULL DEFAULT 0,    -- held for review (link count / banned word)
    held_reason     NVARCHAR(100) NULL,        -- 'link_count' | 'banned_word:{word}'
    parent_comment_id INT NULL                 -- reserved for Phase 2 threading; always NULL at MVP
);

CREATE INDEX IX_pn_comments_article_visible
    ON pn_comments (article_id, created_at)
    WHERE is_deleted = 0 AND is_held = 0;

CREATE INDEX IX_pn_comments_ip_rate_limit
    ON pn_comments (ip_address, created_at);

CREATE INDEX IX_pn_comments_held
    ON pn_comments (is_held, created_at)
    WHERE is_held = 1 AND is_deleted = 0;

-- pn_comment_audit_log: append-only log of all comment deletions
CREATE TABLE pn_comment_audit_log (
    id                   INT IDENTITY(1,1) PRIMARY KEY,
    comment_id           INT NOT NULL,
    deleted_by_user_id   INT NOT NULL,          -- Umbraco CMS user ID
    deleted_by_username  NVARCHAR(256) NOT NULL,
    deleted_at           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    original_display_name NVARCHAR(50) NOT NULL,
    original_text        NVARCHAR(2000) NOT NULL,
    original_ip          NVARCHAR(45) NOT NULL,
    reason               NVARCHAR(500) NULL      -- optional free text; defaults to NULL
);

CREATE INDEX IX_pn_comment_audit_comment_id
    ON pn_comment_audit_log (comment_id);

CREATE INDEX IX_pn_comment_audit_created_at
    ON pn_comment_audit_log (deleted_at DESC);
```

**Audit log immutability**: No `UPDATE` or `DELETE` endpoint is exposed on `pn_comment_audit_log` through any CMS interface. Additionally, the SQL Server database user used by the application (`pn_app_user`) is **not granted** `DELETE` or `UPDATE` permissions on this table. Only `INSERT` and `SELECT` are granted.

#### 5.1.2 Service Layer

```csharp
public interface ICommentService
{
    /// <summary>Returns visible (non-deleted, non-held) comments for an article, ordered oldest-first.</summary>
    Task<IReadOnlyList<CommentDto>> GetVisibleCommentsAsync(int articleId);

    /// <summary>Returns comment count for an article (visible only).</summary>
    Task<int> GetCommentCountAsync(int articleId);

    /// <summary>Batch comment counts for listing pages (avoids N+1 queries).</summary>
    Task<IReadOnlyDictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> articleIds);

    /// <summary>Processes the anti-spam pipeline, persists if allowed, returns the result.</summary>
    Task<CommentSubmissionResult> SubmitCommentAsync(CommentSubmissionRequest request);

    /// <summary>Soft-deletes a comment and writes an audit log entry.</summary>
    Task DeleteCommentAsync(int commentId, int deletedByUserId, string deletedByUsername, string? reason = null);

    /// <summary>Approves a held comment (sets is_held = false).</summary>
    Task ApproveCommentAsync(int commentId);

    /// <summary>Returns held comments for the CMS moderation interface.</summary>
    Task<PagedResult<HeldCommentDto>> GetHeldCommentsAsync(int page = 1, int pageSize = 20);
}

public record CommentSubmissionRequest(
    int ArticleId,
    string DisplayName,
    string CommentText,
    string IpAddress,
    string? HoneypotField    // must be empty/null for legitimate submissions
);

public enum CommentSubmissionStatus
{
    Accepted,        // visible immediately
    Held,            // stored but not visible (link_count or banned_word)
    RateLimited,     // rejected, 429 response
    HoneypotTripped, // silently discarded
    Invalid          // validation failure
}

public record CommentSubmissionResult(
    CommentSubmissionStatus Status,
    string? UserMessage,     // Bulgarian-language message for the visitor
    CommentDto? Comment      // populated if Status == Accepted
);
```

#### 5.1.3 Anti-Spam Pipeline

The pipeline executes **sequentially** on every `POST /api/comments` request (ref: Architecture §5, `FR-CM-003` through `FR-CM-006`):

```
Request received
    │
    ▼
[1] CSRF token validation         → Fail: HTTP 403
    │
    ▼
[2] Honeypot check                → Filled: HTTP 200 (silent discard, no log, no storage)
    │
    ▼
[3] Input validation              → Invalid: HTTP 400 with field errors
    │   - display_name: required, max 50 chars, strip HTML tags
    │   - comment_text: required, max 2000 chars
    │   - article_id: must be a published article
    ▼
[4] IP rate limit                 → Exceeded: HTTP 429 with Bulgarian message
    │   - Check: sliding window, max 3 per IP per 5 minutes
    │   - Implementation: ASP.NET Core RateLimiter (SlidingWindowRateLimiter, in-memory)
    ▼
[5] URL/link count                → ≥ 2 URLs: stored with is_held=1, HTTP 200 + informational message
    │   - Simple regex: count http(s):// occurrences
    ▼
[6] Banned word check             → Match: stored with is_held=1, HTTP 200 + informational message
    │   - Load banned words from SiteSettings.bannedWordsList (comma-separated)
    │   - Case-insensitive substring match
    ▼
[7] Store comment (is_held=0)     → HTTP 200, return comment DTO
```

Rate-limited response message (Bulgarian): `"Моля, изчакайте няколко минути преди да публикувате нов коментар."`
Held-for-review message: `"Коментарът ви ще бъде прегледан преди публикуване."`

#### 5.1.4 Honeypot Design

```html
<!-- Hidden honeypot field — must remain empty for legitimate submissions -->
<!-- field name should not be obviously named "honeypot" -->
<input type="text" name="website" id="comment-website"
       autocomplete="off"
       style="position:absolute; left:-9999px; opacity:0; pointer-events:none;"
       tabindex="-1"
       aria-hidden="true">
```

Server-side: if `Request.Form["website"]` is not null or empty → `AntiSpamResult.HoneypotTripped`.

#### 5.1.5 Rate Limiting Implementation

Rate limiting for comments uses ASP.NET Core's built-in `RateLimiter` middleware (in-memory, single-instance — appropriate for single VPS). See §8.4 for full configuration.

ASSUMPTION: On app pool recycle, in-memory rate limit windows reset. This is an acceptable limitation for MVP — a recycled app pool would briefly allow slightly more submissions, but the honeypot and other pipeline steps remain active.

#### 5.1.6 Security: XSS Prevention

- `display_name`: HTML tags stripped server-side before storage using `HtmlEncoder.Default.Encode()` (System.Text.Encodings.Web).
- `comment_text`: Stored as plain text; **HTML-encoded on output** in the Razor template using `@Html.Encode()` or `@Model.CommentText` (automatic Razor encoding). Never rendered as raw HTML.
- Search query display: HTML-encoded in the "no results" message.

#### 5.1.7 API Endpoint Contract

```
POST /api/comments
Content-Type: application/x-www-form-urlencoded
Headers: X-CSRF-TOKEN (or form field RequestVerificationToken)

Form fields:
  articleId       int (required)
  displayName     string (required, max 50)
  commentText     string (required, max 2000)
  website         string (honeypot, must be empty)
  __RequestVerificationToken  string (CSRF token)

Response 200 OK (Accepted):
{
  "status": "accepted",
  "comment": {
    "id": 42,
    "displayName": "Мария",
    "commentText": "Много интересна статия!",
    "createdAt": "24.02.2026, 14:30"
  }
}

Response 200 OK (Held):
{ "status": "held", "message": "Коментарът ви ще бъде прегледан преди публикуване." }

Response 429 Too Many Requests:
{ "status": "rate_limited", "message": "Моля, изчакайте няколко минути..." }

Response 400 Bad Request:
{ "status": "invalid", "errors": { "displayName": "Полето е задължително." } }
```

#### 5.1.8 Display Name Cookie Persistence (FR-CM-002)

After a successful comment submission, the response includes a `Set-Cookie` header:

```
pn_comment_name={url-encoded-display-name}; Path=/; SameSite=Lax; Max-Age=2592000 (30 days)
```

The Razor comment form partial reads this cookie to prefill the `displayName` field on subsequent page loads (server-side cookie read).

#### 5.1.9 Comment Rendering in Templates

The `_CommentList.cshtml` and `_CommentForm.cshtml` partials are included in `Article.cshtml`:

```csharp
// In Article.cshtml
@await Html.PartialAsync("_CommentList", Model.Comments)
@await Html.PartialAsync("_CommentForm", new CommentFormViewModel
{
    ArticleId = Model.Id,
    PrefillName = Context.Request.Cookies["pn_comment_name"] ?? string.Empty,
    AntiForgeryToken = Antiforgery.GetAndStoreTokens(Context).RequestToken
})
```

For authenticated CMS users (Writer/Editor/Admin) viewing the public site, a delete button appears next to each comment. This is rendered by checking `User.IsInRole("Writer") || User.IsInRole("Editor") || User.IsInRole("Admin")` in the template.

---

### 5.2 Poll Module

Implements `FR-CT-007`, `FR-PW-005`, `FR-AB-003`.

#### 5.2.1 Database Schema

```sql
-- pn_polls: one row per poll; only one row may have is_active = 1 at a time
CREATE TABLE pn_polls (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    question        NVARCHAR(500) NOT NULL,
    is_active       BIT NOT NULL DEFAULT 0,
    created_at      DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    open_date       DATETIME2 NULL,
    close_date      DATETIME2 NULL,
    created_by_user_id INT NOT NULL
);

-- Filtered unique index enforces the "only one active poll" rule at DB level
CREATE UNIQUE INDEX UQ_pn_polls_single_active
    ON pn_polls (is_active)
    WHERE is_active = 1;

CREATE TABLE pn_poll_options (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    poll_id         INT NOT NULL,
    option_text     NVARCHAR(200) NOT NULL,
    option_order    SMALLINT NOT NULL DEFAULT 1,
    vote_count      INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_pn_poll_options_poll FOREIGN KEY (poll_id) REFERENCES pn_polls(id)
);

CREATE INDEX IX_pn_poll_options_poll_id ON pn_poll_options (poll_id, option_order);
```

**"Only one active poll" enforcement (dual-layer):**
1. **DB level**: The filtered unique index on `pn_polls(is_active) WHERE is_active = 1` prevents two active rows.
2. **Service level**: `IPollService.ActivatePollAsync()` runs in a transaction: first sets all polls to `is_active = 0`, then sets the target poll to `is_active = 1`.

#### 5.2.2 Service Interface

```csharp
public interface IPollService
{
    Task<PollDto?> GetActivePollAsync();
    Task<PollDto?> GetPollByIdAsync(int pollId);
    Task<IReadOnlyList<PollDto>> GetAllPollsAsync(int page = 1, int pageSize = 20);
    Task<PollDto> CreatePollAsync(CreatePollRequest request, int createdByUserId);
    Task ActivatePollAsync(int pollId);
    Task DeactivatePollAsync(int pollId);
    Task<VoteResult> RecordVoteAsync(int pollId, int optionId);
    Task<PollResultsDto> GetResultsAsync(int pollId);
}
```

#### 5.2.3 Vote Endpoint Contract

```
POST /api/poll/vote
Content-Type: application/json

Request body:
{
  "pollId": 3,
  "optionId": 12,
  "__RequestVerificationToken": "..."
}

Validation:
  1. CSRF token check
  2. Poll exists and is_active = 1 AND (close_date IS NULL OR close_date > GETUTCDATE())
  3. Cookie check: if cookie "pn_voted_{pollId}" exists → return results without recording vote
  4. optionId belongs to pollId

Response 200 OK:
{
  "success": true,
  "alreadyVoted": false,
  "results": [
    { "optionId": 12, "optionText": "Да", "voteCount": 42, "percentage": 65.6 },
    { "optionId": 13, "optionText": "Не", "voteCount": 22, "percentage": 34.4 }
  ]
}
```

Vote is incremented via `UPDATE pn_poll_options SET vote_count = vote_count + 1 WHERE id = @optionId` — atomic increment, no concurrency issues.

#### 5.2.4 Cookie Strategy for Vote Deduplication

After a successful vote:

```
Set-Cookie: pn_voted_{pollId}={optionId}; Path=/; SameSite=Lax; Max-Age=31536000 (1 year)
```

On subsequent visits, the poll widget reads this cookie server-side in the Razor partial. If present → render results view. If absent → render voting view.

**Known limitation** (per PRD §6.1.7): Cookie deletion or incognito mode allows re-voting. This is accepted for MVP.

#### 5.2.5 Result Aggregation & Caching

Poll results are **output-cached** with the poll widget HTML (60-second staleness, same as other public pages). The `ContentPublishedNotification` handler does NOT need to invalidate the poll cache on every article publish — the 60-second staleness is acceptable.

However, **after a vote is recorded**, the response returns fresh results directly from the `RecordVoteAsync` call (not from cache), so the voter sees their vote counted immediately.

#### 5.2.6 Backoffice Poll Management

A custom Umbraco backoffice section "Анкети" (Polls) is registered. It provides:
- List view of all polls (active, scheduled, closed)
- Create poll form (question + 2–4 options, open/close dates)
- Activate/deactivate toggle (with confirmation dialog)
- Results view (bar chart and raw counts per option)

ASSUMPTION: The backoffice section is implemented as a custom Umbraco 17 Dashboard using the standard Backoffice UI extensibility pattern (TypeScript/Lit components in the Umbraco backoffice framework).

---

### 5.3 Ads & Sponsored Content Module

Implements `FR-MN-001` through `FR-MN-005`, `FR-AB-005`.

#### 5.3.1 Database Schema

```sql
-- pn_ad_slots: one row per slot; 6 rows seeded on migration
CREATE TABLE pn_ad_slots (
    id                      INT IDENTITY(1,1) PRIMARY KEY,
    slot_id                 NVARCHAR(50) NOT NULL,     -- e.g., 'ad-header-leaderboard'
    slot_name               NVARCHAR(100) NOT NULL,    -- human-readable label
    mode                    NVARCHAR(20) NOT NULL DEFAULT 'adsense',  -- 'adsense' | 'direct'
    adsense_code            NVARCHAR(MAX) NULL,
    direct_image_media_id   INT NULL,                  -- Umbraco media node ID
    direct_destination_url  NVARCHAR(2000) NULL,
    direct_alt_text         NVARCHAR(200) NULL,
    direct_tracking_pixel_url NVARCHAR(2000) NULL,
    direct_start_date       DATETIME2 NULL,
    direct_end_date         DATETIME2 NULL,
    updated_at              DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    updated_by_user_id      INT NULL,
    CONSTRAINT UQ_pn_ad_slots_slot_id UNIQUE (slot_id),
    CONSTRAINT CHK_pn_ad_slots_mode CHECK (mode IN ('adsense', 'direct'))
);
```

**Seed data** (inserted during `CreateCustomTablesV1Migration`):

| slot_id | slot_name | Default mode |
|---|---|---|
| `ad-header-leaderboard` | Хедър Leaderboard (728×90) | `adsense` |
| `ad-sidebar-1` | Сайдбар 1 (300×250) | `adsense` |
| `ad-sidebar-2` | Сайдбар 2 (300×600) | `adsense` |
| `ad-article-mid` | В Статия (след 3-ти параграф) | `adsense` |
| `ad-article-bottom` | Под Статия | `adsense` |
| `ad-footer-banner` | Над Футъра (970×90) | `adsense` |

#### 5.3.2 Slot Rendering Logic

```csharp
public interface IAdSlotService
{
    /// <summary>Returns the configuration for a slot, applying the direct-sold date logic.</summary>
    Task<AdSlotDto> GetSlotAsync(string slotId);
    Task UpdateSlotAsync(string slotId, UpdateAdSlotRequest request, int updatedByUserId);
    Task<IReadOnlyList<AdSlotDto>> GetAllSlotsAsync();
}

public record AdSlotDto(
    string SlotId,
    string SlotName,
    bool IsDirectActive,          // true if mode='direct' AND start_date <= now <= end_date
    string? AdSenseCode,
    string? DirectImageUrl,
    string? DirectDestinationUrl,
    string? DirectAltText,
    string? DirectTrackingPixelUrl
);
```

**Rendering decision tree** (in `_AdSlot.cshtml`):

```
1. Load AdSlotDto from IAdSlotService
2. IF slot.IsDirectActive:
   → Render direct-sold banner:
     <div class="ad-slot" data-slot="{slotId}">
       <span class="ad-label">Реклама</span>
       <a href="{DirectDestinationUrl}" target="_blank" rel="noopener nofollow sponsored">
         <img src="{DirectImageUrl}" alt="{DirectAltText}" loading="lazy">
       </a>
       [if DirectTrackingPixelUrl] <img src="{TrackingPixelUrl}" alt="" width="1" height="1" style="display:none;">
     </div>
3. ELSE IF slot.AdSenseCode is not empty:
   → Render AdSense:
     <div class="ad-slot" data-slot="{slotId}">
       <span class="ad-label">Реклама</span>
       @Html.Raw(slot.AdSenseCode)   <!-- AdSense code is admin-entered; sanitized on save -->
     </div>
4. ELSE:
   → Render nothing (slot collapses; no empty rectangle)
```

**Auto-revert to AdSense:** The `IsDirectActive` flag in `AdSlotDto` is computed at read time:
```csharp
bool IsDirectActive = mode == "direct"
    && direct_start_date <= DateTime.UtcNow
    && (direct_end_date == null || direct_end_date >= DateTime.UtcNow);
```
No scheduled job is needed — the slot simply renders as AdSense once `direct_end_date` passes.

**"Реклама" label:** Rendered by the `_AdSlot.cshtml` partial for every slot regardless of mode. It is template-enforced and cannot be suppressed per-slot (`FR-MN-005`).

**Sidebar slots on mobile:** `ad-sidebar-1` and `ad-sidebar-2` are wrapped in a CSS class that hides them at viewports `< 1024px`. The slot partial still renders in the HTML but is hidden via `display: none` — AdSense may or may not load in hidden elements; this is acceptable for MVP.

#### 5.3.3 Sponsored Article Rendering

Sponsored article banners and `rel="sponsored"` link rewriting are handled in `Article.cshtml`:

**"Платена публикация" banners** (template-enforced — not a CMS option):

```csharp
// In Article.cshtml — banner above headline
@if (Model.IsSponsored)
{
    @await Html.PartialAsync("_SponsoredBanner", new SponsoredBannerViewModel
    {
        Position = "top",
        SponsorName = Model.SponsorName
    })
}

// ... headline, body ...

// Banner below body
@if (Model.IsSponsored)
{
    @await Html.PartialAsync("_SponsoredBanner", new SponsoredBannerViewModel
    {
        Position = "bottom",
        SponsorName = Model.SponsorName
    })
}
```

**`rel="sponsored noopener"` auto-attribution** (`FR-MN-004`):

The article body HTML is post-processed before rendering when `IsSponsored = true`. A helper method rewrites `<a href="...">` tags for external links:

```csharp
public static class SponsoredLinkRewriter
{
    private static readonly Regex LinkRegex = new(@"<a\s+([^>]*)href=""([^""]+)""([^>]*)>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string RewriteLinks(string html, string siteHostname)
    {
        return LinkRegex.Replace(html, match =>
        {
            var href = match.Groups[2].Value;
            if (IsExternalLink(href, siteHostname))
            {
                // Add rel="sponsored noopener", removing any existing rel attribute
                var existingRel = ExtractRel(match.Groups[1].Value + match.Groups[3].Value);
                var newRel = "sponsored noopener";
                return RebuildAnchorTag(match, newRel);
            }
            return match.Value; // internal link: unchanged
        });
    }

    private static bool IsExternalLink(string href, string siteHostname) =>
        href.StartsWith("http") && !href.Contains(siteHostname);
}
```

ASSUMPTION: HtmlAgilityPack or AngleSharp can be added as a NuGet dependency for more robust HTML parsing if the Regex approach proves insufficient. For MVP, the Regex approach is sufficient given the controlled TinyMCE output.

The rewriting is called in the `ArticleViewModel` builder, so the processed HTML is cached in the output cache along with the page.

#### 5.3.4 AdSense Code Security

The `adSenseSiteScript` and per-slot `adsense_code` values are entered by Admin and rendered via `@Html.Raw()`. Since only Admin can set these values, the XSS risk is limited to Admin self-XSS. Content Security Policy (CSP) in enforce mode (Phase 1.1) will further restrict executable script sources.

---

### 5.4 Email Signup Module

Implements `FR-PW-006`, `FR-AB-004`, NFR-PR-005.

#### 5.4.1 Database Schema

```sql
CREATE TABLE pn_email_subscribers (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    email           NVARCHAR(256) NOT NULL,
    signed_up_at    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    consent_flag    BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_pn_email_subscribers_email UNIQUE (email)
);
```

No extra columns. GDPR minimization: only email, timestamp, and consent flag.

#### 5.4.2 Duplicate Handling

Before inserting, the service calls:

```sql
IF NOT EXISTS (SELECT 1 FROM pn_email_subscribers WHERE email = @email)
    INSERT INTO pn_email_subscribers (email, signed_up_at, consent_flag)
    VALUES (@email, GETUTCDATE(), 1);
```

Both paths return the same success response to the user (`FR-PW-006 AC5`). No error is shown for duplicates.

Alternatively, use `INSERT OR IGNORE` / `INSERT ... WHERE NOT EXISTS` or handle the unique constraint violation as a successful no-op.

#### 5.4.3 API Endpoint Contract

```
POST /api/email-signup
Content-Type: application/x-www-form-urlencoded

Form fields:
  email          string (required, valid email format)
  consent        bool   (required, must be "true")
  __RequestVerificationToken

Rate limit: 5 per IP per 10 minutes (ASP.NET Core RateLimiter)

Response 200 OK:
{ "success": true, "message": "Благодарим! Email адресът ви беше записан." }

Response 400 Bad Request:
{ "success": false, "errors": { "email": "Въведете валиден email адрес." } }

Response 429:
{ "success": false, "message": "Моля, опитайте отново след малко." }
```

#### 5.4.4 CSV Export

```
GET /umbraco/api/email-subscribers/export
Authorization: Umbraco session cookie (must be Editor or Admin role)

Response: CSV file download
Content-Disposition: attachment; filename="subscribers-{YYYYMMDD}.csv"
Content-Type: text/csv; charset=utf-8

CSV columns: Email, SignedUpAt (ISO 8601 UTC), ConsentFlag
```

The export endpoint is a minimal Umbraco API controller. It queries `pn_email_subscribers` and streams the response as UTF-8 CSV with BOM (for Excel compatibility).

#### 5.4.5 Phase 2 Extensibility Hook

An `IEmailSignupExportService` interface is registered in DI. For Phase 2, a Mailchimp/ESP implementation can be injected alongside the local DB implementation via a decorator pattern without modifying the endpoint.

---

### 5.5 Contact Form Module

Implements `FR-CT-009`, `FR-AB-007` (contact recipient setting), NFR-PR-003.

#### 5.5.1 Database Schema

```sql
CREATE TABLE pn_contact_submissions (
    id              INT IDENTITY(1,1) PRIMARY KEY,
    name            NVARCHAR(100) NOT NULL,
    email           NVARCHAR(256) NOT NULL,
    subject         NVARCHAR(200) NOT NULL,
    message         NVARCHAR(4000) NOT NULL,
    submitted_at    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ip_address      NVARCHAR(45) NOT NULL
);

CREATE INDEX IX_pn_contact_submissions_date
    ON pn_contact_submissions (submitted_at DESC);
```

#### 5.5.2 Anti-Spam Pipeline

Identical structure to comments anti-spam (CSRF → Honeypot → Rate Limit → Validation):

- **Rate limit**: 3 submissions per IP per 10-minute rolling window
- **Honeypot field**: `<input name="phone_extra" ...>` (hidden, same CSS technique as comments)
- **Rate-limit message**: `"Моля, опитайте отново след няколко минути."`

#### 5.5.3 SMTP Integration

```csharp
public interface IEmailService
{
    Task<bool> SendContactFormEmailAsync(ContactSubmissionDto submission, string recipientEmail);
}

// Configuration (from environment variables, not appsettings.json):
// PredelNews__Email__SmtpHost
// PredelNews__Email__SmtpPort (int)
// PredelNews__Email__SmtpUser
// PredelNews__Email__SmtpPassword
// PredelNews__Email__FromAddress
// PredelNews__Email__UseSsl (bool, default true)
```

**Retry behavior**: No automatic retry. On SMTP failure:
1. The `pn_contact_submissions` row has already been inserted (submission is not lost).
2. The error is logged at `Error` level with Serilog, including the submission ID.
3. The visitor receives the normal success response (`"Съобщението ви беше изпратено успешно."` — per NFR-RL-005, graceful degradation).
4. Admin can view the submission in the CMS or via SQL and re-contact the visitor manually.

ASSUMPTION: SMTP is configured to a transactional email provider (SendGrid, Mailgun, or hosting SMTP relay). Direct Gmail SMTP is not recommended for production.

#### 5.5.4 Data Retention (NFR-PR-003)

Contact form submissions older than 12 months must be deleted. At MVP, this is a **documented manual procedure**:
- Monthly calendar reminder: run the following SQL query on the production database:
  ```sql
  DELETE FROM pn_contact_submissions
  WHERE submitted_at < DATEADD(MONTH, -12, GETUTCDATE());
  ```
- Phase 2: automate via a SQL Server Agent job or a hosted .NET background service.

---

## 6. Search Implementation

Implements `FR-SN-001`, NFR-PF-008, NFR-SE-001.

### 6.1 Examine Index Configuration

Umbraco Examine uses Lucene under the hood. A custom `ExternalIndex` is configured for the public article search:

```csharp
// In IUmbracoBuilder extensions (Program.cs)
builder.Services.AddExamineLuceneIndex<UmbracoExamineIndex, ConfigurationEnabledDirectoryFactory>(
    PredelNewsIndexConstants.ArticleIndex,
    fieldDefinitions: new FieldDefinitionCollection(
        new FieldDefinition("headline",     FieldDefinitionTypes.FullTextSortable),
        new FieldDefinition("subtitle",     FieldDefinitionTypes.FullText),
        new FieldDefinition("bodyText",     FieldDefinitionTypes.FullText),
        new FieldDefinition("tags",         FieldDefinitionTypes.FullText),
        new FieldDefinition("categoryName", FieldDefinitionTypes.FullText),
        new FieldDefinition("regionName",   FieldDefinitionTypes.FullText),
        new FieldDefinition("authorName",   FieldDefinitionTypes.FullText),
        new FieldDefinition("publishDate",  FieldDefinitionTypes.DateTime),
        new FieldDefinition("isSponsored",  FieldDefinitionTypes.Raw),
        new FieldDefinition("articleUrl",   FieldDefinitionTypes.Raw)
    ),
    analyzer: new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48)
);
```

**Analyzer:** `StandardAnalyzer` (built-in Lucene). It handles whitespace tokenization and basic stopword removal. Bulgarian morphology is not handled — acceptable for MVP. Phase 2 may add a custom Bulgarian analyzer.

**What is indexed:** Only published `article` document type nodes. Draft, scheduled, and unpublished articles are excluded.

**Index location:** `D:\PredelNews\data\ExamineIndexes\ArticleIndex\` (VPS file system). Rebuilt from SQL on app startup if the directory is missing or corrupted.

### 6.2 Indexing Strategy

An `INotificationHandler<ContentPublishedNotification>` forces re-index on publish:

```csharp
public class ArticleIndexNotificationHandler
    : INotificationHandler<ContentPublishedNotification>
{
    public void Handle(ContentPublishedNotification notification)
    {
        foreach (var content in notification.PublishedEntities
            .Where(c => c.ContentType.Alias == "article"))
        {
            _examineManager.TryGetIndex(PredelNewsIndexConstants.ArticleIndex, out var index);
            index?.IndexItems(new[] { _publishedContentValueSetBuilder.GetValueSets(content) });
        }
    }
}
```

Similarly, `ContentUnpublishedNotification` removes the article from the index.

### 6.3 Search Controller

```csharp
// GET /search?q={query}&page={page}
public class SearchSurfaceController : SurfaceController
{
    public IActionResult Index(string q, int page = 1)
    {
        if (string.IsNullOrWhiteSpace(q))
            return View(SearchViewModel.Empty(q));

        // Sanitize query: strip HTML, trim to 200 chars
        var cleanQuery = HtmlEncoder.Default.Encode(q.Trim()).Substring(0, Math.Min(q.Length, 200));

        if (_examineManager.TryGetIndex(PredelNewsIndexConstants.ArticleIndex, out var index)
            && index is IUmbracoIndex umbracoIndex)
        {
            var searcher = umbracoIndex.Searcher;

            var results = searcher.CreateQuery("content")
                .ManagedQuery(cleanQuery, new[] { "headline", "subtitle", "bodyText", "tags" })
                .Execute(QueryOptions.SkipTake((page - 1) * PageSize, PageSize));

            return View(new SearchViewModel(cleanQuery, results, page, (int)results.TotalItemCount));
        }

        return View(SearchViewModel.Error(cleanQuery));
    }
}
```

**Result view model fields** (for `_SearchResult.cshtml`):

- `Headline` (with query terms highlighted via simple `string.Replace` bold-wrap)
- `Excerpt` (first 160 chars of body, with query terms highlighted)
- `PublishDate` (Bulgarian format: `DD.MM.YYYY`)
- `CategoryName` + `CategoryUrl`
- `RegionName` + `RegionUrl`
- `ArticleUrl`

**Empty results**: Renders the message `"Не бяха намерени резултати за \"{query}\"."` (Bulgarian).

---

## 7. Routing, URLs, and SEO Plumbing

### 7.1 URL Patterns

| Content Type | URL Pattern | Example |
|---|---|---|
| Homepage | `/` | `predelnews.com/` |
| Article | `/novini/{slug}/` | `/novini/pozhar-v-blagoevgrad/` |
| Category archive | `/kategoriya/{slug}/` | `/kategoriya/politika/` |
| Region archive | `/region/{slug}/` | `/region/blagoevgrad/` |
| Tag archive | `/tag/{slug}/` | `/tag/komunalni-uslugi/` |
| Author archive | `/avtor/{slug}/` | `/avtor/ivan-petrov/` |
| All News | `/vsichki-novini/` | `/vsichki-novini/` |
| All News paginated | `/vsichki-novini/?page=2` | — |
| Category paginated | `/kategoriya/politika/?page=3` | — |
| Search | `/pregled/?q={query}` | `/pregled/?q=pожар+благоевград` |
| За нас | `/za-nas/` | — |
| Реклама | `/reklama/` | — |
| Рекламна оферта | `/reklamna-oferta/` | — |
| Контакти | `/kontakti/` | — |
| Политика за поверителност | `/politika-za-poveritelnost/` | — |

ASSUMPTION: Pagination uses query-string format (`?page=N`) rather than path-based (`/page/2/`) for compatibility with Umbraco's built-in URL routing and simpler cache key handling.

### 7.2 Slug Transliteration

Article slugs (and all taxonomy slugs) are auto-generated by transliterating the Bulgarian Cyrillic title to Latin characters. A custom `ISlugGenerator` service handles this using a standard BG→Latin transliteration table:

```
а→a, б→b, в→v, г→g, д→d, е→e, ж→zh, з→z, и→i, й→y, к→k, л→l, м→m,
н→n, о→o, п→p, р→r, с→s, т→t, у→u, ф→f, х→h, ц→ts, ч→ch, ш→sh,
щ→sht, ъ→a, ь→(omitted), ю→yu, я→ya
```

After transliteration: lowercase, replace spaces and special chars with hyphens (`-`), collapse multiple hyphens, strip leading/trailing hyphens.

### 7.3 Slug Change → 301 Redirect

When an Editor/Admin changes an article's slug, Umbraco 17's built-in URL redirect management creates a 301 redirect from the old URL automatically. This satisfies `NFR-SE-005`.

The Umbraco redirect storage uses the `umbracoRedirectUrl` table. Custom taxonomy slugs (categories, regions, tags) should also use Umbraco's redirect management or a custom redirect table; use `IUmbracoRedirectUrlService` for programmatic redirect creation.

### 7.4 Canonical URL Generation

Every public page includes a `<link rel="canonical">` tag:

- **Article page**: `https://predelnews.com/novini/{slug}/`
- **Archive pages (page 1)**: `https://predelnews.com/kategoriya/{slug}/`
- **Archive pages (page N > 1)**: `https://predelnews.com/kategoriya/{slug}/?page=N` (self-referencing; not pointing to page 1)
- **Homepage**: `https://predelnews.com/`

Canonical URLs always use HTTPS and the `predelnews.com` domain (no trailing slash variation — consistent).

The `_SeoMeta.cshtml` partial generates the canonical tag using `IUmbracoContextAccessor` to resolve the current page's URL.

### 7.5 XML Sitemap Generation

A custom `SitemapController` handles `GET /sitemap.xml`. It queries Umbraco's published content cache for all published nodes of the relevant document types:

```csharp
[Route("sitemap.xml")]
public IActionResult SitemapXml()
{
    // Collect all published articles, categories, regions, tags, authors, static pages
    var nodes = _contentCache
        .GetByContentType("article", "category", "region", "newsTag", "author",
                          "staticPage", "contactPage", "allNewsPage", "homePage")
        .Where(n => n.IsPublished());

    // Build SitemapNode list with <loc>, <lastmod>, <changefreq>, <priority>
    var sitemapXml = BuildSitemapXml(nodes);

    Response.Headers["Cache-Control"] = "public, max-age=3600"; // 1-hour sitemap cache
    return Content(sitemapXml, "application/xml", Encoding.UTF8);
}
```

**Freshness:** The sitemap is cached at the controller level for up to 1 hour. It is invalidated via `ContentPublishedNotification` (output cache tag invalidation). New articles appear in the sitemap within ≤ 60 minutes of publication (`NFR-SE-002`).

**Format:** Standard Sitemaps.org protocol. Includes `<lastmod>` from Umbraco's `UpdateDate` property. Excludes draft, scheduled, and unpublished content.

### 7.6 `robots.txt`

Served via a minimal controller at `GET /robots.txt`:

```
User-agent: *
Disallow: /umbraco/
Disallow: /App_Data/
Disallow: /novini/?preview=*

Sitemap: https://predelnews.com/sitemap.xml
```

### 7.7 JSON-LD Structured Data

#### Article Page — `NewsArticle` schema

Rendered inline in the `<head>` by `_JsonLd.cshtml`:

```json
{
  "@context": "https://schema.org",
  "@type": "NewsArticle",
  "headline": "{Model.Headline}",
  "datePublished": "{Model.PublishDate:O}",
  "dateModified": "{Model.LastModifiedDate:O}",
  "author": {
    "@type": "Person",
    "name": "{Model.AuthorName}"
  },
  "publisher": {
    "@type": "Organization",
    "name": "PredelNews",
    "logo": {
      "@type": "ImageObject",
      "url": "{SiteSettings.SiteLogoAbsoluteUrl}"
    }
  },
  "image": "{Model.CoverImageAbsoluteUrl}",
  "description": "{Model.SeoDescription}",
  "url": "{Model.CanonicalUrl}",
  "isAccessibleForFree": true,
  "isPartOf": {
    "@type": "WebSite",
    "name": "PredelNews",
    "url": "https://predelnews.com"
  }
}
```

If `isSponsored = true`, add:
```json
"sponsor": {
  "@type": "Organization",
  "name": "{Model.SponsorName}"
}
```

#### All Pages — `BreadcrumbList` schema

```json
{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    { "@type": "ListItem", "position": 1, "name": "Начало", "item": "https://predelnews.com/" },
    { "@type": "ListItem", "position": 2, "name": "{CategoryName}", "item": "{CategoryUrl}" },
    { "@type": "ListItem", "position": 3, "name": "{ArticleHeadline}", "item": "{ArticleUrl}" }
  ]
}
```

### 7.8 OG / Twitter Card Meta Tags

Generated in `_SeoMeta.cshtml` for all pages. Article-page values:

```html
<meta property="og:title" content="{SeoTitle ?? Headline}">
<meta property="og:description" content="{SeoDescription ?? Subtitle ?? BodyExcerpt160}">
<meta property="og:image" content="{OgImage ?? CoverImage, absolute URL, min 1200×630}">
<meta property="og:image:width" content="1200">
<meta property="og:image:height" content="630">
<meta property="og:type" content="article">
<meta property="og:url" content="{CanonicalUrl}">
<meta property="og:site_name" content="PredelNews">
<meta property="og:locale" content="bg_BG">
<meta property="article:published_time" content="{PublishDate:O}">
<meta property="article:modified_time" content="{LastModifiedDate:O}">
<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="{SeoTitle ?? Headline}">
<meta name="twitter:description" content="{SeoDescription ?? Subtitle ?? BodyExcerpt160}">
<meta name="twitter:image" content="{OgImage ?? CoverImage, absolute URL}">
```

**OG image fallback chain:** `ogImage` → `coverImage` → `siteSettings.defaultOgImage` (if configured). The image is served as a 1200×630 crop via ImageSharp.

---

## 8. Security & Compliance Implementation

### 8.1 Security Headers Middleware

A custom `SecurityHeadersMiddleware` is added early in the ASP.NET Core pipeline:

```csharp
public class SecurityHeadersMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            // CSP: report-only at MVP; enforced in Phase 1.1
            headers["Content-Security-Policy-Report-Only"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' https://pagead2.googlesyndication.com " +
                "https://www.googletagmanager.com https://www.google-analytics.com; " +
                "frame-src https://www.youtube.com https://www.youtube-nocookie.com; " +
                "img-src 'self' data: https:; " +
                "connect-src 'self' https://www.google-analytics.com; " +
                "report-uri /api/csp-report";

            return Task.CompletedTask;
        });
        await next(context);
    }
}
```

The Umbraco backoffice requires `X-Frame-Options: SAMEORIGIN` for its internal iframe use. A path-based exception is applied:

```csharp
if (context.Request.Path.StartsWithSegments("/umbraco"))
    headers["X-Frame-Options"] = "SAMEORIGIN";
else
    headers["X-Frame-Options"] = "DENY";
```

### 8.2 CSRF Protection

All state-changing API endpoints use ASP.NET Core anti-forgery tokens:

```csharp
// In Program.cs
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";          // for AJAX requests
    options.Cookie.Name = "pn_csrf";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
});

// Applied to all API controllers via global filter or attribute
[AutoValidateAntiforgeryToken]
public class CommentsApiController : ControllerBase { ... }
```

CSRF token is embedded in all Razor forms via `@Html.AntiForgeryToken()`. For AJAX requests, the token is read from the cookie and sent as the `X-CSRF-TOKEN` header by the client JS.

### 8.3 Input Validation Strategy

A centralized validation approach using ASP.NET Core model validation and Data Annotations:

```csharp
public class CommentSubmissionRequest
{
    [Required]
    [MaxLength(50)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string CommentText { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue)]
    public int ArticleId { get; set; }
}
```

An `InputSanitizerService` provides helper methods:
- `StripHtml(string input)` — strips all HTML tags (for display names stored in DB)
- `HtmlEncode(string input)` — encodes HTML entities (for output in templates; Razor does this automatically via `@`)
- `SanitizeSearchQuery(string query)` — trims, HTML-encodes, limits to 200 chars

All API controllers call `ModelState.IsValid` before processing.

### 8.4 Rate Limiting Configuration

Using ASP.NET Core's built-in `RateLimiter` middleware (in-memory, single partition key = IP address):

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("comments", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = appSettings.RateLimit.CommentsPerIpPer5Min,  // default: 3
                Window = TimeSpan.FromMinutes(5),
                SegmentsPerWindow = 5,
                QueueLimit = 0
            }));

    options.AddPolicy("contact-form", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = appSettings.RateLimit.ContactFormPerIpPer10Min, // default: 3
                Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 5,
                QueueLimit = 0
            }));

    options.AddPolicy("email-signup", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetClientIp(httpContext),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = appSettings.RateLimit.EmailSignupPerIpPer10Min, // default: 5
                Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 5,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { status = "rate_limited", message = "Моля, изчакайте няколко минути преди да опитате отново." },
            cancellationToken: cancellationToken);
    };
});

// Thresholds are configurable via appsettings / environment variables (NFR-SC-005)
static string GetClientIp(HttpContext ctx) =>
    ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
    ?? ctx.Connection.RemoteIpAddress?.ToString()
    ?? "unknown";
```

Each API controller applies the rate limiting policy via `[EnableRateLimiting("comments")]` attribute.

**Thresholds** are read from `appsettings.json` → `PredelNews:RateLimit` section so they can be tuned via environment variables without code changes.

### 8.5 Secrets Management (NFR-SC-010)

**Rule: No plaintext secrets in source code or `appsettings.json`.**

All secrets (DB connection string, SMTP credentials) are stored as **Windows environment variables** on the VPS. ASP.NET Core's configuration system reads these automatically.

```csharp
// appsettings.json (committed to source control — no secrets here):
{
  "ConnectionStrings": {
    "umbracoDbDSN": ""           // overridden by environment variable
  },
  "PredelNews": {
    "Email": {
      "SmtpHost": "",            // overridden by environment variable
      "SmtpPort": 587,
      "SmtpUser": "",            // overridden by environment variable
      "SmtpPassword": "",        // overridden by environment variable
      "FromAddress": "noreply@predelnews.com",
      "UseSsl": true
    }
  }
}
```

Windows environment variable naming convention (double-underscore replaces `:` in paths):

```
ConnectionStrings__umbracoDbDSN = "Server=.;Database=PredelNewsDB;..."
PredelNews__Email__SmtpHost = "smtp.sendgrid.net"
PredelNews__Email__SmtpUser = "apikey"
PredelNews__Email__SmtpPassword = "SG.xxxx..."
```

ASSUMPTION: Windows environment variables are set via IIS Manager (Environment Variables on the Application Pool) or via a Windows PowerShell script during provisioning. They are not stored in any file on the VPS that could be committed to Git.

### 8.6 CMS Backoffice Access Hardening (NFR-SC-003)

Configured in `appsettings.json` → Umbraco section:

```json
"Umbraco": {
  "CMS": {
    "Security": {
      "KeepUserLoggedIn": false,
      "UserPasswordMinLength": 12,
      "UserPasswordRequireNonLetterOrDigit": true,
      "UserPasswordRequireDigit": true,
      "UserPasswordRequireLowercase": true,
      "UserPasswordRequireUppercase": true,
      "MaxLoginAttempts": 5,
      "AllowedIpAddresses": []
    }
  }
}
```

Session timeout: 30-minute inactivity timeout configured in Umbraco's user session settings.

Session cookie attributes: `Secure=true`, `HttpOnly=true`, `SameSite=Lax` — Umbraco's default for backoffice cookies.

`/umbraco/` excluded from `robots.txt`.

### 8.7 Comment IP Address Access Control (NFR-PR-006)

IP addresses are stored in `pn_comments.ip_address` but are **never rendered on public pages**. In the CMS backoffice comment moderation view, IP addresses are displayed only to users with Editor or Admin roles (enforced server-side via `[Authorize(Roles = "Editor,Admin")]` on the moderation API endpoints).

---

## 9. Performance & Caching Details

### 9.1 Output Caching Configuration

Using ASP.NET Core's built-in `OutputCache` middleware:

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("PublicPage", builder =>
        builder
            .Expire(TimeSpan.FromSeconds(60))           // max 60s staleness
            .SetVaryByQuery("page", "q")               // vary by pagination and search
            .Tag("public-content")                     // cache tag for invalidation
    );

    options.AddPolicy("NoCache", builder =>
        builder.NoCache()
    );
});

// Apply to controllers:
[OutputCache(PolicyName = "PublicPage")]
public IActionResult Article(string slug) { ... }

// Excluded from caching:
[OutputCache(PolicyName = "NoCache")]  // or [OutputCache(NoStore = true)]
public class CommentsApiController { ... }

// Umbraco backoffice: excluded via path-based middleware condition
```

**Cache invalidation on publish**: A `ContentPublishedNotification` handler calls:

```csharp
await _outputCacheStore.EvictByTagAsync("public-content", cancellationToken);
```

This invalidates all cached public pages. ASSUMPTION: This broad invalidation is acceptable for MVP (low traffic, small cache). A more targeted per-URL invalidation can be added in Phase 2 if needed.

### 9.2 IIS Static File Caching

`web.config` (or `appsettings.json` IIS middleware config) sets long-lived cache headers for static assets:

```xml
<staticContent>
  <clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00" />
</staticContent>
```

Cache-busting: CSS and JS files are referenced with a version query string generated from the build timestamp or a hash:
`<link rel="stylesheet" href="/css/site.css?v=20260224-001">`.

### 9.3 Image Processing Configuration (ImageSharp / Umbraco)

Umbraco's built-in ImageSharp pipeline handles resizing, format conversion, and responsive image serving.

**Article cover image (srcset):**

```csharp
// In ArticleViewModel builder:
public string CoverImageSrcSet => $"{_mediaUrl}/novini/{Slug}?width=400&format=webp 400w, " +
                                   $"{_mediaUrl}/novini/{Slug}?width=800&format=webp 800w, " +
                                   $"{_mediaUrl}/novini/{Slug}?width=1200&format=webp 1200w";

// Or using Umbraco's GetCropUrl extension:
// @coverImage.GetCropUrl(width: 400, furtherOptions: "&format=webp") 400w,
```

**Three size presets:**

| Preset | Width | Usage |
|---|---|---|
| Thumbnail | 400px | Article cards on listing pages |
| Medium | 800px | Article page on mobile |
| Large | 1200px | Article page on desktop; OG image |

**WebP with fallback:** Umbraco ImageSharp serves WebP by default when the browser supports it (via `Accept` header negotiation). JPEG/PNG are served as fallback. No `<picture>` element needed — the ImageSharp pipeline handles format selection.

**Cover image preload (LCP optimization):**

```html
<!-- In <head> on article pages -->
<link rel="preload" as="image"
      href="@coverImage.GetCropUrl(800)?format=webp"
      imagesrcset="@Model.CoverImageSrcSet"
      imagesizes="(max-width: 767px) 100vw, 800px">
```

**Below-fold images:** `loading="lazy"` on all content images below the fold.

**Maximum file size per variant:** Umbraco ImageSharp quality setting set to 80 (WebP) / 85 (JPEG), targeting ≤ 200 KB per variant. Uploads are rejected if > 10 MB (configured in Umbraco's media validation settings).

### 9.4 Known Performance Trade-offs

| Trade-off | Impact | Mitigation at MVP |
|---|---|---|
| No CDN | Higher latency for users far from VPS; all static asset load on VPS | 7-day `Cache-Control` for assets; WebP images; 60s output cache |
| No WAF | No edge-level rate limiting or DDoS absorption | ASP.NET Core RateLimiter; security headers; IIS request limits |
| Single VPS | No horizontal scaling | Output cache absorbs read traffic; VPS sized for 3–5× baseline; alert at CPU > 80% |
| In-process Examine | Search index on same server as app | Index is rebuilt on startup if corrupted; manual re-index from backoffice |
| AdSense loading | Third-party JS can affect INP/CLS | Load `async`/`defer`; no AdSense adjacent to main content LCP element; ad slots have fixed height in CSS to prevent CLS |

---

## 10. Environment & Deployment Details

### 10.1 Environments

| Environment | Purpose | Host |
|---|---|---|
| **Local dev** | Feature development; unit testing | Developer machine (Windows/macOS) |
| **Staging** | Pre-deployment testing; restore drills | Mirror of production VPS (same OS, IIS, .NET, Umbraco) |
| **Production** | Live site | Windows VPS, IIS, SQL Server Express |

### 10.2 Expected `appsettings` Structure

```json
// appsettings.json (no secrets — committed to Git)
{
  "ConnectionStrings": {
    "umbracoDbDSN": ""
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Umbraco": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "D:\\PredelNews\\logs\\predellog-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}"
        }
      }
    ]
  },
  "Umbraco": {
    "CMS": {
      "Global": {
        "MainDomLock": "FileMainDomLock",
        "SanitizeTinyMce": true
      },
      "Content": {
        "AllowEditInvariantFromNonDefault": false
      },
      "Security": {
        "MaxLoginAttempts": 5,
        "UserPasswordMinLength": 12,
        "UserPasswordRequireDigit": true,
        "UserPasswordRequireLowercase": true,
        "UserPasswordRequireUppercase": true,
        "UserPasswordRequireNonLetterOrDigit": true
      },
      "Hosting": {
        "ApplicationVirtualPath": "/",
        "LocalTempStorageLocation": "D:\\PredelNews\\data\\temp"
      },
      "Examine": {
        "LuceneDirectoryFactory": "ConfigurationEnabledDirectoryFactory"
      }
    }
  },
  "PredelNews": {
    "RateLimit": {
      "CommentsPerIpPer5Min": 3,
      "ContactFormPerIpPer10Min": 3,
      "EmailSignupPerIpPer10Min": 5
    },
    "Email": {
      "SmtpPort": 587,
      "FromAddress": "noreply@predelnews.com",
      "UseSsl": true
    },
    "Media": {
      "MaxUploadSizeMb": 10,
      "AllowedExtensions": ["jpg", "jpeg", "png", "webp", "gif", "pdf"]
    }
  }
}
```

Secrets (DB connection string, SMTP credentials) are provided via Windows environment variables — see §8.5.

### 10.3 IIS Site and App Pool Configuration

**App Pool settings:**

| Setting | Value |
|---|---|
| .NET CLR Version | No Managed Code (in-process hosting with .NET 10) |
| Managed Pipeline Mode | Integrated |
| Identity | ApplicationPoolIdentity or a dedicated service account |
| Start Mode | Always Running |
| Idle Time-out | 0 (disabled — app pool should not stop due to inactivity) |
| Regular Time Interval Recycle | 1740 minutes (29 hours — recycle during off-peak) |
| Disable Overlapped Recycle | true (prevents two instances of the app running simultaneously) |

**Site bindings:**

- `http://predelnews.com:80` → IIS HTTP Redirect → `https://predelnews.com` (301)
- `https://predelnews.com:443` → Umbraco application; TLS via Let's Encrypt certificate

**Request filtering (IIS):**
- Maximum request size: 50 MB (for media uploads up to 10 MB + Umbraco overhead)
- Request path length limit: 4096 bytes

**Static file serving:** IIS serves files from `wwwroot\` and `D:\PredelNews\media\` as static content with `Cache-Control: max-age=604800` (7 days).

### 10.4 File System Layout (Production)

```
D:\PredelNews\
├── app\                         # IIS web root — published Umbraco application
│   ├── wwwroot\                 # CSS, JS, favicons, manifest.json
│   └── web.config
├── media\                       # Umbraco media library files (images, PDFs)
├── data\
│   ├── temp\                    # Umbraco runtime temp files
│   └── ExamineIndexes\          # Lucene index files
├── logs\                        # Serilog rotating logs (30-day retention)
│   └── predellog-YYYYMMDD.txt
└── backups\                     # Local pre-deploy snapshots (off-VPS copy is primary backup)
```

### 10.5 Deployment Process Summary

(Full procedure: `docs/technical/deployment.md` — to be authored)

1. Pre-deploy: automated DB backup to `D:\PredelNews\backups\` + copy to off-VPS storage.
2. Run Lighthouse CI on staging — must pass ≥ 90 performance score.
3. Build: `dotnet publish -c Release -o ./publish`
4. Stop IIS app pool (app pool stop command via PowerShell or `appcmd`).
5. Deploy: xcopy/robocopy publish output to `D:\PredelNews\app\` (overwrite, excluding `web.config` which is environment-specific).
6. Start IIS app pool.
7. Smoke test: homepage returns HTTP 200; article page loads; CMS login works.
8. Rollback: restore previous publish folder from backup tag + restore DB if schema changed.

ASSUMPTION: No CI/CD pipeline is set up at MVP. Deployment is manual via RDP with a documented PowerShell script. A GitHub Actions or Azure DevOps pipeline is a Phase 2 enhancement.

---

## 11. Logging, Monitoring & Observability Hooks

### 11.1 Logging Framework

**Serilog** is used as the logging framework with the file sink. It is registered in `Program.cs` via the Serilog integration NuGet packages.

**Log format** (as configured in §10.2):
```
{Timestamp} [{Level}] {Message} {Properties}
{Exception}
```

**Log levels:**
- `Information` — normal request processing, article publish events, comment submissions
- `Warning` — rate limit hits, validation failures, duplicate email signup attempts
- `Error` — unhandled exceptions, SMTP failures, DB connection errors, CSRF rejections
- `Fatal` — app startup failures (Serilog writes these before DI is available)

### 11.2 Log Categories

| Category | Level | What is logged |
|---|---|---|
| `PredelNews.Comments` | Info/Warn | Comment accepted, held, rate-limited, honeypot tripped |
| `PredelNews.Security` | Warn/Error | CSRF failure, rate-limit exceeded (with IP), 403/401 responses |
| `PredelNews.Email` | Error | SMTP delivery failure (submission ID + exception) |
| `PredelNews.Audit` | Info | Article state changes, comment deletions, ad slot changes, user role changes |
| `PredelNews.Search` | Warn | Examine index unavailable, query errors |
| `PredelNews.Polls` | Info | Vote recorded, poll activated/deactivated |
| `Umbraco.Core` | Warning+ | Umbraco internal warnings and errors |

**Security incident logging example:**

```csharp
_logger.LogWarning("CSRF_FAILURE: Request to {Path} from {IP} rejected — invalid or missing token",
    context.Request.Path, clientIp);

_logger.LogWarning("RATE_LIMIT_HIT: Endpoint={Endpoint}, IP={IP}, Policy={Policy}",
    endpoint, clientIp, policyName);
```

### 11.3 Log Accessibility (NFR-MN-006)

Logs are accessible without RDP via a secured admin-only endpoint:

```
GET /umbraco/api/logs/download?date={YYYYMMDD}
Authorization: Umbraco Admin role

Response: log file download (text/plain)
```

This endpoint streams the Serilog file for the requested date. It is registered as an Umbraco API controller with `[Authorize(Roles = "Admin")]`.

### 11.4 Uptime Monitoring Setup (NFR-OB-003)

Configure **UptimeRobot** (free tier) or equivalent external monitor:

- Monitor type: HTTP(s)
- URL: `https://predelnews.com/`
- Interval: 5 minutes (1-minute preferred if budget allows)
- Alert contacts: OPS email + optional SMS / Telegram notification
- Expected status code: 200

### 11.5 Infrastructure Monitoring (NFR-OB-002)

Windows Performance Counters monitored via a lightweight agent:

| Metric | Alert Threshold | Method |
|---|---|---|
| CPU utilization | > 80% for 5 min | Windows Task Scheduler script → email; or UptimeRobot custom monitor |
| Memory utilization | > 85% for 5 min | Same |
| Disk utilization (`D:\`) | > 80% | PowerShell scheduled task → email |
| IIS App Pool health | Stopped / crashed | UptimeRobot HTTP monitor detects (HTTP 503) |
| Let's Encrypt cert expiry | < 30 days | UptimeRobot SSL check |

ASSUMPTION: A simple PowerShell script runs every 5 minutes via Windows Task Scheduler, checks resource thresholds, and sends an email alert via the configured SMTP if thresholds are exceeded. This is documented in `docs/technical/observability.md`.

### 11.6 Audit Trail Implementation (NFR-OB-004)

All auditable events write to `pn_audit_log` (see §5 — created in the DB migration):

```sql
CREATE TABLE pn_audit_log (
    id                  INT IDENTITY(1,1) PRIMARY KEY,
    event_type          NVARCHAR(50) NOT NULL,       -- 'ArticleStateChange', 'UserRoleChange',
                                                     -- 'AdSlotChange', 'CommentDelete'
    acting_user_id      INT NOT NULL,
    acting_username     NVARCHAR(256) NOT NULL,
    created_at          DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    entity_type         NVARCHAR(50) NULL,
    entity_id           NVARCHAR(100) NULL,
    previous_value      NVARCHAR(MAX) NULL,          -- JSON
    new_value           NVARCHAR(MAX) NULL,          -- JSON
    notes               NVARCHAR(500) NULL
);

CREATE INDEX IX_pn_audit_log_type_date ON pn_audit_log (event_type, created_at DESC);
CREATE INDEX IX_pn_audit_log_date ON pn_audit_log (created_at DESC);
```

**Immutability enforced by:** No `UPDATE` or `DELETE` endpoints on `pn_audit_log`. The SQL Server database user `pn_app_user` is granted only `INSERT` and `SELECT` on this table.

**Retention:** Indefinite for article/user/ad events. Comment-related entries purged after 24 months per NFR-PR-004 (manual procedure at MVP; automated in Phase 2).

---

## 12. Risks, Constraints & Technical Decisions

### 12.1 Key Technical Decisions

| Decision | Choice | Rationale |
|---|---|---|
| **Database** | SQL Server Express | Free, battle-tested with Umbraco on Windows/IIS. 10 GB limit is not a concern at MVP (media is on file system). Upgrade to Standard when DB approaches 8 GB. |
| **Article URL prefix** | `/novini/{slug}/` | Namespaces articles, prevents slug conflicts with `/tag/`, `/avtor/`, and static pages. See §7.1. |
| **Slug encoding** | Latin transliteration (custom `ISlugGenerator`) | Maximum browser compatibility, social sharing, and SEO-friendliness. No Cyrillic characters in URLs. Resolves PRD OQ1. |
| **Rate limiting storage** | ASP.NET Core in-memory `RateLimiter` | Single VPS — no Redis needed. Resets on app pool recycle (acceptable limitation). Simplest correct solution for MVP traffic. |
| **Body editor** | TinyMCE (Umbraco built-in RTE) | Writers familiar with word-processor metaphor. Supports all MVP content types. Phase 2 can migrate to Block Editor if richer structured content is needed. |
| **Poll storage** | Custom DB tables (`pn_polls`, `pn_poll_options`) | Polls have a lifecycle (active/inactive/closed) and aggregate vote counts, which don't map cleanly to Umbraco document types. |
| **`Is Sponsored` access** | Admin only (not Editor) | FS §FR-UR-002 explicitly restricts to Admin. Configurable to include Editor without code change. |
| **Sponsored link rewriting** | Regex-based HTML post-processor | Simple and sufficient for TinyMCE-generated HTML. HtmlAgilityPack is available as a dependency upgrade if edge cases emerge. |
| **Sitemap generation** | On-demand with 1-hour output cache | Avoids scheduled task complexity. Always fresh within 60 minutes of publish. |
| **Search analyzer** | `StandardAnalyzer` (Lucene 4.8) | No Bulgarian stemmer available in Examine out of the box. StandardAnalyzer is adequate for keyword search at MVP volumes. Phase 2 can add a Bulgarian Snowball stemmer. |
| **No CDN at MVP** | Accepted constraint per PRD/Architecture | 7-day static asset caching + WebP + output cache mitigates impact. Phase 2: Cloudflare. |
| **CSP in report-only mode** | Initial MVP launch | Allows cataloging all legitimate script sources before enforcement. Move to enforced mode in Phase 1.1 (within 30 days of launch). |

### 12.2 Known Limitations

| Limitation | Impact | Notes |
|---|---|---|
| SQL Server Express 10 GB limit | Medium (long-term) | Media is on file system; text content grows slowly. Alert at 8 GB. |
| In-memory rate limiting resets on restart | Low | App pool restarts are rare. Honeypot provides primary spam defense. |
| No MFA for CMS | Medium | Mitigated by strong passwords, lockout, small account count. Phase 2. |
| Comment vote deduplication via cookie | Low | Cookie deletion allows re-vote. Accepted per PRD. |
| Single VPS — no redundancy | Medium | Off-site backups. Documented RTO ≤ 4 hours. Phase 2: Cloudflare + possible second VPS. |
| Examine search — no Bulgarian stemmer | Low | Searches for exact words work. Morphological variants (пожар/пожари) may miss results. Acceptable for MVP. |
| CSP report-only mode | Low | XSS mitigation via server-side encoding; CSP is defense-in-depth. |
| Manual data retention procedures | Low | 12-month contact form and 24-month audit log retention are documented manual procedures. Phase 2: automate. |

---

## 13. Open Questions / TBDs

| # | Question | Owner | Needed by | Options / Recommendation |
|---|---|---|---|---|
| OQ-T1 | **Which transactional email provider?** SendGrid free tier (100 emails/day), Mailgun, or hosting SMTP relay? | Tech Lead | Sprint 2 start | Recommendation: SendGrid free tier (simplest SDK, no MX configuration needed) |
| OQ-T2 | **Umbraco 17 backoffice extensibility method:** Custom sections use TypeScript/Lit or the older Angular-based API? Umbraco 14+ uses Vite/Lit. Confirm before implementing poll/ads/audit backoffice sections. | Tech Lead | Sprint 1 (architecture sign-off) | Recommendation: Use Umbraco 17's native Backoffice Extensibility API (Lit-based) |
| OQ-T3 | **Branded maintenance page implementation:** Static HTML file served by IIS directly (bypassing ASP.NET Core), or a simple Razor endpoint? | Tech Lead / OPS | Pre-launch | Recommendation: Static HTML file at `maintenance.html` served by IIS with a URL rewrite rule when maintenance mode is active |
| OQ-T4 | **Backup destination:** Hosting provider snapshot vs. external storage (e.g., Backblaze B2, Azure Blob, or a second VPS)? Off-VPS storage is required per NFR-RL-003. | OPS | Sprint 1 | Decision needed before provisioning. |
| OQ-T5 | **VPS sizing:** What are the CPU/RAM specs of the production VPS? Minimum recommended: 4 vCPU, 8 GB RAM, 100 GB SSD for MVP. | Tech Lead / Founder | Sprint 1 start | Affects load test baselines and alerting thresholds |
| OQ-T6 | **`web.config` vs. environment variable injection for IIS:** Confirm that the IIS App Pool on the target hosting provider supports custom environment variables (most do, but some shared hosts restrict this). | Tech Lead / OPS | Sprint 1 | If environment variables aren't supported: use Windows DPAPI-encrypted `appsettings.production.json` as fallback |
| OQ-T7 | **Comment moderation UI access from the public article page:** Should authenticated Writer/Editor/Admin users see a "Delete" button next to each comment when browsing the public site? Or moderation-only from the CMS backoffice? | PM / Editor | Sprint 2 | Recommendation: Yes — show delete button on public article page for authenticated users (saves round-trips to backoffice) |
| OQ-T8 | **Initial topic categories — final list:** The FS lists 8 categories. Has the Editor-in-Chief confirmed this list, and are the display names final? | Editor | CMS setup (Sprint 1) | See FS §FR-CT-003; current list is a seed proposal |
| OQ-T9 | **AdSense account setup:** Has the AdSense account for `predelnews.com` been created and approved? AdSense approval takes 1–4 weeks. If not yet started, begin immediately. | PM / Founder | Before Sprint 3 end | Start application process in parallel with development |
| OQ-T10 | **Cookie consent scope:** Legal review required — is single opt-in with a basic "Accept" banner sufficient for the Bulgarian market under GDPR, or is pre-consent analytics loading a compliance risk? | PM / Legal | Sprint 3 | See NFR assumption NA7; legal review recommended before launch |
| OQ-T11 | **Archive parent node URLs (`/kategoriya/`, `/region/`, `/novini/`):** Should these redirect to the homepage, the All News page, or return 404? | Tech Lead / PM | Sprint 1 | Recommendation: 301 redirect `/novini/` → `/vsichki-novini/`; redirect `/kategoriya/` and `/region/` → homepage |
| OQ-T12 | **Tag creation by Writers:** Should Writers be able to create new tags inline during article editing, or should tag creation be restricted to Editor/Admin? | Editor | CMS setup | Recommendation: Allow Writer to create tags inline (common CMS pattern; editors can prune later) |

---

*End of Technical Specification. This document should be reviewed by the Tech Lead, Product Manager, and Editor-in-Chief before Sprint 1 begins. It is a living document — update it as implementation decisions are finalized and open questions are resolved.*

*Related documents to author next:*
- `docs/technical/database-schema.md` — complete DDL extracted from §5 of this document
- `docs/technical/deployment.md` — elaborates §10
- `docs/technical/observability.md` — elaborates §11
- `docs/editorial/cms-guide.md` — writer and editor CMS usage guide
