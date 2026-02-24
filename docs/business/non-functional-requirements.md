# Non-Functional Requirements — PredelNews

**Product:** PredelNews — Regional News Website for Southwest Bulgaria  
**Domain:** predelnews.com  
**Platform:** Umbraco 17 LTS (.NET 10) on Windows VPS (IIS)  
**Document owner:** Solutions Architect / QA Lead  
**Status:** Draft v1.0  
**Last updated:** 2025-02-23

> **Repository path:** `docs/business/non-functional-requirements.md`  
> Parent document: `docs/business/prd.md`  
> Related: `docs/business/functional-specification.md` · `docs/technical/technical-specification.md` · `docs/technical/architecture.md` · `docs/technical/observability.md`

---

## Table of Contents

1. [Purpose & Scope](#1-purpose--scope)
2. [Assumptions](#2-assumptions)
3. [Quality Attribute Summary](#3-quality-attribute-summary)
4. [Detailed NFRs by Category](#4-detailed-nfrs-by-category)
   - A. [UX & Branding](#a-ux--branding)
   - B. [Accessibility](#b-accessibility)
   - C. [Performance & Core Web Vitals](#c-performance--core-web-vitals)
   - D. [SEO & Indexing](#d-seo--indexing)
   - E. [Security](#e-security)
   - F. [Privacy & GDPR](#f-privacy--gdpr)
   - G. [Reliability & Availability](#g-reliability--availability)
   - H. [Observability](#h-observability)
   - I. [Maintainability & Operability](#i-maintainability--operability)
   - J. [Compatibility](#j-compatibility)
5. [Measurement & Verification Matrix](#5-measurement--verification-matrix)
6. [Definition of Done (NFR) — MVP](#6-definition-of-done-nfr--mvp)
7. [Out of Scope / Future Enhancements](#7-out-of-scope--future-enhancements)

---

## 1. Purpose & Scope

### 1.1 Purpose

This document defines the **non-functional requirements** (quality attributes, constraints, and operational expectations) for the PredelNews MVP (Phase 1). It specifies **how well** the system must behave — performance, security, reliability, accessibility, privacy — rather than **what** the system does (which is covered in the functional specification).

Every NFR is written to be **measurable and verifiable** so that QA engineers, developers, and operations can confirm compliance before launch and monitor compliance post-launch.

### 1.2 Scope

**In scope:** All quality attributes for the Phase 1 / MVP public website, CMS backoffice, and supporting infrastructure as deployed on a Windows VPS.

**Out of scope:** Business goals and revenue targets (see PRD §12), functional behavior of features (see functional specification), and deep implementation architecture (see technical specification). Phase 2 features are noted only where they create constraints on MVP design decisions.

### 1.3 Audience

Developers, QA engineers, DevOps/infrastructure, the product manager, and any external auditors reviewing the site before launch.

### 1.4 Conventions

- **NFR IDs** follow the pattern `NFR-{CATEGORY}-{NNN}` (e.g., `NFR-PF-001`).
- **Priority levels:**
  - **Must** — Required for MVP launch. Launch is blocked without it.
  - **Should** — Expected for MVP. Can be descoped to a fast-follow (Phase 1.1) release only under exceptional schedule pressure.
  - **Could** — Desirable. May be deferred to Phase 2 without blocking launch.
- **Owner abbreviations:** PM = Product Manager, ENG = Engineering/Development, OPS = DevOps/Infrastructure.

---

## 2. Assumptions

Where answers were not fully specified or where reasonable defaults were applied, the following assumptions are in effect. Stakeholders should validate or override each item.

| ID | Assumption | Impact if Wrong |
|----|-----------|-----------------|
| NA1 | The hosting environment is a **single Windows VPS** running IIS with .NET 10. No load balancer, no auto-scaling, no container orchestration at MVP. | If hosting changes to Linux or cloud PaaS, patching, backup, and deployment procedures must be revised. |
| NA2 | **No CDN is deployed at MVP.** All static assets (images, CSS, JS) are served directly from the VPS file system. CDN migration is a planned Phase 2 enhancement. | Without CDN, latency for users far from the server is higher and the VPS bears full traffic load including static assets. Spike resilience is reduced. |
| NA3 | **No WAF is deployed at MVP.** Basic protections (rate limiting, secure headers, IIS request filtering) are applied at the application and server level. A WAF/CDN (e.g., Cloudflare) is a Phase 2 recommendation. | The site relies on application-layer defenses only. Sophisticated DDoS or application-layer attacks have limited mitigation. |
| NA4 | **MFA for CMS accounts is not required at MVP.** Authentication uses standard username + password with strong password policy and brute-force protection. | If an admin password is compromised, there is no second authentication factor. Risk is mitigated by limited account count, lockout policies, and audit logging. |
| NA5 | Traffic at launch is **low** (~1K–3K monthly uniques in the first 90 days, growing to 5K–10K by 6 months). The system must handle **3×–5× burst spikes** for 10–30 minutes with degraded but functional performance. | If traffic significantly exceeds projections (e.g., viral national story), the single VPS may become unresponsive. Mitigation: monitoring alerts + documented scale-up procedure. |
| NA6 | **GDPR compliance** for the Bulgarian market follows the standard EU GDPR framework. No Bulgaria-specific data protection regulations beyond GDPR are assumed to apply additional constraints at MVP. | If the Bulgarian Commission for Personal Data Protection imposes additional requirements, the privacy implementation may need revision. |
| NA7 | **Cookie consent at MVP** is a basic notice banner (analytics loads regardless of banner interaction). Full consent management with granular opt-in/opt-out is Phase 2. | This is an accepted simplification. If legal review determines pre-consent loading of analytics is non-compliant for the target jurisdiction, analytics script loading must be gated behind consent before launch. |
| NA8 | **Backups** are file-system-level (VPS snapshot or file copy) plus SQL database backups. No managed backup service is assumed. | If the hosting provider offers managed backups, the backup procedures can be simplified but the RPO/RTO targets remain the same. |
| NA9 | **SSL/TLS certificates** are provisioned and maintained (e.g., via Let's Encrypt or hosting provider). HTTPS is enforced on all pages. | If certificate provisioning is manual, renewal must be tracked to avoid expiry-related outages. |
| NA10 | The site targets a **Bulgarian-speaking audience**. All public-facing text, error messages, labels, and UI chrome are in Bulgarian. NFR measurements (e.g., readability of error messages) apply to Bulgarian-language content. | If multi-language support is added later, accessibility and UX NFRs must be re-evaluated per language. |

---

## 3. Quality Attribute Summary

| Category | NFR ID Range | Primary Goals |
|----------|-------------|---------------|
| A. UX & Branding | NFR-UX-001 – 006 | Consistent light theme; professional visual identity; clear ad/editorial separation; Bulgarian-language UI |
| B. Accessibility | NFR-AC-001 – 005 | Critical WCAG 2.1 AA compliance at MVP; full AA as fast-follow |
| C. Performance & Core Web Vitals | NFR-PF-001 – 009 | Fast mobile experience; Core Web Vitals "Good"; graceful spike handling |
| D. SEO & Indexing | NFR-SE-001 – 005 | Crawlable, indexable, structured; fast discovery of new content |
| E. Security | NFR-SC-001 – 010 | OWASP baseline; hardened CMS access; rate limiting; secure headers |
| F. Privacy & GDPR | NFR-PR-001 – 007 | Lawful data collection; data minimization; retention enforcement; consent |
| G. Reliability & Availability | NFR-RL-001 – 005 | 99.5% uptime; defined RPO/RTO; graceful degradation |
| H. Observability | NFR-OB-001 – 005 | Actionable logs; uptime monitoring; alerts for critical failures |
| I. Maintainability & Operability | NFR-MN-001 – 007 | Documented ops; reproducible deployments; environment parity; patching |
| J. Compatibility | NFR-CM-001 – 003 | Modern browsers; mobile-first; responsive 320–1920px |

---

## 4. Detailed NFRs by Category

### A. UX & Branding

---

#### NFR-UX-001 — Light Theme Enforcement

**Priority:** Must

**Requirement:** The site must render in a light color theme on all public pages under all conditions. The CSS must include `color-scheme: light only` (or equivalent). No `prefers-color-scheme: dark` media query may be present in any stylesheet delivered to the browser. No dark-mode toggle or automatic dark-mode switching may exist anywhere on the site.

**Rationale:** Dark mode is explicitly out of scope per PRD §3. Preventing OS/browser dark-mode override ensures visual consistency and avoids untested contrast issues.

**Verification:** Automated CSS audit (grep for `prefers-color-scheme: dark`); manual test on macOS/Windows/Android/iOS with OS dark mode enabled — site must remain light. Lighthouse accessibility audit confirms no contrast regressions.

**Owner:** ENG

---

#### NFR-UX-002 — Consistent Visual Identity

**Priority:** Must

**Requirement:** All public pages must use the approved brand color palette, typography, and logo. Ad slots, sponsored content labels ("Платена публикация"), and editorial content must be visually distinct. No page may render without the site logo in the header.

**Rationale:** Professional visual identity builds reader trust — a core product principle.

**Verification:** Visual regression testing (screenshot comparison) on homepage, article page, and archive page across desktop and mobile viewports. Manual review against brand guidelines document.

**Owner:** ENG / PM

---

#### NFR-UX-003 — Ad/Editorial Visual Separation

**Priority:** Must

**Requirement:** Every rendered ad slot must display a "Реклама" label that is visually distinct from editorial content (defined in the global stylesheet, not per-slot). Sponsored article labels ("Платена публикация") must use a consistent, globally-styled banner that cannot be suppressed per-article.

**Rationale:** Regulatory compliance and editorial trust require clear distinction between advertising and journalism.

**Verification:** Manual inspection of all 6 ad slot positions and sponsored article rendering (card and full page) across viewports. Automated check that the "Реклама" label element is present in the DOM adjacent to each ad container.

**Owner:** ENG

---

#### NFR-UX-004 — Bulgarian-Language UI

**Priority:** Must

**Requirement:** All visitor-facing text — navigation labels, error messages, form labels, validation messages, empty states, pagination controls, cookie banners, and confirmation messages — must be in Bulgarian. No English-language placeholder text (e.g., "No results found") may appear on the public site.

**Rationale:** The target audience is Bulgarian-speaking. English UI text signals an unfinished product and erodes trust.

**Verification:** Full crawl of public pages; manual review of all interactive flows (search with no results, comment submission, form validation errors, rate-limit messages, email signup, pagination edge cases). Each must display Bulgarian text.

**Owner:** ENG / PM

---

#### NFR-UX-005 — Error Page Presentation

**Priority:** Must

**Requirement:** Custom error pages must exist for HTTP 404 (Not Found) and HTTP 500 (Server Error) responses. Each must display the site header, navigation, a user-friendly message in Bulgarian (e.g., "Страницата не е намерена" for 404), and a link back to the homepage. Error pages must not expose stack traces, server paths, or technical details to the visitor.

**Rationale:** Default IIS/ASP.NET error pages leak technical information and damage user experience and brand perception.

**Verification:** Manually navigate to a non-existent URL — verify 404 page renders correctly with Bulgarian message and site navigation. Force a 500 error in a test environment — verify custom page displays and no technical details are exposed. Automated scan of error responses confirming no stack trace patterns.

**Owner:** ENG

---

#### NFR-UX-006 — Interstitial-Free Experience

**Priority:** Must

**Requirement:** The public site must not display any interstitial overlays, pop-ups, or modals that block content on page load, with the sole exception of the cookie consent banner (which must be dismissible and not overlay the main content area). No pop-up ad formats are permitted.

**Rationale:** Google penalizes intrusive interstitials on mobile. The PRD cites pop-up overload as a key user frustration.

**Verification:** Manual navigation through homepage, article pages, and archives on mobile viewport — no interstitial should appear. Lighthouse audit for "Avoids intrusive interstitials" signal.

**Owner:** ENG / PM

---

### B. Accessibility

---

#### NFR-AC-001 — Critical Accessibility (MVP)

**Priority:** Must

**Requirement:** At MVP launch, the following WCAG 2.1 AA criteria must be met on all public pages:

- All interactive elements (links, buttons, form fields) are reachable and operable via keyboard alone (no keyboard traps).
- All form inputs have associated `<label>` elements or `aria-label` attributes.
- Visible focus indicators are present on all focusable elements and meet a minimum 3:1 contrast ratio against adjacent colors.
- Text color contrast meets WCAG AA minimums: ≥ 4.5:1 for normal text, ≥ 3:1 for large text (≥ 18pt or ≥ 14pt bold).
- All images have `alt` attributes. Decorative images use `alt=""`. Cover images and inline article images have descriptive alt text.
- The page has a logical heading hierarchy (single H1 per page, no skipped heading levels).
- Skip-to-content link is present on all pages.

**Rationale:** These are the highest-impact accessibility features that affect the largest number of users, including those using keyboards, screen readers, and low-vision assistive tools.

**Verification:** Automated: axe-core or Lighthouse accessibility audit on homepage, article page, archive page, contact page, and search results page — zero critical or serious violations for the criteria listed above. Manual: keyboard-only navigation test through all primary user flows (read article, submit comment, use search, submit contact form, vote in poll, navigate via header).

**Owner:** ENG

---

#### NFR-AC-002 — Full WCAG 2.1 AA Compliance (Fast-Follow)

**Priority:** Should

**Requirement:** Within 30 days of MVP launch (Phase 1.1), the site must achieve full WCAG 2.1 Level AA conformance across all public pages, including but not limited to: reflow (no horizontal scroll at 400% zoom / 320px equivalent), target size (minimum 44×44px touch targets), error identification and suggestion for all forms, and consistent navigation.

**Rationale:** Full AA compliance is both an ethical commitment and a risk mitigation (EU Accessibility Act applicability is expanding). Deferring non-critical items to Phase 1.1 allows MVP to launch on time while maintaining a firm commitment.

**Verification:** Full WCAG 2.1 AA audit (automated + manual) using axe-core, WAVE, and manual screen reader testing (NVDA on Windows, VoiceOver on iOS). All Level A and AA success criteria must pass. Audit report documented and any remaining issues tracked with resolution dates.

**Owner:** ENG / PM

---

#### NFR-AC-003 — Semantic HTML Structure

**Priority:** Must

**Requirement:** All public pages must use semantic HTML5 elements: `<header>`, `<nav>`, `<main>`, `<article>`, `<aside>`, `<footer>`, `<section>` as appropriate. ARIA landmarks must be present where semantic elements are insufficient. Each page must have exactly one `<main>` element.

**Rationale:** Semantic markup enables screen readers and assistive technologies to navigate page regions efficiently. It also supports SEO (search engines interpret semantic structure).

**Verification:** Automated HTML validation (W3C validator or html-validate). axe-core landmark audit. Manual review of homepage and article page source.

**Owner:** ENG

---

#### NFR-AC-004 — Accessible Media

**Priority:** Must

**Requirement:** All non-decorative images must have meaningful `alt` text (enforced at the CMS level for cover images and inline images). YouTube embeds must have a descriptive `title` attribute on the `<iframe>`. No media auto-plays with sound.

**Rationale:** Users relying on screen readers cannot perceive image or video content without text alternatives.

**Verification:** CMS validation: attempt to save an article with a cover image that has empty alt text — must be blocked. Automated scan of public article pages for `<img>` without `alt` and `<iframe>` without `title`. Manual spot check of 10 published articles.

**Owner:** ENG

---

#### NFR-AC-005 — Reduced Motion Respect

**Priority:** Should

**Requirement:** Any CSS animations or transitions on the public site must respect the `prefers-reduced-motion: reduce` media query by disabling or minimizing motion when the user has indicated a preference for reduced motion at the OS level.

**Rationale:** Motion-sensitive users (e.g., vestibular disorders) may experience discomfort from animations.

**Verification:** Enable "Reduce motion" in OS accessibility settings; navigate the site — all transitions/animations should be minimal or instant. CSS audit for `prefers-reduced-motion` media query presence.

**Owner:** ENG

---

### C. Performance & Core Web Vitals

---

#### NFR-PF-001 — Core Web Vitals Targets

**Priority:** Must

**Requirement:** All public page types (homepage, article page, category archive, region archive, tag archive, author archive, "All News," static pages, search results) must meet the following Core Web Vitals thresholds as measured by Lighthouse v12+ in mobile simulation (Moto G Power on simulated 4G) and confirmed by field data (Chrome UX Report) once sufficient traffic exists:

| Metric | Target | "Good" Threshold (Google) |
|--------|--------|--------------------------|
| Largest Contentful Paint (LCP) | ≤ 2.5 s | ≤ 2.5 s |
| Interaction to Next Paint (INP) | ≤ 200 ms | ≤ 200 ms |
| Cumulative Layout Shift (CLS) | ≤ 0.1 | ≤ 0.1 |

**Rationale:** Core Web Vitals directly affect Google Search ranking and user experience. The PRD (§5, Principle 3) mandates speed as a feature. The target audience uses mid-range Android devices on 4G.

**Verification:** Lighthouse CI run on every deployment for homepage, article page (with ads loaded), and one archive page. All three metrics must be in the "Good" range. Post-launch: monitor via Google Search Console Core Web Vitals report.

**Owner:** ENG

---

#### NFR-PF-002 — Lighthouse Performance Score

**Priority:** Must

**Requirement:** All public page types must achieve a Lighthouse Performance score ≥ 90 on mobile simulation (Moto G Power, simulated 4G throttling) with ad slots loaded.

**Rationale:** A composite performance score provides a single gate for deployment decisions. The PRD (§8.1, F1 AC1.5, F2 AC2.5) requires ≥ 90 for homepage and article page.

**Verification:** Lighthouse CI integrated into deployment pipeline. Score < 90 on any monitored page type blocks deployment to production (or raises a mandatory review flag for the team to assess).

**Owner:** ENG

---

#### NFR-PF-003 — Time to First Byte (TTFB)

**Priority:** Must

**Requirement:** Server-side response time (TTFB) must be ≤ 600 ms for 95th percentile of requests under normal traffic conditions (≤ 10K monthly uniques). Under burst conditions (3×–5× spike for 10–30 minutes), TTFB must remain ≤ 1500 ms for 95th percentile.

**Rationale:** TTFB is the foundation of all downstream performance metrics. A slow server negates any frontend optimization.

**Verification:** Synthetic monitoring (e.g., UptimeRobot or equivalent) pinging homepage and a sample article page every 5 minutes, measuring TTFB. Load testing tool (e.g., k6, Artillery) simulating 5× concurrent traffic for 15 minutes — verify TTFB stays within spike threshold.

**Owner:** ENG / OPS

---

#### NFR-PF-004 — Image Optimization

**Priority:** Must

**Requirement:** All images served on public pages must meet the following criteria:

- Cover images and inline article images are served in a modern format (WebP preferred, with JPEG/PNG fallback for unsupported browsers).
- Responsive `srcset` and `sizes` attributes are present on all content images, providing at least 3 size variants (e.g., 400w, 800w, 1200w).
- All images below the fold are lazy-loaded (`loading="lazy"` or equivalent).
- Maximum file size for any single served image variant: ≤ 200 KB.
- The cover image (hero/featured) on article pages may be preloaded (`<link rel="preload">`) to improve LCP.

**Rationale:** Images are typically the largest resources on news pages and the primary driver of LCP. Without CDN at MVP, optimized images are critical to compensate for direct VPS serving.

**Verification:** Lighthouse audit for "Serve images in next-gen formats," "Properly size images," and "Defer offscreen images." Automated crawl of 20 sample article pages checking for `srcset` presence and `loading="lazy"` on below-fold images. File size spot check on media library uploads.

**Owner:** ENG

---

#### NFR-PF-005 — CSS & JavaScript Delivery

**Priority:** Must

**Requirement:** Critical CSS for above-the-fold content must be inlined or loaded in a render-blocking-minimized manner. Total blocking JavaScript on initial page load must be ≤ 50 KB (compressed). Third-party scripts (AdSense, analytics) must load asynchronously or deferred and must not block the main thread for > 100 ms.

**Rationale:** Render-blocking resources directly degrade LCP and INP. Third-party ad scripts are the most common source of performance regressions on news sites.

**Verification:** Lighthouse audits for "Eliminate render-blocking resources," "Reduce unused JavaScript," and "Minimize main-thread work." Chrome DevTools Performance panel trace on article page — verify third-party scripts do not block main thread > 100 ms.

**Owner:** ENG

---

#### NFR-PF-006 — Server-Side Output Caching

**Priority:** Must

**Requirement:** Public pages must be served from an output cache with a maximum staleness of 60 seconds. Cache must be invalidated or refreshed when content is published, updated, or unpublished. Article pages, homepage, and archive pages are all cacheable. CMS backoffice pages and preview URLs must not be cached.

**Rationale:** Caching reduces TTFB and VPS CPU load, which is critical on a single-server deployment without CDN. The 60-second staleness aligns with the functional specification's requirement for editorial changes to be reflected within 60 seconds (FR-PW-003 AC6).

**Verification:** Measure TTFB for a homepage request with a cold cache vs. warm cache — warm cache must be ≤ 50 ms TTFB. Publish an article and verify it appears on the homepage within 60 seconds. Verify CMS backoffice pages return `Cache-Control: no-store` or equivalent.

**Owner:** ENG

---

#### NFR-PF-007 — Spike Resilience

**Priority:** Must

**Requirement:** The system must remain functional (all pages load, comments can be submitted, search returns results) during a traffic spike of 3×–5× the normal concurrent user count sustained for up to 30 minutes. During such spikes:

- Pages may load slower but must complete within 5 seconds (TTFB + full page load on simulated 4G).
- No HTTP 503 errors may be returned to visitors.
- Comment submission and contact form submission must continue to function.

Degraded ad rendering (slower ad load or ad slot timeout) during spikes is acceptable.

**Rationale:** Local breaking news stories can cause sudden traffic surges. A single VPS without auto-scaling must be tuned to handle realistic spikes without becoming unresponsive.

**Verification:** Load test simulating 5× baseline concurrent users for 30 minutes. Monitor HTTP response codes (zero 503s), page completion times (≤ 5s for 95th percentile), and successful comment/form submissions during the test.

**Owner:** ENG / OPS

---

#### NFR-PF-008 — Search Response Time

**Priority:** Must

**Requirement:** Site search queries must return results within ≤ 500 ms (server-side processing time, measured from request receipt to response dispatch) for 95th percentile of queries under normal traffic.

**Rationale:** Aligns with FR-SN-001 AC7. Slow search degrades user experience and discourages use of the feature.

**Verification:** Synthetic search queries against the production site measuring server response time. Load test with concurrent search queries to verify 95th percentile remains within target.

**Owner:** ENG

---

#### NFR-PF-009 — File System Storage Constraints

**Priority:** Must

**Requirement:** The media library (uploaded images, PDFs) must be stored on the VPS file system with the following constraints:

- A documented maximum storage allocation for media files (recommended: start with 10 GB, alert at 80% utilization).
- Media uploads must be validated for file type (images: JPEG, PNG, WebP, GIF; documents: PDF only) and maximum file size (individual upload: ≤ 10 MB).
- Media storage is included in the backup scope (see NFR-RL-003).

**Rationale:** Without cloud object storage or CDN, the VPS disk is the sole storage location. Uncontrolled growth risks disk exhaustion and service interruption.

**Verification:** Monitor disk utilization via server monitoring (NFR-OB-002). Attempt to upload a file exceeding 10 MB — must be rejected with a user-friendly error. Attempt to upload a disallowed file type (e.g., `.exe`) — must be rejected.

**Owner:** OPS / ENG

---

### D. SEO & Indexing

---

#### NFR-SE-001 — Crawlability

**Priority:** Must

**Requirement:** All public pages must be crawlable by search engine bots. Specifically:

- `robots.txt` at `/robots.txt` must allow crawling of all public content and disallow `/umbraco/` and any other admin/preview paths.
- `robots.txt` must reference the sitemap URL.
- No public page may contain a `<meta name="robots" content="noindex">` tag unless it is explicitly unpublished or a CMS preview.
- Server response time for any public page must be ≤ 600 ms TTFB (see NFR-PF-003) to avoid Googlebot timeout during crawl.

**Rationale:** SEO is a primary traffic acquisition channel. Crawl errors directly prevent content from appearing in search results.

**Verification:** Google Search Console inspection after launch — verify zero crawl errors on published content. Fetch `robots.txt` and verify structure. Automated crawl of the full public sitemap verifying HTTP 200 responses and absence of noindex tags.

**Owner:** ENG

---

#### NFR-SE-002 — Sitemap Freshness

**Priority:** Must

**Requirement:** The XML sitemap at `/sitemap.xml` must:

- Include all published articles, category archives, region archives, tag archives, author pages, and static pages.
- Reflect new or updated content within 60 minutes of publication/update.
- Exclude unpublished, draft, or scheduled (not-yet-live) content.
- Be valid per the Sitemaps.org protocol (schema validation).
- Contain `<lastmod>` dates that accurately reflect the most recent modification of each URL.

**Rationale:** An accurate, fresh sitemap accelerates search engine discovery of new content — critical for a news site competing on timeliness.

**Verification:** Publish a new article; verify it appears in `/sitemap.xml` within 60 minutes. Unpublish an article; verify removal. Validate sitemap XML against the Sitemaps.org schema. Cross-check `<lastmod>` dates against actual article modification timestamps.

**Owner:** ENG

---

#### NFR-SE-003 — Canonical URL Correctness

**Priority:** Must

**Requirement:** Every public page must include a `<link rel="canonical">` tag. The canonical URL must:

- Point to the preferred version of the page (HTTPS, with or without trailing slash — consistent convention).
- Be self-referencing on paginated pages (page 2's canonical points to page 2).
- Not point to a non-existent or redirected URL.

**Rationale:** Incorrect canonicals cause search engines to deindex or consolidate the wrong pages, wasting crawl budget and losing ranking.

**Verification:** Automated crawl of all public URLs checking for canonical tag presence and self-reference correctness. Spot check paginated archives (page 1, 2, last page).

**Owner:** ENG

---

#### NFR-SE-004 — Structured Data Validity

**Priority:** Must

**Requirement:** JSON-LD structured data on article pages (`NewsArticle` schema) and all pages (`BreadcrumbList` schema) must be valid and error-free as tested by Google's Rich Results Test. The structured data must accurately reflect the page content (headline, dates, author, image, publisher).

**Rationale:** Valid structured data enables rich results in Google Search (article carousels, breadcrumb display) which improve click-through rates.

**Verification:** Run Google Rich Results Test on 5 sample article pages and the homepage — zero errors. Automated extraction and validation of JSON-LD blocks during the crawl pipeline.

**Owner:** ENG

---

#### NFR-SE-005 — URL Permanence

**Priority:** Must

**Requirement:** Once an article is published, its URL slug must not change unless an Editor/Admin explicitly edits it. If a slug is changed, a 301 redirect from the old URL to the new URL must be created automatically and persist indefinitely.

**Rationale:** Broken URLs lose accumulated link equity, return 404 errors to users arriving from social shares or bookmarks, and damage trust.

**Verification:** Publish an article. Change its slug in the CMS. Request the old URL — verify HTTP 301 redirect to the new URL. Verify the old URL does not return a 404.

**Owner:** ENG

---

### E. Security

---

#### NFR-SC-001 — HTTPS Enforcement

**Priority:** Must

**Requirement:** All traffic to the site (public and CMS backoffice) must be served over HTTPS. HTTP requests must be redirected to HTTPS with a 301 redirect. HSTS (HTTP Strict Transport Security) header must be set with a minimum `max-age` of 31536000 (1 year) and `includeSubDomains`.

**Rationale:** HTTPS protects data in transit (login credentials, form submissions, cookies). HSTS prevents downgrade attacks. Google Search uses HTTPS as a ranking signal.

**Verification:** Request `http://predelnews.com` — verify 301 redirect to `https://`. Inspect response headers for `Strict-Transport-Security` with correct `max-age`. SSL Labs test (ssllabs.com) — target grade A or A+.

**Owner:** OPS / ENG

---

#### NFR-SC-002 — Secure HTTP Headers

**Priority:** Must

**Requirement:** All responses from the public site and CMS must include the following security headers:

| Header | Value |
|--------|-------|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` (or `SAMEORIGIN` if Umbraco backoffice requires framing) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | Deny unnecessary features: `camera=(), microphone=(), geolocation=()` (minimum) |
| `Content-Security-Policy` | Defined policy allowing only necessary sources (self, AdSense domains, Analytics domains, YouTube embed domain). Report-only mode acceptable at MVP if full enforcement is not yet stable. |

**Rationale:** Security headers mitigate XSS, clickjacking, MIME sniffing, and information leakage. They are a baseline OWASP recommendation.

**Verification:** Inspect response headers on homepage and article page using browser DevTools or `curl`. Use securityheaders.com — target grade A. CSP violations should be logged (report-uri or report-to) if in report-only mode.

**Owner:** ENG

---

#### NFR-SC-003 — CMS Backoffice Access Hardening

**Priority:** Must

**Requirement:** The Umbraco backoffice (`/umbraco/`) must be protected with the following measures:

- Account lockout after 5 consecutive failed login attempts within 10 minutes. Lockout duration: 15 minutes.
- Strong password policy enforced: minimum 12 characters, at least 1 uppercase, 1 lowercase, 1 digit, 1 special character.
- Maximum of 5 active CMS accounts at MVP (1 Admin, 1 Editor, up to 3 Writers). New accounts require Admin approval.
- Session timeout: 30 minutes of inactivity.
- Session cookies must have `Secure`, `HttpOnly`, and `SameSite=Lax` (or `Strict`) attributes.

**Rationale:** The CMS is the highest-value attack target. Without MFA at MVP, strong password policies, lockout, and session controls are the primary defenses.

**Verification:** Attempt 6 consecutive failed logins — verify lockout on the 6th attempt. Attempt to create an account with a weak password (e.g., "password1") — verify rejection. Inspect session cookie attributes in browser DevTools. Verify session expires after 30 minutes of inactivity.

**Owner:** ENG / OPS

---

#### NFR-SC-004 — CMS Path Obscurity (Recommendation)

**Priority:** Could

**Requirement:** If Umbraco supports it without significant effort, rename the backoffice URL from `/umbraco/` to a non-default path. If not feasible, retain `/umbraco/` but ensure it is excluded from `robots.txt`, returns no information-revealing error pages, and is monitored for brute-force attempts.

**Rationale:** Default CMS paths are targeted by automated scanners. Obscurity is not a security solution but adds a minor barrier.

**Verification:** Navigate to the configured backoffice path — verify access. If path is changed from default, navigate to `/umbraco/` — verify it returns 404 or redirect.

**Owner:** ENG

---

#### NFR-SC-005 — Rate Limiting (Forms & Comments)

**Priority:** Must

**Requirement:** Server-side rate limiting must be applied to all public form submission endpoints:

| Endpoint | Limit | Window |
|----------|-------|--------|
| Comment submission | 3 per IP | 5-minute rolling window |
| Contact form submission | 3 per IP | 10-minute rolling window |
| Poll vote | 1 per browser (cookie) | Lifetime of poll |
| Email signup | 5 per IP | 10-minute rolling window |
| CMS login | 5 attempts per username | 10-minute window (lockout) |

Rate-limited requests must return a user-friendly Bulgarian-language message (e.g., "Моля, изчакайте няколко минути преди да опитате отново.") and HTTP 429 status code.

**Rationale:** Rate limiting is the primary anti-abuse defense for anonymous interactions at MVP (no CAPTCHA, no WAF).

**Verification:** Automated test: submit 4 comments from the same IP within 5 minutes — verify 4th is rejected with 429 and Bulgarian message. Repeat for contact form (4th within 10 min) and email signup (6th within 10 min). Verify CMS lockout after 5 failed logins.

**Owner:** ENG

---

#### NFR-SC-006 — Input Validation & Output Encoding

**Priority:** Must

**Requirement:** All user inputs (comment text, display names, contact form fields, email signup, search queries) must be validated server-side for type, length, and format. All user-supplied content rendered on public pages must be output-encoded to prevent Cross-Site Scripting (XSS). Specifically:

- Comment display names: max 50 characters, stripped of HTML tags.
- Comment text: max 2000 characters, HTML-encoded on output.
- Search queries: HTML-encoded when displayed in the "Не бяха намерени резултати за '{query}'" message.
- No raw HTML from user input is ever rendered on public pages.

**Rationale:** XSS is a top OWASP vulnerability. Anonymous comment systems are a primary XSS attack vector.

**Verification:** Attempt to submit a comment containing `<script>alert('xss')</script>` — verify the script is HTML-encoded on the article page (displayed as text, not executed). Attempt to submit a search query with HTML tags — verify encoded output on results page. Automated OWASP ZAP scan against public pages and form endpoints.

**Owner:** ENG

---

#### NFR-SC-007 — SQL Injection Prevention

**Priority:** Must

**Requirement:** All database queries must use parameterized queries or an ORM (Entity Framework / Umbraco's built-in data access). No raw SQL string concatenation with user input is permitted anywhere in the codebase.

**Rationale:** SQL injection is OWASP #1. Umbraco + Entity Framework provide this by default, but custom queries (comments, polls, email signup) must also comply.

**Verification:** Code review of all custom data access code. Automated OWASP ZAP SQL injection scan against all form endpoints and URL parameters.

**Owner:** ENG

---

#### NFR-SC-008 — CSRF Protection

**Priority:** Must

**Requirement:** All state-changing form submissions (comment posting, contact form, email signup, poll voting, CMS backoffice actions) must be protected with anti-CSRF tokens. The server must reject submissions without a valid token.

**Rationale:** CSRF attacks can cause visitors' browsers to submit comments or forms without their knowledge.

**Verification:** Inspect form HTML for anti-CSRF token field. Submit a form request without the token or with a tampered token — verify HTTP 400 or 403 response.

**Owner:** ENG

---

#### NFR-SC-009 — Dependency & Framework Patching

**Priority:** Must

**Requirement:** The .NET runtime, Umbraco CMS, and all NuGet dependencies must be kept within one minor version of the latest stable release. Critical security patches (CVE with CVSS ≥ 7.0) must be applied within 7 days of release. A dependency audit must be run at least monthly.

**Rationale:** Unpatched frameworks are a primary attack vector. Small teams often defer updates until a breach occurs.

**Verification:** Monthly automated dependency scan (e.g., `dotnet list package --vulnerable`). Document the current version inventory and patch dates. Verify no known critical CVEs are unpatched for > 7 days.

**Owner:** OPS / ENG

---

#### NFR-SC-010 — Secrets Management

**Priority:** Must

**Requirement:** No credentials, API keys, connection strings, or other secrets may be stored in source code, version control, or client-accessible files. Secrets must be stored in environment variables, Windows DPAPI-protected configuration, or an equivalent secrets management approach appropriate for the Windows VPS environment. The `web.config` or `appsettings.json` in production must not contain plaintext secrets if the file is accessible via the web server.

**Rationale:** Leaked credentials are a leading cause of breaches. News sites are targeted for their publishing capability.

**Verification:** Code review: grep repository for patterns matching connection strings, API keys, and passwords. Verify `appsettings.json` in production uses environment variable references or encrypted sections. Attempt to access `appsettings.json` via a browser URL — must return 404 or 403.

**Owner:** ENG / OPS

---

### F. Privacy & GDPR

---

#### NFR-PR-001 — Data Minimization

**Priority:** Must

**Requirement:** The system must collect only the minimum personal data necessary for each function:

| Function | Data Collected | Justification |
|----------|---------------|---------------|
| Comment submission | Display name (free text), comment text, IP address, timestamp | Moderation, rate limiting, audit |
| Contact form | Name, email, subject, message | Responding to inquiry |
| Email signup | Email address, timestamp, consent flag | Future newsletter |
| Poll vote | Cookie identifier only | Duplicate prevention |
| Analytics | Anonymized data via GA4 | Traffic measurement |

No additional personal data (e.g., phone number, physical address, device fingerprint) is collected from visitors at MVP.

**Rationale:** GDPR Article 5(1)(c) requires data minimization. Collecting less data reduces breach impact and compliance burden.

**Verification:** Review all form fields on the public site — verify no extra fields beyond those listed. Review database schema for visitor-facing tables — verify no columns storing data beyond the listed set. Review GA4 configuration for IP anonymization setting.

**Owner:** ENG / PM

---

#### NFR-PR-002 — Cookie Consent Banner

**Priority:** Should

**Requirement:** A cookie consent banner must be displayed to first-time visitors. The banner must:

- Inform the visitor in Bulgarian that the site uses cookies for analytics and functionality (e.g., "Този сайт използва бисквитки за анализ на трафика и подобряване на функционалността.").
- Include an "Приемам" (Accept) button that dismisses the banner and sets a cookie remembering acknowledgment.
- Not reappear for visitors who have acknowledged it (cookie-based persistence).
- Not overlay or obstruct the main content area (positioned as a bar at the top or bottom of the viewport).

**MVP simplification:** Analytics scripts (GA4) load regardless of banner interaction. This is an accepted trade-off (see Assumption NA7). Full consent-gated loading is Phase 2.

**Rationale:** A basic cookie notice demonstrates good faith compliance and sets the foundation for full consent management in Phase 2.

**Verification:** First visit to the site — verify banner appears with Bulgarian text and "Приемам" button. Click "Приемам" — verify banner dismisses and does not reappear on subsequent page loads. Clear cookies — verify banner reappears. Verify banner does not overlap content on mobile viewport.

**Owner:** ENG / PM

---

#### NFR-PR-003 — Contact Form Data Retention

**Priority:** Must

**Requirement:** Contact form submissions must be retained for a maximum of **12 months** from submission date. After 12 months, submissions must be deleted or anonymized (personal fields: name, email replaced with "ANONYMIZED"; subject and message retained for operational reference if needed, or deleted entirely).

An automated or semi-automated process must enforce this retention schedule. If fully automated deletion is not feasible at MVP, a documented manual procedure with a monthly calendar reminder is acceptable, with automated enforcement as a Phase 2 enhancement.

**Rationale:** GDPR storage limitation principle (Article 5(1)(e)). Contact form data has no purpose beyond the inquiry lifecycle.

**Verification:** Check the database for contact form submissions older than 12 months — none should contain personal data. If manual process: verify the procedure document exists and the calendar reminder is set. If automated: verify the scheduled job runs and deletes/anonymizes records correctly.

**Owner:** ENG / OPS

---

#### NFR-PR-004 — Comment Audit Log Retention

**Priority:** Must

**Requirement:** Comment audit logs (deletion metadata: who deleted, when, original content, IP address, reason) must be retained for a maximum of **24 months**. After 24 months, audit log entries must be deleted or have personal data (IP address, original comment content containing personal information) anonymized.

**Rationale:** Audit logs serve moderation accountability and potential legal obligations (e.g., responding to complaints). A 24-month retention period balances accountability with data minimization.

**Verification:** Check the database for audit log entries older than 24 months — personal data fields should be empty or anonymized. Verify retention policy is documented.

**Owner:** ENG / OPS

---

#### NFR-PR-005 — Email Subscriber Data Handling

**Priority:** Must

**Requirement:** Email subscriber records must be:

- Stored with the email address, signup timestamp, and consent flag (`true`).
- Retained until the subscriber unsubscribes (Phase 2 feature). Upon unsubscribe, the record must be deleted within 30 days, with only a minimal suppression entry (hashed email) retained to prevent re-addition.
- Exportable as CSV by Admin/Editor (functional requirement FR-AB-004) — the export must use HTTPS and be accessible only to authenticated CMS users.
- Not shared with third parties without explicit consent (separate from the newsletter consent).

**Rationale:** Email addresses are personal data under GDPR. The suppression hash ensures unsubscribed users are not inadvertently re-added when the newsletter launches in Phase 2.

**Verification:** Verify database schema stores only email, timestamp, and consent flag. Verify CSV export is served over HTTPS and requires CMS authentication. Verify no third-party services receive subscriber data at MVP (no email platform integration in Phase 1).

**Owner:** ENG / PM

---

#### NFR-PR-006 — Comment IP Address Handling

**Priority:** Must

**Requirement:** IP addresses collected with comments (for rate limiting and moderation) must:

- Be stored in the database alongside the comment record.
- Be accessible only to Editor and Admin roles in the CMS (not visible to Writers or on the public site).
- Be included in the audit log for deleted comments.
- Be anonymized or deleted as part of the comment audit log retention policy (NFR-PR-004, 24 months).

**Rationale:** IP addresses are personal data under GDPR. Storage is justified for anti-abuse purposes but must be minimized and access-controlled.

**Verification:** Submit a comment; verify IP is stored in the database. Log in as Writer — verify IP address is not visible in the comment management interface. Verify IP is present in audit log entries for deleted comments. Verify retention schedule applies.

**Owner:** ENG

---

#### NFR-PR-007 — Privacy Policy Page

**Priority:** Must

**Requirement:** A publicly accessible Privacy Policy page must be published before launch. The page must be linked from the site footer and the cookie consent banner. At minimum, the privacy policy must describe:

- What data is collected (comments, contact form, email signup, analytics cookies, poll cookies, comment name cookies).
- Why it is collected (purpose for each data type).
- How long it is retained (retention periods as defined in NFR-PR-003, 004, 005).
- Who has access (editorial team, no third-party sharing except analytics).
- How to request data deletion or correction (contact information).
- Cookie usage summary.

**Rationale:** GDPR Articles 13 and 14 require transparent disclosure of data processing. Launching without a privacy policy is a compliance risk.

**Verification:** Privacy policy page exists at a public URL (e.g., `/politika-za-poveritelnost/`). Footer contains a link to it. Cookie consent banner contains a link to it. Content covers all listed topics. Legal advisor review is recommended.

**Owner:** PM / Legal

---

### G. Reliability & Availability

---

#### NFR-RL-001 — Uptime Target

**Priority:** Must

**Requirement:** The public site must achieve ≥ **99.5% uptime** per calendar month, measured as the percentage of 1-minute checks from an external monitoring service that return HTTP 200 for the homepage. This allows a maximum of approximately 3.6 hours of unplanned downtime per month.

Planned maintenance windows (OS patching, deployments) are excluded from the calculation if announced at least 24 hours in advance and scheduled during low-traffic hours (02:00–06:00 EET).

**Rationale:** A news site must be available when news breaks. 99.5% is a realistic target for a single VPS without redundancy.

**Verification:** External uptime monitor (e.g., UptimeRobot free tier, Hetrixtools, or equivalent) checking the homepage every 1 minute. Monthly uptime report reviewed by OPS.

**Owner:** OPS

---

#### NFR-RL-002 — Planned Maintenance Protocol

**Priority:** Must

**Requirement:** Planned maintenance that causes downtime must:

- Be scheduled during the low-traffic window (02:00–06:00 EET).
- Be communicated to the editorial team at least 24 hours in advance.
- Display a branded maintenance page (site logo, Bulgarian message: "Сайтът е в профилактика. Ще бъдем отново онлайн скоро.") rather than a generic IIS error.
- Not exceed 30 minutes for routine operations (deployments, patches).

**Rationale:** Unannounced or prolonged downtime during business hours damages reader trust and may cause missed breaking news windows.

**Verification:** Deploy a maintenance page to the test environment and verify it renders correctly. Conduct a mock maintenance window and verify the process completes within 30 minutes. Verify the editorial team receives advance notification.

**Owner:** OPS

---

#### NFR-RL-003 — Backup Policy (RPO)

**Priority:** Must

**Requirement:** Full system backups must be performed with the following Recovery Point Objective (RPO):

| Component | Backup Frequency | RPO |
|-----------|-----------------|-----|
| SQL Server database | Daily automated + before each deployment | ≤ 24 hours |
| Media files (images, PDFs) | Daily automated (file-system level or sync) | ≤ 24 hours |
| CMS configuration and code (deployment package) | Version-controlled in Git | Always recoverable (latest commit) |
| VPS full snapshot | Weekly | ≤ 7 days |

Backups must be stored in a location **separate from the production VPS** (e.g., a different disk/volume, remote storage, or off-site backup service). A backup that is only on the same disk as production does not satisfy this requirement.

**Rationale:** A single VPS with no redundancy means disk failure or OS corruption could cause total data loss. Off-site backups are the sole disaster recovery mechanism.

**Verification:** Verify daily backup job runs successfully (check logs/email notification). Verify backup files exist on the off-site location with timestamps within RPO. Attempt a restore from backup in a test environment (at least once before launch and quarterly thereafter).

**Owner:** OPS

---

#### NFR-RL-004 — Restore Procedure (RTO)

**Priority:** Must

**Requirement:** The system must be fully restorable from backups with a Recovery Time Objective (RTO) of ≤ **4 hours** from the decision to restore. "Fully restorable" means: the public site is accessible, all published articles are present, the CMS is functional, and the media library is intact.

A documented, step-by-step restore procedure must exist. The procedure must be tested at least once before launch.

**Rationale:** Without a tested restore procedure, backup existence is meaningless. A 4-hour RTO is aggressive but achievable for a single-VPS setup with documentation.

**Verification:** Conduct a full restore drill to a test/staging VPS from the most recent backup set. Measure time from "start restore" to "site fully operational." Document the results.

**Owner:** OPS

---

#### NFR-RL-005 — Graceful Degradation

**Priority:** Should

**Requirement:** If a non-critical subsystem fails, the public site must continue to serve content. Specific degradation scenarios:

| Failed Subsystem | Expected Behavior |
|-----------------|-------------------|
| AdSense script unreachable | Ad slots collapse (empty, no broken layout); editorial content is unaffected |
| Google Analytics unreachable | Pages load normally; no visible error; analytics data is lost for the period |
| Poll data query fails | Poll widget is hidden; rest of homepage renders |
| Search index unavailable | Search returns a user-friendly error message in Bulgarian; rest of site is unaffected |
| Email delivery service down | Contact form submissions are queued or logged locally; user sees normal confirmation |

**Rationale:** Third-party service outages should not take down the editorial product. Reader trust is maintained by always serving the news.

**Verification:** Simulate each failure scenario (block external script domains, stop the search index, disable email SMTP) and verify the site degrades as described. No full-page errors or blank pages should result from third-party failures.

**Owner:** ENG

---

### H. Observability

---

#### NFR-OB-001 — Application Error Logging

**Priority:** Must

**Requirement:** All unhandled exceptions and application errors must be logged with:

- Timestamp (UTC).
- Error type and message.
- Stack trace.
- Request URL and HTTP method.
- User identifier (for CMS-authenticated requests; "anonymous" for public visitors).

Logs must be written to a persistent location (file system log directory or Windows Event Log) and must survive application restarts. Log files must be rotated (e.g., daily rotation, retain 30 days) to prevent disk exhaustion.

**Rationale:** Without error logs, diagnosing production issues is guesswork. Persistent, rotated logs are the minimum observability baseline.

**Verification:** Trigger a known error (e.g., navigate to a URL that causes a handled exception) — verify the error appears in the log file with all required fields. Verify log rotation is configured (check log directory after 2+ days of operation).

**Owner:** ENG

---

#### NFR-OB-002 — Infrastructure Monitoring

**Priority:** Must

**Requirement:** The following VPS-level metrics must be monitored with alerts:

| Metric | Alert Threshold | Notification Method |
|--------|----------------|---------------------|
| CPU utilization | > 80% sustained for 5 minutes | Email to OPS |
| Memory utilization | > 85% sustained for 5 minutes | Email to OPS |
| Disk utilization | > 80% of allocated storage | Email to OPS |
| IIS application pool health | Recycled unexpectedly or stopped | Email to OPS |

Monitoring can use Windows Performance Counters, a lightweight agent (e.g., Prometheus windows_exporter, Datadog free tier, or a simple scheduled script), or the hosting provider's built-in monitoring.

**Rationale:** On a single VPS, resource exhaustion is the most likely cause of downtime. Proactive alerts enable intervention before users are affected.

**Verification:** Verify the monitoring tool/agent is installed and running. Simulate a high-CPU condition — verify alert is received within 5 minutes. Verify disk utilization alert fires correctly by testing with a threshold close to current usage.

**Owner:** OPS

---

#### NFR-OB-003 — Uptime Monitoring

**Priority:** Must

**Requirement:** An external uptime monitoring service must check the homepage URL (`https://predelnews.com/`) at least every **5 minutes** (every 1 minute preferred). Downtime alerts must be sent via email (and optionally SMS or messaging app) to the OPS contact within 5 minutes of detection.

**Rationale:** External monitoring detects outages that internal monitoring cannot (network issues, DNS failures, ISP problems). Fast alerting minimizes downtime duration.

**Verification:** Verify the monitoring service is configured and active. Simulate downtime (stop IIS) — verify alert is received within 5–10 minutes. Verify the alert includes the URL and timestamp.

**Owner:** OPS

---

#### NFR-OB-004 — Audit Trail Integrity

**Priority:** Must

**Requirement:** Audit logs (comment deletions, article state changes, user role changes, ad slot configuration changes) must be:

- Append-only: no user, including Admin, can edit or delete audit log entries through the CMS interface.
- Stored in the database with a tamper-evident structure (at minimum: sequential IDs, timestamps, and acting user are immutable after creation).
- Retained per NFR-PR-004 (24 months for comment-related entries; indefinite for article and user events unless a retention policy is defined later).

**Rationale:** Audit logs exist for accountability and potential legal obligations. If they can be tampered with, they have no evidentiary value.

**Verification:** Log in as Admin. Attempt to edit or delete an audit log entry via the CMS — must fail. Verify database table constraints prevent UPDATE or DELETE on audit log rows (or that no CMS interface exposes these operations). Inspect audit entries after a comment deletion — verify all fields are populated and immutable.

**Owner:** ENG

---

#### NFR-OB-005 — Backup Monitoring

**Priority:** Must

**Requirement:** Backup job outcomes (success/failure) must be logged and monitored. A failed backup must trigger an alert to OPS within 1 hour of the scheduled backup time. Two consecutive failed backups must trigger an escalated alert (email + secondary contact).

**Rationale:** Silent backup failures are the most common cause of data loss in small-team deployments. If nobody knows backups are failing, the RPO guarantee is void.

**Verification:** Simulate a backup failure (e.g., remove write access to the backup destination) — verify alert is sent. Verify backup success logs are recorded for the last 7 consecutive days.

**Owner:** OPS

---

### I. Maintainability & Operability

---

#### NFR-MN-001 — Deployment Process

**Priority:** Must

**Requirement:** A documented deployment procedure must exist that:

- Describes the steps to deploy a new version of the application to the production VPS.
- Includes a pre-deployment checklist (backup database, backup media, verify staging test).
- Includes a rollback procedure (restore previous deployment package + database backup).
- Completes within 15 minutes for a routine deployment (excluding backup time).
- Requires no remote desktop access to the production server for routine deployments (scripted or tool-based deployment preferred; manual RDP-based deployment is acceptable at MVP if documented).

**Rationale:** A small team cannot afford ambiguous deployment procedures. Documentation ensures any team member can deploy or roll back.

**Verification:** Document exists in `docs/technical/deployment.md`. A team member who did not write the procedure can follow it to complete a deployment to a test environment. Rollback procedure is tested at least once before launch.

**Owner:** ENG / OPS

---

#### NFR-MN-002 — Environment Parity

**Priority:** Should

**Requirement:** A staging or test environment must exist that mirrors the production environment in: OS version (Windows), IIS configuration, .NET runtime version, Umbraco version, and database engine version. The staging environment is used for pre-deployment testing and restore drills.

Content data in staging does not need to mirror production, but the schema and configuration must be identical.

**Rationale:** Deploying untested changes directly to production is the leading cause of outages in small-team projects. A staging environment catches environment-specific issues.

**Verification:** Compare production and staging environment configurations (OS version, .NET version, Umbraco version, IIS settings). Deploy the current production build to staging — verify it runs without errors.

**Owner:** OPS

---

#### NFR-MN-003 — Source Control

**Priority:** Must

**Requirement:** All application code, CMS configuration-as-code (Umbraco content type definitions, data type configurations), deployment scripts, and documentation must be stored in a Git repository. The `main` branch must always represent a deployable state. No production deployments may occur from uncommitted or unreviewed code.

**Rationale:** Version control is the foundation of reproducibility, auditability, and collaboration. It also serves as the code backup mechanism.

**Verification:** Verify the Git repository exists and contains all listed artifacts. Verify the latest production deployment matches a tagged commit on `main`. Verify no uncommitted files exist on the production server that are not in the repository.

**Owner:** ENG

---

#### NFR-MN-004 — OS & Server Patching

**Priority:** Must

**Requirement:** The Windows VPS operating system must have critical and security updates applied within **14 days** of release by Microsoft. Patch application must follow the planned maintenance protocol (NFR-RL-002): scheduled during low-traffic window, announced in advance, tested on staging first if possible.

**Rationale:** Unpatched Windows servers are a primary target for automated attacks. A 14-day window balances urgency with testing time.

**Verification:** Check Windows Update history on the production VPS monthly — verify no critical/security updates are pending for > 14 days. Document patch dates.

**Owner:** OPS

---

#### NFR-MN-005 — Documentation Requirements

**Priority:** Must

**Requirement:** The following operational documents must exist before launch:

| Document | Location | Owner |
|----------|----------|-------|
| Deployment procedure | `docs/technical/deployment.md` | ENG |
| Rollback procedure | `docs/technical/deployment.md` (section) | ENG |
| Backup configuration & schedule | `docs/technical/backup-and-restore.md` | OPS |
| Restore procedure (tested) | `docs/technical/backup-and-restore.md` (section) | OPS |
| Monitoring & alerting setup | `docs/technical/observability.md` | OPS |
| Incident response contacts | `docs/technical/incident-response.md` | OPS / PM |
| CMS user guide (basic) | `docs/editorial/cms-guide.md` | ENG / PM |

**Rationale:** Documentation is the safety net for a small team. If the primary operator is unavailable, another team member must be able to deploy, restore, or respond to an incident.

**Verification:** Each document exists at the specified path. A team member other than the author can follow the deployment and restore procedures successfully.

**Owner:** ENG / OPS / PM

---

#### NFR-MN-006 — Log Accessibility

**Priority:** Must

**Requirement:** Application error logs (NFR-OB-001) must be accessible to developers without requiring remote desktop access to the production server. Acceptable methods: a log file accessible via a secured (authenticated, HTTPS) URL, a log aggregation service, or a log download script.

**Rationale:** Requiring RDP access for log review slows incident diagnosis and increases the risk of accidental server changes.

**Verification:** A developer can retrieve the last 24 hours of application logs without RDP access. Verify the log access method is secured (requires authentication, served over HTTPS).

**Owner:** OPS / ENG

---

#### NFR-MN-007 — Database Maintenance

**Priority:** Should

**Requirement:** A documented database maintenance procedure should exist covering: index rebuilding, statistics updates, and log file management for the SQL Server database. Maintenance should run at least weekly, during off-peak hours.

**Rationale:** SQL Server performance degrades over time without index maintenance, especially as article and comment counts grow. Proactive maintenance prevents slow queries from affecting TTFB.

**Verification:** Verify a maintenance plan or scheduled job exists and runs weekly. Check SQL Server logs for maintenance completion. Monitor query performance trends over time.

**Owner:** OPS / ENG

---

### J. Compatibility

---

#### NFR-CM-001 — Browser Support

**Priority:** Must

**Requirement:** The public site must render correctly and all interactive features must function in the following browsers (latest 2 major versions at time of launch):

| Browser | Platform | Priority |
|---------|----------|----------|
| Google Chrome | Android, Windows, macOS | Must |
| Safari | iOS, macOS | Must |
| Samsung Internet | Android | Must |
| Mozilla Firefox | Windows, macOS | Should |
| Microsoft Edge | Windows | Should |

"Render correctly" means: no layout breakage, all content visible, all interactive features functional (navigation, search, comments, forms, poll voting, share buttons), and no console errors that affect functionality.

**Rationale:** Chrome and Safari dominate the Bulgarian market (> 85% combined). Samsung Internet has significant share on Android. Firefox and Edge are secondary but should not be broken.

**Verification:** Manual cross-browser testing of homepage, article page, archive page, contact form, and comment form in each listed browser (latest 2 versions). Automated screenshot comparison across browsers using a tool like Playwright or BrowserStack (if budget allows).

**Owner:** ENG / QA

---

#### NFR-CM-002 — Responsive Viewport Range

**Priority:** Must

**Requirement:** All public pages must render correctly on viewport widths from **320px to 1920px** with no horizontal scrollbar. Primary breakpoints:

| Breakpoint | Description |
|------------|-------------|
| 320px – 767px | Mobile (single column, hamburger nav, no sidebar) |
| 768px – 1023px | Tablet (adaptive layout) |
| 1024px – 1920px | Desktop (full layout with sidebar where applicable) |

Touch targets on mobile viewports must be ≥ 44×44px (per WCAG 2.1 AA target size recommendation).

**Rationale:** Mobile-first is a core product principle. The target audience primarily uses smartphones. 320px is the minimum width of a supported device (iPhone SE / budget Android).

**Verification:** Resize the browser window continuously from 320px to 1920px on the homepage and article page — no horizontal scrollbar at any width. Automated responsive testing at 320, 375, 414, 768, 1024, 1280, 1440, and 1920px widths. Tap target audit on mobile viewport using Lighthouse.

**Owner:** ENG / QA

---

#### NFR-CM-003 — Progressive Enhancement

**Priority:** Should

**Requirement:** Core content (article text, headlines, navigation links) must be accessible and readable even if JavaScript fails to load or is disabled. Interactive enhancements (poll voting, comment submission, share buttons) may require JavaScript but should show a graceful fallback (e.g., non-functional share links rather than missing elements) when JS is unavailable.

**Rationale:** Search engine crawlers, users with script blockers, and slow connections benefit from a functional no-JS baseline. Content is the primary product and must always be accessible.

**Verification:** Disable JavaScript in the browser. Navigate to the homepage and an article page — verify all editorial content is visible and readable. Verify navigation links work (server-side rendered). Verify interactive elements degrade gracefully (no blank areas or broken layouts).

**Owner:** ENG

---

## 5. Measurement & Verification Matrix

| NFR ID | Category | Verification Method | Tool / Approach | Frequency |
|--------|----------|-------------------|-----------------|-----------|
| NFR-UX-001 | UX | CSS audit + manual OS dark-mode test | grep, browser testing | Each deployment |
| NFR-UX-002 | UX | Visual regression | Screenshot comparison | Each deployment |
| NFR-UX-003 | UX | DOM inspection + manual review | Browser DevTools | Each deployment |
| NFR-UX-004 | UX | Full-site crawl + manual flow testing | Crawl script, manual QA | Pre-launch + monthly |
| NFR-UX-005 | UX | Navigate to invalid URL; force 500 | Manual + automated | Pre-launch + each deployment |
| NFR-UX-006 | UX | Mobile navigation test | Manual, Lighthouse | Pre-launch + each deployment |
| NFR-AC-001 | Accessibility | axe-core / Lighthouse + keyboard test | Automated + manual | Each deployment |
| NFR-AC-002 | Accessibility | Full WCAG audit | axe-core, WAVE, manual | Phase 1.1 (within 30 days) |
| NFR-AC-003 | Accessibility | HTML validation | W3C validator, axe-core | Each deployment |
| NFR-AC-004 | Accessibility | CMS validation + page scan | CMS test, automated crawl | Pre-launch + spot checks |
| NFR-AC-005 | Accessibility | OS reduced-motion test | Manual | Pre-launch |
| NFR-PF-001 | Performance | Lighthouse CI (mobile) | Lighthouse CI | Each deployment |
| NFR-PF-002 | Performance | Lighthouse CI score gate | Lighthouse CI | Each deployment |
| NFR-PF-003 | Performance | Synthetic monitoring + load test | UptimeRobot, k6/Artillery | Continuous + pre-launch |
| NFR-PF-004 | Performance | Lighthouse image audits + crawl | Lighthouse, crawl script | Each deployment |
| NFR-PF-005 | Performance | Lighthouse JS/CSS audits + DevTools | Lighthouse, Chrome DevTools | Each deployment |
| NFR-PF-006 | Performance | TTFB comparison (cold/warm cache) | curl, load test | Pre-launch + spot checks |
| NFR-PF-007 | Performance | Load test (5× spike) | k6/Artillery | Pre-launch + quarterly |
| NFR-PF-008 | Performance | Synthetic search queries | Scripted tests | Pre-launch + monthly |
| NFR-PF-009 | Performance | Disk monitoring + upload tests | Server monitoring, manual | Continuous + pre-launch |
| NFR-SE-001 | SEO | robots.txt fetch + crawl audit | curl, Google Search Console | Pre-launch + weekly |
| NFR-SE-002 | SEO | Sitemap validation + freshness test | Schema validator, manual | Pre-launch + weekly |
| NFR-SE-003 | SEO | Canonical tag crawl | Crawl script | Each deployment |
| NFR-SE-004 | SEO | Rich Results Test | Google Rich Results Test | Pre-launch + monthly |
| NFR-SE-005 | SEO | Slug change + redirect test | Manual | Pre-launch |
| NFR-SC-001 | Security | HTTP→HTTPS redirect + header check | curl, SSL Labs | Pre-launch + monthly |
| NFR-SC-002 | Security | Header inspection | securityheaders.com, curl | Each deployment |
| NFR-SC-003 | Security | Brute-force simulation + cookie audit | Manual + scripted | Pre-launch |
| NFR-SC-004 | Security | Path access test | Manual | Pre-launch |
| NFR-SC-005 | Security | Rate limit simulation per endpoint | Automated scripts | Pre-launch + after changes |
| NFR-SC-006 | Security | XSS payload submission | Manual + OWASP ZAP | Pre-launch + quarterly |
| NFR-SC-007 | Security | Code review + SQLi scan | Manual review, OWASP ZAP | Pre-launch + quarterly |
| NFR-SC-008 | Security | CSRF token inspection + tampered submit | Manual + scripted | Pre-launch |
| NFR-SC-009 | Security | Dependency vulnerability scan | `dotnet list package --vulnerable` | Monthly |
| NFR-SC-010 | Security | Source code grep + URL access test | grep, browser test | Pre-launch + each deployment |
| NFR-PR-001 | Privacy | Schema review + form field audit | Manual | Pre-launch |
| NFR-PR-002 | Privacy | Banner appearance + cookie test | Manual | Pre-launch |
| NFR-PR-003 | Privacy | DB query for old records | SQL query, manual/automated | Monthly |
| NFR-PR-004 | Privacy | DB query for old audit records | SQL query, manual/automated | Monthly |
| NFR-PR-005 | Privacy | Schema review + export test | Manual | Pre-launch |
| NFR-PR-006 | Privacy | Role-based access test for IP display | Manual (login as Writer) | Pre-launch |
| NFR-PR-007 | Privacy | Page existence + content review | Manual + legal review | Pre-launch |
| NFR-RL-001 | Reliability | External uptime monitor report | UptimeRobot or equivalent | Continuous |
| NFR-RL-002 | Reliability | Mock maintenance drill | Manual | Pre-launch |
| NFR-RL-003 | Reliability | Backup verification + off-site check | Manual/scripted | Daily (automated) + weekly (manual spot check) |
| NFR-RL-004 | Reliability | Full restore drill | Manual on test VPS | Pre-launch + quarterly |
| NFR-RL-005 | Reliability | Failure simulation per subsystem | Manual (block domains, stop services) | Pre-launch |
| NFR-OB-001 | Observability | Error trigger + log inspection | Manual | Pre-launch |
| NFR-OB-002 | Observability | Alert trigger simulation | Manual (simulate high CPU) | Pre-launch |
| NFR-OB-003 | Observability | Downtime simulation | Stop IIS | Pre-launch |
| NFR-OB-004 | Observability | Tamper attempt on audit logs | Manual (Admin CMS test) | Pre-launch |
| NFR-OB-005 | Observability | Backup failure simulation | Manual | Pre-launch |
| NFR-MN-001 | Maintainability | Deployment by non-author | Manual | Pre-launch |
| NFR-MN-002 | Maintainability | Environment comparison | Manual | Pre-launch |
| NFR-MN-003 | Maintainability | Repo audit | Manual | Pre-launch |
| NFR-MN-004 | Maintainability | Windows Update history check | Manual | Monthly |
| NFR-MN-005 | Maintainability | Document existence check | Manual | Pre-launch |
| NFR-MN-006 | Maintainability | Log retrieval without RDP | Manual | Pre-launch |
| NFR-MN-007 | Maintainability | Maintenance job log check | Manual | Weekly |
| NFR-CM-001 | Compatibility | Cross-browser testing | Manual + Playwright/BrowserStack | Pre-launch + each major deployment |
| NFR-CM-002 | Compatibility | Responsive resize + tap target audit | Manual + Lighthouse | Each deployment |
| NFR-CM-003 | Compatibility | JS-disabled navigation test | Manual | Pre-launch |

---

## 6. Definition of Done (NFR) — MVP

The MVP is considered non-functionally complete when **all Must-priority** items below are confirmed:

### Performance
- [ ] Lighthouse Performance score ≥ 90 on mobile for homepage, article page, and one archive page (with ads loaded).
- [ ] Core Web Vitals (LCP ≤ 2.5s, INP ≤ 200ms, CLS ≤ 0.1) pass on mobile simulation for all page types.
- [ ] TTFB ≤ 600ms (95th percentile) under normal traffic, confirmed by synthetic monitoring.
- [ ] Load test demonstrates 5× spike resilience (no 503s, page load ≤ 5s for 95th percentile).
- [ ] Output caching is active; editorial changes reflected within 60 seconds.
- [ ] Images served with responsive `srcset`, lazy loading, and ≤ 200 KB per variant.

### Security
- [ ] HTTPS enforced on all pages; HSTS header present; SSL Labs grade ≥ A.
- [ ] Security headers present on all responses (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, CSP in at least report-only mode).
- [ ] CMS lockout after 5 failed logins; strong password policy enforced; session cookies are Secure/HttpOnly/SameSite.
- [ ] Rate limiting active on all form endpoints (comments, contact, email signup, CMS login).
- [ ] XSS test: script tag in comment/search does not execute on public pages.
- [ ] No secrets in source code or client-accessible files.
- [ ] CSRF tokens present on all state-changing forms.

### Privacy & GDPR
- [ ] Cookie consent banner displayed on first visit with Bulgarian text and "Приемам" button.
- [ ] Privacy policy page published and linked from footer and cookie banner.
- [ ] Data minimization confirmed: no extra personal data fields beyond documented set.
- [ ] Comment IP addresses visible only to Editor/Admin, not Writers or public.
- [ ] Data retention policies documented (12 months contact form, 24 months audit logs).

### Accessibility
- [ ] Zero critical axe-core violations on homepage, article page, archive page, contact page, and search results.
- [ ] Keyboard-only navigation completes all primary user flows without traps.
- [ ] All form inputs have labels; all images have alt text; focus indicators are visible.
- [ ] Skip-to-content link present on all pages.

### UX & Branding
- [ ] Light theme enforced; no dark mode activates under any OS/browser condition.
- [ ] Custom 404 and 500 error pages display in Bulgarian with site navigation.
- [ ] All visitor-facing text is in Bulgarian (no English placeholders).
- [ ] Ad slots display "Реклама" label; sponsored articles display "Платена публикация" in all contexts.

### SEO
- [ ] `/sitemap.xml` valid and includes all published content; updates within 60 minutes.
- [ ] `/robots.txt` correctly configured (allow public, disallow `/umbraco/`).
- [ ] Canonical URLs present and correct on all pages.
- [ ] JSON-LD structured data (NewsArticle, BreadcrumbList) passes Google Rich Results Test.

### Reliability & Operations
- [ ] External uptime monitoring active (homepage checked every ≤ 5 minutes).
- [ ] Daily database and media file backups running, stored off-site, verified.
- [ ] Restore procedure documented and tested at least once.
- [ ] Deployment and rollback procedures documented.
- [ ] Application error logging active with 30-day rotation.
- [ ] Infrastructure monitoring with alerts for CPU, memory, and disk.
- [ ] Backup failure alerts configured and tested.

### Compatibility
- [ ] Site renders correctly in Chrome (Android + desktop), Safari (iOS + macOS), and Samsung Internet (latest 2 versions each).
- [ ] No horizontal scrollbar at any viewport width from 320px to 1920px.

---

## 7. Out of Scope / Future Enhancements

The following items are explicitly **not required for MVP** but are documented as future enhancements:

| Item | Target Phase | Notes |
|------|-------------|-------|
| **CDN for static assets and images** | Phase 2 | Recommended: Cloudflare (free tier). Migrate media storage to CDN origin. Will significantly improve global latency and spike resilience. NFR-PF-003 and NFR-PF-007 targets should be tightened post-CDN. |
| **WAF (Web Application Firewall)** | Phase 2 | Recommended: Cloudflare WAF or equivalent. Provides DDoS protection, bot management, and additional OWASP rule enforcement beyond application-layer defenses. |
| **MFA for CMS accounts** | Phase 1.1 or Phase 2 | Strongly recommended for Admin accounts. Evaluate Umbraco's built-in 2FA support or third-party providers. |
| **Full GDPR consent management** | Phase 2 | Replace the basic cookie notice with a granular consent banner (accept/reject per cookie category). Gate analytics script loading behind consent. Evaluate tools like Cookiebot, CookieYes, or open-source alternatives. |
| **Full WCAG 2.1 AA compliance** | Phase 1.1 (within 30 days) | Complete audit and remediation of all Level AA criteria not covered by NFR-AC-001. |
| **Content Security Policy enforcement** | Phase 1.1 | Move CSP from report-only to enforced mode once all legitimate script/style sources are cataloged and violations are resolved. |
| **Automated performance regression testing** | Phase 2 | Integrate Lighthouse CI into a CI/CD pipeline with score gates that block deployment. |
| **Real User Monitoring (RUM)** | Phase 2 | Supplement synthetic monitoring with real field data (e.g., via Google CrUX or a lightweight RUM library). |
| **Automated data retention enforcement** | Phase 2 | Replace manual retention procedures (NFR-PR-003, 004) with automated scheduled jobs that delete/anonymize expired records. |
| **Load balancing / horizontal scaling** | Phase 2+ | If traffic exceeds single-VPS capacity (> 50K monthly uniques or frequent sustained spikes), evaluate adding a second VPS behind a load balancer. |
| **Dark mode** | Not planned | Explicitly out of scope per PRD §3. No timeline for reconsideration. |

---

*End of Non-Functional Requirements. This document is maintained in `docs/business/non-functional-requirements.md` and should be updated as requirements evolve. Changes must be reviewed by the Solutions Architect, QA Lead, and Product Manager.*
