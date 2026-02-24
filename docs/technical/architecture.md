# Architecture — PredelNews

**Product:** PredelNews — Regional News Website for Southwest Bulgaria
**Domain:** predelnews.com
**Platform:** Umbraco 17 LTS (.NET 10) on Windows VPS (IIS)
**Document owner:** Solutions Architect
**Status:** Draft v1.0
**Last updated:** 2026-02-23

> **Repository path:** `docs/technical/architecture.md`
> Related: `docs/business/prd.md` · `docs/business/non-functional-requirements.md` · `docs/technical/technical-specification.md` · `docs/technical/database-schema.md` · `docs/technical/observability.md`

---

## Assumptions

The following assumptions were made where answers were not provided or where the user asked for a recommendation. Stakeholders should validate each.

| ID | Assumption |
|----|-----------|
| AA1 | **Database: SQL Server Express.** Recommended for this stack. It is the most battle-tested database for Umbraco on Windows/IIS, is free (no license cost at MVP), and supports all Umbraco features. The 10 GB database-size limit is not a concern because article and metadata volumes at MVP are small; binary media is stored on the file system, not in the database. Upgrade to SQL Server Standard when the DB approaches 8 GB or if SQL Agent scheduling becomes necessary. |
| AA2 | **Email delivery: SMTP via a transactional provider** (e.g., SendGrid free tier, Mailgun, or the hosting provider's SMTP relay). Used for contact-form delivery and CMS editorial notifications only. No in-app bulk mailing at MVP. |
| AA3 | **Polls are standalone (site-wide).** One active poll at a time, displayed on the homepage. Cookie-based vote deduplication. Not article-attached in MVP. |
| AA4 | **No CDN at MVP.** All assets (images, CSS, JS) are served directly from the VPS. CDN migration is planned for Phase 2 (see §12). |
| AA5 | **No WAF/Cloudflare at MVP.** Defense is application-layer: rate limiting, security headers, input validation. Cloudflare is a Phase 2 recommendation. |
| AA6 | **No reverse proxy in front of IIS.** IIS handles HTTPS termination using a Let's Encrypt certificate (e.g., via win-acme). |

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture Goals & Principles](#2-architecture-goals--principles)
3. [System Context](#3-system-context)
4. [Containers](#4-containers)
5. [Components](#5-components)
6. [Data](#6-data)
7. [Key Flows](#7-key-flows)
8. [Security Model](#8-security-model)
9. [Performance & Caching](#9-performance--caching)
10. [Deployment Topology](#10-deployment-topology)
11. [Risks & Mitigations](#11-risks--mitigations)
12. [Future Evolution](#12-future-evolution)

---

## 1. Overview

PredelNews is a Bulgarian-language regional news website targeting Southwest Bulgaria (primary: Blagoevgrad region). It is built on **Umbraco 17 LTS** running on **.NET 10**, hosted on a **single Windows VPS** behind **IIS**.

The system has two distinct surfaces:

- **Public website** — fast, SEO-optimised, mobile-first pages for readers: article pages, taxonomy archives, homepage, search, comments, polls, and ads.
- **Backoffice (CMS)** — Umbraco's built-in editorial UI, extended with custom sections for comments, ads, polls, and audit logs; used by Admins, Editors, and Writers.

### Key MVP Constraints

| Constraint | Value |
|-----------|-------|
| Hosting | Single Windows VPS, IIS |
| Database | SQL Server Express (recommended) |
| Media storage | Local file system on the VPS |
| Authentication | Username + password only (no MFA at MVP) |
| CDN | None at MVP |
| WAF | None at MVP |
| Search | Umbraco Examine (Lucene, built-in) |
| Dark mode | Not supported; `color-scheme: light only` |
| Comments | Anonymous, immediate publish; soft delete + audit |
| Ad monetization | Google AdSense + direct-sold banners |
| Sponsored content label | "Платена публикация" (template-enforced) |

---

## 2. Architecture Goals & Principles

These goals derive directly from the NFRs and PRD product principles. Every architectural decision must be weighed against them.

| Goal | Principle | Key NFRs |
|------|-----------|---------|
| **Speed** | Pages ≤ 2.5 s LCP on mobile; Lighthouse ≥ 90. Output cache with 60 s staleness compensates for the absence of a CDN. Images are served with WebP + responsive srcset. | NFR-PF-001 – 006 |
| **SEO-first** | Server-side rendering for all public pages (Umbraco Razor templates). No client-side-only rendering that blocks indexing. Canonical URLs, structured data (NewsArticle, BreadcrumbList), XML sitemap, robots.txt. | NFR-SE-001 – 005 |
| **Security baseline** | OWASP Top 10 mitigations: parameterised queries, output encoding (XSS), CSRF tokens, secure headers, rate limiting, brute-force lockout. No secrets in source control. | NFR-SC-001 – 010 |
| **Operational simplicity** | Single VPS deployment. Git-based code backup. Documented deploy + rollback scripts. Manual or script-based backups. Small team can operate without dedicated DevOps. | NFR-MN-001 – 007 |
| **Resilience within constraints** | Output caching absorbs traffic spikes. Graceful degradation if third-party scripts (AdSense, Analytics) are unavailable. No page renders blank because of a failed ad load. | NFR-RL-005, NFR-PF-007 |
| **Editorial trust** | Sponsored content labelling ("Платена публикация") is template-enforced and cannot be suppressed per-article. Audit logs are append-only. | FR-MN-003, NFR-OB-004 |
| **Privacy by default** | Minimum data collection. GDPR retention schedules documented. Comment IP visible only to Editor/Admin. | NFR-PR-001 – 007 |

---

## 3. System Context

### Actors

| Actor | Description |
|-------|-------------|
| **Visitor / Reader** | Anonymous public user. Reads articles, posts comments (anonymous), votes in polls, signs up for email updates, submits contact form. |
| **Writer** | Authenticated CMS user. Creates and submits article drafts. Can delete comments. |
| **Editor** | Authenticated CMS user. Publishes, schedules, and manages all content. Moderates comments. Manages taxonomy, polls, email subscribers. |
| **Admin** | Authenticated CMS user with full access: user management, ad slots, site settings, audit logs. |
| **Search Engine Bot** | Google, Bing, etc. Crawls public pages via sitemap and links. |

### External Systems

| System | Role | Notes |
|--------|------|-------|
| **Google AdSense** | Automated display advertising | JS snippet loaded per-slot. Graceful empty slot on failure. |
| **Google Analytics 4** | Traffic analytics | Async script; configurable GA4 Measurement ID in CMS settings. |
| **SMTP / Transactional Email Provider** | Contact form delivery, CMS editorial notifications | Configurable SMTP settings. Not used for bulk mail at MVP. |
| **Let's Encrypt (win-acme)** | SSL/TLS certificate for HTTPS | Auto-renewed on the VPS. |
| **YouTube** | Video embeds in article body | Responsive `<iframe>` only; no video hosting on-site. |
| **Social platforms (Facebook, Viber)** | Share button targets; OG meta for rich link previews | No SDK loaded; share links are plain URLs. |

---

## 4. Containers

This section describes the major runtime processes and storage units. At MVP, everything runs on a single VPS.

```
┌─────────────────────────────────────────────────────────────────────┐
│  Windows VPS                                                        │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │  IIS (w3svc)                                                  │  │
│  │                                                               │  │
│  │  ┌──────────────────────────────────────────────────────────┐ │  │
│  │  │  Umbraco 17 / .NET 10 Application                        │ │  │
│  │  │  (ASP.NET Core app pool)                                 │ │  │
│  │  │                                                          │ │  │
│  │  │  Public Razor Pages  ◄──── Visitors (HTTPS :443)        │ │  │
│  │  │  Backoffice /umbraco  ◄─── CMS Users (HTTPS :443)       │ │  │
│  │  │  API controllers (comments, polls, email signup)         │ │  │
│  │  └──────────────────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌──────────────────────────┐   ┌───────────────────────────────┐  │
│  │  SQL Server Express      │   │  File System Media Store      │  │
│  │                          │   │  (D:\PredelNews\media\)       │  │
│  │  - Umbraco CMS data      │   │                               │  │
│  │  - Custom tables:        │   │  Images (WebP, JPEG, PNG)     │  │
│  │    comments, polls,      │   │  PDFs (media kit)             │  │
│  │    email_subscribers,    │   │  Examine index files          │  │
│  │    contact_submissions,  │   │  (Lucene index on disk)       │  │
│  │    audit_log,            │   │                               │  │
│  │    ad_slots              │   └───────────────────────────────┘  │
│  └──────────────────────────┘                                       │
│                                                                     │
│  ┌──────────────────────────┐   ┌───────────────────────────────┐  │
│  │  Application Logs        │   │  Backup Storage               │  │
│  │  (file-based, 30-day     │   │  (off-site or separate        │  │
│  │   rotation)              │   │   volume; daily DB + media)   │  │
│  └──────────────────────────┘   └───────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
         │ HTTP :80 (redirects to HTTPS)
         │ HTTPS :443 (TLS via Let's Encrypt)
    [Internet / Visitors / Bots]
```

### Container Descriptions

| Container | Technology | Responsibility |
|-----------|-----------|---------------|
| **Umbraco Web App** | Umbraco 17, ASP.NET Core, .NET 10 | Serves all HTTP traffic: public Razor pages, backoffice, custom API endpoints (comments POST, poll vote, email signup, contact form). Hosts Examine search index in-process. |
| **SQL Server Express** | SQL Server Express (latest stable) | Persistent store for all structured data: Umbraco CMS data, articles, taxonomy, users, and all custom tables (comments, polls, ads, subscribers, contact submissions, audit log). |
| **File System Media Store** | NTFS on VPS disk | Binary media files uploaded through Umbraco's media library: article images (source originals), WebP variants generated by Umbraco image processing. Examine Lucene index files also live on disk. |
| **Application Logs** | Serilog (file sink) | Rotating log files; 30-day retention. Accessible without RDP via a secured internal endpoint or log download script. |
| **Backup Storage** | Off-site / separate volume | Daily SQL Server `.bak` + media file copies. Must not share the same physical disk as the production VPS. |

---

## 5. Components

This section describes the logical components **within** the Umbraco web application.

```
Umbraco 17 Web Application
├── Public Site Rendering
│   ├── Razor templates (homepage, article, archive, search, static pages)
│   ├── View components (article card, pagination, breadcrumb, OG meta)
│   └── SEO module (structured data, sitemap, canonical, robots.txt)
├── Backoffice
│   ├── Standard Umbraco backoffice (content tree, media library, users)
│   ├── Custom dashboard (editorial metrics, held comments, email count)
│   └── Site Settings section (analytics ID, contact email, social links)
├── Content Models
│   ├── Article (headline, body, cover image, category, region, tags, author,
│   │           Is Sponsored, sponsor name, publish date, status, SEO fields)
│   ├── Author
│   ├── Category taxonomy
│   ├── Region taxonomy
│   ├── Tag taxonomy
│   └── Static Pages (За нас, Реклама, Рекламна оферта, Контакти)
├── Comments Module
│   ├── Comment submission API endpoint (POST /api/comments)
│   ├── Anti-spam pipeline (honeypot check → rate limit → link count → banned words)
│   ├── Soft delete + audit log write
│   └── CMS moderation section (held comments, delete, approve)
├── Poll Module
│   ├── Vote API endpoint (POST /api/poll/vote)
│   ├── Cookie-based deduplication
│   ├── Results query
│   └── CMS poll management section (create, activate, close, results view)
├── Ads / Sponsored Module
│   ├── Ad slot configuration store (6 named slots in SQL)
│   ├── Slot renderer (AdSense code vs. direct-sold banner; date-range logic)
│   ├── "Реклама" label template component (template-enforced, not per-slot)
│   ├── Sponsored article renderer ("Платена публикация" banner, rel=sponsored links)
│   └── CMS ad management section (Admin only)
├── Search Module
│   ├── Umbraco Examine index (published articles only)
│   ├── Search results controller + Razor template
│   └── Bulgarian-language field mapping (headline, subtitle, body, tags)
├── Email Collection Module
│   ├── Signup API endpoint (POST /api/email-signup)
│   ├── Duplicate email check
│   └── CSV export action (Editor/Admin only)
├── Contact Form Module
│   ├── Contact form POST endpoint
│   ├── Honeypot + rate limiting
│   ├── Contact submission persistence (12-month retention)
│   └── SMTP delivery via configured transactional provider
└── Output Cache Middleware
    ├── Cache public Razor responses (60-second max staleness)
    ├── Vary by URL; no caching for /umbraco/, preview URLs, or POST endpoints
    └── Cache invalidation on Umbraco content publish/unpublish events
```

### Component Responsibilities

| Component | Key Boundaries |
|-----------|---------------|
| **Public Site Rendering** | All visitor-facing HTML is server-side rendered via Razor. No SPA framework. JavaScript is progressive enhancement only (share buttons, hamburger nav, cookie banner). Core content must be readable with JS disabled. |
| **Backoffice** | Standard Umbraco 17 backoffice + custom sections. All role checks are server-side enforced (not UI-only). |
| **Content Models** | Defined as Umbraco document types. The `Is Sponsored` field is read-only/hidden for Writer role at the property editor level and enforced server-side. |
| **Comments Module** | Anti-spam pipeline executes sequentially: honeypot → IP rate limit → link count → banned word list. Comments that pass all checks are stored and rendered immediately. Comments that fail the link/banned-word check are stored with status `held` and not rendered publicly. Rate-limited or honeypot submissions are discarded. |
| **Poll Module** | Only one poll can be `is_active = true` at a time (enforced by a DB constraint + service logic). Vote deduplication uses a browser cookie; cookie loss allows a re-vote (accepted limitation for MVP). |
| **Ads / Sponsored Module** | The "Платена публикация" banner and "Реклама" label are emitted by the Razor template, not injected by JavaScript, ensuring they cannot be easily suppressed by ad blockers or per-slot configuration. |
| **Search Module** | Umbraco Examine indexes published articles on save/publish events. Index is stored on the VPS file system alongside the Lucene libraries. No external search service at MVP. |
| **Output Cache Middleware** | ASP.NET Core output caching (or IIS kernel-mode caching for static assets). Cache is busted via Umbraco's content-saved notifications. CMS backoffice, preview URLs, and all API POST endpoints are excluded from caching. |

---

## 6. Data

### Storage Locations Summary

| Data | Storage | Notes |
|------|---------|-------|
| Articles, categories, regions, tags, authors, static pages | SQL Server — Umbraco `cmsDo*` / `umbraco*` tables | Managed by Umbraco's ORM |
| Media metadata (filename, path, alt text, dimensions) | SQL Server — Umbraco media tables | Umbraco manages metadata |
| Media binary files (images, PDFs) | File system (`/media/`) | Served as static files by IIS |
| Examine / Lucene search index | File system (`/App_Data/TEMP/ExamineIndexes/`) | Rebuilt from SQL on startup if missing |
| Comments | SQL Server — custom `pn_comments` table | Soft delete; includes IP, display name, timestamp, `is_deleted`, `is_held` |
| Comment audit log | SQL Server — custom `pn_comment_audit_log` table | Append-only; 24-month retention |
| Poll definitions + vote counts | SQL Server — custom `pn_polls`, `pn_poll_options` tables | Vote deduplication via cookie (no per-user DB row) |
| Ad slot configuration | SQL Server — custom `pn_ad_slots` table | 6 rows (one per slot); updated by Admin |
| Email subscribers | SQL Server — custom `pn_email_subscribers` table | email, signed_up_at, consent_flag |
| Contact form submissions | SQL Server — custom `pn_contact_submissions` table | 12-month GDPR retention |
| Article state transitions (audit) | SQL Server — custom `pn_audit_log` table | Covers article, user role, and ad slot changes |
| CMS users, roles, sessions | SQL Server — Umbraco member/user tables | Managed by Umbraco identity |
| Application logs | File system (`/logs/`) | Serilog; daily rotation; 30-day retention |
| Backups | Off-site storage (or secondary volume) | Daily `.bak` + media sync |

### Custom Table Group

All custom tables use the `pn_` prefix to avoid collisions with Umbraco's schema. They are created and migrated via Umbraco's `IMigrationPlan` / `PackageMigration` mechanism so migrations run automatically on app startup.

---

## 7. Key Flows

### 7.1 Publish Article (Draft → Review → Publish)

1. **Writer** logs into `/umbraco`, creates an Article document (sets headline, body, cover image, category, region, tags, author).
2. Writer sets article status to **In Review** and saves. Umbraco records the state transition in `pn_audit_log`.
3. **Editor** sees a badge/count on the "In Review" dashboard widget.
4. Editor opens the article, makes edits if needed, sets the Publish Date (now or future), and clicks **Publish**.
5. Umbraco saves the article as Published (or Scheduled), fires a `ContentPublishedNotification`.
6. The notification handler:
   - Invalidates the output cache for affected URLs (homepage, category archive, the article URL).
   - Triggers an Examine re-index of the article.
   - Updates the XML sitemap (or marks it dirty for regeneration within 60 minutes).
7. The article appears on the public site within the cache staleness window (≤ 60 seconds).
8. If Scheduled: the Umbraco scheduler polls at 1-minute intervals, publishes when `publish_date ≤ now`, and fires the same notification.

### 7.2 View Article Page (Render + Related + Ads + Comments)

1. **Visitor** requests `https://predelnews.com/article-slug/`.
2. IIS routes the request to the ASP.NET Core application.
3. **Output cache** checks for a cached response. If warm (< 60 s old): return cached HTML directly (TTFB ≤ 50 ms).
4. If cache miss: Umbraco resolves the content node, maps it to the `ArticleViewModel`.
   - Loads article fields, author, category, region, tags from Umbraco's in-memory cache (backed by SQL).
   - Queries `pn_comments` for visible (non-deleted, non-held) comments for this article.
   - Runs the related articles algorithm (tag overlap → same category → most recent).
   - Loads the 6 ad slot configurations from `pn_ad_slots`.
5. Razor template renders HTML:
   - If `is_sponsored = true`: injects "Платена публикация" banner above headline and below body (template-enforced).
   - Injects ad slot HTML (AdSense snippet or direct-sold `<img>` + tracking pixel).
   - Emits JSON-LD `NewsArticle` schema, OG/Twitter Card meta tags, canonical `<link>`.
6. HTML is stored in the output cache and returned to the visitor.
7. Browser loads page; AdSense JS and GA4 scripts load asynchronously (do not block LCP).

### 7.3 Post Comment (Anonymous) + Anti-Spam + Store + Display

1. **Visitor** fills in "Име" (display name) and "Коментар" (text) in the comment form and submits.
2. Browser POSTs to `/api/comments` with CSRF token, form fields, and a hidden honeypot field.
3. Server-side pipeline:
   1. **CSRF validation** — reject (403) if token missing/invalid.
   2. **Honeypot check** — if honeypot field is non-empty: silently return 200 (discard, no storage).
   3. **IP rate limit** — check `pn_comments` for IP submissions in the last 5 minutes. If ≥ 3: return 429 with Bulgarian message ("Моля, изчакайте...").
   4. **Input validation** — display name ≤ 50 chars, comment text ≤ 2000 chars; HTML-strip display name; HTML-encode comment for output.
   5. **Link count** — count URLs in comment text. If ≥ 2: store with `is_held = true`; return 200 with informational message.
   6. **Banned word check** — if any banned word matches: store with `is_held = true`.
   7. **Store** — insert row into `pn_comments` with `is_deleted = false`, `is_held = false`, timestamp, IP, article ID.
4. Response triggers a partial page refresh (or full redirect) to render the new comment in the thread.
5. Comment is displayed immediately: display name, HTML-encoded text, timestamp.

### 7.4 Delete Comment (Soft Delete + Audit)

1. **Writer / Editor / Admin** clicks "Изтрий" next to a comment in the CMS or on the article page (for authenticated users viewing the public site with moderation UI).
2. Server validates the acting user's role (server-side check: must be Writer, Editor, or Admin).
3. Application:
   - Sets `pn_comments.is_deleted = true` on the target row (soft delete — row remains in DB).
   - Inserts a row into `pn_comment_audit_log`: `comment_id`, `deleted_by_user_id`, `deleted_at` (UTC), `original_display_name`, `original_text`, `original_ip`, `reason` (optional free text).
4. The comment disappears from the public article page immediately (query filters `is_deleted = false`).
5. The audit log entry is immutable: no UPDATE/DELETE is exposed on the `pn_comment_audit_log` table through any CMS interface. The table enforces this via an application-layer guard (no delete endpoint) and ideally a DB-level `INSTEAD OF DELETE` trigger or revoked DELETE permission for the app's DB user.

### 7.5 Contact Form Submit (Store + Email)

1. **Visitor** fills in name, email, subject, message on `/kontakti/` and submits.
2. Server validates CSRF token, honeypot field, IP rate limit (max 3 per IP per 10 minutes), and field completeness.
3. If valid:
   - Insert row into `pn_contact_submissions`: name, email, subject, message, `submitted_at`, IP.
   - Send email via configured SMTP transactional provider to the CMS-configured recipient address.
   - If SMTP fails: log the error; the submission row in the DB ensures the inquiry is not lost. Return success to visitor (graceful degradation per NFR-RL-005).
4. Visitor sees Bulgarian confirmation message ("Съобщението ви беше изпратено успешно.").

---

## 8. Security Model

### Roles & Permissions

Three CMS roles: **Writer**, **Editor**, **Admin**. All permission checks are server-side enforced in application code — not just hidden in the UI.

| Capability | Writer | Editor | Admin |
|-----------|--------|--------|-------|
| Create/edit own drafts | ✅ | ✅ | ✅ |
| Publish / schedule / unpublish | ❌ | ✅ | ✅ |
| Set `Is Sponsored` flag | ❌ | ❌ | ✅ |
| Manage taxonomy, polls, authors | ❌ | ✅ | ✅ |
| Delete/approve comments | ✅ (delete) | ✅ | ✅ |
| Manage ad slots | ❌ | ❌ | ✅ |
| Manage users/roles, site settings | ❌ | ❌ | ✅ |
| View audit logs | ❌ | ❌ | ✅ |
| View/export email subscribers | ❌ | ✅ | ✅ |

### Public Endpoints (Unauthenticated)

All public endpoints accept anonymous requests and must be defended against abuse:

| Endpoint | Defense |
|---------|---------|
| `GET /*` (article, archive, search pages) | Output cache; no user data exposed |
| `POST /api/comments` | CSRF token, honeypot, IP rate limit (3/5 min), input validation, output encoding |
| `POST /api/poll/vote` | CSRF token, cookie dedup, input validation |
| `POST /api/email-signup` | CSRF token, IP rate limit (5/10 min), email format validation, duplicate check |
| `POST /api/contact` | CSRF token, honeypot, IP rate limit (3/10 min), input validation |

### CMS Backoffice (`/umbraco/`)

- Excluded from `robots.txt`.
- Account lockout after 5 failed login attempts (15-minute lockout).
- Strong password policy: ≥ 12 characters, mixed case, digit, special character.
- Session timeout: 30 minutes of inactivity.
- Session cookies: `Secure`, `HttpOnly`, `SameSite=Lax`.
- No MFA at MVP; planned for Phase 2.

### HTTP Security Headers

All responses include: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy` (camera, microphone, geolocation denied), `Strict-Transport-Security` (1 year + includeSubDomains), and `Content-Security-Policy` (report-only at MVP; enforced in Phase 1.1).

### Data Protection

- No plaintext secrets in source code or `appsettings.json` (use environment variables or Windows DPAPI-protected configuration).
- All database queries via Umbraco ORM or parameterised ADO.NET (no raw SQL string concatenation with user input).
- Comment IP addresses stored in DB but only visible to Editor/Admin in the CMS; never on public pages.

---

## 9. Performance & Caching

### Caching Strategy

| Layer | Mechanism | Staleness | Scope |
|-------|-----------|-----------|-------|
| **Output cache** | ASP.NET Core `OutputCache` middleware | 60 seconds | All public Razor page responses (article, archive, homepage, search). Invalidated on content publish/unpublish. |
| **IIS static file cache** | IIS kernel-mode caching + `Cache-Control: max-age` | 7 days (images, CSS, JS) | `/media/*`, `/css/*`, `/js/*` etc. |
| **Umbraco in-memory cache** | Umbraco's built-in `IPublishedContentCache` | Refreshed on publish event | Published content and media metadata; eliminates per-request SQL reads for content. |
| **Examine / Lucene index** | File-system Lucene index | Updated on article publish/save | Full-text search; no per-query SQL. |

### Image Optimisation

Umbraco's built-in image processing (ImageSharp) serves WebP variants with JPEG/PNG fallback via `srcset`. Three size variants per image (400w, 800w, 1200w). All below-fold images are lazy-loaded (`loading="lazy"`). Maximum served image size: 200 KB per variant. Hero/cover images on article pages are `<link rel="preload">`-ed to improve LCP.

### Third-Party Scripts

AdSense and GA4 scripts are loaded `async` / `defer` and must not block the main thread. If either script fails to load (network timeout, ad blocker), the page renders normally: ad slots collapse gracefully, analytics data is simply lost for that session.

### Future CDN (Phase 2)

When Cloudflare or another CDN is introduced, the IIS static file caching headers will transition to CDN edge caching. The `/media/` path will move to an object storage origin (e.g., Azure Blob or Cloudflare R2). The output cache TTL for Razor pages may be extended, and the CDN will handle most cache invalidation.

---

## 10. Deployment Topology

### Environments

| Environment | Purpose | Notes |
|-------------|---------|-------|
| **Production** | Live site at `predelnews.com` | Single Windows VPS; IIS; SQL Server Express |
| **Staging** | Pre-deployment testing, restore drills | Mirrors production OS, .NET, Umbraco, IIS config. Separate DB instance. |
| **Local (Developer)** | Development and unit testing | Developer's machine; local SQL Server Express or SQLite for lightweight dev. |

### Production Topology (Single VPS)

```
Internet
   │ HTTPS :443
   ▼
IIS (Windows VPS)
   │  Site binding: predelnews.com
   │  TLS: Let's Encrypt certificate (win-acme auto-renewal)
   │  HTTP → HTTPS redirect (301)
   ▼
ASP.NET Core App Pool (.NET 10)
   │
   ├── Umbraco 17 application
   │     └── wwwroot/ (static assets: CSS, JS, favicons)
   │
   ├── D:\PredelNews\media\     (Umbraco media files)
   ├── D:\PredelNews\logs\      (Serilog rotating logs)
   └── D:\PredelNews\data\      (Examine index, Umbraco runtime data)
   │
   ▼
SQL Server Express (same VPS, local TCP connection)
   └── [PredelNewsDB]
```

### Deployment Process (Summary)

1. Developer pushes to `main` branch on Git.
2. Pre-deployment: database backup + media backup (scripted).
3. Deploy application to staging; run smoke tests.
4. Deploy to production: publish `.zip` artifact to IIS web root; app pool recycles.
5. Post-deployment: smoke test; verify Examine index intact; verify cache invalidation.
6. Rollback: restore previous artifact from Git tag + restore DB backup if schema changed.

Full procedure: `docs/technical/deployment.md` (to be authored).

### Backup Schedule

| Asset | Frequency | RPO | Destination |
|-------|-----------|-----|-------------|
| SQL Server DB | Daily automated + before each deployment | 24 h | Off-VPS (separate volume or remote storage) |
| Media files | Daily automated file sync | 24 h | Off-VPS |
| Application code | Always (Git `main`) | Latest commit | Git remote |
| Full VPS snapshot | Weekly | 7 days | Hosting provider snapshot |

Target RTO: ≤ 4 hours from decision to restore. Restore procedure: `docs/technical/backup-and-restore.md`.

---

## 11. Risks & Mitigations

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | **Single VPS failure** (disk, OS crash) | Low | High | Daily off-VPS backups (DB + media). Tested restore procedure. RTO ≤ 4 h. Weekly full snapshot. |
| R2 | **Traffic spike overwhelms VPS** (viral local story) | Medium | Medium | Output caching absorbs most read traffic. Load test for 5× spike (NFR-PF-007). Monitoring alerts (CPU > 80%). Documented scale-up procedure (vertical resize or Phase 2 CDN). |
| R3 | **Comment spam / abuse flood** | High | Medium | Honeypot + IP rate limiting + link count filter + banned word list. Can disable comments per article. reCAPTCHA available as fast-follow (Phase 2) if volume escalates. |
| R4 | **CMS account compromise** | Low–Medium | High | No MFA at MVP — mitigated by strong password policy, brute-force lockout (5 attempts / 15 min), session timeout (30 min), and very small account count (≤ 5 users). MFA planned Phase 2. |
| R5 | **SQL Server Express 10 GB database size limit** | Low (long-term) | Medium | Media binaries are on the file system, not in the DB. Text content (articles, comments) grows slowly. Monitor DB size; alert at 8 GB; migrate to SQL Server Standard when needed. |
| R6 | **Examine index corruption or staleness** | Low | Medium | Examine index is rebuilt from SQL on startup if missing or corrupt. Index files are included in daily backup. Re-index can be triggered manually from the Umbraco backoffice. |
| R7 | **Let's Encrypt certificate expiry** | Low | High | win-acme (or equivalent) auto-renews ≥ 30 days before expiry. Monitor cert expiry via UptimeRobot SSL check. Alert on renewal failure. |
| R8 | **AdSense policy violation** | Medium | High | Ensure all ad slots display "Реклама" label (template-enforced). Never place ads directly adjacent to comment text. Review AdSense content policies before launch. Avoid ad density violations. Monitor AdSense console for warnings. |

---

## 12. Future Evolution

The architecture is designed for MVP simplicity. The following evolutionary steps are planned or recommended after launch.

| Evolution | Phase | Impact on Architecture |
|-----------|-------|----------------------|
| **CDN for static assets and media** (Cloudflare or Cloudflare R2) | Phase 2 | Move `/media/` to object storage. Configure Cloudflare in front of IIS. IIS becomes origin server. Edge caching eliminates most static asset load on VPS. Image `srcset` URLs point to CDN origin. |
| **WAF / DDoS protection** (Cloudflare WAF, free tier) | Phase 2 | Cloudflare sits in front of IIS. Provides bot mitigation, OWASP ruleset, rate limiting at the edge. Application-layer defences remain as defence-in-depth. |
| **MFA for CMS accounts** | Phase 1.1 or Phase 2 | Umbraco's built-in 2FA or a third-party TOTP provider. Required before adding more user accounts or enabling remote access without VPN. |
| **Newsletter integration** (Mailchimp or similar) | Phase 2 | Email subscriber table already populated in Phase 1. Phase 2 adds a Mailchimp API integration (or SFTP export for bulk tools). No architectural change needed to the DB layer. |
| **Full GDPR consent management** | Phase 2 | Replace basic cookie banner with a consent management platform (CMP). Gate GA4 script loading behind consent. Evaluate Cookiebot / CookieYes / open-source alternatives. |
| **Search improvement** (Algolia or Elasticsearch) | Phase 2+ | If Examine search quality degrades as content volume grows (> 5,000 articles), replace with an external provider. Requires an index sync job and a new search controller. The public search UI remains unchanged. |
| **Membership / paywall** (optional) | Phase 3+ | Umbraco's Members subsystem is available but out of scope. If introduced, it requires reader-facing login, GDPR-compliant profile management, and payment integration. |
| **Horizontal scaling** (second VPS + load balancer) | Phase 2+ if traffic > 50K monthly uniques | Requires shared session store (Redis or SQL-backed), shared media mount (or CDN origin as above), and Examine distributed indexing. |
| **Comment threading / replies** | Phase 2 | Add `parent_comment_id` FK to `pn_comments`. Template change to render nested thread (one level). DB schema is already soft-delete and audit-log–compatible. |
| **RSS feeds** | Phase 2 | Per-category and per-region Atom/RSS feeds. Umbraco feed controller; no schema changes. |

---

*End of Architecture. This document should be reviewed by the Solutions Architect, Tech Lead, and Product Manager before development sprint 1 begins. It will be maintained as a living document alongside `docs/technical/technical-specification.md`.*
