FILE: docs/planning/epics/EPIC-06-search-and-navigation.md

# EPIC-06 — Search & Navigation

## Goal / Outcome

Implement site search powered by Umbraco Examine (Lucene-based) and the global navigation system (header with category links, region filter, and search entry point). Visitors can find articles by keyword and navigate the site through intuitive menus.

## In Scope (MVP)

- Umbraco Examine index configured for published articles (headline, subtitle, body, tags)
- Search results page with article title, excerpt, date, category, and region
- Search query input (from header search icon)
- Empty results handling with Bulgarian message
- Examine index updates on article publish/unpublish/save
- Header navigation with dynamically generated category links
- Region filter dropdown/link list in header
- Search response time ≤ 500 ms (server-side)

## Out of Scope (MVP)

- Algolia or Elasticsearch (Phase 2+ if Examine degrades at scale)
- Search autocomplete / suggestions
- Search filters (by date, category, region)
- Search analytics dashboard (GA4 tracks search queries)

## Dependencies

- EPIC-01 (Foundation) — Umbraco project with Examine available
- EPIC-02 (Content Model) — Article and taxonomy document types
- EPIC-03 (Public Site Core) — base layout with header/nav structure

## High-Level Acceptance Criteria

- [ ] Search returns relevant results for Bulgarian-language queries within 500 ms
- [ ] Empty results display a user-friendly Bulgarian message
- [ ] The Examine index updates when articles are published, unpublished, or updated
- [ ] Category links in the navigation are dynamically generated from active categories
- [ ] Region filter shows all active regions

---

## User Stories

### US-06.01 — Examine Search Index Configuration

**As a** developer, **I want** the Umbraco Examine index configured to include all published article fields (headline, subtitle, body, tags), **so that** search queries return accurate and complete results.

**Acceptance Criteria:**
- Examine index includes: headline, subtitle, body (text extracted from rich text), tags (from linked tag nodes)
- Only published articles are included in the index (drafts, scheduled, and unpublished articles are excluded)
- The index updates automatically when an article is published, unpublished, or edited
- The index can be rebuilt manually from the Umbraco backoffice

---

### US-06.02 — Search Results Page

**As a** visitor, **I want** to search for articles by keyword and see results with title, excerpt, date, category, and region, **so that** I can quickly find the content I'm looking for.

**Acceptance Criteria:**
- Search results page is accessible at `/search/?q={query}` (or equivalent)
- Each result displays: article title (linked), text excerpt with query highlights, publish date, category badge, region badge
- Results are sorted by relevance (Lucene default scoring)
- Search processes within ≤ 500 ms (server-side)
- The search query is preserved in the search input field on the results page

---

### US-06.03 — Empty Search Results

**As a** visitor, **I want** to see a helpful message when my search returns no results, **so that** I'm not confused by a blank page.

**Acceptance Criteria:**
- When no results match, the page displays a message in Bulgarian (e.g., "Няма намерени резултати за „{query}". Опитайте с различни ключови думи.")
- The search input remains visible for a new query
- The page does not show an error or broken layout

---

### US-06.04 — Search Input in Header

**As a** visitor, **I want** to access search from any page via a search icon in the header, **so that** I can find content without navigating back to the homepage.

**Acceptance Criteria:**
- A search icon/button is present in the site header on every page
- Clicking/tapping the icon reveals a search input field (inline or overlay)
- Submitting the search form navigates to the search results page with the entered query
- On mobile, the search input is accessible from the hamburger menu or a dedicated icon

---

### US-06.05 — Header Category Navigation

**As a** visitor, **I want** the header navigation to show links to all active topic categories, **so that** I can jump directly to articles about topics I care about.

**Acceptance Criteria:**
- Category links are dynamically generated from the active categories in the CMS taxonomy
- Adding or removing a category in the CMS updates the navigation without code changes
- Categories are displayed in the order defined in the CMS content tree
- Each link navigates to the corresponding category archive page

---

### US-06.06 — Region Filter

**As a** visitor, **I want** a region filter in the header that lets me browse articles from a specific region, **so that** I can focus on news from my local area.

**Acceptance Criteria:**
- A region dropdown or link list is present in the header
- All active regions from the CMS taxonomy are listed
- Selecting a region navigates to the corresponding region archive page (`/region/{slug}/`)
- The region filter is accessible on both desktop and mobile layouts

---

### US-06.07 — Search Query Sanitization

**As a** developer, **I want** search queries to be sanitized and validated before being passed to Examine, **so that** the search is safe from injection attacks and handles edge cases gracefully.

**Acceptance Criteria:**
- HTML tags in the search query are stripped
- Lucene special characters are escaped to prevent query syntax errors
- Empty or whitespace-only queries redirect back to the search page with no error
- Queries exceeding 200 characters are truncated
- XSS payloads in the query do not execute (output encoded in the results page)
