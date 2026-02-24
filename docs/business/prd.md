# Product Requirements Document — PredelNews

**Product:** PredelNews — Regional News Website for Southwest Bulgaria
**Domain:** predelnews.com
**Platform:** Umbraco 17 LTS (.NET 10)
**Document owner:** Product Manager
**Status:** Draft v1.0
**Last updated:** 2025-02-23

> **Repository path:** `business/prd.md`
> Related documents: `business/functional-specification.md` · `business/non-functional-requirements.md` · `business/go-to-market.md` · `business/media-kit.md` · `technical/technical-specification.md` · `technical/architecture.md` · `technical/database-schema.md` · `technical/observability.md` · `planning/epics/` · `planning/development-plan.md`

---

## 1. Executive Summary

PredelNews is a Bulgarian-language news website focused on original journalism for Southwest Bulgaria, with the Blagoevgrad region as its primary coverage area. The site will be built on Umbraco 17 LTS and developed using Claude Code. It will launch with zero legacy content and a lean editorial team (1 editor, 1–3 writers).

The product serves two core jobs: (1) give residents of underserved regional communities a trustworthy, fast-loading source for local news, and (2) provide a sustainable monetization model through display advertising (Google AdSense) and clearly labeled sponsored/paid articles.

Phase 1 (MVP) delivers the public-facing news site with editorial workflow, ad placements, and a custom comments system. Phase 2 adds newsletter distribution, richer analytics dashboards, and expanded monetization features.

---

## 2. Problem Statement & Opportunity

### Problem

Southwest Bulgaria — particularly the Blagoevgrad, Kyustendil, and Pernik regions — is underserved by dedicated, trustworthy digital news outlets. Existing coverage is either national media with shallow regional depth, or small blogs with inconsistent publishing and poor user experience. Readers have no single destination for timely, well-structured regional news they can trust.

### Opportunity

- **Audience gap:** Regional readers actively search for local news but find fragmented, low-quality sources.
- **Advertising potential:** Local businesses (restaurants, clinics, retailers, real-estate agencies) need digital advertising channels that reach a geographically relevant audience. National ad networks cannot offer this granularity.
- **Low competition:** Few professional-grade news websites target this geography, creating a first-mover advantage for a well-executed product.
- **SEO upside:** Original, region-specific content with disciplined SEO practices can dominate local search results quickly.

---

## 3. Goals and Non-Goals

### Goals

| ID | Goal | Measured by |
|----|------|-------------|
| G1 | Launch a fast, mobile-first regional news site with professional editorial quality | Lighthouse performance score ≥ 90; Core Web Vitals pass |
| G2 | Establish a reliable editorial workflow for a small team | Avg. time from draft to publish ≤ 2 hours for breaking news |
| G3 | Monetize through display ads and sponsored content from Month 1 | First AdSense revenue within 30 days of launch |
| G4 | Build an email subscriber list from Day 1 | ≥ 500 emails collected in first 90 days |
| G5 | Earn reader trust through transparency and editorial integrity | Visible correction policy; labeled sponsored content; no anonymous sourcing without editorial note |

### Non-Goals (explicitly out of scope)

- **Dark mode** — not supported.
- **Reader accounts / paywall / membership** — not in Phase 1 or Phase 2.
- **Multi-language support** — Bulgarian only.
- **Mobile native apps** — responsive web only.
- **Content scraping or syndication** — all content is original.
- **Video hosting / live streaming** — embed-only (YouTube/Facebook) in Phase 1.
- **Forum or community features** — beyond article comments.
- **Newsletter sending** — Phase 2 (email collection only in Phase 1).

---

## 4. Target Audience & Personas

### 4.1 Persona: Regional Reader — "Мария"

- **Age:** 30–55
- **Location:** Blagoevgrad, Sandanski, Gotse Delchev, Petrich
- **Behavior:** Checks news on her phone during commute and lunch break; shares articles on Facebook; cares about local politics, crime/incidents, and community events.
- **Needs:** Fast page load on mid-range Android device over 4G; easy scanning of headlines; trust that content is original and factual.
- **Frustration:** Pop-up overload, clickbait headlines, outdated articles without timestamps.

### 4.2 Persona: National/Diaspora Reader — "Георги"

- **Age:** 25–45
- **Location:** Sofia or abroad (Western Europe diaspora)
- **Behavior:** Searches Google for news about his hometown; lands on article pages via search or social share; reads 1–2 articles and leaves.
- **Needs:** Clear dateline and region labels so he knows the article is relevant; social sharing metadata (OG tags) that look professional when shared in Viber/Facebook groups.

### 4.3 Persona: Local Business Owner — "Стоян"

- **Age:** 35–60
- **Behavior:** Wants to advertise his hotel/restaurant/clinic to the regional audience; currently uses Facebook ads only.
- **Needs:** Simple ad booking process; transparent pricing; proof of reach (page views, impressions). See `business/media-kit.md`.

### 4.4 Persona: Writer — "Ива"

- **Role:** Freelance or part-time journalist
- **Needs:** Simple CMS interface to draft articles, attach images, assign categories/regions/tags, and submit for review. No training on complex CMS features.

### 4.5 Persona: Editor-in-Chief — "Петър"

- **Role:** Reviews, edits, and publishes content; manages editorial calendar; moderate comments; handles corrections.
- **Needs:** Dashboard showing drafts awaiting review; ability to schedule articles; quick publish for breaking news; comment moderation tools; basic traffic stats at a glance.

---

## 5. Product Principles

These principles guide every product and design decision:

1. **Editorial Trust** — Every article is original. Sources are cited. Corrections are published visibly. Sponsored content is always labeled. Reader trust is the product's most valuable asset; it is never traded for short-term revenue.

2. **Transparency** — The site clearly identifies its team (About page), advertising policies (Ads page), and editorial standards. Sponsored articles carry a persistent, non-removable label.

3. **Speed** — Pages load fast on low-end devices and 4G networks. Performance is a feature, not an afterthought. Target: ≤ 2.5 s Largest Contentful Paint on mobile.

4. **SEO Discipline** — Every content type has structured data (Article schema, BreadcrumbList), semantic HTML, canonical URLs, XML sitemaps, and optimized meta tags. URLs are clean, human-readable, and permanent.

5. **Simplicity for the Team** — The CMS experience must be learnable in under 1 hour for a writer. Workflow friction is minimized: the fewest clicks from idea to published article.

6. **Mobile-First** — Design and develop for the phone viewport first; scale up to desktop. No feature is desktop-only.

---

## 6. Key Features

### 6.1 Visitor Experience (Public Website)

#### 6.1.1 Homepage

The homepage is the editorial storefront. It is composed of the following content blocks, ordered top-to-bottom:

| Block | Description | Content Rules |
|-------|-------------|---------------|
| **Header / Navigation** | Logo, main nav (topic categories), region dropdown/filter, search icon | Sticky on scroll (mobile: collapsed hamburger) |
| **Breaking / Hot News Banner** | 1 featured article (large image + headline) + 3–5 breaking headlines list | Editorially curated; editor pins articles to this block |
| **National & World Headlines** | Horizontal card row: 4–6 articles from "България" region + "World" | Auto-populated by most recent; editor can override |
| **Category Blocks** | One block per active topic category (e.g., Политика, Криминално, Спорт, Култура) showing 3–4 latest articles each | Auto-populated; "Виж всички" link to category archive |
| **Latest Articles Feed** | Chronological reverse-date list, paginated or infinite-scroll | All published articles regardless of category |
| **Poll Widget** | Single active poll with 2–4 answer options; shows results after vote | One active poll at a time; editor creates/closes polls; cookie-based duplicate vote prevention |
| **Email Signup Bar** | Single email input + submit; stores email in database for future newsletter (Phase 2) | GDPR-compliant consent checkbox; confirmation message on submit |
| **Footer** | Links: Всички новини, За нас, Реклама, Рекламна оферта, Контакти | Static pages managed in Umbraco |

#### 6.1.2 Article Page

| Element | Details |
|---------|---------|
| **Headline (H1)** | Required; max 120 characters recommended |
| **Dateline** | Publish date + time (Bulgarian format: DD.MM.YYYY, HH:MM); updated date shown if edited |
| **Category label** | Linked pill/badge to topic category |
| **Region label** | Linked pill/badge to region |
| **Cover image** | Required; responsive srcset; lazy-loaded; alt text required for accessibility |
| **Article body** | Rich text (Umbraco block list or RTE); supports headings, bold/italic, links, embedded images, YouTube/Facebook embeds, block quotes |
| **Tags** | 0–10 keyword tags; each links to a tag archive page |
| **Sponsored label** | If article is sponsored: persistent, non-dismissible banner reading "Платена публикация" (Paid content) at top and bottom of body |
| **Author byline** | Author name (linked to author archive page) |
| **Share buttons** | Facebook, Viber, Copy Link (minimum); OG and Twitter Card meta tags for rich previews |
| **Related articles** | 3–4 articles from same category or matching tags; algorithm: shared tags > same category > most recent |
| **Comments section** | See §6.1.3 |
| **Ad slots** | See §6.4 |

#### 6.1.3 Comments System (Custom-Built)

- Anonymous posting allowed (display name + comment text; no account required).
- Comments appear immediately (no pre-moderation queue).
- **Anti-spam measures:**
  - Rate limiting: max 3 comments per IP per 5-minute window.
  - Simple math-based or honeypot challenge (no third-party CAPTCHA dependency in MVP). **[ASSUMPTION: honeypot + server-side rate limiting preferred over reCAPTCHA to avoid third-party dependency; can add reCAPTCHA later if spam escalates.]**
  - Link limit: comments containing ≥ 2 URLs are held for manual review.
  - Configurable banned-word list.
- **Moderation:**
  - Writers, Editors, and Admins can delete any comment.
  - All deletions are audit-logged (who deleted, when, original content preserved in logs).
- Comments are flat (no threading) in MVP. **[ASSUMPTION: flat comments are sufficient for Phase 1; threading is a Phase 2 candidate.]**
- Comment count shown on article cards on the homepage and archive pages.

#### 6.1.4 Archive & Taxonomy Pages

- **Category archive:** Paginated list of articles filtered by topic category (e.g., `/категория/политика/`).
- **Region archive:** Paginated list filtered by region (e.g., `/регион/благоевград/`).
- **Tag archive:** Paginated list filtered by tag.
- **Author archive:** Paginated list filtered by author.
- **"All News" page:** Full reverse-chronological feed of all published articles, paginated.
- Each archive page has its own SEO meta title/description template.

#### 6.1.5 Search

- Server-side search powered by Umbraco Examine (Lucene-based).
- Search results page with article title, excerpt, date, category, and region.
- Search query tracked in analytics.

#### 6.1.6 Footer Pages (Static)

| Page | Slug | Purpose |
|------|------|---------|
| Всички новини | `/всички-новини/` | Full article archive |
| За нас | `/за-нас/` | About the team, mission, editorial standards |
| Реклама | `/реклама/` | Advertising information, rate card summary, contact |
| Рекламна оферта | `/рекламна-оферта/` | Downloadable media kit (PDF), detailed pricing; see `business/media-kit.md` |
| Контакти | `/контакти/` | Contact form, email, phone, social links |

**[ASSUMPTION: URL slugs will be transliterated to Latin characters for maximum compatibility (e.g., `/vsichki-novini/`, `/za-nas/`). Final slug strategy to be confirmed in `technical/technical-specification.md`.]**

#### 6.1.7 Poll Widget

- Editor creates a poll question with 2–4 options.
- Only one poll is active at a time (displayed on homepage).
- Vote stored with cookie-based duplicate prevention (no login required).
- Results shown immediately after voting (percentage bar chart).
- Poll has open/close dates; closed polls show final results.
- Past polls accessible via an admin archive (not public in MVP).

### 6.2 Editorial Workflow

#### 6.2.1 Roles & Permissions

| Role | Permissions |
|------|-------------|
| **Writer** | Create draft articles; edit own drafts; submit for review; delete comments on any article |
| **Editor** | All Writer permissions + edit/publish/unpublish/schedule any article; manage categories, regions, tags; create/close polls; delete comments; manage email subscribers list; access content analytics |
| **Admin** | All Editor permissions + manage users/roles; manage ad placements; manage site settings; access audit logs; manage footer pages |

#### 6.2.2 Article Lifecycle

```
[Draft] → [In Review] → [Published]
                ↑              ↓
                └── [Unpublished / Revision]
```

- **Draft:** Writer creates/edits. Not visible on public site.
- **In Review:** Writer submits. Editor receives notification (email or CMS dashboard alert). Article is locked from writer edits until editor acts.
- **Published:** Live on site. Editor can unpublish or make post-publication edits (which re-trigger "Updated" dateline).
- **Scheduled:** Editor can set a future publish date/time.

#### 6.2.3 Article Editor Fields

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Headline | Text | ✅ | Max 120 chars recommended |
| Slug | Auto-generated | ✅ | From headline; editable |
| Subtitle / Deck | Text | ❌ | Short summary for cards/SEO |
| Body | Rich Text / Block List | ✅ | |
| Cover Image | Media Picker | ✅ | Alt text required |
| Topic Category | Dropdown (single) | ✅ | |
| Region | Dropdown (single) | ✅ | |
| Tags | Tag Picker (multi) | ❌ | 0–10 |
| Author | Content Picker | ✅ | Linked to Author node |
| SEO Title | Text | ❌ | Falls back to Headline |
| SEO Description | Textarea | ❌ | Falls back to first 160 chars of body |
| OG Image | Media Picker | ❌ | Falls back to Cover Image |
| Is Sponsored | Toggle | ✅ (default: off) | When on: "Платена публикация" label is enforced site-wide and cannot be hidden |
| Sponsor Name | Text | Conditional | Required if Is Sponsored = true |
| Publish Date | Date/Time Picker | ❌ | For scheduling; defaults to now |

### 6.3 Admin Features

- **Dashboard:** Article count (draft / in review / published today/this week), recent comments, email signups count.
- **Taxonomy management:** CRUD for topic categories, regions, tags.
- **Poll management:** Create, activate, close polls; view results.
- **Email list export:** CSV download of collected email addresses.
- **Ad placement management:** See §6.4.
- **Audit log viewer:** Comment deletions, article state changes, user actions.
- **Site settings:** Site name, social links, default SEO metadata, analytics tracking code field (Google Analytics / Tag Manager snippet).

### 6.4 Advertising & Sponsored Content

#### 6.4.1 Display Ad Slots

Ad slots are defined template regions where AdSense code or direct-sold banner HTML is injected. Slot configuration is managed in the CMS (Admin role).

| Slot ID | Location | Format | Notes |
|---------|----------|--------|-------|
| `ad-header-leaderboard` | Below header, above content | 728×90 / responsive | Desktop prominent; collapses on mobile or switches to 320×100 |
| `ad-sidebar-1` | Right sidebar (desktop) top | 300×250 | Sticky optional |
| `ad-sidebar-2` | Right sidebar (desktop) mid | 300×600 / 300×250 | |
| `ad-article-mid` | Inserted after 3rd paragraph of article body | In-content responsive | |
| `ad-article-bottom` | Below article body, above comments | 728×90 / responsive | |
| `ad-footer-banner` | Above footer | 970×90 / responsive | |

Each slot supports two modes:
1. **AdSense (auto):** Slot renders the configured AdSense ad unit code.
2. **Direct-sold:** Admin uploads a banner image, destination URL, alt text, and optional impression tracking pixel. Direct-sold takes priority over AdSense when active.

All ad slots must be clearly distinguishable from editorial content (CSS border/label "Реклама").

#### 6.4.2 Sponsored / Paid Articles

- A sponsored article is a full article created through the normal editorial workflow with `Is Sponsored = true`.
- **Labeling rules (non-negotiable):**
  - A visible "Платена публикация" (Paid content) banner appears at the top of the article, above the headline.
  - The same label appears at the bottom of the article body.
  - The sponsor name is displayed.
  - The label styling is defined in the global stylesheet and cannot be overridden per-article.
  - Sponsored articles are included in feeds and archives but the label/badge is visible on article cards as well.
- **SEO:** Sponsored articles carry `rel="sponsored"` on any outbound links to the sponsor. **[ASSUMPTION: this aligns with Google's guidelines for paid content.]**
- **Governance:** Only the Editor or Admin can toggle `Is Sponsored`. Writers cannot mark their own articles as sponsored.
- **Pricing and packages:** Defined in `business/media-kit.md`.

---

## 7. User Journeys

### 7.1 Visitor Journey — "Мария reads morning news"

1. Opens `predelnews.com` on her phone (Chrome Android).
2. Sees the Breaking News banner with the top story; scans the 5 breaking headlines.
3. Taps a headline about a local infrastructure project → article page loads in < 2.5 s.
4. Reads the article; sees it is tagged "Благоевград" and category "Общество."
5. Scrolls past the article; sees related articles and the comments section.
6. Leaves an anonymous comment (enters display name "Мария" and comment text; passes honeypot check).
7. Shares the article link via Viber using the share button.
8. Returns to homepage; scrolls to "Криминално" category block; taps another article.
9. Sees the email signup bar; enters her email for future newsletter.
10. Session ends.

### 7.2 Writer Journey — "Ива writes an article"

1. Logs into Umbraco backoffice (`predelnews.com/umbraco`).
2. Clicks "Create Article."
3. Enters headline, body text, selects cover image from media library (uploads if new), picks topic category "Общество" and region "Благоевград," adds 3 tags.
4. Previews the article.
5. Sets status to "In Review" and clicks Save.
6. Editor "Петър" receives a dashboard notification.
7. Ива can view her draft's status but cannot edit until the editor acts.

### 7.3 Editor Journey — "Петър reviews and publishes"

1. Opens Umbraco dashboard; sees 2 articles "In Review."
2. Opens Ива's article; reads, makes minor edits to the headline and body.
3. Checks that cover image has alt text; confirms SEO description is populated.
4. Sets publish date to "Now" and clicks "Publish."
5. Article appears on the live site within 60 seconds (cache purge).
6. Later, Петър notices a factual error reported via the contact form.
7. Edits the article, adds a correction note in the body, and republishes; "Updated" dateline appears.
8. Checks the comments section for a reported offensive comment; deletes it (audit-logged).

### 7.4 Admin Journey — "Managing ad placements"

1. Logs into Umbraco as Admin.
2. Navigates to Ad Management section.
3. Sees the 6 defined ad slots. `ad-sidebar-1` is currently running AdSense.
4. A local hotel has booked a direct banner for 30 days. Admin clicks `ad-sidebar-1`, switches mode to "Direct-sold."
5. Uploads the hotel's banner image (300×250), enters the destination URL and alt text, sets start/end dates.
6. Saves. The sidebar now shows the hotel banner instead of AdSense.
7. After 30 days, the slot automatically reverts to AdSense.

### 7.5 Sales / Ads Manager Journey — "Selling a sponsored article"

1. Local business contacts PredelNews via the Контакти page or directly.
2. Sales person (or founder) agrees on a sponsored article package (see `business/media-kit.md`).
3. Business provides article text and images (or PredelNews writes it for a fee).
4. Writer creates the article in Umbraco with `Is Sponsored = true`, enters sponsor name.
5. Editor reviews, ensures the "Платена публикация" label is visible, confirms outbound links have `rel="sponsored"`, and publishes.
6. Admin optionally pins the sponsored article to a homepage block for the agreed duration.
7. At the end of the campaign, Admin unpins the article; it remains in the archive with the sponsored label permanently.

---

## 8. MVP Scope — Phase 1

### 8.1 Included in MVP

Each feature below includes acceptance criteria (AC) that are testable.

#### F1: Homepage

- **AC1.1:** Homepage loads with all blocks described in §6.1.1 populated with content (or graceful empty states if < 5 articles exist).
- **AC1.2:** Breaking News block shows exactly 1 featured image article + up to 5 headline links, as curated by the editor.
- **AC1.3:** Category blocks auto-populate with the 3–4 most recent published articles per category.
- **AC1.4:** Latest Articles feed paginates at 20 articles per page.
- **AC1.5:** Page achieves Lighthouse Performance score ≥ 90 on mobile simulation.
- **AC1.6:** Poll widget displays current active poll; vote is recorded and results are shown; duplicate vote from same browser is prevented.

#### F2: Article Page

- **AC2.1:** All fields from §6.1.2 render correctly (headline, dateline, category, region, image, body, tags, author, share buttons).
- **AC2.2:** Sponsored articles display "Платена публикация" banner at top and bottom; banner cannot be hidden via CMS fields.
- **AC2.3:** OG meta tags (title, description, image, type, URL) and Twitter Card tags are present and correct (verifiable via Facebook Sharing Debugger and Twitter Card Validator).
- **AC2.4:** Related articles section shows 3–4 relevant articles.
- **AC2.5:** Article page achieves Lighthouse Performance score ≥ 90.

#### F3: Taxonomy & Archive Pages

- **AC3.1:** Category, region, tag, and author archive pages render paginated article lists.
- **AC3.2:** Each archive page has a unique, templated SEO meta title and description.
- **AC3.3:** "All News" page shows all published articles in reverse-chronological order, paginated.

#### F4: Search

- **AC4.1:** Search returns relevant results for Bulgarian-language queries within 500 ms.
- **AC4.2:** Search results display article title, excerpt, date, category, and region.
- **AC4.3:** Empty results show a user-friendly message.

#### F5: Comments System

- **AC5.1:** Anonymous user can submit a comment with display name and text.
- **AC5.2:** Comment appears on the article page immediately after submission.
- **AC5.3:** Rate limiting blocks the 4th comment from the same IP within 5 minutes (returns user-friendly error).
- **AC5.4:** Comments with ≥ 2 URLs are held and not displayed until reviewed.
- **AC5.5:** Editor/Admin/Writer can delete a comment; deletion is audit-logged with original content.
- **AC5.6:** Honeypot field is present and hidden; submissions filling it are silently discarded.

#### F6: Editorial Workflow

- **AC6.1:** Writer can create, edit, and submit articles for review.
- **AC6.2:** Editor is notified of pending reviews (dashboard indicator).
- **AC6.3:** Editor can publish, schedule, unpublish, and edit any article.
- **AC6.4:** Article status transitions follow the lifecycle in §6.2.2.
- **AC6.5:** Scheduled articles auto-publish at the set date/time (± 1 minute tolerance).

#### F7: Ad Slots

- **AC7.1:** All 6 ad slots from §6.4.1 render on the correct pages.
- **AC7.2:** AdSense code renders and loads ads (verified in AdSense console).
- **AC7.3:** Admin can switch any slot to Direct-sold mode with image, URL, and date range.
- **AC7.4:** Direct-sold slot reverts to AdSense after the end date.
- **AC7.5:** All ad slots are labeled "Реклама."

#### F8: Sponsored Articles

- **AC8.1:** `Is Sponsored` toggle is available only to Editor and Admin roles.
- **AC8.2:** Published sponsored article displays "Платена публикация" label in all contexts (article page, homepage card, archive card).
- **AC8.3:** Outbound links in sponsored articles carry `rel="sponsored noopener"`.

#### F9: Footer Pages

- **AC9.1:** All 5 footer pages (Всички новини, За нас, Реклама, Рекламна оферта, Контакти) are published and accessible.
- **AC9.2:** Contact form submits successfully and delivers to the configured email address.

#### F10: Email Collection

- **AC10.1:** Email signup bar is visible on the homepage.
- **AC10.2:** Submitted email is stored in the database with timestamp and consent flag.
- **AC10.3:** Admin can export the email list as CSV.
- **AC10.4:** Duplicate email submissions are handled gracefully (no error; no duplicate stored).

#### F11: SEO Foundations

- **AC11.1:** XML sitemap is generated and accessible at `/sitemap.xml`.
- **AC11.2:** `robots.txt` is configured correctly.
- **AC11.3:** Article pages include JSON-LD `Article` schema markup.
- **AC11.4:** Breadcrumb navigation is present and marked up with `BreadcrumbList` schema.
- **AC11.5:** Canonical URLs are set on all pages.
- **AC11.6:** Favicon and web app manifest are configured.

#### F12: Analytics Integration

- **AC12.1:** Google Analytics 4 (or Tag Manager container) tracking code fires on every page.
- **AC12.2:** Tracking code snippet is configurable via CMS site settings (Admin).

#### F13: Responsive Design (Light Mode Only)

- **AC13.1:** All pages render correctly on viewports from 320px to 1920px.
- **AC13.2:** No horizontal scroll on any page at any viewport.
- **AC13.3:** Dark mode is NOT supported; no dark color scheme is applied regardless of OS preference.

### 8.2 Explicitly Excluded from MVP

- Newsletter sending and campaign management.
- Reader accounts / login / registration.
- Multi-language support.
- Dark mode.
- Comment threading / replies.
- Comment pre-moderation queue (comments are live immediately).
- Advanced analytics dashboards (beyond GA4).
- Social login.
- Push notifications.
- AMP pages.
- Native mobile apps.
- A/B testing infrastructure.
- Content migration tools (starting from zero).

---

## 9. Phase 2 / V1 Scope

The following features are planned for the first post-MVP iteration:

| Feature | Description | Dependency |
|---------|-------------|------------|
| **Newsletter** | Integration with email platform (Mailchimp or similar); weekly digest; subscriber management | Email list from Phase 1 |
| **Comment threading** | One level of reply nesting | Phase 1 comments system |
| **reCAPTCHA** | Add Google reCAPTCHA v3 if spam volume exceeds manual moderation capacity | Spam metrics from Phase 1 |
| **Enhanced poll** | Public poll archive page; multiple concurrent polls; poll results sharing | Phase 1 poll widget |
| **Editorial analytics dashboard** | In-CMS dashboard showing top articles, traffic trends, comment activity | GA4 data API or server-side analytics |
| **Expanded regions** | Add more regions as coverage grows | Taxonomy system from Phase 1 |
| **Push notifications** | Web push for breaking news | Service worker infrastructure |
| **Media gallery** | Photo gallery article type for events | Media library expansion |
| **RSS feeds** | Per-category and per-region RSS feeds | Taxonomy system |
| **Performance monitoring** | Real User Monitoring (RUM) dashboard | See `technical/observability.md` |
| **Direct ad reporting** | Impression/click counts for direct-sold banners visible to Admin | Phase 1 ad slots |
| **Cookie consent banner** | Full GDPR/ePrivacy cookie consent management | Legal review |

---

## 10. Monetization Strategy

### 10.1 Revenue Streams

| Stream | Phase | Description |
|--------|-------|-------------|
| **Google AdSense** | 1 | Automated display ads across 6 slots; revenue scales with traffic |
| **Direct-sold banners** | 1 | Premium placements sold directly to local businesses at fixed CPM or flat monthly rates |
| **Sponsored articles** | 1 | Paid articles from businesses/organizations, clearly labeled, published through editorial workflow |
| **Newsletter sponsorship** | 2 | Sponsored section in weekly newsletter |

### 10.2 Ad Slot Pricing Model

Detailed pricing, packages, and rate cards are defined in `business/media-kit.md`. Summary principles:

- Direct-sold slots are priced at a premium over estimated AdSense eCPM.
- Sponsored articles are priced per-article with optional homepage pinning as an upsell.
- Introductory launch pricing for the first 90 days to build advertiser relationships.

### 10.3 Sponsored Content Governance

These rules are non-negotiable and encoded in the CMS:

1. Sponsored articles must have `Is Sponsored = true` — this is enforced at the data model level.
2. The "Платена публикация" label is rendered by the page template; it cannot be removed per-article.
3. Only Editor or Admin can set the sponsored flag.
4. Outbound links in sponsored content automatically receive `rel="sponsored noopener"`.
5. Sponsored articles are never auto-featured in the Breaking News block unless explicitly pinned by the Editor.
6. The ratio of sponsored to editorial content should not exceed 1:10 in any given week. **[ASSUMPTION: this ratio is a reasonable starting guideline; can be adjusted based on advertiser demand and reader feedback.]**

---

## 11. Content & Editorial Policy Summary

> A full editorial policy document should be created separately. This section captures the key principles that affect the product.

### 11.1 Source Standards

- All articles are original reporting by PredelNews writers.
- No scraping, reposting, or automated aggregation of content from other publishers.
- When referencing other publications, proper attribution with links is required.
- Anonymous sourcing must be approved by the Editor and noted in the article (e.g., "source who requested anonymity").

### 11.2 Corrections Policy

- Factual errors are corrected in-place with a visible correction note appended to the article body.
- The article's "Updated" dateline reflects the correction time.
- Corrections are never silent; the original error and the fix should be described.

### 11.3 Sponsored Content Labeling

- See §6.4.2 and §10.3. Sponsorship is always disclosed. No native advertising disguised as editorial.

### 11.4 AI Usage Policy

- **Content generation:** AI tools (including Claude) may be used as writing aids (research assistance, grammar checking, headline brainstorming) but not as the sole author. Every article must have a human writer who takes editorial responsibility. **[ASSUMPTION: this is a reasonable starting policy; to be reviewed and formalized before launch.]**
- **Development:** Claude Code is used for software development of the website platform. This does not affect editorial content.
- **Disclosure:** If AI tools contribute substantially to an article's research or drafting, this should be noted in the article (e.g., "Тази статия е подготвена с помощта на AI инструменти").

### 11.5 Comments Policy

- Comments that contain hate speech, personal threats, or illegal content will be deleted.
- Deletion reasons are not publicly displayed but are audit-logged internally.
- PredelNews reserves the right to disable comments on individual articles if moderation becomes unmanageable.

---

## 12. Success Metrics

### 12.1 Traffic & Engagement (measured via GA4)

| Metric | Target (90 days post-launch) | Target (6 months) |
|--------|-----------------------------|--------------------|
| Monthly unique visitors | 5,000 | 20,000 |
| Monthly page views | 15,000 | 80,000 |
| Avg. session duration | ≥ 1 min 30 s | ≥ 2 min |
| Bounce rate | ≤ 70% | ≤ 60% |
| Returning visitors (%) | ≥ 20% | ≥ 30% |
| Pages per session | ≥ 1.5 | ≥ 2.0 |

### 12.2 Content Production

| Metric | Target |
|--------|--------|
| Articles published per week | ≥ 15 (MVP); ≥ 25 (6 months) |
| Avg. time from draft to publish (non-breaking) | ≤ 4 hours |
| Avg. time from draft to publish (breaking) | ≤ 30 minutes |

### 12.3 Email & Community

| Metric | Target (90 days) | Target (6 months) |
|--------|------------------|--------------------|
| Email subscribers collected | 500 | 2,000 |
| Comments per article (avg.) | ≥ 1 | ≥ 3 |
| Poll participation rate (votes / homepage visits) | ≥ 5% | ≥ 10% |

### 12.4 Revenue

| Metric | Target (90 days) | Target (6 months) |
|--------|------------------|--------------------|
| AdSense monthly revenue | > 0 (baseline established) | Growing MoM |
| Direct-sold ad campaigns | 1 | 3–5 active |
| Sponsored articles published | 1–2 | 5+ cumulative |
| **[ASSUMPTION: Specific revenue targets in BGN/EUR to be set after first 30 days of AdSense data and initial advertiser conversations.]** | | |

### 12.5 Technical Health

| Metric | Target |
|--------|--------|
| Lighthouse Performance (mobile) | ≥ 90 |
| Core Web Vitals (LCP / FID / CLS) | All "Good" |
| Uptime | ≥ 99.5% monthly |
| Error rate (5xx) | < 0.1% of requests |

---

## 13. Risks & Mitigations

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | **Legal: Defamation / libel claims** from published articles | Medium | High | Editorial review before publish; Editor approval required; maintain corrections policy; consult legal advisor before launch for editorial guidelines |
| R2 | **Copyright infringement** — accidental use of copyrighted images | Medium | Medium | Use only licensed/owned images; implement editorial checklist; add image source field to article editor; consider stock photo subscription |
| R3 | **SEO penalty** from thin content or sponsored content mishandling | Low | High | Enforce sponsored content labeling and `rel="sponsored"` links; maintain high editorial quality bar; follow Google's content guidelines |
| R4 | **AdSense policy violation** — ad placement near inappropriate content or invalid clicks | Medium | High | Review AdSense content policies before launch; avoid ad placement directly adjacent to comment sections; monitor AdSense console for policy warnings |
| R5 | **Comment spam / abuse** overwhelming moderation capacity | High | Medium | Phase 1 anti-spam measures (honeypot, rate limiting, link filtering); escalate to reCAPTCHA in Phase 2 if needed; ability to disable comments per article |
| R6 | **Reputation damage** from publishing inaccurate information | Medium | High | Mandatory editor review; visible corrections policy; source attribution requirements |
| R7 | **Single editor bottleneck** — editor unavailability blocks all publishing | High | High | Cross-train 1 writer to have Editor-level CMS access as backup; define emergency publishing protocol |
| R8 | **Low initial traffic** — insufficient content volume to attract organic search traffic | Medium | Medium | Pre-launch content strategy: prepare 20–30 articles before go-live; aggressive local SEO; social media launch campaign (see `business/go-to-market.md`) |
| R9 | **GDPR non-compliance** for email collection or comments | Medium | High | Implement consent checkbox for email signup; prepare privacy policy; consult legal advisor; minimize personal data collection |
| R10 | **Hosting downtime or performance issues** | Low | Medium | Choose reliable hosting with SLA; implement monitoring and alerting (see `technical/observability.md`); CDN for static assets |

---

## 14. Dependencies

### 14.1 People

| Role | Need | Status |
|------|------|--------|
| Editor-in-Chief | 1 person, at least part-time at launch | **Required before launch** |
| Writers | 1–3, can be freelance | At least 1 required before launch |
| Developer | Claude Code + human oversight for development | In progress |
| Legal Advisor | One-time review of editorial policy, privacy policy, terms of use | **[ASSUMPTION: not a full-time hire; one-time consultation.]** |
| Designer | Visual design / UI | **[ASSUMPTION: development will use a clean, functional design system without a dedicated designer; professional branding (logo, color palette) is a prerequisite.]** |

### 14.2 Tools & Services

| Tool | Purpose | Phase |
|------|---------|-------|
| Umbraco 17 LTS | CMS | 1 |
| .NET 10 | Runtime | 1 |
| Google AdSense | Display advertising | 1 |
| Google Analytics 4 | Traffic analytics | 1 |
| Hosting provider (TBD) | Web hosting with .NET support | 1 |
| Domain registrar | `predelnews.com` | 1 |
| SSL certificate | HTTPS | 1 |
| Email service (transactional) | Contact form delivery, editorial notifications | 1 |
| Email marketing platform | Newsletter distribution | 2 |
| Claude Code | Development tool | 1 |

### 14.3 Content Prerequisites for Launch

- **Minimum 20 published articles** across at least 3 categories and 2 regions before public launch.
- **All 5 footer pages** populated with real content (About, Ads, Ads Offer, Contacts, All News).
- **At least 1 active poll.**
- **Logo and brand assets** finalized.
- **Privacy policy and terms of use** published.

---

## 15. Timeline (High Level)

| Phase | Duration | Milestone |
|-------|----------|-----------|
| **Discovery & Design** | 2 weeks | PRD approved; technical spec complete; wireframes approved; hosting provisioned |
| **Development Sprint 1** | 3 weeks | CMS setup; content types; article page; taxonomy pages; editorial workflow |
| **Development Sprint 2** | 3 weeks | Homepage assembly; comments system; search; ad slots; email collection |
| **Development Sprint 3** | 2 weeks | SEO implementation; performance optimization; sponsored content workflow; footer pages |
| **QA & Content Loading** | 2 weeks | Full QA; accessibility audit; 20+ articles published; footer pages populated; AdSense integration verified |
| **Soft Launch** | 1 week | Limited audience (social circles, local contacts); bug fixing; feedback collection |
| **Public Launch** | — | Go-to-market execution (see `business/go-to-market.md`) |
| **Phase 2 kickoff** | 4–6 weeks post-launch | Newsletter integration; Phase 2 features based on data |

**[ASSUMPTION: Total MVP timeline is approximately 10–13 weeks from PRD approval to public launch, depending on team velocity with Claude Code.]**

---

## 16. Open Questions

| # | Question | Owner | Needed by |
|---|----------|-------|-----------|
| OQ1 | What is the final URL slug strategy — Cyrillic or transliterated Latin? | Tech Lead / PM | Technical Spec |
| OQ2 | Which hosting provider will be used (Azure, DigitalOcean, other)? | Tech Lead | Sprint 1 start |
| OQ3 | Is a one-time legal review of editorial policy, privacy policy, and terms of use confirmed and scheduled? | PM / Founder | Before content loading phase |
| OQ4 | Has the `predelnews.com` domain been registered and DNS configured? | Admin | Sprint 1 start |
| OQ5 | Logo and brand guidelines (colors, typography) — are they ready, or do we need to commission design? | PM / Founder | Design phase |
| OQ6 | What are the initial topic categories? (e.g., Политика, Общество, Криминално, Спорт, Култура, Икономика, Здраве — confirm final list) | Editor | Content type setup |
| OQ7 | Specific revenue targets in BGN for 6-month and 12-month milestones? | Founder | Go-to-market plan |
| OQ8 | Backup editor: which writer will be cross-trained for Editor-level access? | Editor / PM | Before launch |
| OQ9 | Will the contact form use a transactional email service (SendGrid, Mailgun) or direct SMTP? | Tech Lead | Sprint 2 |
| OQ10 | Cookie consent banner scope for Phase 1 — basic notice or full consent management? | Legal / PM | Sprint 3 |

---

*End of PRD. This document should be reviewed and approved by the Product Manager, Editor-in-Chief, and Founder before development begins. It will be maintained as a living document in `business/prd.md`.*
