FILE: docs/planning/epics/EPIC-02-content-model-and-taxonomies.md

# EPIC-02 — Content Model & Taxonomies

## Goal / Outcome

Define all Umbraco document types, compositions, taxonomies, and the content tree structure so that the CMS is fully usable for content entry. Writers and editors can create articles with all required fields, assign categories/regions/tags, and manage authors — all before public-facing templates exist.

## In Scope (MVP)

- All Umbraco document types: `Article`, `Category`, `Region`, `newsTag`, `Author`, `HomePage`, `SiteSettings`, `StaticPage`, `ContactPage`, `AllNewsPage`
- Compositions: `SeoComposition`, `PageMetaComposition`
- Content tree structure per tech spec §4.10
- Seed taxonomy data: 8 categories, 5 regions
- Cyrillic → Latin slug transliteration service
- TinyMCE toolbar configuration for article body (H2/H3, bold/italic, lists, links, images, YouTube embeds, blockquotes)
- User groups: Writer, Editor, Admin with permission boundaries
- `isSponsored` field restriction (Admin-only, enforced server-side)
- Cover image alt text validation
- `SiteSettings` typed model helper

## Out of Scope (MVP)

- Facebook/X embeds in article body (Phase 2)
- Block List editor (Phase 2 candidate)
- Tables in article body (Phase 2)

## Dependencies

- EPIC-01 (Foundation & Environments) — Umbraco project must be scaffolded and database running

## High-Level Acceptance Criteria

- [ ] An Editor can create an Article with all fields, assign category/region/tags/author, and save successfully
- [ ] A Writer cannot see or modify the `isSponsored` toggle
- [ ] Slug auto-generation produces correct Latin transliterations (e.g., "Пожар в Благоевград" → `pozhar-v-blagoevgrad`)
- [ ] Seed categories and regions are present in the content tree after initial setup
- [ ] All document types match the schema defined in tech spec §4

---

## User Stories

### US-02.01 — Article Document Type

**As an** editor, **I want** an Article content type in the CMS with headline, subtitle, body, cover image, category, region, tags, author, SEO fields, and sponsored content fields, **so that** I can create complete news articles.

**Acceptance Criteria:**
- All fields from FR-CT-001 are present in the CMS article editor
- Headline field displays a character counter; visual warning appears above 120 characters
- An article cannot be saved if any required field (headline, body, cover image, category, region, author) is empty
- The `isSponsored` toggle and `sponsorName` field are in a dedicated "Sponsored" property group

**Notes:** The `isSponsored` field visibility restriction is covered in US-02.07.

---

### US-02.02 — Article Body Rich Text Editor

**As a** writer, **I want** the article body editor to support paragraphs, headings (H2/H3), bold/italic/underline, lists, links, inline images, YouTube embeds, and blockquotes, **so that** I can create rich, well-formatted articles.

**Acceptance Criteria:**
- TinyMCE toolbar includes: Bold, Italic, Underline, H2, H3, Ordered List, Unordered List, Hyperlink (with `rel` attribute option), Insert Image (from media library, alt text required), YouTube embed, Blockquote
- H1 is excluded from the toolbar (reserved for article headline)
- YouTube embeds render as responsive iframes with `loading="lazy"` and a `<div class="video-embed">` wrapper
- External links default to `rel="noopener"`

---

### US-02.03 — Topic Categories Taxonomy

**As an** editor, **I want** a Categories taxonomy with seed data (Общество, Политика, Криминално, Икономика/Бизнес, Спорт, Култура, Любопитно, Хайлайф), **so that** every article can be classified by topic.

**Acceptance Criteria:**
- All 8 seed categories are created during initial CMS setup
- Editor and Admin can add, rename, reorder, and soft-delete categories via the CMS
- Deleting a category with associated articles is blocked; a warning displays the count of affected articles
- Each category has a name (BG), a URL slug (Latin transliterated, auto-generated, editable), and an optional SEO description

---

### US-02.04 — Regions Taxonomy

**As an** editor, **I want** a Regions taxonomy with seed data (Благоевград, Кюстендил, Перник, София, България), **so that** every article can be tagged with its geographic coverage area.

**Acceptance Criteria:**
- All 5 seed regions are created during initial CMS setup
- Editor and Admin can add, rename, reorder, and soft-delete regions
- Deleting a region with associated articles is blocked or requires reassignment
- Each region has a name (BG), a URL slug (Latin), and an optional SEO description

---

### US-02.05 — Tags System

**As a** writer, **I want** to assign 0–10 free-form tags to an article with type-ahead suggestions from existing tags, **so that** readers can discover related content across categories.

**Acceptance Criteria:**
- Writers can create new tags inline while editing an article (type-ahead with existing tag suggestions)
- Editor and Admin can manage (rename, merge, delete) tags via the CMS backoffice
- Deleting a tag removes it from all associated articles (no orphan references)
- Each tag has a name, an auto-generated Latin slug, and an optional SEO description
- Maximum 10 tags per article is enforced

---

### US-02.06 — Author Content Type

**As an** editor, **I want** Author nodes with full name, bio, photo, and internal email, **so that** articles display proper bylines and each author has an archive page.

**Acceptance Criteria:**
- An Author node can be created by Editor or Admin
- The Author picker in the article editor shows a list of available Author nodes
- Author email is in a separate "Internal" property group visible only to Editor/Admin
- Author email is never rendered on public pages

---

### US-02.07 — Sponsored Content Field Restriction

**As an** admin, **I want** only Admin-role users to be able to toggle the `isSponsored` field on articles, **so that** sponsored content labeling is governed by authorized personnel only.

**Acceptance Criteria:**
- The "Sponsored" property group (containing `isSponsored` and `sponsorName`) is visible only to Admin users
- Writers and Editors cannot see or modify `isSponsored` in the CMS UI
- A `ContentSavingNotification` handler rejects attempts to set `isSponsored = true` by non-Admin users (server-side enforcement)
- When `isSponsored` is toggled on, `sponsorName` becomes required; saving without it fails with a validation error

**Notes:** PRD §6.4.2 states only Editor or Admin can set the flag. Architecture §8 refines this to Admin only. The tech spec §4.2 confirms Admin-only. This story follows the tech spec.

---

### US-02.08 — Cyrillic-to-Latin Slug Generator

**As a** developer, **I want** an `ISlugGenerator` service that transliterates Bulgarian Cyrillic text to clean Latin URL slugs, **so that** all content URLs are browser-compatible and SEO-friendly.

**Acceptance Criteria:**
- Input "Пожар в Благоевград" produces `pozhar-v-blagoevgrad`
- Special characters, punctuation, and extra whitespace are stripped or replaced with hyphens
- Consecutive hyphens are collapsed to a single hyphen
- Slugs are lowercase
- Slug uniqueness is enforced (appends `-2`, `-3` etc. for duplicates)
- Unit tests cover standard transliteration cases, edge cases (numbers, mixed Cyrillic/Latin, empty input)

---

### US-02.09 — Content Tree Structure & Site Settings

**As an** editor, **I want** the CMS content tree organized with clear root nodes for articles, categories, regions, tags, authors, and static pages, plus a SiteSettings singleton, **so that** content is easy to navigate and site-wide configuration is centralized.

**Acceptance Criteria:**
- Content tree matches the structure in tech spec §4.10 (homePage, newsRoot, categoryRoot, regionRoot, tagRoot, authorRoot, static pages, siteSettings)
- SiteSettings node is accessible via a typed model helper from Umbraco's content cache (no per-request DB query)
- SiteSettings includes: siteName, siteLogo, analyticsTrackingId, adSenseSiteScript, contactRecipientEmail, socialFacebook, socialInstagram, defaultSeoDescription, bannedWordsList, maintenanceMode

---

### US-02.10 — Cover Image Alt Text Validation

**As an** editor, **I want** the CMS to prevent saving an article if the cover image has no alt text, **so that** all published articles meet accessibility requirements.

**Acceptance Criteria:**
- Attempting to save an article with a cover image that has empty alt text produces a validation error
- The error message is in Bulgarian (e.g., "Моля, добавете алтернативен текст за основната снимка.")
- The validation fires on both Save and Publish actions

---

### US-02.11 — Static Page & Contact Page Document Types

**As an** editor, **I want** StaticPage and ContactPage document types so I can manage "За нас", "Реклама", "Рекламна оферта", and "Контакти" pages, **so that** all footer pages are editable through the CMS.

**Acceptance Criteria:**
- StaticPage has: pageTitle, body (rich text), mediaKitPdf (media picker for PDF), and SeoComposition fields
- ContactPage has: pageTitle, introText, phoneNumber, displayEmail, and SeoComposition fields
- AllNewsPage document type exists with only SEO fields (content is auto-generated)
- All 5 footer page nodes are created in the content tree during initial setup

## Open Questions

- **OQ1:** The PRD (§6.4.2) says "Only the Editor or Admin can toggle Is Sponsored" but the architecture (§8) and tech spec (§4.2) restrict it to Admin only. This epic follows the tech spec (Admin only). Confirm with stakeholders.
