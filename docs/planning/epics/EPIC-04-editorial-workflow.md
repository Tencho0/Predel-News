FILE: docs/planning/epics/EPIC-04-editorial-workflow.md

# EPIC-04 — Editorial Workflow

## Goal / Outcome

Implement the complete article lifecycle (Draft → In Review → Published / Scheduled / Unpublished) with role-based permissions, editorial notifications, post-publication corrections, article preview, and an editorial dashboard. After this epic, the editorial team can operate a full newsroom workflow through the CMS.

## In Scope (MVP)

- Article lifecycle states with role-based transitions
- Writer lock when article is "In Review"
- Scheduled publishing via Umbraco scheduler (± 1 minute tolerance)
- Post-publication edits with "Updated" dateline
- Article preview for unpublished content
- Editorial dashboard (articles in review, published today/this week, recent comments count, held comments count, email signups count)
- Article state transition audit logging
- Version history for articles (Umbraco built-in)
- Output cache invalidation on publish/unpublish

## Out of Scope (MVP)

- Email notifications for state changes (Phase 2)
- Multi-editor collaborative editing
- Approval chains / multi-step review

## Dependencies

- EPIC-01 (Foundation) — base infrastructure
- EPIC-02 (Content Model) — Article document type and user groups

## High-Level Acceptance Criteria

- [ ] A Writer can create a draft, submit for review; an Editor sees it in the dashboard, can edit and publish; the article appears on the public site within 60 seconds
- [ ] A scheduled article auto-publishes at the set time (± 1 minute)
- [ ] An Editor can unpublish an article; it disappears from the public site within 60 seconds
- [ ] All state transitions are audit-logged with acting user and timestamp
- [ ] Article version history is accessible to Editor/Admin

---

## User Stories

### US-04.01 — Article Lifecycle States

**As a** writer, **I want** to create draft articles and submit them for editorial review, **so that** my work goes through proper editorial oversight before publication.

**Acceptance Criteria:**
- A newly created article defaults to Draft state
- A Writer can move a Draft to "In Review" via a "Submit for Review" action
- A Writer cannot publish or schedule an article directly
- The current state is clearly visible in the CMS article editor
- All state transitions are logged in `pn_audit_log` with user ID, action, and timestamp

---

### US-04.02 — Editor Review & Publish

**As an** editor, **I want** to review submitted articles, make edits, and publish or schedule them, **so that** I maintain editorial quality and control over what goes live.

**Acceptance Criteria:**
- Editor can open an "In Review" article and make edits
- Editor can move an article from "In Review" (or Draft) to "Published" or "Scheduled"
- Editor can return an "In Review" article to "Draft" (with optional note visible to the Writer)
- When an article is published, the output cache is invalidated for the homepage, category archive, and the article URL
- The article appears on the public site within 60 seconds of publishing

---

### US-04.03 — Writer Lock During Review

**As an** editor, **I want** articles in "In Review" to be locked from writer edits, **so that** the writer doesn't make changes while I'm reviewing.

**Acceptance Criteria:**
- While an article is "In Review," the originating Writer cannot edit the content fields
- The Writer can view the article in read-only mode and see its current status
- Editor/Admin can still edit the article during review
- When the Editor returns the article to "Draft," the Writer regains edit access

---

### US-04.04 — Scheduled Publishing

**As an** editor, **I want** to schedule an article for future publication by setting a date and time, **so that** content goes live at the optimal time without manual intervention.

**Acceptance Criteria:**
- Editor can set a Publish Date in the future and save as "Scheduled"
- The Umbraco scheduler automatically publishes the article at the set date/time (± 1 minute tolerance)
- Upon auto-publish, the same cache invalidation and Examine re-index logic fires as for manual publish
- A scheduled article is not visible on the public site before its publish time

---

### US-04.05 — Unpublish Article

**As an** editor, **I want** to unpublish a published article, **so that** I can remove incorrect or sensitive content from the public site immediately.

**Acceptance Criteria:**
- Editor/Admin can unpublish a Published article
- The article is removed from the public site within the cache refresh interval (≤ 60 seconds)
- The unpublished article remains in the CMS for future editing or re-publishing
- The unpublish action is audit-logged

---

### US-04.06 — Post-Publication Edits & Corrections

**As an** editor, **I want** to edit a published article and have an "Updated" dateline appear automatically, **so that** corrections are transparent to readers.

**Acceptance Criteria:**
- Editor/Admin can edit a Published article's content directly
- When saved with changes, an "Updated" dateline is displayed on the public article page (e.g., "Обновена: DD.MM.YYYY, HH:MM") in addition to the original publish date
- The CMS maintains version history; Editor/Admin can view previous versions
- Only the latest published version is displayed on the public site

---

### US-04.07 — Article Preview

**As a** writer, **I want** to preview my unpublished article as it would appear on the public site, **so that** I can verify formatting and layout before submitting for review.

**Acceptance Criteria:**
- A "Preview" action is available for articles in Draft, In Review, and Scheduled states
- The preview renders the article using the public article page template with current content
- Preview is accessible only to authenticated CMS users (not publicly accessible via URL)

---

### US-04.08 — Editorial Dashboard

**As an** editor, **I want** a CMS dashboard showing articles awaiting review, recently published articles, and key metrics, **so that** I can manage the editorial queue efficiently.

**Acceptance Criteria:**
- Dashboard shows: count of articles "In Review," list of articles in review (sorted oldest first), articles published today/this week, recent comments count, held comments count, email signups count
- The dashboard refreshes on page load (no real-time push required at MVP)
- Dashboard is visible to Editor and Admin roles
- Clicking an article in the "In Review" list navigates directly to that article's editor

---

### US-04.09 — Editorial Notification (Dashboard Indicator)

**As an** editor, **I want** to see a badge or count indicator when new articles are submitted for review, **so that** I know there is work waiting for me without checking manually.

**Acceptance Criteria:**
- When a Writer submits an article for review, the "In Review" count updates on the Editor's dashboard
- Notification is CMS-internal (dashboard indicator), not email-based (email is Phase 2)
- The indicator is visible when navigating to the backoffice dashboard

**Notes:** This is a "Should" priority per FS FR-EW-002. Can be descoped to a basic list view if implementation is complex.
