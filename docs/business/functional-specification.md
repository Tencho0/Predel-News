# Functional Specification — PredelNews

**Product:** PredelNews — Regional News Website for Southwest Bulgaria
**Domain:** predelnews.com
**Platform:** Umbraco 17 LTS (.NET 10)
**Document owner:** Business Analyst
**Status:** Draft v1.0
**Last updated:** 2025-02-23

> **Repository path:** `docs/business/functional-specification.md`
> Parent document: `docs/business/prd.md`
> Related: `docs/business/non-functional-requirements.md` · `docs/technical/technical-specification.md` · `docs/technical/database-schema.md` · `docs/planning/epics/`

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Assumptions](#2-assumptions)
3. [Content Types & Taxonomies](#3-content-types--taxonomies)
4. [Editorial Workflow](#4-editorial-workflow)
5. [Public Website](#5-public-website)
6. [Search & Navigation](#6-search--navigation)
7. [Comments System](#7-comments-system)
8. [Monetization](#8-monetization)
9. [Users, Roles & Permissions](#9-users-roles--permissions)
10. [SEO & Social](#10-seo--social)
11. [Analytics & Tracking](#11-analytics--tracking)
12. [Admin & Backoffice Tools](#12-admin--backoffice-tools)
13. [Definition of Done (Functional)](#13-definition-of-done-functional)

---

## 1. Introduction

### 1.1 Purpose

This document translates the Product Requirements Document (PRD) into testable, module-level functional requirements for the PredelNews MVP (Phase 1). It defines **what** the system must do — the behavior visible to visitors, writers, editors, and administrators — without prescribing **how** it is implemented at a code level. Implementation details belong in `docs/technical/technical-specification.md` and `docs/technical/architecture.md`.

### 1.2 Scope

**In scope:** All Phase 1 / MVP features as defined in PRD §8.1 (F1–F13).

**Out of scope (Phase 2+):** Newsletter sending, comment threading, reCAPTCHA, push notifications, AMP, native apps, reader accounts/login, dark mode, multi-language support. These are listed in PRD §8.2 and §9.

### 1.3 Audience

Developers, QA engineers, the product manager, and the editor-in-chief. This document is the primary reference for building user stories, writing test cases, and verifying acceptance at the end of each sprint.

### 1.4 Conventions

- **Requirement IDs** follow the pattern `FR-{MODULE}-{NNN}` (e.g., `FR-CT-001`).
- **Priority levels:**
  - **Must** — Required for MVP launch. Launch is blocked without it.
  - **Should** — Expected for MVP. Can be descoped to a fast-follow release only under exceptional schedule pressure.
  - **Could** — Desirable. May be deferred to Phase 2 without blocking launch.
- **UI labels** referenced in this spec are in Bulgarian. Final copy will be confirmed by the editor before launch, but the labels here represent the intent.
- **AC** = Acceptance Criteria.

---

## 2. Assumptions

The following assumptions were made where the PRD left decisions open or where clarification was needed. Each is flagged so stakeholders can validate or override.

| ID | Assumption | Impact if Wrong |
|----|-----------|-----------------|
| A1 | URL slugs for categories, regions, tags, and static pages will be **transliterated Latin** (e.g., `/kategoriya/politika/`) for maximum browser and sharing compatibility. Final slug convention is confirmed in `docs/technical/technical-specification.md`. | URL scheme and routing rules would need to change. |
| A2 | The contact form uses **rate limiting + honeypot** as anti-spam for MVP. No third-party CAPTCHA service. reCAPTCHA is a Phase 2 candidate. | If spam volume is high at launch, a CAPTCHA integration may need to be fast-tracked. |
| A3 | Email signup is **single opt-in** with a consent checkbox. No confirmation email is sent in Phase 1. | May need revision based on legal review (GDPR double opt-in requirements vary by jurisdiction). |
| A4 | There is **no dedicated Sales/Ads Manager CMS role**. All ad-slot and sponsored-content administration is handled by the Admin role. | If advertising operations are later delegated, a new role with scoped permissions will be needed. |
| A5 | The article body editor must support images, YouTube embeds, blockquotes, headings (H2/H3), ordered/unordered lists, and internal/external links. Facebook/X embeds and tables are Phase 2. | Writers wanting Facebook embeds at launch would need to use raw HTML or wait for Phase 2. |
| A6 | Related articles support **manual editor override** (select 3–6 articles) that takes precedence over the default algorithm. | Without override, editors lose control over cross-promotion of specific content. |
| A7 | Comment deletion is a **soft delete**: the comment is hidden from public view but the original content is preserved in the database with audit metadata. | Hard deletes would lose audit trail data and potentially conflict with legal obligations. |
| A8 | Poll vote deduplication relies on a **browser cookie**. Users who clear cookies or use incognito mode can vote again. This is an accepted limitation for MVP. | Vote counts are approximate, not fraud-proof. Acceptable for a lightweight engagement feature. |
| A9 | The "Breaking / Hot News" homepage block is populated via a **dedicated homepage curation interface** (not solely via article-level flags). The editor selects and orders articles within this block. | If only an article flag is used, ordering control is lost. |
| A10 | Anonymous comment display names are stored **per-comment** in the database, with the last-used name saved in a **browser cookie** to prefill the field on subsequent visits. | Cookie loss means user re-types their name; this is acceptable. |

---

## 3. Content Types & Taxonomies

### 3.1 Article

---

#### FR-CT-001 — Article Content Type

**Priority:** Must

**Description:** The system must provide an "Article" content type in the CMS with the following fields. This is the core publishable unit of the site.

| Field | Label (BG) | Type | Required | Notes |
|-------|-----------|------|----------|-------|
| Headline | Заглавие | Short text | Yes | Recommended max 120 characters; CMS shows character counter |
| Slug | URL адрес | Auto-generated from Headline | Yes | Transliterated Latin; editable by Editor/Admin |
| Subtitle / Deck | Подзаглавие | Short text | No | Used on cards and as fallback SEO description |
| Body | Съдържание | Rich content editor | Yes | See FR-CT-002 for capabilities |
| Cover Image | Основна снимка | Media picker | Yes | Alt text sub-field is required |
| Topic Category | Категория | Single-select dropdown | Yes | From Category taxonomy |
| Region | Регион | Single-select dropdown | Yes | From Region taxonomy |
| Tags | Тагове | Multi-select tag picker | No | 0–10 tags |
| Author | Автор | Content picker (Author node) | Yes | |
| SEO Title | SEO заглавие | Short text | No | Falls back to Headline |
| SEO Description | SEO описание | Textarea (≤ 320 chars) | No | Falls back to Subtitle, then first 160 chars of Body |
| OG Image | OG изображение | Media picker | No | Falls back to Cover Image |
| Is Sponsored | Спонсорирана | Boolean toggle | Yes (default: off) | Only Editor/Admin can set to true. See FR-MN-003. |
| Sponsor Name | Име на спонсор | Short text | Conditional | Required when Is Sponsored = true |
| Publish Date | Дата на публикуване | Date/time picker | No | For scheduling; defaults to current time on publish |
| Status | Статус | Workflow state | Auto-managed | Draft → In Review → Published / Scheduled / Unpublished |

**Acceptance Criteria:**
- AC1: All fields above are present in the CMS article editor.
- AC2: Headline field displays a character counter; no hard limit is enforced, but a visual warning appears above 120 characters.
- AC3: Slug is auto-generated from the headline using Latin transliteration. Editor/Admin can manually edit the slug. Writers cannot edit the slug.
- AC4: Cover Image cannot be saved without alt text populated.
- AC5: An article cannot be submitted for review or published if any required field is empty. The CMS displays validation errors per field.
- AC6: The Is Sponsored toggle is visible only to users with Editor or Admin role. Writers see the field as read-only (or hidden).
- AC7: When Is Sponsored is toggled on, the Sponsor Name field becomes required and validation prevents saving without it.

---

#### FR-CT-002 — Article Body Editor Capabilities

**Priority:** Must

**Description:** The article body editor must support the following content elements at MVP. The choice of underlying editor component (Rich Text Editor, Block List, or hybrid) is a technical decision documented in `docs/technical/technical-specification.md`.

**MVP required capabilities:**
- Paragraph text with bold, italic, and underline formatting.
- Headings: H2 and H3 (H1 is reserved for the article headline).
- Ordered and unordered lists.
- Hyperlinks: internal (to other articles or pages) and external (with option to set `target="_blank"` and `rel` attributes).
- Inline images with alt text and optional caption.
- YouTube video embed (paste URL or embed code → rendered as responsive iframe).
- Block quotes.

**Phase 2 candidates (not required for MVP):**
- Facebook post/video embed.
- X (Twitter) post embed.
- Tables.
- Custom content blocks (e.g., info box, callout).

**Acceptance Criteria:**
- AC1: A writer can insert each MVP-required element type into an article body and it renders correctly on the public article page.
- AC2: YouTube embeds render as responsive iframes that maintain aspect ratio across viewports.
- AC3: Inline images include alt text and are lazy-loaded on the public page.
- AC4: External links in article body can have `rel` attributes set (at minimum `noopener`; `rel="sponsored noopener"` is auto-applied for sponsored articles — see FR-MN-004).

---

### 3.2 Taxonomies

---

#### FR-CT-003 — Topic Categories Taxonomy

**Priority:** Must

**Description:** Topic categories are editor-configurable taxonomy nodes. Each article is assigned exactly one primary category. Categories are used for navigation, archive pages, homepage blocks, and SEO.

**MVP seed list (UI display names in Bulgarian):**

| Category | Notes |
|----------|-------|
| Общество | Society / Community |
| Политика | Politics |
| Криминално | Crime / Incidents |
| Икономика / Бизнес | Economy / Business (display name TBD by editor) |
| Спорт | Sports |
| Култура | Culture |
| Любопитно | Curious / Interesting |
| Хайлайф | Highlife / Lifestyle |

**Acceptance Criteria:**
- AC1: The seed categories above are created during initial CMS setup.
- AC2: Admin and Editor can add, rename, reorder, and soft-delete categories without code changes via the CMS backoffice.
- AC3: Deleting a category that has associated articles is blocked (or requires reassignment of articles first). The system displays a warning with the count of affected articles.
- AC4: Each category has a name (BG), a URL slug (Latin transliterated, auto-generated, editable), and an optional SEO description.
- AC5: Categories appear in the main site navigation and in the article editor dropdown.

---

#### FR-CT-004 — Regions Taxonomy

**Priority:** Must

**Description:** Regions represent the geographic coverage area. Each article is assigned exactly one region. Regions are used for filtering, archive pages, and reader context.

**MVP seed list:**

| Region | Notes |
|--------|-------|
| Благоевград | Primary coverage area |
| Кюстендил | |
| Перник | |
| София | Sofia city and Sofia province |
| България | National news not specific to one region |

An optional "Свят" (World/International) region may be added by the editor at any time.

**Acceptance Criteria:**
- AC1: The seed regions above are created during initial CMS setup.
- AC2: Admin and Editor can add, rename, reorder, and soft-delete regions via the CMS backoffice.
- AC3: Deleting a region with associated articles is blocked or requires article reassignment.
- AC4: Each region has a name (BG), a URL slug (Latin), and an optional SEO description.
- AC5: Regions are available as a navigation filter on the public site (see FR-PW-002).

---

#### FR-CT-005 — Tags

**Priority:** Must

**Description:** Tags are free-form keywords assigned to articles (0–10 per article). Tags enable cross-cutting discovery beyond categories and regions.

**Acceptance Criteria:**
- AC1: Writers can create new tags inline while editing an article (type-ahead with existing tag suggestions).
- AC2: Admin and Editor can manage (rename, merge, delete) tags via the CMS backoffice.
- AC3: Deleting a tag removes it from all associated articles (no orphan references).
- AC4: Each tag generates an archive page on the public site (FR-PW-009).
- AC5: Tags are displayed on article cards and article pages as linked pills.

---

### 3.3 Author

---

#### FR-CT-006 — Author Content Type

**Priority:** Must

**Description:** Each article is linked to an Author node. Author nodes enable bylines and author archive pages.

| Field | Label (BG) | Type | Required |
|-------|-----------|------|----------|
| Full Name | Име | Short text | Yes |
| Slug | URL адрес | Auto-generated from name | Yes |
| Bio | Биография | Textarea | No |
| Photo | Снимка | Media picker | No |
| Email | Email | Email field | No (internal only; not displayed publicly) |

**Acceptance Criteria:**
- AC1: An Author node can be created by Editor or Admin.
- AC2: The Author picker in the article editor shows a list of available Author nodes.
- AC3: Each author has a public archive page listing all their published articles (FR-PW-010).
- AC4: Author email is never rendered on the public site.

---

### 3.4 Poll

---

#### FR-CT-007 — Poll Content Type

**Priority:** Must

**Description:** A poll is a simple question with 2–4 answer options, displayed on the homepage. Only one poll can be active at a time.

| Field | Type | Required |
|-------|------|----------|
| Question text | Short text | Yes |
| Options (2–4) | Repeatable short text | Yes (min 2, max 4) |
| Is Active | Boolean | Yes (default: false) |
| Open Date | Date/time | No |
| Close Date | Date/time | No |

**Acceptance Criteria:**
- AC1: Editor or Admin can create a poll with a question and 2–4 options.
- AC2: Activating a new poll automatically deactivates any previously active poll.
- AC3: A closed poll (past Close Date or manually deactivated) shows its final results but no longer accepts votes.
- AC4: Poll results (vote counts and percentages) are visible to Editor/Admin in the CMS.
- AC5: Past polls are listed in the CMS backoffice for reference (not publicly accessible in MVP).

---

### 3.5 Static Pages

---

#### FR-CT-008 — Static / Footer Pages

**Priority:** Must

**Description:** The site includes the following static content pages, editable via the CMS using a rich text editor. Each has a fixed role in the site footer navigation.

| Page | Slug (proposed) | Purpose |
|------|----------------|---------|
| Всички новини | `/vsichki-novini/` | Full article archive (auto-generated listing, not a static body) |
| За нас | `/za-nas/` | About the team, mission, editorial standards |
| Реклама | `/reklama/` | Advertising information, rate card summary, contact details |
| Рекламна оферта | `/reklamna-oferta/` | Downloadable media kit (PDF upload), detailed pricing |
| Контакти | `/kontakti/` | Contact form + editorial contact info |

**Acceptance Criteria:**
- AC1: All 5 pages are created during initial setup and linked from the site footer.
- AC2: "За нас", "Реклама", and "Рекламна оферта" are editable rich-text pages managed by Admin/Editor.
- AC3: "Рекламна оферта" supports a media kit PDF upload (single file). The public page renders a download link.
- AC4: "Контакти" includes both a contact form (FR-CT-009) and static contact information editable in the CMS.
- AC5: "Всички новини" renders an auto-generated, paginated reverse-chronological article listing (not static content).

---

#### FR-CT-009 — Contact Form

**Priority:** Must

**Description:** The Contacts page includes a form for visitor inquiries. Submissions are emailed to a configurable recipient address.

| Field | Label (BG) | Type | Required |
|-------|-----------|------|----------|
| Name | Име | Text input | Yes |
| Email | Email | Email input | Yes |
| Subject | Тема | Text input | Yes |
| Message | Съобщение | Textarea | Yes |

**Anti-spam:** Honeypot hidden field + server-side rate limiting (max 3 submissions per IP per 10-minute window).

**Acceptance Criteria:**
- AC1: All four fields are required. The form shows inline validation errors for empty or malformed fields (e.g., invalid email format).
- AC2: On successful submission, the visitor sees a confirmation message (e.g., "Съобщението ви беше изпратено успешно."). The form fields are cleared.
- AC3: The submitted data is emailed to the recipient address configured in CMS Site Settings.
- AC4: The honeypot field is present in the HTML but hidden via CSS. Submissions with the honeypot field filled are silently discarded (no error shown to the submitter).
- AC5: A 4th submission from the same IP within 10 minutes returns a user-friendly rate-limit message (e.g., "Моля, опитайте отново след няколко минути.").
- AC6: The recipient email address is configurable by Admin in CMS Site Settings without code changes.

---

## 4. Editorial Workflow

---

#### FR-EW-001 — Article Lifecycle States

**Priority:** Must

**Description:** Every article progresses through a defined set of states. The allowed transitions are:

```
                           ┌────────────────────────┐
                           ▼                        │
[Draft] ──► [In Review] ──► [Published] ──► [Unpublished]
                 │              │                    │
                 │              ▼                    │
                 │         [Scheduled] ──► [Published]
                 │                                   │
                 └───────────────────────────────────┘
                     (Editor returns to Draft)
```

| State | Visible on Public Site | Who Can Move Here |
|-------|----------------------|-------------------|
| Draft | No | Writer (create/edit own), Editor/Admin (any) |
| In Review | No | Writer (submit), Editor/Admin (return to draft) |
| Published | Yes | Editor/Admin only |
| Scheduled | No (until publish date) | Editor/Admin only |
| Unpublished | No | Editor/Admin only |

**Acceptance Criteria:**
- AC1: A newly created article defaults to Draft state.
- AC2: A Writer can submit a Draft to "In Review" but cannot publish directly.
- AC3: While an article is "In Review," the originating Writer cannot edit it (locked). Editor/Admin can edit.
- AC4: Editor/Admin can return an "In Review" article to "Draft" (with optional note to Writer).
- AC5: Editor/Admin can move an article from "In Review" or "Draft" to "Published" or "Scheduled."
- AC6: A Scheduled article automatically transitions to Published at its Publish Date/Time (tolerance: ± 1 minute).
- AC7: Editor/Admin can unpublish a Published article. The article is removed from the public site immediately (within cache refresh interval).
- AC8: All state transitions are logged with the acting user and timestamp.

---

#### FR-EW-002 — Editorial Notifications

**Priority:** Should

**Description:** The system notifies relevant users of article state changes.

**Acceptance Criteria:**
- AC1: When a Writer submits an article for review, a notification appears on the Editor's CMS dashboard (e.g., badge count on "In Review" section).
- AC2: Notification mechanism is CMS-internal (dashboard indicator). Email notifications are a Phase 2 candidate.
- AC3: The Editor dashboard shows a list of all articles currently "In Review," sorted by submission date (oldest first).

---

#### FR-EW-003 — Post-Publication Edits & Corrections

**Priority:** Must

**Description:** Published articles may be edited after publication (corrections, updates). The system must transparently reflect these changes.

**Acceptance Criteria:**
- AC1: Editor/Admin can edit a Published article's content directly.
- AC2: When a Published article is saved with changes, an "Updated" dateline is displayed on the public article page showing the date/time of the most recent edit, in addition to the original Publish Date.
- AC3: The CMS maintains a version history for each article. Editor/Admin can view previous versions.
- AC4: The public site displays only the latest published version. No public-facing version diff is required in MVP.

---

#### FR-EW-004 — Article Preview

**Priority:** Must

**Description:** Authors and editors can preview an unpublished article as it would appear on the public site.

**Acceptance Criteria:**
- AC1: A "Preview" action is available for articles in Draft, In Review, and Scheduled states.
- AC2: The preview renders the article using the public article page template with current content.
- AC3: Preview is accessible only to authenticated CMS users (not publicly accessible via a URL).

---

## 5. Public Website

### 5.1 Homepage

---

#### FR-PW-001 — Homepage Layout & Blocks

**Priority:** Must

**Description:** The homepage is composed of the following content blocks, rendered top-to-bottom in the order listed. Blocks degrade gracefully if insufficient content exists (e.g., a category block is hidden if 0 published articles exist in that category).

| # | Block | Content Source | Editor Control |
|---|-------|---------------|----------------|
| 1 | Header / Navigation | Site settings + taxonomies | Logo, nav links, region filter |
| 2 | Breaking / Hot News Banner | Curated (FR-PW-003) | Editor selects & orders articles |
| 3 | National & World Headlines | Auto: most recent articles from "България" region + "Свят" if it exists | Editor can override selection |
| 4 | Category Blocks (one per active category) | Auto: 3–4 most recent articles per category | Auto-populated; "Виж всички" link |
| 5 | Latest Articles Feed | Auto: all published articles, reverse-chronological | Paginated (FR-PW-007) |
| 6 | Poll Widget | Active poll (FR-PW-005) | Editor creates/activates polls |
| 7 | Email Signup Bar | Static form (FR-PW-006) | — |
| 8 | Footer | Site settings + footer pages | Links managed in CMS |

**Acceptance Criteria:**
- AC1: All blocks above render on the homepage in the specified order.
- AC2: If a category has 0 published articles, its category block is not rendered (no empty block).
- AC3: If no poll is active, the Poll Widget block is not rendered.
- AC4: The homepage loads with correct content when the site has as few as 1 published article (graceful degradation).

---

#### FR-PW-002 — Header & Navigation

**Priority:** Must

**Description:** The site header appears on every page and provides primary navigation.

**Components:**
- Site logo (linked to homepage).
- Main navigation: links to each active topic category archive page.
- Region filter: dropdown or link list to region archive pages.
- Search icon/button opening the search interface.

**Acceptance Criteria:**
- AC1: The header is present on every public page.
- AC2: On mobile viewports (< 768px), the navigation collapses into a hamburger menu.
- AC3: The header is sticky (remains visible on scroll) on both desktop and mobile.
- AC4: Category links in the nav are dynamically generated from the active categories taxonomy (adding/removing a category in the CMS updates the nav without code changes).
- AC5: Region filter shows all active regions from the Regions taxonomy.
- AC6: Search icon navigates to or opens the search interface (FR-SN-001).

---

#### FR-PW-003 — Breaking / Hot News Block (Homepage Curation)

**Priority:** Must

**Description:** The top editorial block on the homepage features 1 primary article (large image + headline) and up to 5 additional headline links. Content is selected and ordered by the Editor via a homepage curation interface in the CMS.

**Acceptance Criteria:**
- AC1: The CMS provides a homepage curation interface where Editor/Admin can select articles for the Breaking News block.
- AC2: The editor can assign 1 article as the "featured" article (rendered with large image and headline).
- AC3: The editor can select up to 5 additional articles displayed as headline-only links.
- AC4: The editor can reorder the headline links via drag-and-drop or sequential ordering.
- AC5: If the curation interface is empty (no articles selected), the block falls back to displaying the most recent published article as featured, with the next 5 most recent as headline links.
- AC6: Changes to the curation selection are reflected on the public homepage within 60 seconds (cache refresh).

---

#### FR-PW-004 — Article Card Component

**Priority:** Must

**Description:** Articles are displayed throughout the site (homepage blocks, archive pages, related articles) using a consistent card component.

**Card elements:**
- Cover image (cropped/responsive thumbnail).
- Headline (linked to article).
- Subtitle (if available; truncated).
- Category badge (linked to category archive).
- Region badge (linked to region archive).
- Publish date (Bulgarian format: DD.MM.YYYY).
- Comment count (linked to article comments section).
- "Платена публикация" badge (if Is Sponsored = true, always visible, non-removable).

**Acceptance Criteria:**
- AC1: The card component renders correctly with all available fields.
- AC2: Missing optional fields (subtitle) are omitted without breaking the card layout.
- AC3: Sponsored articles display the "Платена публикация" badge on every card instance (homepage, archive, related articles). The badge cannot be hidden.
- AC4: Comment count reflects the current number of visible (non-deleted) comments on the article.

---

#### FR-PW-005 — Poll Widget (Public)

**Priority:** Must

**Description:** The homepage displays the currently active poll. Visitors can vote without logging in.

**Acceptance Criteria:**
- AC1: The poll displays the question text and 2–4 clickable answer options.
- AC2: After voting, the visitor immediately sees the results (percentage per option, rendered as a bar chart or progress bars).
- AC3: A cookie is set after voting to prevent duplicate votes from the same browser. If the cookie is present, the poll shows results instead of voting options.
- AC4: If no poll is active, the widget area is not rendered (no empty placeholder).
- AC5: A closed poll (manually deactivated or past Close Date) displays its final results with a "Анкетата е приключила" (Poll has ended) indicator and does not accept new votes.

---

#### FR-PW-006 — Email Signup Bar

**Priority:** Must

**Description:** A visible call-to-action bar on the homepage collects email addresses for a future newsletter (Phase 2). Single opt-in with consent checkbox.

**Components:**
- Email input field.
- Consent checkbox with label: "Съгласен/на съм да получавам новини от PredelNews на посочения email адрес." (or similar; final copy confirmed by editor).
- Submit button.

**Acceptance Criteria:**
- AC1: The signup bar is visible on the homepage (positioned as defined in FR-PW-001).
- AC2: Submission requires both a valid email address and the consent checkbox to be checked. Inline validation shows errors for missing/invalid fields.
- AC3: On successful submission, the email, timestamp, and consent flag (true) are stored in the database.
- AC4: A confirmation message is displayed (e.g., "Благодарим! Email адресът ви беше записан.").
- AC5: Duplicate email submissions are handled gracefully: no duplicate record is created, no error is shown to the user, and the confirmation message is displayed as normal.
- AC6: Admin can export the email list as CSV (see FR-AB-004).

---

#### FR-PW-007 — Pagination

**Priority:** Must

**Description:** All article listing pages (homepage Latest Articles feed, category archives, region archives, tag archives, author archives, All News) use server-side pagination.

**Acceptance Criteria:**
- AC1: Default page size is 20 articles per page.
- AC2: Pagination controls display: Previous / Next links and page number indicators.
- AC3: The current page number is reflected in the URL (e.g., `?page=2` or `/page/2/`) for bookmarkability and SEO.
- AC4: Navigating to a page number beyond the available range shows a user-friendly "No more articles" message or redirects to the last valid page.

---

### 5.2 Article Page

---

#### FR-PW-008 — Article Page Layout

**Priority:** Must

**Description:** The public article page renders a single published article with all associated metadata, sharing tools, related content, comments, and ad slots.

**Page sections (top-to-bottom):**

1. **Sponsored label** (conditional): If Is Sponsored = true → "Платена публикация" banner above headline.
2. **Category badge** — linked to category archive.
3. **Region badge** — linked to region archive.
4. **Headline** (H1).
5. **Dateline** — Publish date/time (format: DD.MM.YYYY, HH:MM). If edited after publication: "Обновена: DD.MM.YYYY, HH:MM."
6. **Author byline** — Author name linked to author archive page.
7. **Cover image** — Responsive, lazy-loaded, with alt text.
8. **Article body** — Rich content.
9. **Sponsored label** (conditional, repeated): Same "Платена публикация" banner below body.
10. **Tags** — Linked pills.
11. **Share buttons** — Facebook, Viber, Copy Link (minimum set).
12. **Related articles** — 3–4 articles (FR-PW-011).
13. **Comments section** (FR-CM-001 through FR-CM-007).
14. **Ad slots** — Positioned per FR-MN-001.

**Acceptance Criteria:**
- AC1: All sections listed above render correctly for a standard (non-sponsored) article.
- AC2: For sponsored articles, the "Платена публикация" banner appears both above the headline and below the body. The banner is rendered by the template and cannot be removed or hidden per-article.
- AC3: Sponsor Name is displayed adjacent to or within the sponsored banner.
- AC4: The dateline shows "Обновена:" with the update timestamp when the article has been edited post-publication.
- AC5: Share button for Facebook generates a share dialog with correct OG metadata. Viber share generates a `viber://forward?text=` link. Copy Link copies the canonical article URL to clipboard.
- AC6: Cover image uses responsive `srcset` and is lazy-loaded.
- AC7: The article page achieves Lighthouse Performance score ≥ 90 on mobile (with ads loaded).

---

### 5.3 Archive & Listing Pages

---

#### FR-PW-009 — Category Archive Page

**Priority:** Must

**Description:** Each topic category has a public archive page (e.g., `/kategoriya/politika/`) listing all published articles in that category, reverse-chronological, paginated.

**Acceptance Criteria:**
- AC1: The page title is the category name.
- AC2: Articles are rendered using the standard article card component (FR-PW-004).
- AC3: Pagination follows FR-PW-007.
- AC4: The page has a unique, templated SEO meta title (e.g., "{Category Name} — PredelNews") and meta description.

---

#### FR-PW-010 — Region Archive Page

**Priority:** Must

**Description:** Each region has a public archive page listing all published articles in that region.

**Acceptance Criteria:**
- AC1–AC4: Same structure as FR-PW-009, scoped to Region.

---

#### FR-PW-011 — Tag Archive Page

**Priority:** Must

**Description:** Each tag has a public archive page listing all published articles with that tag.

**Acceptance Criteria:**
- AC1–AC4: Same structure as FR-PW-009, scoped to Tag.

---

#### FR-PW-012 — Author Archive Page

**Priority:** Must

**Description:** Each author has a public page showing their bio (if available), photo, and a paginated list of their published articles.

**Acceptance Criteria:**
- AC1: The page displays the author's name, photo (if uploaded), and bio (if provided).
- AC2: Below the bio, published articles by this author are listed using the article card component, paginated.
- AC3: The page has a unique SEO meta title (e.g., "{Author Name} — PredelNews").

---

#### FR-PW-013 — "All News" Page

**Priority:** Must

**Description:** A single page listing all published articles across all categories and regions, reverse-chronological, paginated.

**Acceptance Criteria:**
- AC1: Accessible from the footer link "Всички новини" and at the slug `/vsichki-novini/`.
- AC2: Lists all published articles using the article card component.
- AC3: Paginated per FR-PW-007.

---

#### FR-PW-014 — Related Articles

**Priority:** Must

**Description:** Each article page displays 3–4 related articles below the article body. Related articles are determined by algorithm with optional manual override.

**Algorithm (default, when no manual override is set):**
1. Articles sharing the most tags with the current article (ranked by tag overlap count).
2. If fewer than 4 results: fill with articles from the same category.
3. If still fewer than 4: fill with the most recent published articles.
4. Exclude the current article from results.

**Manual override:** Editor/Admin can select 3–6 specific articles as "Related" for a given article. When manual selections exist, they replace the algorithm entirely.

**Acceptance Criteria:**
- AC1: The related articles section displays 3–4 articles using the article card component.
- AC2: When the editor has manually selected related articles, those are displayed (up to 4, in the editor-specified order).
- AC3: When no manual override exists, the algorithm produces relevant results following the priority described above.
- AC4: The current article never appears in its own related articles.

---

### 5.4 Responsive Design & Theme

---

#### FR-PW-015 — Light Theme Only

**Priority:** Must

**Description:** The site uses a light color theme. Dark mode is explicitly not supported and must not activate regardless of the visitor's OS or browser preference.

**Acceptance Criteria:**
- AC1: The site renders in light theme on all pages.
- AC2: The CSS includes `color-scheme: light only` (or equivalent) and does not include a `prefers-color-scheme: dark` media query.
- AC3: No dark-mode toggle or automatic dark-mode switching exists anywhere on the site.

---

#### FR-PW-016 — Responsive Layout

**Priority:** Must

**Description:** All pages render correctly on viewports from 320px to 1920px. Design is mobile-first.

**Acceptance Criteria:**
- AC1: No horizontal scrollbar appears on any page at any viewport width between 320px and 1920px.
- AC2: All interactive elements (buttons, links, form fields) are touch-friendly on mobile (min tap target 44×44px).
- AC3: Images are responsive (fluid width, no overflow).
- AC4: Ad slots adapt to viewport (e.g., leaderboard collapses or swaps format on mobile — see FR-MN-001).

---

## 6. Search & Navigation

---

#### FR-SN-001 — Site Search

**Priority:** Must

**Description:** The site provides a server-side search function allowing visitors to find articles by keyword. Search is powered by Umbraco Examine (Lucene-based) or equivalent. Only published articles are indexed.

**Acceptance Criteria:**
- AC1: A search input is accessible from the header on every page (via icon or visible field).
- AC2: Submitting a search query navigates to a search results page.
- AC3: Search results display: article headline (linked), text excerpt with query terms highlighted, publish date, category badge, and region badge.
- AC4: Results are ranked by relevance (implementation-defined; relevance scoring details in `docs/technical/technical-specification.md`).
- AC5: Bulgarian-language queries return relevant results (e.g., searching "пожар Благоевград" matches articles containing those terms).
- AC6: If no results match, a user-friendly message is displayed (e.g., "Не бяха намерени резултати за '{query}'.").
- AC7: Search response time is ≤ 500ms for 95th percentile of queries (measured server-side; see `docs/business/non-functional-requirements.md`).

---

#### FR-SN-002 — Region Filter Navigation

**Priority:** Should

**Description:** Visitors can filter or navigate to region-specific content from the header.

**Acceptance Criteria:**
- AC1: A region selector (dropdown or link list) is present in the header.
- AC2: Selecting a region navigates to that region's archive page (FR-PW-010).
- AC3: The region list updates dynamically if regions are added/removed in the CMS.

---

## 7. Comments System

---

#### FR-CM-001 — Anonymous Comment Submission

**Priority:** Must

**Description:** Visitors can leave comments on published articles without creating an account. Comments require a display name and comment text.

**Form fields:**
- Display Name (Име) — text input, required, max 50 characters.
- Comment Text (Коментар) — textarea, required, max 2000 characters.
- Honeypot field — hidden input, not visible to humans.

**Acceptance Criteria:**
- AC1: The comment form is displayed below the article body on every published article page.
- AC2: Both Display Name and Comment Text are required. Submitting with empty fields shows inline validation errors.
- AC3: On successful submission, the comment appears on the article page immediately (no pre-moderation queue).
- AC4: The comment displays: display name, comment text, and timestamp (Bulgarian format: DD.MM.YYYY, HH:MM).
- AC5: Comments are ordered chronologically (oldest first) on the article page.
- AC6: Comments are flat (no threading or reply nesting) in MVP.

---

#### FR-CM-002 — Display Name Cookie Persistence

**Priority:** Should

**Description:** The last-used display name is stored in a browser cookie and used to prefill the "Име" field for subsequent comments.

**Acceptance Criteria:**
- AC1: After submitting a comment, a cookie stores the display name used.
- AC2: On subsequent visits to any article's comment form, the Display Name field is prefilled with the cookie value.
- AC3: The visitor can overwrite the prefilled name before submitting.
- AC4: If no cookie exists, the field is empty.

---

#### FR-CM-003 — Anti-Spam: Honeypot

**Priority:** Must

**Description:** The comment form includes a hidden honeypot field. Bots that fill all fields (including the hidden one) are silently rejected.

**Acceptance Criteria:**
- AC1: The honeypot field is present in the HTML form but hidden from human users via CSS.
- AC2: If the honeypot field is filled on submission, the server silently discards the comment (HTTP 200 response, no comment stored, no error shown).
- AC3: The honeypot field name should not be obviously named (e.g., avoid "honeypot"; use a plausible name like "website" or "url").

---

#### FR-CM-004 — Anti-Spam: Rate Limiting

**Priority:** Must

**Description:** The system limits comment frequency per IP address to prevent flooding.

**Rule:** Maximum 3 comments per IP address per 5-minute rolling window.

**Acceptance Criteria:**
- AC1: The 1st, 2nd, and 3rd comments from the same IP within 5 minutes are accepted normally.
- AC2: The 4th comment attempt within the 5-minute window is rejected with a user-friendly message (e.g., "Моля, изчакайте няколко минути преди да публикувате нов коментар.").
- AC3: After the 5-minute window expires, the visitor can comment again.

---

#### FR-CM-005 — Anti-Spam: Link Filtering

**Priority:** Must

**Description:** Comments containing 2 or more URLs are held for manual review and not displayed publicly until approved.

**Acceptance Criteria:**
- AC1: A comment with 0 or 1 URL is published immediately.
- AC2: A comment with ≥ 2 URLs is stored but not displayed on the public page. It is flagged as "held for review" in the CMS.
- AC3: Editor/Admin can approve a held comment (making it visible) or delete it.
- AC4: The submitter receives a message such as "Коментарът ви ще бъде прегледан преди публикуване." (not an error; informational).

---

#### FR-CM-006 — Anti-Spam: Banned Word List

**Priority:** Should

**Description:** A configurable list of banned words/phrases. Comments containing banned words are held for review.

**Acceptance Criteria:**
- AC1: Admin can manage a banned word list via CMS settings (add/remove words).
- AC2: Comments matching any banned word (case-insensitive, whole word or substring — configurable) are held for review, same behavior as FR-CM-005 AC2–AC4.
- AC3: The banned word list check runs server-side (not client-side).

---

#### FR-CM-007 — Comment Deletion & Audit Logging

**Priority:** Must

**Description:** Writer, Editor, and Admin can delete any comment. Deletion is a soft delete (hidden from public, retained in database). All deletions are audit-logged.

**Acceptance Criteria:**
- AC1: A "Delete" action is available next to each comment for authenticated CMS users with Writer, Editor, or Admin role.
- AC2: Clicking "Delete" hides the comment from the public page immediately.
- AC3: The original comment text, display name, IP address, and timestamp are preserved in the database (soft delete).
- AC4: An audit log entry is created for each deletion, recording: deleted comment ID, deleting user (CMS username), deletion timestamp, and reason (optional free-text field, or "no reason given" default).
- AC5: Audit logs are viewable by Admin in the CMS backoffice (FR-AB-006).

---

#### FR-CM-008 — Comment Count on Article Cards

**Priority:** Should

**Description:** The article card component displays the number of visible (non-deleted) comments for the article.

**Acceptance Criteria:**
- AC1: The comment count is shown on article cards on the homepage, archive pages, and related articles.
- AC2: The count reflects only publicly visible comments (excludes soft-deleted and held-for-review comments).
- AC3: Clicking the comment count navigates to the article page's comments section (anchor link).

---

## 8. Monetization

---

#### FR-MN-001 — Display Ad Slots

**Priority:** Must

**Description:** The site defines 6 ad placement slots. Each slot can render either an AdSense ad unit or a direct-sold banner, configured by Admin in the CMS.

| Slot ID | Location | Viewport Behavior |
|---------|----------|-------------------|
| `ad-header-leaderboard` | Below header, above content | Desktop: 728×90; Mobile: 320×100 or collapses |
| `ad-sidebar-1` | Right sidebar, top (desktop only) | Hidden on mobile |
| `ad-sidebar-2` | Right sidebar, mid (desktop only) | Hidden on mobile |
| `ad-article-mid` | After 3rd paragraph of article body | Responsive in-content |
| `ad-article-bottom` | Below article body, above comments | Responsive |
| `ad-footer-banner` | Above site footer | Responsive |

**Each slot operates in one of two modes:**

1. **AdSense mode:** Renders the configured AdSense ad unit code (HTML/JS snippet stored in CMS).
2. **Direct-sold mode:** Renders a banner image with destination URL, alt text, and optional impression tracking pixel.

**Acceptance Criteria:**
- AC1: All 6 ad slots render in their defined positions on the correct pages.
- AC2: Admin can configure each slot independently via CMS: select mode (AdSense or Direct-sold), enter AdSense code, or upload banner details.
- AC3: For Direct-sold mode, Admin enters: banner image (upload), destination URL, alt text, start date, and end date.
- AC4: When a Direct-sold campaign's end date passes, the slot automatically reverts to AdSense mode.
- AC5: Direct-sold mode takes priority: if both AdSense code and a direct-sold campaign are configured for the same slot and the campaign is within its date range, the direct-sold banner is shown.
- AC6: All ad slots are visually distinguished from editorial content with a label "Реклама" (Advertisement) rendered above or adjacent to the ad.
- AC7: Sidebar slots (`ad-sidebar-1`, `ad-sidebar-2`) are hidden on mobile viewports where no sidebar is rendered.
- AC8: Ad slots do not break page layout when no ad fills (empty slot is collapsed, not a blank rectangle).

---

#### FR-MN-002 — AdSense Integration

**Priority:** Must

**Description:** The site supports Google AdSense for automated display advertising.

**Acceptance Criteria:**
- AC1: The AdSense site-level script tag is loaded on all pages (configurable in CMS Site Settings by Admin).
- AC2: Individual ad unit codes are configured per slot in the ad management interface.
- AC3: AdSense ads load and are visible on the live site (verifiable in the AdSense console).

---

#### FR-MN-003 — Sponsored Article Governance

**Priority:** Must

**Description:** Sponsored articles are published through the standard editorial workflow with additional labeling and link rules. Governance rules are enforced by the CMS and page templates — not by editorial discretion alone.

**Non-negotiable rules (encoded in the system):**

1. The `Is Sponsored` flag can only be set by Editor or Admin.
2. When `Is Sponsored = true`, the "Платена публикация" label is rendered by the page template at the top (above headline) and bottom (below body) of the article page. This rendering is automatic and cannot be suppressed per-article.
3. The Sponsor Name field is required when `Is Sponsored = true`.
4. The "Платена публикация" badge is rendered on all article card instances for sponsored articles (homepage, archives, related articles).
5. Outbound links in sponsored articles automatically receive `rel="sponsored noopener"` (FR-MN-004).

**Acceptance Criteria:**
- AC1: A Writer cannot set `Is Sponsored` to true (field is hidden or read-only for Writers).
- AC2: An Editor or Admin can toggle `Is Sponsored` on any article.
- AC3: A published sponsored article displays the "Платена публикация" label at the top and bottom of the article page, using a globally styled, non-overridable template element.
- AC4: A sponsored article card displays the "Платена публикация" badge in all listing contexts.
- AC5: Attempting to save an article with `Is Sponsored = true` and empty Sponsor Name triggers a validation error.

---

#### FR-MN-004 — Sponsored Link Attribution

**Priority:** Must

**Description:** All outbound hyperlinks within the body of a sponsored article (`Is Sponsored = true`) must carry the `rel="sponsored noopener"` attribute, per Google's guidelines for paid content.

**Acceptance Criteria:**
- AC1: When a sponsored article is rendered on the public site, every `<a>` tag in the body pointing to an external domain has `rel="sponsored noopener"` applied.
- AC2: This attribution is applied at render time (not dependent on the writer/editor manually adding `rel` attributes in the editor).
- AC3: Internal links (pointing to other predelnews.com pages) within sponsored articles do not receive `rel="sponsored"`.

---

#### FR-MN-005 — Ad Labeling

**Priority:** Must

**Description:** All display ad slots are clearly labeled to distinguish them from editorial content.

**Acceptance Criteria:**
- AC1: Each rendered ad slot displays a label reading "Реклама" (Advertisement) in a consistent style defined in the global stylesheet.
- AC2: The label is present for both AdSense and Direct-sold modes.
- AC3: The label is not removable per-slot by Admin (it is template-enforced).

---

## 9. Users, Roles & Permissions

---

#### FR-UR-001 — Role Definitions

**Priority:** Must

**Description:** The CMS defines three roles with the following permission scopes. No Sales/Ads Manager role exists in MVP; ad management is an Admin responsibility.

| Permission | Writer | Editor | Admin |
|------------|--------|--------|-------|
| Create article drafts | ✅ Own | ✅ Any | ✅ Any |
| Edit articles | ✅ Own drafts | ✅ Any, any state | ✅ Any, any state |
| Submit article for review | ✅ Own | ✅ Any | ✅ Any |
| Publish / schedule / unpublish | ❌ | ✅ | ✅ |
| Set `Is Sponsored` flag | ❌ | ❌ | ✅ |
| Edit article slug | ❌ | ✅ | ✅ |
| Manage categories / regions / tags | ❌ | ✅ | ✅ |
| Create / manage polls | ❌ | ✅ | ✅ |
| Curate homepage Breaking News block | ❌ | ✅ | ✅ |
| Delete comments | ✅ Any | ✅ Any | ✅ Any |
| Approve held comments | ❌ | ✅ | ✅ |
| Manage ad slots | ❌ | ❌ | ✅ |
| Manage site settings | ❌ | ❌ | ✅ |
| Manage users and roles | ❌ | ❌ | ✅ |
| Export email subscriber list | ❌ | ✅ | ✅ |
| View audit logs | ❌ | ❌ | ✅ |
| View CMS dashboard (articles, comments) | ✅ (own) | ✅ (all) | ✅ (all) |
| Edit footer / static pages | ❌ | ✅ | ✅ |
| Manage authors | ❌ | ✅ | ✅ |

**Acceptance Criteria:**
- AC1: Each role listed above exists in the CMS and enforces the specified permissions.
- AC2: A user with the Writer role cannot access admin-only sections (ad management, site settings, user management, audit logs).
- AC3: Permission checks are enforced server-side (not merely hidden from the UI).
- AC4: Admin can assign/change roles for any CMS user.

---

#### FR-UR-002 — Sponsored Article Toggle Access

**Priority:** Must

**Description:** The `Is Sponsored` toggle is restricted to Admin only at MVP. This is a deliberate governance control.

**Acceptance Criteria:**
- AC1: Only users with the Admin role see and can interact with the `Is Sponsored` toggle on the article editor.
- AC2: Editor and Writer roles cannot see or modify the `Is Sponsored` field.
- AC3: If the requirement changes post-launch to allow Editor access, this should be a configuration change, not a code change (permission mapping in CMS).

> **Note:** The PRD states "Editor or Admin" for this permission. After clarification, MVP restricts to Admin only. Editor access is a configurable future option. Update this requirement if the stakeholder decision changes.

---

## 10. SEO & Social

---

#### FR-SE-001 — XML Sitemap

**Priority:** Must

**Description:** The site generates and maintains an XML sitemap including all published articles, category archives, region archives, tag archives, author pages, and static pages.

**Acceptance Criteria:**
- AC1: Sitemap is accessible at `/sitemap.xml`.
- AC2: New articles appear in the sitemap within 60 minutes of publication.
- AC3: Unpublished articles are removed from the sitemap.
- AC4: The sitemap is valid per the Sitemaps.org protocol.

---

#### FR-SE-002 — Robots.txt

**Priority:** Must

**Description:** The site serves a `robots.txt` file that allows search engine crawling of public content and disallows admin/backoffice paths.

**Acceptance Criteria:**
- AC1: `robots.txt` is accessible at `/robots.txt`.
- AC2: Umbraco backoffice paths (`/umbraco/`) are disallowed.
- AC3: A reference to the sitemap URL is included.

---

#### FR-SE-003 — Canonical URLs

**Priority:** Must

**Description:** Every public page includes a `<link rel="canonical">` tag pointing to its preferred URL.

**Acceptance Criteria:**
- AC1: Each article page has a canonical URL matching its published slug.
- AC2: Archive pages (category, region, tag, author) include canonical URLs.
- AC3: Paginated pages use self-referencing canonical (page 2's canonical points to page 2, not page 1).

---

#### FR-SE-004 — Open Graph & Twitter Card Meta Tags

**Priority:** Must

**Description:** All article pages include Open Graph and Twitter Card meta tags for rich social sharing previews.

**Required tags per article:**
- `og:title` — SEO Title (fallback: Headline)
- `og:description` — SEO Description (fallback: Subtitle, then first 160 chars of body)
- `og:image` — OG Image (fallback: Cover Image)
- `og:type` — `article`
- `og:url` — Canonical URL
- `og:site_name` — "PredelNews"
- `og:locale` — `bg_BG`
- `twitter:card` — `summary_large_image`
- `twitter:title`, `twitter:description`, `twitter:image` — same values as OG

**Acceptance Criteria:**
- AC1: All listed tags are present in the HTML `<head>` of every article page.
- AC2: Tags use appropriate fallback values when optional fields are empty.
- AC3: Sharing an article URL on Facebook produces a rich preview with image, title, and description (verifiable via Facebook Sharing Debugger).
- AC4: Non-article pages (homepage, archives, static pages) include basic OG tags (title, description, site name, URL).

---

#### FR-SE-005 — Structured Data (JSON-LD)

**Priority:** Must

**Description:** Article pages include JSON-LD structured data for Google Search.

**Required schemas:**
- `Article` (or `NewsArticle`) schema on article pages: headline, datePublished, dateModified, author, image, publisher.
- `BreadcrumbList` schema on all pages.

**Acceptance Criteria:**
- AC1: Each article page includes a valid `NewsArticle` JSON-LD block in `<head>` or `<body>`.
- AC2: The structured data passes Google's Rich Results Test without errors.
- AC3: BreadcrumbList schema is present on article pages (e.g., Home > Category > Article Title) and archive pages.

---

#### FR-SE-006 — SEO Meta Title & Description Templates

**Priority:** Must

**Description:** Non-article pages use templated SEO meta titles and descriptions so that each page has unique, meaningful metadata.

**Templates (examples; final copy confirmed by editor):**

| Page Type | Meta Title Template |
|-----------|-------------------|
| Homepage | "PredelNews — Новини от Югозападна България" |
| Category archive | "{Category Name} — PredelNews" |
| Region archive | "Новини от {Region Name} — PredelNews" |
| Tag archive | "{Tag Name} — PredelNews" |
| Author archive | "{Author Name} — PredelNews" |
| Search results | "Търсене: {query} — PredelNews" |
| Static page | "{Page Title} — PredelNews" |

**Acceptance Criteria:**
- AC1: Every public page has a unique `<title>` tag and `<meta name="description">` that is not empty or generic.
- AC2: Templates auto-populate using taxonomy/page data. Manual override is possible via CMS fields where available (e.g., category SEO description).

---

#### FR-SE-007 — Favicon & Web App Manifest

**Priority:** Should

**Description:** The site has a favicon and a basic web app manifest for mobile browsers.

**Acceptance Criteria:**
- AC1: A favicon is displayed in browser tabs across all major browsers.
- AC2: A `manifest.json` (or `site.webmanifest`) is present with site name, icons, and theme color.

---

## 11. Analytics & Tracking

---

#### FR-AN-001 — Google Analytics / Tag Manager Integration

**Priority:** Must

**Description:** The site loads a configurable analytics tracking script on every public page.

**Acceptance Criteria:**
- AC1: Admin can enter a Google Analytics 4 Measurement ID or Google Tag Manager container ID in CMS Site Settings.
- AC2: The corresponding script tag fires on every public page load.
- AC3: Changing the tracking ID in CMS settings updates the script on all pages without code deployment.
- AC4: If the tracking ID field is empty, no analytics script is loaded (no broken tags or console errors).

---

#### FR-AN-002 — Cookie Consent Behavior (MVP)

**Priority:** Should

**Description:** For MVP, the site implements a basic cookie notice banner informing visitors about cookie usage (analytics, comment name, poll vote). Full GDPR consent management (granular opt-in/opt-out per cookie category) is Phase 2.

**Acceptance Criteria:**
- AC1: A cookie notice banner is displayed on the first visit, informing the user that the site uses cookies for analytics and functionality.
- AC2: The banner includes an "Accept" (Приемам) button. Clicking it dismisses the banner and sets a cookie remembering the visitor's acknowledgment.
- AC3: The banner does not reappear for returning visitors who have acknowledged it.
- AC4: Analytics scripts load regardless of banner interaction in MVP (pre-consent loading). This is an accepted simplification; full consent gating is Phase 2.

> **[ASSUMPTION A3 RELATED]:** Legal review should confirm whether MVP cookie behavior is sufficient for the target audience/jurisdiction. Full consent management is planned for Phase 2 (see PRD §9).

---

## 12. Admin & Backoffice Tools

---

#### FR-AB-001 — Editorial Dashboard

**Priority:** Must

**Description:** The CMS backoffice displays a dashboard for editorial staff with at-a-glance metrics and action items.

**Dashboard content:**

| Widget | Description | Visible To |
|--------|-------------|------------|
| Articles In Review | Count + list of articles awaiting review | Editor, Admin |
| Articles Published Today | Count | Writer (own), Editor, Admin |
| Articles Published This Week | Count | Editor, Admin |
| Recent Comments | List of latest 10 comments (with link to article) | Editor, Admin |
| Held Comments | Count + list of comments held for review (links, banned words) | Editor, Admin |
| Email Signups | Total count of collected emails | Editor, Admin |

**Acceptance Criteria:**
- AC1: The dashboard is the default landing page after CMS login for Editor and Admin.
- AC2: Writers see a simplified dashboard showing their own articles by status.
- AC3: All counts/lists reflect real-time data (or near real-time; delay ≤ 5 minutes).

---

#### FR-AB-002 — Taxonomy Management

**Priority:** Must

**Description:** Admin and Editor can perform full CRUD operations on topic categories, regions, and tags via the CMS backoffice.

**Acceptance Criteria:**
- AC1: Categories, regions, and tags each have a management interface in the backoffice.
- AC2: Creating a new category/region automatically generates its URL slug (Latin transliterated), which is editable.
- AC3: Renaming a category/region updates its display name; the slug can be updated independently (with a redirect from old slug if changed — implementation detail in `docs/technical/technical-specification.md`).
- AC4: Deleting a category or region with associated articles shows a warning and requires reassignment.
- AC5: Tags can be deleted (removal from all articles) or merged (combining two tags into one).

---

#### FR-AB-003 — Poll Management

**Priority:** Must

**Description:** Editor and Admin can create, activate, deactivate, and view poll results.

**Acceptance Criteria:**
- AC1: The CMS provides a poll management interface listing all polls (active and past).
- AC2: Creating a poll requires question text and 2–4 options.
- AC3: Activating a poll automatically deactivates the previously active poll (at most one active at a time).
- AC4: Poll results (vote counts per option, total votes) are visible in the CMS.
- AC5: A poll can be manually closed by setting Is Active to false or setting a Close Date.

---

#### FR-AB-004 — Email Subscriber Export

**Priority:** Must

**Description:** Admin and Editor can export the collected email list.

**Acceptance Criteria:**
- AC1: A "Download CSV" action is available in the CMS.
- AC2: The CSV includes: email address, signup timestamp, and consent flag.
- AC3: The export includes all non-duplicate email records.

---

#### FR-AB-005 — Ad Slot Management

**Priority:** Must

**Description:** Admin can manage the 6 defined ad slots via a dedicated CMS interface.

**Acceptance Criteria:**
- AC1: Each of the 6 slots (as defined in FR-MN-001) is listed in the ad management section.
- AC2: For each slot, Admin can: select mode (AdSense / Direct-sold), enter/update AdSense ad unit code, configure Direct-sold campaign (image, URL, alt text, start date, end date).
- AC3: Changes take effect on the public site within 60 seconds (cache refresh).
- AC4: The interface shows the current status of each slot (active mode, campaign dates if direct-sold).

---

#### FR-AB-006 — Audit Log Viewer

**Priority:** Must

**Description:** Admin can view a log of auditable actions in the system.

**Auditable events (MVP):**
- Comment deletions (who deleted, when, original comment content).
- Article state changes (who changed, from/to state, timestamp).
- User role changes.
- Ad slot configuration changes.

**Acceptance Criteria:**
- AC1: Admin can access the audit log viewer in the CMS backoffice.
- AC2: The log is filterable by event type and date range.
- AC3: Each log entry shows: event type, acting user, timestamp, and event-specific details.
- AC4: Audit logs are append-only (cannot be edited or deleted by any user, including Admin).

---

#### FR-AB-007 — Site Settings

**Priority:** Must

**Description:** Admin can manage global site configuration via a CMS settings interface.

**Configurable settings:**

| Setting | Description |
|---------|-------------|
| Site Name | Displayed in header, footer, meta tags |
| Site Logo | Media picker |
| Analytics Tracking ID | GA4 Measurement ID or GTM Container ID |
| AdSense Site-Level Script | JS snippet for AdSense |
| Contact Form Recipient Email | Email address receiving contact form submissions |
| Social Links | URLs for Facebook, Instagram, etc. (displayed in footer) |
| Default SEO Meta Description | Fallback for pages without a custom description |
| Banned Words List | For comment moderation (FR-CM-006) |

**Acceptance Criteria:**
- AC1: All settings above are editable by Admin in the CMS.
- AC2: Changes to settings take effect on the public site without code deployment (within cache refresh interval).

---

#### FR-AB-008 — Comment Moderation Interface

**Priority:** Must

**Description:** Editor and Admin can review and act on held comments (those caught by link filtering or banned word detection).

**Acceptance Criteria:**
- AC1: A "Held Comments" section in the CMS lists all comments currently held for review.
- AC2: For each held comment, the moderator can see: display name, comment text (with flagged URLs or banned words highlighted), article title (linked), submission timestamp, and IP address.
- AC3: Moderator can approve a held comment (making it publicly visible) or delete it (soft delete with audit log).
- AC4: The held comments list shows the reason the comment was held (e.g., "Contains 2+ links" or "Banned word: {word}").

---

## 13. Definition of Done (Functional)

The MVP is considered functionally complete and ready for launch when **all** of the following conditions are met:

### Content & Editorial
- [ ] All content types (Article, Category, Region, Tag, Author, Poll, Static Page) are created in the CMS and editable by appropriate roles.
- [ ] The editorial workflow (Draft → In Review → Published/Scheduled) functions correctly with role-based permissions enforced.
- [ ] At least 20 articles are published across ≥ 3 categories and ≥ 2 regions.
- [ ] All 5 footer/static pages are populated with real content.
- [ ] At least 1 poll is active.

### Public Website
- [ ] Homepage renders all blocks per FR-PW-001, with graceful degradation for sparse content.
- [ ] Article pages render correctly for standard and sponsored articles, with all metadata, sharing buttons, related articles, and comment section.
- [ ] Category, region, tag, author, and "All News" archive pages render paginated listings.
- [ ] Search returns relevant results for Bulgarian-language queries.
- [ ] Pagination works on all listing pages.
- [ ] The site is light-theme only; no dark mode activates under any condition.
- [ ] All pages are responsive from 320px to 1920px with no horizontal overflow.

### Comments
- [ ] Anonymous comment submission works (display name + text).
- [ ] Comments appear immediately after posting.
- [ ] Honeypot silently rejects bot submissions.
- [ ] Rate limiting blocks the 4th comment from the same IP within 5 minutes.
- [ ] Comments with ≥ 2 URLs are held for review.
- [ ] Writer/Editor/Admin can delete comments (soft delete + audit log).
- [ ] Comment count displays on article cards.

### Monetization
- [ ] All 6 ad slots render correctly in their defined positions.
- [ ] AdSense ads load and display (verified in AdSense console).
- [ ] Admin can configure Direct-sold mode for any slot with image, URL, and date range.
- [ ] Direct-sold campaigns revert to AdSense after end date.
- [ ] All ad slots display the "Реклама" label.
- [ ] Sponsored articles display "Платена публикация" label in all contexts (article page top/bottom, all card instances).
- [ ] Outbound links in sponsored articles have `rel="sponsored noopener"`.

### SEO & Social
- [ ] XML sitemap at `/sitemap.xml` includes all published content.
- [ ] `robots.txt` is correctly configured.
- [ ] Canonical URLs are set on all pages.
- [ ] OG and Twitter Card meta tags are present on article pages and produce correct social previews.
- [ ] JSON-LD structured data (NewsArticle, BreadcrumbList) is valid per Google's Rich Results Test.
- [ ] Favicon and web manifest are present.

### Analytics
- [ ] GA4 / GTM tracking fires on every page when configured.
- [ ] Cookie notice banner is displayed on first visit.

### Admin
- [ ] CMS dashboard shows editorial metrics (articles in review, published today/this week, recent comments, held comments, email signup count).
- [ ] Taxonomy management (categories, regions, tags) is fully operational.
- [ ] Poll management (create, activate, close, view results) works.
- [ ] Email list CSV export works.
- [ ] Ad slot management interface allows mode switching and campaign configuration.
- [ ] Audit logs capture comment deletions and article state changes; Admin can view logs.
- [ ] Site settings (analytics ID, contact email, social links, banned words) are configurable.

### Roles & Permissions
- [ ] Writer, Editor, and Admin roles enforce the permission matrix in FR-UR-001.
- [ ] `Is Sponsored` toggle is accessible only to Admin.
- [ ] Permission checks are server-side enforced.

### Performance & Quality
- [ ] All pages achieve Lighthouse Performance score ≥ 90 on mobile simulation.
- [ ] Contact form submits successfully and delivers to configured email.
- [ ] Email signup stores data correctly with no duplicates.
- [ ] No critical or high-severity bugs remain open.

---

*End of Functional Specification. This document is maintained in `docs/business/functional-specification.md` and should be updated as requirements evolve. Changes must be reviewed by the Product Manager and Editor-in-Chief.*
