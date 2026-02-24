# Development Plan — PredelNews MVP (Phase 1)

**Product:** PredelNews — Regional News Website for Southwest Bulgaria  
**Domain:** predelnews.com  
**Platform:** Umbraco 17 LTS (.NET 10) on Windows VPS (IIS)  
**Document owner:** Tech Lead / Engineering Manager  
**Status:** Draft v1.0  
**Last updated:** 2026-02-24

> **Repository path:** `docs/planning/development-plan.md`  
> Related: `docs/business/prd.md` · `docs/business/functional-specification.md` · `docs/business/non-functional-requirements.md` · `docs/technical/technical-specification.md` · `docs/technical/architecture.md` · `docs/planning/epics/`

---

## Table of Contents

1. [Purpose & Scope](#1-purpose--scope)
2. [Assumptions](#2-assumptions)
3. [Development Approach](#3-development-approach)
4. [High-Level Phases & Milestones](#4-high-level-phases--milestones)
5. [Phase Details](#5-phase-details)
6. [Definition of Done](#6-definition-of-done)
7. [Testing Strategy](#7-testing-strategy)
8. [Quality Gates](#8-quality-gates)
9. [Environments & Release Strategy](#9-environments--release-strategy)
10. [Risk Management](#10-risk-management)
11. [Rollout Plan](#11-rollout-plan)
12. [Post-Launch Plan](#12-post-launch-plan)

---

## 1. Purpose & Scope

This document defines how the PredelNews MVP is built, tested, and shipped. It provides the execution framework — phases, milestones, quality gates, testing cadence, and rollout strategy — without decomposing work into individual tickets. Epic and story breakdowns live in `docs/planning/epics/` and will be derived from this plan.

**In scope:** All Phase 1 / MVP features (PRD §8.1: F1–F13), infrastructure provisioning, content seeding coordination, soft launch, and the first 4 weeks post-launch.

**Out of scope:** Phase 2 features (newsletter sending, comment threading, reCAPTCHA, push notifications), CI/CD pipeline setup (post-launch enhancement), and CDN migration.

---

## 2. Assumptions

The following assumptions were made based on stakeholder answers and reasonable defaults. Flag any that are incorrect before development begins.

| ID | Assumption |
|----|-----------|
| DA1 | **Two developers** work on the project using Claude Code as an assistant. Both can work in parallel on different modules once the foundation is stable. |
| DA2 | **The Editor-in-Chief** is available throughout development for feedback, UAT, and content seeding decisions. Content seeding (20+ articles, footer pages) happens in parallel with development once content types are ready in the CMS. |
| DA3 | **No CI/CD pipeline at MVP.** Deployments are manual (RDP + IIS). CI/CD is a Phase 1.1 / post-launch enhancement. Automated test runs happen locally and on staging via CLI before each deployment. |
| DA4 | **Development starts locally.** Staging VPS is provisioned mid-project (end of Phase 2 or start of Phase 3). Production VPS is provisioned during Phase 5 (Hardening), before launch. |
| DA5 | **Domain (`predelnews.com`)** is acquired early (Phase 0 or Phase 1). DNS configuration happens when the production VPS is provisioned. |
| DA6 | **AdSense and GA4 accounts** may not be ready at development start. The system is built with configurable placeholders. Accounts are set up during/after Phase 5, and verified during soft launch. |
| DA7 | **Brand assets (logo, color palette, typography)** start as developer placeholders and are refined before soft launch. Final brand assets must be ready before public launch. |
| DA8 | **No hard external deadline.** The team ships when quality gates are met. Target: production-ready MVP in approximately 10–13 weeks from Phase 1 start, but quality overrides speed. |
| DA9 | **SQL Server Express** is the database for all environments (local, staging, production). |
| DA10 | **The Umbraco 17 LTS release** and its dependencies (.NET 10, TinyMCE integration, Examine/Lucene) are stable and available. If Umbraco 17 is not yet released at development start, the team uses the latest stable Umbraco LTS (15 or 14) and migrates when 17 ships, or adjusts the plan accordingly. |

---

## 3. Development Approach

### 3.1 Principles

**MVP-first, iterative, thin vertical slices.** Every phase delivers a working, testable increment of the product. No phase produces only backend code without a visible frontend, or only frontend without data persistence. Each phase ends with something a human can interact with.

**Content-type-first, then template, then behavior.** For every feature, the build order is: (1) define the Umbraco document type and data schema, (2) build the Razor template that renders it, (3) add any interactive behavior (JS, API endpoints). This ensures the Editor-in-Chief can start populating content as soon as templates exist, even before all interactive features are complete.

**Pair on foundations, parallelize on modules.** Both developers work together on the Umbraco project setup, content model, and shared infrastructure (layout, routing, caching). Once that foundation is stable, they split: one focuses on public-facing templates and SEO, the other on custom modules (comments, polls, ads, email signup). They merge frequently.

**Test as you build, not after.** Unit tests are written alongside service-layer code. Integration tests are added when a module's API surface is complete. E2E smoke tests are layered on once the first full user journey (publish → view article) is functional. See §7 for details.

### 3.2 Work Rhythm

- **Weekly check-in** with the Editor-in-Chief: demo what's new, collect feedback, adjust priorities.
- **Daily sync between developers:** short standup (15 min) to coordinate, unblock, and avoid merge conflicts.
- **Phase gate review** at the end of each phase: checklist verification against the phase's acceptance criteria before moving on.

### 3.3 Branch Strategy

- `main` — always deployable. Represents the latest verified state.
- `feature/{short-name}` — short-lived feature branches (1–3 days max). Merged to `main` via pull request with peer review.
- No long-lived branches. No `develop` branch. Keep it simple for a two-person team.

---

## 4. High-Level Phases & Milestones

The project is structured into 7 phases. Phases are sequential but overlap is expected (e.g., the editorial team seeds content during Phases 3–5 while developers continue building).

| Phase | Name | Duration (est.) | Key Deliverable |
|-------|------|-----------------|-----------------|
| **0** | Project Setup & Kickoff | 3–4 days | Repository, tooling, domain acquired, dev environments running |
| **1** | Foundation & Content Model | ~2 weeks | Umbraco running locally with all document types, seed taxonomies, article CRUD in backoffice, base layout template |
| **2** | Public Site Core | ~2 weeks | Article page, homepage, archive pages, search — all rendering with real content from the CMS. First deployable vertical slice. |
| **3** | Editorial Workflow & Custom Modules | ~2.5 weeks | Editorial lifecycle (Draft → Review → Publish), comments system, poll widget, email signup, contact form — all functional end-to-end |
| **4** | Monetization, SEO & Polish | ~2 weeks | Ad slots, sponsored content workflow, full SEO plumbing (sitemap, structured data, OG tags), responsive polish, accessibility baseline |
| **5** | Hardening & MVP Freeze | ~1.5 weeks | Security hardening, performance optimization, production VPS provisioned, load testing, backup/restore tested. **All development complete. Code frozen.** |
| — | **MVP FREEZE** | — | All quality gates passed. No new features. Only critical bug fixes allowed past this point. |
| **6** | Content Loading | ~1 week | Editorial team populates production CMS: 20+ articles, footer pages, poll, brand assets. Developers provide support but write no feature code. |
| **7** | Soft Launch | ~1 week | Limited audience (50–100 people). Monitoring, bug triage, final go/no-go. |
| **8** | Public Launch | Day 1 + first 48h | Go-to-market execution, intensive monitoring, on-call. |

**Total estimated duration:** 11–13 weeks from Phase 1 start to MVP Freeze (excluding Phase 0). Content loading and launch add 2–3 weeks of operational activity after the freeze.

```
Week:  0   1   2   3   4   5   6   7   8   9  10  11  12  13  14  15
       ├─0─┤
           ├──── Phase 1 ────┤
                             ├──── Phase 2 ────┤
                                  ├─ Staging VPS ─┤
                                               ├───── Phase 3 ──────┤
                                               │  Editor gets staging CMS access ─┤
                                                                    ├──── Phase 4 ────┤
                                                                                      ├── Phase 5 ──┤
                                                                                      ├─ Prod VPS ──┤
                                                                                                    ▼ MVP FREEZE
                                                                                                    ├─ Phase 6 ─┤
                                                                                                      (content)
                                                                                                                ├ Phase 7 ┤
                                                                                                                (soft lnch)
                                                                                                                          ▲
                                                                                                                   Public Launch
```

---

## 5. Phase Details

### Phase 0 — Project Setup & Kickoff

**Duration:** 3–4 days  
**Goal:** Everything needed to start writing application code is in place.

**Checklist:**
- [ ] Git repository created with the agreed folder structure (`src/`, `docs/`, `CLAUDE.md`)
- [ ] Umbraco 17 LTS project scaffolded (`dotnet new umbraco`) and builds locally for both developers
- [ ] SQL Server Express installed and running locally for both developers
- [ ] Serilog configured with file sink (daily rotation, 30-day retention)
- [ ] `.editorconfig` and code formatting rules agreed and committed
- [ ] `CLAUDE.md` populated with project conventions, naming rules, and coding standards
- [ ] Domain `predelnews.com` acquired (or confirmed acquisition is in progress)
- [ ] PRD, functional spec, NFRs, architecture, and tech spec reviewed by both developers — open questions flagged
- [ ] Placeholder brand assets created (logo placeholder, basic color palette, font choice)
- [ ] Editor-in-Chief briefed on the plan: when they'll get CMS access, when to start writing content, and feedback cadence

**Exit criteria:** Both developers can run the Umbraco backoffice locally at `https://localhost/umbraco/`, create a test content node, and see it persisted in the local SQL Server database.

---

### Phase 1 — Foundation & Content Model

**Duration:** ~2 weeks  
**Goal:** All Umbraco document types, compositions, taxonomies, and the base page layout exist. The CMS is usable for content entry. No public-facing pages yet (beyond the default Umbraco template).

**Scope:**
- Umbraco document types: `Article`, `Category`, `Region`, `newsTag`, `Author`, `HomePage`, `SiteSettings`, `StaticPage`, `ContactPage`, `AllNewsPage` (per tech spec §4)
- Compositions: `SeoComposition`, `PageMetaComposition`
- Content tree structure established (per tech spec §4.10)
- Seed taxonomy data: 8 categories, 5 regions (created via migration or manual CMS setup)
- Custom `ISlugGenerator` service for Cyrillic → Latin transliteration
- `IMigrationPlan` / `PackageMigration` scaffolded: creates all `pn_*` custom tables (`pn_comments`, `pn_comment_audit_log`, `pn_polls`, `pn_poll_options`, `pn_ad_slots`, `pn_email_subscribers`, `pn_contact_submissions`, `pn_audit_log`)
- Ad slot seed data (6 rows in `pn_ad_slots`)
- Base `_Layout.cshtml` with semantic HTML5 structure (`<header>`, `<nav>`, `<main>`, `<footer>`), skip-to-content link, and placeholder CSS
- `SiteSettings` typed model helper (reads singleton node from Umbraco content cache)
- User groups configured: Writer, Editor, Admin — with permission boundaries as specified in FR-UR-001
- `isSponsored` field restriction enforced: property group visibility + `ContentSavingNotification` handler
- TinyMCE toolbar configured for article body (per tech spec §4.2.1)
- Article body cover image validation: alt text required (CMS validator)

**Acceptance criteria:**
- [ ] An Editor can log into the CMS, create an Article with all fields, assign a category/region/tags/author, save, and retrieve it
- [ ] A Writer can create an Article but cannot see or modify the `isSponsored` toggle
- [ ] Slug auto-generation produces correct Latin transliterations (e.g., "Пожар в Благоевград" → `pozhar-v-blagoevgrad`)
- [ ] All `pn_*` tables exist in the database after app startup (migration ran successfully)
- [ ] Seed categories and regions are present in the content tree
- [ ] The base layout renders in a browser with correct semantic structure (verified via browser DevTools)
- [ ] Unit tests pass for: `ISlugGenerator`, `ContentSavingNotification` handler (isSponsored restriction), and slug uniqueness logic

**Testing focus:** Unit tests for the slug generator and content validation handlers. No integration or E2E tests yet.

---

### Phase 2 — Public Site Core

**Duration:** ~2 weeks  
**Goal:** The public website renders real content from the CMS. A visitor can browse the homepage, read an article, navigate archives, and use search. This is the first **deployable vertical slice** — the point at which the staging VPS should be provisioned and a first deployment attempted.

**Scope:**
- Article page template (`Article.cshtml`) with all metadata: headline, dateline, category/region badges, cover image (responsive `srcset`, lazy loading), body, tags, author byline, share buttons (Facebook, Viber, Copy Link)
- Homepage template (`Homepage.cshtml`) with all blocks: Breaking News (curated), National/World headlines, per-category blocks, Latest Articles feed (paginated)
- Homepage curation interface in the CMS (`featuredArticles` picker on `HomePage` document type)
- Article card component (`_ArticleCard.cshtml`) — reusable across homepage, archives, related articles
- Archive pages: Category, Region, Tag, Author, "All News" — all paginated
- Pagination component (`_Pagination.cshtml`) with query-string format (`?page=N`)
- Search implementation: Examine index configured, `SearchSurfaceController`, search results template
- Breadcrumb component (`_Breadcrumb.cshtml`)
- Static page templates: `StaticPage.cshtml`, `ContactPage.cshtml` (form UI only — submission logic in Phase 3)
- Routing: all URL patterns from tech spec §7.1 working
- `robots.txt` controller
- Custom 404 and 500 error pages (Bulgarian text, site navigation, no stack traces)
- CSS: mobile-first responsive layout (320px–1920px), light theme enforced (`color-scheme: light only`)
- Core JS: hamburger nav, copy-link share button

**Acceptance criteria:**
- [ ] A visitor can navigate from the homepage → article → category archive → back to homepage without broken links
- [ ] Article page renders all fields correctly for a test article created in the CMS
- [ ] Homepage Breaking News block displays editorially curated articles; falls back to most recent if curation is empty
- [ ] Category blocks auto-populate; empty categories are hidden
- [ ] Search returns relevant results for a Bulgarian query; empty results show a Bulgarian message
- [ ] All pages render without horizontal scroll at 320px, 768px, and 1920px viewport widths
- [ ] 404 page renders with site branding and Bulgarian text when navigating to a non-existent URL
- [ ] `robots.txt` is accessible and disallows `/umbraco/`
- [ ] Lighthouse Performance score ≥ 80 on mobile for homepage and article page (≥ 90 target deferred to Phase 5 optimization)

**Testing focus:** Unit tests for the related articles algorithm, pagination logic, and search query sanitization. First integration tests: Examine index correctly includes/excludes articles based on publish state. Manual cross-browser smoke test (Chrome Android, Safari iOS, Chrome desktop).

**Milestone event:** First deployment to staging VPS. Validate that the deployment process works, IIS serves the site, and SQL Server connects.

---

### Phase 3 — Editorial Workflow & Custom Modules

**Duration:** ~2.5 weeks  
**Goal:** The editorial lifecycle is complete. All custom interactive modules (comments, polls, email signup, contact form) are functional end-to-end. The Editor-in-Chief can begin real content creation.

**Scope:**
- **Editorial workflow:** Article lifecycle states (Draft → In Review → Published / Scheduled / Unpublished), role-based transitions, `ContentSavingNotification` enforcement, scheduled publish via Umbraco scheduler, "Updated" dateline on post-publication edits, article preview for unpublished content
- **Editorial dashboard:** CMS dashboard showing articles in review, published today/this week, recent comments, held comments count, email signup count
- **Comments module:** Full anti-spam pipeline (CSRF → honeypot → rate limit → input validation → link count → banned word check), comment storage, public rendering (chronological, flat), display name cookie persistence, comment count on article cards
- **Comment moderation:** Soft delete with audit log, held comments list in CMS, approve/delete actions, delete button on public article page for authenticated CMS users
- **Poll module:** Poll CRUD in CMS, single active poll enforcement (DB constraint + service logic), public poll widget with vote endpoint, cookie-based deduplication, results display
- **Email signup module:** Signup form on homepage, duplicate handling, CSV export endpoint for Editor/Admin
- **Contact form module:** Form submission with honeypot + rate limiting, SMTP delivery, DB persistence, graceful degradation on SMTP failure
- **Rate limiting:** ASP.NET Core `RateLimiter` middleware configured for all form endpoints (per tech spec §8.4)
- **CSRF protection:** Anti-forgery tokens on all state-changing forms and API endpoints
- **Security headers middleware:** `SecurityHeadersMiddleware` with all required headers (CSP in report-only mode)

**Acceptance criteria:**
- [ ] A Writer can create a draft, submit for review; an Editor sees it in the dashboard, can edit and publish; the article appears on the public site within 60 seconds
- [ ] A scheduled article auto-publishes at the set time (± 1 minute)
- [ ] An Editor can unpublish an article; it disappears from the public site
- [ ] A visitor can submit an anonymous comment; it appears immediately on the article page
- [ ] The 4th comment from the same IP within 5 minutes is rejected with a Bulgarian rate-limit message
- [ ] A comment with the honeypot field filled is silently discarded (HTTP 200, no storage)
- [ ] A comment with ≥ 2 URLs is held for review; the visitor sees an informational message
- [ ] An Editor can approve or delete a held comment from the CMS
- [ ] Deleting a comment creates an audit log entry; the original text is preserved in `pn_comment_audit_log`
- [ ] The poll widget displays on the homepage; a vote is recorded; results are shown; a repeat vote from the same browser is prevented
- [ ] Email signup stores the email with consent flag; duplicates are handled gracefully; Admin can export CSV
- [ ] Contact form submission is stored in the DB and emailed to the configured recipient
- [ ] All API endpoints return HTTP 403 when CSRF token is missing
- [ ] Security headers are present on all responses (verified via browser DevTools)

**Testing focus:** Unit tests for all service-layer logic: anti-spam pipeline steps, rate limiting behavior, poll vote deduplication, email duplicate handling, audit log creation. Integration tests for: comment submission end-to-end (POST → DB → public page), poll vote flow, contact form submission + SMTP mock. First E2E smoke test: the "publish article → view on public site → post comment" journey.

**Milestone event:** Editor-in-Chief gets CMS access on the staging environment. Content seeding begins in parallel with remaining development.

---

### Phase 4 — Monetization, SEO & Polish

**Duration:** ~2 weeks  
**Goal:** Ad slots render correctly, sponsored content workflow is complete, SEO plumbing is in place, and the site is visually polished and accessible.

**Scope:**
- **Ad slots:** `IAdSlotService` implementation, `_AdSlot.cshtml` partial with AdSense/direct-sold rendering logic, ad slot management section in CMS (Admin only), "Реклама" label on all slots, direct-sold date-range auto-revert, sidebar slot hiding on mobile
- **Sponsored content:** "Платена публикация" banners (top + bottom of article, template-enforced), sponsored badge on article cards, `SponsoredLinkRewriter` for `rel="sponsored noopener"` on external links, sponsor name display
- **SEO:** XML sitemap controller (`/sitemap.xml`), canonical URL generation (`_SeoMeta.cshtml`), JSON-LD structured data (`NewsArticle`, `BreadcrumbList`), OG and Twitter Card meta tags, SEO meta title/description templates for all page types, favicon and web app manifest
- **Cookie consent banner:** Basic Bulgarian-language banner with "Приемам" button, cookie persistence, non-intrusive positioning
- **Accessibility baseline (NFR-AC-001):** Keyboard navigation, form labels, focus indicators, color contrast (≥ 4.5:1 normal text), alt text enforcement, heading hierarchy audit, skip-to-content link verification
- **Responsive polish:** Final pass on all page types at 320px, 375px, 768px, 1024px, 1440px, 1920px. Touch targets ≥ 44×44px on mobile. No horizontal overflow at any width.
- **Related articles:** Algorithm implementation (tag overlap → same category → most recent) + manual override via `relatedArticlesOverride` picker
- **Audit log viewer:** CMS backoffice section for Admin to view audit log entries, filterable by event type and date range
- **GA4 integration:** Configurable tracking ID in Site Settings; script loads on all public pages when ID is present

**Acceptance criteria:**
- [ ] All 6 ad slots render in correct positions on homepage and article page
- [ ] Admin can switch a slot to direct-sold mode with image, URL, and date range; the banner displays; after the end date, the slot reverts to AdSense
- [ ] All ad slots display the "Реклама" label (verified by DOM inspection)
- [ ] A sponsored article displays "Платена публикация" at top and bottom of the article page, and on every card instance
- [ ] External links in a sponsored article body have `rel="sponsored noopener"` (verified by page source inspection)
- [ ] `/sitemap.xml` is valid, includes all published articles and taxonomy pages, and updates within 60 minutes of a new publish
- [ ] Google Rich Results Test passes with zero errors for a sample article page (NewsArticle + BreadcrumbList)
- [ ] Facebook Sharing Debugger shows correct OG title, description, and image for a shared article URL
- [ ] axe-core audit on homepage, article page, and contact page returns zero critical or serious violations
- [ ] Keyboard-only navigation through all primary flows completes without traps
- [ ] Cookie consent banner appears on first visit, dismisses on click, and does not reappear
- [ ] Lighthouse Accessibility score ≥ 85 on mobile

**Testing focus:** Unit tests for `SponsoredLinkRewriter`, ad slot rendering logic (date-range checks, mode selection), related articles algorithm, sitemap URL generation. Integration tests for: ad slot CRUD + rendering, sponsored article end-to-end (create → publish → verify labels and link rewriting). Automated axe-core accessibility scan integrated into the test suite (run against rendered HTML of key pages). SEO validation tests: sitemap schema validation, canonical URL correctness checks, JSON-LD structure validation.

---

### Phase 5 — Hardening & MVP Freeze

**Duration:** ~1.5 weeks  
**Goal:** The system is production-ready: secure, performant, monitored, backed up, and deployable to production. All NFR "Must" items are verified. At the end of this phase, **the code is frozen**. No new features or non-critical changes are permitted past this point.

**Scope:**
- **Production VPS provisioning:** Windows Server, IIS, .NET 10 hosting bundle, SQL Server Express, file system layout (`D:\PredelNews\...`), Let's Encrypt certificate via win-acme
- **HTTPS enforcement:** HTTP → HTTPS 301 redirect, HSTS header, SSL Labs grade ≥ A
- **Secrets management:** All credentials moved to Windows environment variables; zero secrets in `appsettings.json` or source code
- **CMS hardening:** Account lockout (5 attempts / 15 min), strong password policy, session timeout (30 min), session cookie attributes (`Secure`, `HttpOnly`, `SameSite=Lax`)
- **Output caching:** ASP.NET Core `OutputCache` configured (60s staleness), cache invalidation on publish, backoffice excluded
- **Image optimization verification:** WebP serving, `srcset` presence, lazy loading, ≤ 200 KB per variant, cover image preload
- **Performance optimization:** Critical CSS review, JS bundle size check (≤ 50 KB compressed), third-party script async/defer, ad slot fixed-height CSS (CLS prevention)
- **Load testing:** Simulate 5× baseline concurrent users for 30 minutes (k6 or Artillery); verify zero 503s, TTFB ≤ 1500ms at 95th percentile, comment/form submission functional during load
- **Backup setup:** Daily automated DB backup + media file backup to off-VPS storage; backup failure alerting; one full restore drill on staging
- **Monitoring setup:** UptimeRobot (or equivalent) for homepage HTTP check every 5 minutes; PowerShell-based infrastructure monitoring (CPU, memory, disk alerts); log accessibility endpoint for Admin
- **Privacy policy page:** Content prepared (PM + Legal); page template is already built — content is entered during this phase or Phase 6
- **Deployment documentation:** `docs/technical/deployment.md` written and tested; rollback procedure verified on staging
- **Final security scan:** OWASP ZAP scan against all public pages and form endpoints; XSS payload test in comments and search; CSRF token validation test; `appsettings.json` not accessible via browser
- **Final test suite run:** All unit, integration, and E2E tests pass. Quality gates from §8 are verified for all phases.

**Acceptance criteria:**
- [ ] Lighthouse Performance score ≥ 90 on mobile for homepage, article page, and one archive page (with ad slots loaded)
- [ ] Core Web Vitals pass: LCP ≤ 2.5s, INP ≤ 200ms, CLS ≤ 0.1 on mobile simulation
- [ ] TTFB ≤ 600ms (95th percentile) under normal conditions on staging
- [ ] Load test passes: zero 503s at 5× concurrent users for 30 minutes
- [ ] SSL Labs grade ≥ A for `predelnews.com`
- [ ] All security headers present (verified via securityheaders.com — target grade A)
- [ ] OWASP ZAP scan returns zero high-severity findings
- [ ] `<script>alert('xss')</script>` in comment or search query is HTML-encoded on output, not executed
- [ ] Database backup runs successfully to off-VPS storage; restore drill completes on staging within 4 hours
- [ ] UptimeRobot sends a downtime alert within 10 minutes of IIS stop
- [ ] Deployment to production VPS succeeds; homepage returns HTTP 200; CMS login works
- [ ] Rollback procedure tested: previous version restored and functional on staging
- [ ] No secrets in source code (verified via repository grep)
- [ ] Functional spec Definition of Done (FS §13) is fully satisfied
- [ ] NFR Definition of Done (NFR §6) is fully satisfied for all "Must" items
- [ ] All automated tests pass (unit, integration, E2E)
- [ ] Both developers and PM sign off on the MVP Freeze

**Testing focus:** Full NFR verification — run the Measurement & Verification Matrix from `non-functional-requirements.md` §5 for all "Must" items. Load test execution and analysis. Security scan review and remediation. Restore drill with measured RTO.

---

### ── MVP FREEZE ──

**This is a hard gate, not a phase.** At this point:

- All development work is complete. The `main` branch is tagged (e.g., `v1.0.0-mvp`).
- No new features, refactors, or non-critical changes are permitted.
- The only code changes allowed past this point are **critical bug fixes** — defined as: site is down, data loss, security vulnerability, or a user flow is completely broken. Each fix requires peer review and a re-run of the full test suite before deployment.
- The production environment is fully provisioned and verified (deployment tested, backups running, monitoring active).
- The system is ready to receive content.

**Why a hard freeze matters:** Mixing feature development with content loading and launch operations creates unpredictable risk. A frozen codebase means the editorial team can populate content with confidence that the platform beneath them won't shift. It also means that any issue found during content loading or soft launch is either a genuine bug (fix it) or a feature request (backlog it for post-launch).

---

### Phase 6 — Content Loading

**Duration:** ~1 week  
**Goal:** The production site is populated with real content and fully configured for launch. This is an **editorial operations phase**, not a development phase. Developers provide CMS support and troubleshooting but write no feature code.

**Scope:**
- **CMS accounts:** Editor-in-Chief and Writers receive production CMS accounts
- **Content creation:** ≥ 20 articles published across ≥ 3 categories and ≥ 2 regions. Articles have real headlines, body text, cover images with alt text, correct categories/regions/tags, and author bylines.
- **Footer pages:** All 5 populated with real content (За нас, Реклама, Рекламна оферта, Контакти, Всички новини)
- **Privacy policy:** Published at `/politika-za-poveritelnost/` and linked from footer and cookie banner
- **Homepage curation:** Editor-in-Chief configures the Breaking News block with featured articles
- **Poll:** At least 1 active poll created
- **Brand assets:** Production logo, color palette, and typography applied; all placeholder assets removed
- **AdSense:** Account approved and ad slot codes configured (or: application submitted and slots collapse gracefully pending approval)
- **GA4:** Measurement ID configured in CMS Site Settings
- **Site Settings:** Contact recipient email, social links, default SEO description — all configured with real values
- **Content QA:** Developers spot-check 5–10 published articles for rendering correctness (images load, categories link correctly, OG preview looks right). Editor-in-Chief reviews homepage layout and content presentation.

**Who does what:**

| Activity | Owner |
|----------|-------|
| Write and publish articles | Editor-in-Chief + Writers |
| Write footer pages, privacy policy | Editor-in-Chief + PM |
| Configure homepage curation, poll | Editor-in-Chief |
| Apply final brand assets (logo, CSS colors) | Developer (one-time commit — the only code change allowed, treated as a configuration change) |
| Configure AdSense codes, GA4 ID, Site Settings | Developer or Admin |
| Spot-check rendered content, troubleshoot CMS issues | Developer |

**Acceptance criteria:**
- [ ] ≥ 20 articles published with real content, proper images (with alt text), correct categories/regions/tags
- [ ] All 5 footer pages have real content (not placeholder text)
- [ ] Privacy policy is published and linked from footer and cookie banner
- [ ] At least 1 poll is active and functional
- [ ] Production logo and brand colors are applied across all pages (no placeholders)
- [ ] GA4 tracking fires on every page (verified in GA4 real-time report)
- [ ] AdSense ads load on at least one slot (or: application submitted; slots configured to render when approved)
- [ ] Site Settings fully configured (contact email, social links, SEO defaults)
- [ ] Homepage Breaking News block is editorially curated with real articles
- [ ] `/sitemap.xml` includes all published content
- [ ] Editor-in-Chief confirms content is ready

**Exit criteria:** The production site looks and feels like a real news website, not a development prototype. Content is real, branding is final, and the site is ready for external visitors.

---

### Phase 7 — Soft Launch

**Duration:** ~1 week  
**Goal:** Validate the production site with a small real audience. Identify bugs, UX issues, and content problems before public exposure. Confirm the system is stable under real-world conditions.

**Audience:** 50–100 people — editorial team's social circles, local contacts, trusted community members.

**Distribution:** Direct links shared via Facebook Messenger, Viber groups, and email. No public social media announcement. No SEO indexing push yet (`noindex` meta tag is optional at this stage — team's call).

**What is validated:**
- All user journeys work on real devices (not just developer machines)
- Comments system handles real-world input (Bulgarian text, emoji, edge cases)
- Poll voting works across different browsers and devices
- Email signup and contact form work end-to-end
- Ad rendering is correct (no layout breakage, "Реклама" labels visible)
- Page load speed is acceptable on mobile over real 4G connections
- GA4 is tracking correctly (real-time dashboard shows activity)
- UptimeRobot is detecting the site as up
- No unexpected errors in Serilog logs

**Daily monitoring routine (both developers):**
- [ ] Check UptimeRobot dashboard — any downtime incidents?
- [ ] Review Serilog error logs (`Error` and `Warning` level)
- [ ] Check GA4 real-time for active users and any anomalies
- [ ] Review recent comments for spam patterns
- [ ] Check AdSense console for policy warnings (if active)
- [ ] Ask Editor-in-Chief for feedback and reported issues

**Bug triage rules (strict — no scope creep):**

| Severity | Definition | Action |
|----------|-----------|--------|
| **Critical** | Site is down, data loss, security vulnerability, payment/ad system broken | Fix immediately. Deploy same day. Full test suite re-run before deploy. |
| **High** | A primary user flow is broken (can't read article, can't comment, can't search) or major layout breakage on a common device | Fix before public launch. |
| **Medium** | Minor UX issue, cosmetic bug, edge case, non-blocking annoyance | Log in backlog. Fix post-launch unless trivial. |
| **Low** | Nice-to-have improvement, feature request, "wouldn't it be nice if..." | Log in backlog. Explicitly **not** addressed before public launch. |

**Acceptance criteria (go/no-go for public launch):**
- [ ] Soft launch has run for at least 5 days
- [ ] Zero critical or high-severity bugs remain open
- [ ] At least 10 unique visitors have used the site (GA4 verified)
- [ ] At least 3 comments have been submitted successfully by real users
- [ ] No spam incidents that overwhelmed moderation
- [ ] Uptime has been ≥ 99% during the soft launch period (UptimeRobot report)
- [ ] No unresolved Serilog errors that indicate systemic issues
- [ ] Editor-in-Chief confirms the site is ready for public audience
- [ ] PM and Tech Lead sign off on go/no-go checklist

---

### Phase 8 — Public Launch

**Duration:** Day 1 + first 48 hours of intensive monitoring  
**Goal:** The site is publicly announced and available to the target audience. The team monitors intensively to catch any issues that only appear at scale.

**Launch day activities:**
- Social media announcement (Facebook page, relevant Blagoevgrad/Southwest Bulgaria community groups)
- Submit XML sitemap to Google Search Console
- Submit site to Bing Webmaster Tools
- Verify `robots.txt` allows crawling (remove any temporary `noindex` tags if used during soft launch)
- Send direct invitations to local business contacts (potential advertisers)
- Take a database backup immediately after launch (capture the "launch state")

**First 48-hour monitoring (on-call rotation between both developers):**
- Error logs reviewed every 4 hours
- UptimeRobot alerts forwarded to phone (SMS or Telegram)
- GA4 real-time monitored for traffic patterns and anomalies
- Comment section checked for spam every 2–4 hours
- Server resource usage reviewed twice daily (CPU, memory, disk)
- AdSense console checked for policy warnings daily
- Rollback plan is ready (DB backup from launch morning, previous deployment archive tagged in Git)

**Escalation protocol:**
- **Site down:** Both developers alerted immediately. Diagnose within 30 minutes. If not resolvable: rollback to last known good state.
- **Performance degradation (TTFB > 3s sustained):** Check output cache health, DB connections, IIS app pool state. If needed: recycle app pool, review error logs.
- **Spam flood:** If > 20 spam comments/hour: enable banned word additions, consider disabling comments temporarily on high-traffic articles. Track as input for reCAPTCHA prioritization.

**Post-launch announcement complete when:**
- [ ] 48 hours have passed since public launch
- [ ] No critical incidents occurred (or all were resolved)
- [ ] Uptime ≥ 99% during the 48-hour window
- [ ] GA4 shows real organic traffic arriving
- [ ] Google Search Console shows the sitemap was processed
- [ ] Team transitions from "launch mode" to "normal operations mode" (see §12)

---

## 6. Definition of Done

### 6.1 DoD for a Feature

A feature is "done" when:

1. Code is committed to `main`, peer-reviewed (pull request approved by the other developer), and builds without errors or warnings.
2. Unit tests cover service-layer logic; all tests pass.
3. Integration tests cover the feature's API surface (where applicable); all tests pass.
4. The feature is manually verified against its acceptance criteria (from this plan or the functional spec).
5. The feature renders correctly on Chrome Android (mobile) and Chrome desktop at minimum.
6. All visitor-facing text is in Bulgarian (no English placeholder strings).
7. No unresolved `// TODO` comments related to the feature remain in the code.

### 6.2 DoD for a Phase/Milestone

A phase is "done" when:

1. All features scoped to the phase meet the feature DoD.
2. The phase's acceptance criteria checklist (in §5) is fully checked off.
3. The quality gates for the phase (in §8) pass.
4. A demo has been given to the Editor-in-Chief (if applicable) and feedback has been addressed or triaged.
5. The `main` branch is in a deployable state.

### 6.3 DoD for MVP Freeze (end of Phase 5)

The MVP code is frozen when:

1. All development phases (0–5) are complete.
2. The functional spec's Definition of Done (FS §13) is fully satisfied.
3. The NFR's Definition of Done (NFR §6) is fully satisfied for all "Must" items.
4. All automated tests pass (unit, integration, E2E).
5. Both developers and PM sign off. The `main` branch is tagged `v1.0.0-mvp`.

After the freeze, only critical bug fixes are allowed (see MVP Freeze gate in §5).

### 6.4 DoD for Public Launch (end of Phase 7 → Phase 8)

The site is ready for public launch when:

1. MVP code is frozen (6.3 satisfied).
2. Content loading is complete (Phase 6 acceptance criteria met).
3. Soft launch has run for ≥ 5 days with zero critical/high bugs remaining.
4. Go/no-go checklist (Phase 7) is signed off by PM, Editor-in-Chief, and Tech Lead.

---

## 7. Testing Strategy

### 7.1 Test Pyramid

The project follows a pragmatic test pyramid appropriate for a two-person team building an MVP:

```
          ┌─────────┐
          │  E2E /  │    ~5–10 critical path smoke tests
          │  Smoke  │    (Playwright or manual script)
          ├─────────┤
          │ Integr- │    ~20–30 tests
          │  ation  │    (API endpoint + DB verification)
          ├─────────┤
          │  Unit   │    ~50–80 tests
          │  Tests  │    (services, utilities, validators)
          └─────────┘
```

The emphasis is on unit tests for business logic and integration tests for API correctness. E2E tests are limited to critical paths to avoid maintenance overhead on a small team.

### 7.2 Unit Tests

**What to test:** Service-layer logic, utility functions, validators — anything with business rules or conditional logic.

**When to write:** With every feature, as part of the feature DoD. A pull request that introduces a new service method without a corresponding test should be flagged in review.

**Key test targets:**

| Module | Unit Test Targets |
|--------|------------------|
| Slug Generator | Cyrillic→Latin transliteration, special character handling, hyphen collapsing, edge cases (empty string, all-special-chars) |
| Comments Anti-Spam | Each pipeline step in isolation: honeypot check, rate limit logic, link count, banned word match |
| Sponsored Link Rewriter | External vs. internal link detection, `rel` attribute insertion, edge cases (malformed HTML, no links) |
| Related Articles | Tag overlap ranking, category fallback, most-recent fallback, current article exclusion, manual override precedence |
| Poll Service | Single-active enforcement, vote count increment, closed poll rejection |
| Email Signup | Duplicate detection, consent flag handling |
| Pagination | Page count calculation, boundary conditions (page 0, page beyond range) |
| Ad Slot Service | Date-range active check, mode selection (direct vs. AdSense), auto-revert logic |
| Input Sanitizer | HTML stripping, encoding, length truncation |
| Sitemap Builder | URL generation, unpublished content exclusion, `lastmod` accuracy |

**Framework:** xUnit (standard for .NET). Mocking: NSubstitute or Moq.

**Minimum coverage goal:** No numeric coverage percentage is mandated. Instead, the rule is: every service method with a conditional branch or business rule has at least one test per logical path. Coverage tools (e.g., `dotnet-coverage`) may be used for visibility but are not a gate at MVP.

### 7.3 Integration Tests

**What to test:** API endpoints end-to-end (HTTP request → controller → service → DB → response), including CSRF validation, rate limiting behavior, and data persistence.

**When to write:** After each module's API surface is complete (typically at the end of a module's development within a phase). Integration tests are not required per-story, but per-module.

**Timing by phase:**

| Phase | Integration Tests Added |
|-------|----------------------|
| Phase 2 | Examine index (publish → indexed → searchable; unpublish → removed). Search controller returns correct results. |
| Phase 3 | Comment submission flow (POST → DB row → visible on page). Rate limiting (4th request returns 429). Honeypot (filled field → 200, no DB row). Poll vote flow. Email signup (duplicate handling). Contact form (submission stored + SMTP mock called). CSRF rejection (missing token → 403). |
| Phase 4 | Ad slot CRUD + rendering. Sponsored article label presence in rendered HTML. Sitemap content and schema validation. JSON-LD validation. Canonical URL correctness. |

**Framework:** xUnit + `WebApplicationFactory<Program>` (ASP.NET Core integration testing). Test database: SQL Server Express LocalDB (or a dedicated test database) — reset between test runs.

**Key pattern:** Integration tests use the real application pipeline (middleware, DI, routing) with a test database. External services (SMTP) are replaced with mocks/fakes registered in the test DI container.

### 7.4 E2E / Smoke Tests

**What to test:** Critical user journeys that cross multiple modules and verify the system works as a whole from the browser's perspective.

**When to create:** Starting in Phase 3, once the first full user journey (publish → view → comment) is functional. Expanded in Phases 4–5.

**Critical paths to cover first (in priority order):**

1. **Publish → View:** Editor publishes an article → visitor navigates to the article URL → page renders with correct content, metadata, and layout
2. **Comment flow:** Visitor submits a comment on an article → comment appears on the page → CMS user deletes it → comment disappears
3. **Homepage rendering:** Homepage loads with curated breaking news, category blocks, and latest articles
4. **Search:** Visitor searches for a term → results page shows matching articles
5. **Contact form:** Visitor submits the contact form → success message displayed
6. **Sponsored article:** Sponsored article displays correct labels in all contexts

**Framework:** Playwright (.NET) recommended. If Playwright setup is too costly for MVP, a documented manual smoke test script (checklist) is acceptable — but automate at least paths 1–3 before launch.

**Cadence:** Run E2E smoke tests before every deployment to staging or production. In practice this means: before each phase-gate deployment and before the soft launch deployment.

### 7.5 Manual Testing

Automated tests do not replace manual verification. The following manual testing is required:

| Activity | When | Who |
|----------|------|-----|
| Cross-browser testing (Chrome Android, Safari iOS, Chrome/Edge desktop, Samsung Internet) | End of Phase 2, end of Phase 4, pre-launch | Developer |
| Responsive spot-check (resize browser 320→1920px) | Every phase gate | Developer |
| Accessibility keyboard navigation walkthrough | End of Phase 4 | Developer |
| Editor-in-Chief UAT (content creation, publishing, moderation) | Phase 3 onwards (weekly) | Editor-in-Chief |
| Load test execution and analysis | Phase 5 | Developer |
| Security scan (OWASP ZAP) and remediation | Phase 5 | Developer |

---

## 8. Quality Gates

Each phase has a set of automated and manual checks that must pass before the phase is considered complete. These gates are cumulative — later phases include all previous gates.

### Gate: Build & Lint (all phases)

- [ ] Solution builds with zero errors and zero warnings (treat warnings as errors in release config)
- [ ] No `.cs` files with unresolved `// HACK` or `// FIXME` markers on `main`
- [ ] Code formatting matches `.editorconfig` rules (verified by `dotnet format --verify-no-changes`)

### Gate: Tests (Phase 1 onwards)

- [ ] All unit tests pass (`dotnet test`)
- [ ] All integration tests pass (Phase 2 onwards)
- [ ] E2E smoke tests pass (Phase 3 onwards, if automated; otherwise manual checklist completed)

### Gate: Security Basics (Phase 3 onwards)

- [ ] CSRF tokens present on all state-changing forms (verified by integration test)
- [ ] Rate limiting active on all form endpoints (verified by integration test)
- [ ] No `@Html.Raw()` on user-supplied content (code review check; `@Html.Raw()` used only for admin-configured AdSense code)
- [ ] Security headers present on responses (verified by integration test or curl)

### Gate: SEO (Phase 4 onwards)

- [ ] `/sitemap.xml` is valid and includes all published content
- [ ] `/robots.txt` is correctly configured
- [ ] Canonical URLs present on all page types (automated crawl or integration test)
- [ ] JSON-LD passes Google Rich Results Test for sample pages
- [ ] OG tags produce correct Facebook preview (manual check via Sharing Debugger)

### Gate: Performance (Phase 5)

- [ ] Lighthouse Performance ≥ 90 on mobile for homepage, article page, one archive page
- [ ] Core Web Vitals "Good" on mobile simulation
- [ ] TTFB ≤ 600ms (95th percentile) on staging
- [ ] Load test: zero 503s at 5× concurrent for 30 minutes

### Gate: Accessibility (Phase 4 onwards)

- [ ] axe-core: zero critical/serious violations on homepage, article, contact, and search pages
- [ ] Keyboard navigation completes all primary flows without traps

### Gate: Privacy & Compliance (Phase 5)

- [ ] Privacy policy page published and linked from footer + cookie banner
- [ ] Cookie consent banner functional
- [ ] No personal data fields beyond the documented minimum (schema review)
- [ ] Data retention policies documented

---

## 9. Environments & Release Strategy

### 9.1 Environments

| Environment | When Provisioned | Purpose | Database | Media |
|-------------|-----------------|---------|----------|-------|
| **Local (dev)** | Phase 0 | Feature development, unit/integration tests | SQL Server Express (local) | Local file system |
| **Staging** | End of Phase 2 | Integration testing, deployment validation, Editor UAT, load testing, restore drills | SQL Server Express (staging VPS) | Staging VPS file system |
| **Production** | Phase 5 | Live site | SQL Server Express (production VPS) | Production VPS file system |

### 9.2 Environment Parity

Staging and production must match on: Windows Server version, IIS version, .NET 10 hosting bundle version, Umbraco version, and SQL Server Express version. Content data does not need to match, but schema and configuration must be identical.

### 9.3 Content Seeding Strategy

| Content Type | Who Creates | Where | When |
|--------------|------------|-------|------|
| Test/placeholder articles (for development) | Developers | Local, then staging | Phases 1–4 |
| Real launch articles (20+) | Editorial team | Staging CMS (once available in Phase 3) | Phases 3–6 |
| Footer pages (За нас, Реклама, etc.) | Editorial team + PM | Staging CMS | Phases 4–6 |
| Privacy policy | PM + Legal | Staging CMS | Phase 5 |
| Poll (at least 1) | Editor-in-Chief | Staging CMS | Phase 6 |

Content created on staging is migrated to production via Umbraco's content export/import or manual re-creation. The database is **not** cloned from staging to production — content is treated as data that the editorial team manages.

### 9.4 Feature Flags

No feature flag system is implemented at MVP. Features are developed on short-lived branches and merged to `main` when complete. If a feature needs to be hidden temporarily (e.g., ad slots before AdSense is approved), it is controlled via CMS configuration (e.g., empty AdSense code = slot collapses) rather than code-level flags.

### 9.5 Release Strategy

**Staging deployments:** After each phase gate is passed. Both developers can deploy to staging.

**Production deployments:** Manual, following the documented deployment procedure (`docs/technical/deployment.md`). Pre-deployment checklist: DB backup + media backup + staging verification. Deployments are scheduled during low-traffic hours (02:00–06:00 EET) for the initial launch and any risky updates.

**Rollback:** Restore previous published application from a tagged archive + restore DB backup if the schema changed. Rollback procedure is tested on staging during Phase 5.

---

## 10. Risk Management

| # | Risk | Likelihood | Impact | Mitigation | Phase to Address |
|---|------|-----------|--------|------------|-----------------|
| R1 | **Comment spam overwhelms the site at launch** | High | Medium | Honeypot + rate limiting + link filter + banned words are the first line. Monitor spam volume during soft launch. If it escalates, disable comments on specific articles or implement reCAPTCHA as an emergency fast-follow. | Phase 3 (build), Phase 6 (monitor) |
| R2 | **Performance misses Lighthouse ≥ 90 target with ads loaded** | Medium | High | AdSense scripts are the biggest risk to INP and CLS. Mitigate with: ad slot fixed-height CSS (prevents CLS), async/defer loading, no ads above the primary LCP element. Test with real AdSense code on staging. If target is unachievable with ads, document the trade-off and target ≥ 85 with ads. | Phase 4–5 |
| R3 | **Deployment to production fails or causes downtime** | Medium | High | Test the full deployment process on staging multiple times before production. Document rollback procedure. Take DB + media backup immediately before deployment. Deploy during off-peak hours. | Phase 5 |
| R4 | **Data loss due to VPS failure or accidental deletion** | Low | Critical | Daily automated backups to off-VPS storage. Weekly full VPS snapshot. Restore drill on staging before launch. Backup failure alerting. | Phase 5 |
| R5 | **SEO penalties from misconfigured structured data, canonicals, or sponsored content** | Low | High | Validate JSON-LD via Rich Results Test. Automated canonical URL checks. Verify `rel="sponsored"` on all outbound links in sponsored articles. Review Google's content policies before launch. | Phase 4 |
| R6 | **Umbraco 17 LTS not yet available** | Medium | Medium | If Umbraco 17 is not released, start with the latest stable LTS (15 or 14). The architecture and document types are compatible. Migration to 17 can happen during or after MVP. | Phase 0 (decision point) |
| R7 | **AdSense approval takes longer than development timeline** | Medium | Low | The site is built to function without AdSense. Ad slots collapse gracefully when no code is configured. Revenue starts later, but the site is fully functional. Submit AdSense application as early as possible (even with placeholder content on the domain). | Phase 4–6 |
| R8 | **Editor-in-Chief bottleneck — content not ready for launch** | Medium | Medium | Content seeding starts in Phase 3 (as soon as CMS is usable on staging). Weekly check-ins track progress. If content is behind, delay soft launch rather than launching with insufficient content. | Phase 3–6 |
| R9 | **Security vulnerability in custom code (XSS, SQL injection)** | Low | High | All user input is parameterized or ORM-based (no raw SQL). All output is HTML-encoded by Razor. OWASP ZAP scan in Phase 5. Code review for every PR. | Phase 3, Phase 5 |
| R10 | **Scope creep during development** | Medium | Medium | This plan defines explicit phase scope and acceptance criteria. Any new request is evaluated against the MVP scope (PRD §8.1). If it's not in Phase 1, it goes to the backlog. Weekly check-ins with the Editor-in-Chief prevent surprise requirements. | All phases |

---

## 11. Rollout Plan

The launch is a three-stage post-freeze process. Each stage has its own phase with detailed acceptance criteria in §5.

### 11.1 Overview

```
MVP FREEZE (Phase 5 complete)
    │
    ▼
Phase 6: Content Loading (~1 week)
    │  Editorial team populates production CMS
    │  Developers troubleshoot, no feature code
    │  Exit: content ready, brand assets final
    ▼
Phase 7: Soft Launch (~1 week)
    │  50–100 invited users
    │  Daily monitoring routine
    │  Bug triage (critical/high only)
    │  Exit: go/no-go checklist signed off
    ▼
Phase 8: Public Launch (Day 1 + 48h)
    │  Social media announcement
    │  Sitemap submitted to Google/Bing
    │  48-hour intensive monitoring
    │  Exit: stable, transition to normal ops
    ▼
Post-Launch (§12)
```

### 11.2 Key Principle: No Feature Work During Launch

The separation between development (Phases 0–5) and launch (Phases 6–8) is intentional and strict. During content loading and soft launch:

- **Allowed:** Critical bug fixes (site down, data loss, security vulnerability, broken primary user flow). Each fix goes through peer review + full test suite re-run.
- **Not allowed:** New features, refactors, "quick improvements," UX enhancements, or any change that isn't fixing a broken thing. These go to the post-launch backlog.

This protects the editorial team from platform instability during their most critical work (populating the launch content) and ensures the soft launch tests the same code that will serve the public.

### 11.3 Content Loading Timing

The editorial team can practice content creation on the **staging** CMS starting in Phase 3 (when the editorial workflow becomes functional). However, **production** content creation happens only in Phase 6 — after the code freeze — so that content is entered on the final, stable platform.

If the editorial team is ahead of schedule and has articles drafted (e.g., in Google Docs), they can enter them into the production CMS quickly during Phase 6. If content creation is the bottleneck, Phase 6 can extend by a few days without affecting code stability.

### 11.4 Rollback During Launch

At every stage of the launch, a rollback path exists:

| Stage | Rollback Action |
|-------|----------------|
| Phase 6 (content loading) | If a platform issue is found, developers fix it as a critical bug against the frozen codebase. Content already entered is preserved (DB backup taken daily). |
| Phase 7 (soft launch) | If a systemic issue is found that can't be fixed quickly, the site can be taken offline (maintenance page) while the issue is diagnosed. Content is safe in the DB. |
| Phase 8 (public launch) | Full rollback plan ready: restore previous deployment archive + DB backup from launch morning. Rollback procedure was tested in Phase 5. |

---

## 12. Post-Launch Plan

### 12.1 First 2 Weeks: Stabilization (starting after Phase 8 handoff)

**Focus:** Bug fixes, performance tuning, and monitoring. This is the first time feature-level code changes are permitted since the MVP Freeze — but only for fixes and improvements, not new features.

- Fix all bugs reported during soft launch and first week of public traffic (prioritize by severity)
- Monitor Core Web Vitals in Google Search Console (field data takes ~28 days to accumulate)
- Monitor AdSense for policy warnings; adjust ad placement if needed
- Review comment spam volume; if > 10 spam comments/day, evaluate reCAPTCHA fast-track
- Review server resource usage (CPU, memory, disk); adjust VPS sizing if needed
- Ensure daily backups are running without failure
- Publish 2–3 corrections/updates to test the editorial correction workflow in production

### 12.2 Weeks 3–4: Hardening & Automation

**Focus:** Reduce operational burden and prepare for growth.

- **CI/CD pipeline:** Set up GitHub Actions (or equivalent) with: build, `dotnet test`, Lighthouse CI score gate. Deployment remains manual but the build/test pipeline runs automatically on every push to `main`.
- **CSP enforcement:** Move Content-Security-Policy from report-only to enforced mode. Review CSP violation reports collected during Weeks 1–2.
- **Full WCAG 2.1 AA audit:** Complete the accessibility audit deferred from MVP (NFR-AC-002). Remediate remaining issues.
- **MFA for Admin account:** Evaluate and enable Umbraco's built-in 2FA for the Admin user.
- **Automated data retention:** Implement a scheduled job (or SQL Server Agent job) for contact form submission cleanup (12-month retention) and audit log anonymization (24-month retention).
- **Documentation review:** Update `deployment.md`, `observability.md`, and `cms-guide.md` with any lessons learned during launch.

### 12.3 Month 2+: Phase 2 Kickoff

Based on data collected during the first month (traffic, engagement, advertiser interest, spam volume, editorial feedback), prioritize Phase 2 features:

- Newsletter integration (Mailchimp or equivalent)
- CDN migration (Cloudflare)
- WAF setup (Cloudflare WAF free tier)
- Comment threading (one level of nesting)
- reCAPTCHA v3 (if spam warrants it)
- RSS feeds
- Enhanced poll features
- Editorial analytics dashboard

Phase 2 planning will be guided by the success metrics in PRD §12 and real user data.

---

*End of Development Plan. This document should be reviewed and approved by the Tech Lead and Product Manager before Phase 0 begins. It is a living document — update it as phases are completed and lessons are learned.*
