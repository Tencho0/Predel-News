FILE: docs/planning/epics/EPIC-03-public-site-core.md

# EPIC-03 — Public Site Core

## Goal / Outcome

Deliver the visitor-facing public website: homepage with all content blocks, article page with full metadata, archive pages (category, region, tag, author, all news), static/footer pages, responsive design, and error pages. After this epic, a visitor can browse the site, read articles, and navigate between sections.

## In Scope (MVP)

- Homepage template with all blocks: Breaking News (curated + fallback), National/World headlines, per-category blocks, Latest Articles feed (paginated), footer
- Article page template with all metadata, cover image (responsive srcset, lazy loading), body, tags, author byline, share buttons (Facebook, Viber, Copy Link), related articles section
- Article card component (reusable across all listing contexts)
- Archive pages: Category, Region, Tag, Author, "All News" — all paginated
- Pagination component
- Breadcrumb component
- Static page templates (За нас, Реклама, Рекламна оферта)
- Contact page template (form UI only — submission logic in EPIC-07)
- Custom 404 and 500 error pages (Bulgarian text, site navigation)
- CSS: mobile-first responsive layout (320px–1920px), light theme enforced
- Core JS: hamburger nav, copy-link share button
- Related articles algorithm (tag overlap → same category → most recent) + manual override

## Out of Scope (MVP)

- Comments rendering (EPIC-05)
- Poll widget (EPIC-07)
- Email signup bar (EPIC-07)
- Ad slot rendering (EPIC-09)
- Dark mode (never)
- AMP pages (never for MVP)

## Dependencies

- EPIC-01 (Foundation) — base layout, environments
- EPIC-02 (Content Model) — all document types and taxonomies must exist

## High-Level Acceptance Criteria

- [ ] A visitor can navigate homepage → article → category archive → back to homepage without broken links
- [ ] All pages render without horizontal scroll at 320px, 768px, and 1920px viewport widths
- [ ] Article page renders all fields correctly for a test article
- [ ] Homepage Breaking News block displays curated articles; falls back to most recent if curation is empty
- [ ] Category blocks auto-populate; categories with 0 articles are hidden
- [ ] 404 page renders with site branding and Bulgarian text
- [ ] Lighthouse Performance score ≥ 80 on mobile (≥ 90 target in EPIC-10)

---

## User Stories

### US-03.01 — Homepage Layout & Content Blocks

**As a** visitor, **I want** the homepage to display a Breaking News banner, National/World headlines, per-category article blocks, and a Latest Articles feed, **so that** I can quickly scan the most important news.

**Acceptance Criteria:**
- All blocks render in the specified order: Header → Breaking News → National/World → Category Blocks → Latest Articles → Footer
- If a category has 0 published articles, its block is not rendered
- If no articles exist in the "България" or "Свят" region, the National/World block is hidden
- The homepage loads correctly with as few as 1 published article (graceful degradation)

---

### US-03.02 — Breaking News Block (Homepage Curation)

**As an** editor, **I want** to curate the Breaking News block by selecting 1 featured article (large image + headline) and up to 5 headline links, **so that** the homepage highlights the most important stories.

**Acceptance Criteria:**
- The CMS `HomePage` node has a `featuredArticles` multi-node picker (max 6, sortable)
- Index 0 = featured article (large image + headline); indexes 1–5 = headline-only links
- If the picker is empty, the block falls back to the 6 most recent published articles
- Changes are reflected on the public homepage within 60 seconds

---

### US-03.03 — Article Page Template

**As a** visitor, **I want** to read a full article with headline, dateline (DD.MM.YYYY, HH:MM), category/region badges, cover image, body, tags, author byline, and share buttons, **so that** I have a complete and trustworthy reading experience.

**Acceptance Criteria:**
- All article fields render: headline (H1), publish date, updated date (if edited), category badge (linked), region badge (linked), cover image (responsive srcset, lazy-loaded below fold), body, tags (linked pills), author name (linked to author archive)
- Share buttons for Facebook, Viber, and Copy Link are present and functional
- Copy Link button copies the article URL to clipboard and shows a confirmation tooltip (e.g., "Линкът е копиран")
- Dateline uses Bulgarian format: DD.MM.YYYY, HH:MM

---

### US-03.04 — Article Card Component

**As a** visitor, **I want** articles displayed as consistent cards across the site (homepage, archives, related articles), **so that** I can easily scan and identify articles of interest.

**Acceptance Criteria:**
- Card shows: cover image thumbnail (cropped/responsive), headline (linked), subtitle (truncated if present), category badge (linked), region badge (linked), publish date (DD.MM.YYYY), comment count
- If `isSponsored = true`, a "Платена публикация" badge is always visible on the card and cannot be removed
- Missing optional fields (subtitle) are omitted without breaking the card layout
- The card component is shared across all listing pages

---

### US-03.05 — Archive Pages (Category, Region, Tag, Author)

**As a** visitor, **I want** to browse paginated lists of articles filtered by category, region, tag, or author, **so that** I can find content relevant to my interests.

**Acceptance Criteria:**
- Category archive at `/kategoriya/{slug}/` shows articles for that category
- Region archive at `/region/{slug}/` shows articles for that region
- Tag archive at `/tag/{slug}/` shows articles with that tag
- Author archive at `/avtor/{slug}/` shows articles by that author
- All archives are paginated (20 articles per page) with the shared pagination component
- Each archive page has a unique SEO meta title and description (templated from taxonomy name)

---

### US-03.06 — All News Page

**As a** visitor, **I want** an "Всички новини" page showing all published articles in reverse-chronological order, **so that** I can browse the complete news feed.

**Acceptance Criteria:**
- "Всички новини" is accessible at `/vsichki-novini/`
- Displays all published articles in reverse-chronological order
- Paginated at 20 articles per page
- Uses the shared article card and pagination components

---

### US-03.07 — Pagination Component

**As a** visitor, **I want** paginated article lists with page numbers and next/previous controls, **so that** I can navigate through large sets of articles.

**Acceptance Criteria:**
- Pagination appears below the article list when total articles exceed the page size (20)
- URL format: `?page=N` (query string)
- Shows: Previous, page numbers (with ellipsis for large ranges), Next
- Current page is visually highlighted
- Page 1 does not show a `?page=1` parameter (clean URL)
- Navigating beyond the last page returns a 404

---

### US-03.08 — Static & Footer Pages

**As a** visitor, **I want** to access "За нас", "Реклама", "Рекламна оферта", and "Контакти" from the site footer, **so that** I can learn about the team, advertising options, and contact information.

**Acceptance Criteria:**
- All 5 footer links are present: Всички новини, За нас, Реклама, Рекламна оферта, Контакти
- "За нас", "Реклама" render rich text content from the CMS
- "Рекламна оферта" renders CMS content plus a download link for the media kit PDF (if uploaded)
- "Контакти" renders CMS content plus the contact form UI (submission logic in EPIC-07)
- Footer links appear on every page

---

### US-03.09 — Custom Error Pages

**As a** visitor, **I want** user-friendly 404 and 500 error pages in Bulgarian with site navigation, **so that** I can recover from dead links without seeing technical gibberish.

**Acceptance Criteria:**
- 404 page displays "Страницата не е намерена" with a link to the homepage and full site navigation
- 500 page displays "Възникна грешка" with a link to the homepage
- No stack traces, server paths, or technical details are exposed to the visitor
- Error pages use the site layout (`_Layout.cshtml`) with header and footer

---

### US-03.10 — Responsive Design & Mobile Navigation

**As a** visitor on a mobile device, **I want** the site to be fully usable on my phone with a hamburger menu and no horizontal scroll, **so that** I can read news comfortably on a small screen.

**Acceptance Criteria:**
- On viewports < 768px, the main navigation collapses into a hamburger menu
- The hamburger menu opens/closes with a tap; shows category links and region filter
- The header is sticky on scroll (both desktop and mobile)
- No horizontal scroll on any page at any viewport from 320px to 1920px
- Touch targets are ≥ 44×44px on mobile

---

### US-03.11 — Related Articles Section

**As a** visitor, **I want** to see 3–4 related articles at the bottom of each article page, **so that** I can continue reading relevant content.

**Acceptance Criteria:**
- The related articles section shows 3–4 articles below the article body
- Default algorithm: articles sharing the most tags → same category → most recent
- If the editor has set a `relatedArticlesOverride` on the article, the manual selection takes priority
- Related articles use the shared article card component

---

### US-03.12 — Breadcrumb Navigation

**As a** visitor, **I want** a breadcrumb trail on article and archive pages, **so that** I can understand my location in the site hierarchy and navigate back easily.

**Acceptance Criteria:**
- Article page breadcrumb: Начало > {Category Name} > {Article Headline}
- Archive page breadcrumb: Начало > {Taxonomy Type} > {Taxonomy Name}
- Static page breadcrumb: Начало > {Page Title}
- Each breadcrumb segment is linked (except the current page)
- Breadcrumb is rendered as an `<ol>` with appropriate ARIA markup
