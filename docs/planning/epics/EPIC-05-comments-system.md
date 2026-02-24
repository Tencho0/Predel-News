FILE: docs/planning/epics/EPIC-05-comments-system.md

# EPIC-05 — Comments System

## Goal / Outcome

Deliver a custom-built anonymous comments system with anti-spam measures, moderation tools, and audit logging. Visitors can post comments without registration, and the editorial team can moderate content effectively.

## In Scope (MVP)

- Anonymous comment posting (display name + comment text, no account required)
- Comments appear immediately after submission (no pre-moderation queue)
- Anti-spam pipeline: CSRF validation → honeypot → IP rate limiting (3 per 5 min) → input validation → link count filter (≥ 2 URLs → held) → banned word check
- Comment moderation: soft delete with audit logging (who, when, original content preserved)
- Held comments queue in CMS (link/banned-word triggers)
- Comment count on article cards
- Display name cookie persistence (prefills on subsequent visits)
- Comment list rendering (flat, chronological) on article page
- Configurable banned-word list via CMS Site Settings

## Out of Scope (MVP)

- Comment threading / replies (Phase 2)
- reCAPTCHA (Phase 2)
- Pre-moderation queue (comments are live immediately)
- Comment editing by the commenter
- Comment voting / reactions

## Dependencies

- EPIC-01 (Foundation) — database migrations for `pn_comments` and `pn_comment_audit_log`
- EPIC-02 (Content Model) — Article document type (comments link to article node ID)
- EPIC-03 (Public Site Core) — article page template where comments render

## High-Level Acceptance Criteria

- [ ] A visitor can submit an anonymous comment that appears immediately on the article page
- [ ] The 4th comment from the same IP within 5 minutes is rejected with a Bulgarian rate-limit message
- [ ] A comment with ≥ 2 URLs is held for review and not displayed publicly
- [ ] A comment triggering the honeypot is silently discarded
- [ ] An Editor/Admin/Writer can delete a comment; deletion is audit-logged with original content preserved
- [ ] Comment count displays correctly on article cards

---

## User Stories

### US-05.01 — Anonymous Comment Submission

**As a** visitor, **I want** to post a comment on an article by entering my display name and comment text without creating an account, **so that** I can share my opinion quickly and easily.

**Acceptance Criteria:**
- The comment form on the article page has two fields: "Име" (display name, max 50 chars) and "Коментар" (comment text, max 2000 chars)
- Both fields are required; inline validation errors appear in Bulgarian for empty fields
- On successful submission, the comment appears in the comment list immediately
- The display name is HTML-stripped; the comment text is HTML-encoded on output (XSS prevention)
- A CSRF token is included in the form and validated server-side

---

### US-05.02 — Comment Display & Chronological List

**As a** visitor, **I want** to see all comments on an article in chronological order with display name and timestamp, **so that** I can read the discussion.

**Acceptance Criteria:**
- Comments are listed below the article body in chronological order (oldest first)
- Each comment shows: display name, comment text, and timestamp (DD.MM.YYYY, HH:MM)
- Deleted comments (`is_deleted = true`) and held comments (`is_held = true`) are not displayed
- Comment count shows the total number of visible comments

---

### US-05.03 — Honeypot Anti-Spam

**As a** developer, **I want** a honeypot hidden field in the comment form that silently discards bot submissions, **so that** automated spam is filtered without impacting legitimate users.

**Acceptance Criteria:**
- The comment form includes a hidden field (via CSS, not `type="hidden"`) that legitimate users won't fill
- If the honeypot field is non-empty, the server returns HTTP 200 (no error shown) but discards the submission (no database storage)
- The honeypot field name is non-obvious (not "honeypot" or "trap")

---

### US-05.04 — IP-Based Rate Limiting

**As a** visitor, **I want** to be informed when I've posted too many comments in a short period, **so that** I understand why my comment wasn't accepted.

**Acceptance Criteria:**
- Maximum 3 comments per IP address within a 5-minute window
- The 4th comment attempt returns a user-friendly message in Bulgarian (e.g., "Моля, изчакайте няколко минути преди да коментирате отново.")
- Rate limiting is enforced server-side by querying `pn_comments` for recent IP submissions
- Rate-limited requests return HTTP 429

---

### US-05.05 — Link Count & Banned Word Filtering

**As a** moderator, **I want** comments with multiple URLs or banned words to be held for review rather than published immediately, **so that** spam and offensive content is caught before readers see it.

**Acceptance Criteria:**
- Comments containing ≥ 2 URLs are stored with `is_held = true` and `held_reason = 'link_count'`
- Comments matching a word from the configurable banned-word list are stored with `is_held = true` and `held_reason = 'banned_word:{word}'`
- Held comments are not displayed on the public article page
- The visitor sees an informational message (e.g., "Коментарът ви ще бъде прегледан от модератор.")
- The banned-word list is configurable in CMS Site Settings (comma-separated)

---

### US-05.06 — Comment Moderation (Soft Delete & Audit)

**As an** editor, **I want** to delete inappropriate comments with a full audit trail, **so that** moderation actions are transparent and the original content is preserved for accountability.

**Acceptance Criteria:**
- Writer, Editor, and Admin can delete any comment (via CMS or on the public article page when authenticated)
- Deletion sets `is_deleted = true` (soft delete); the comment disappears from the public page immediately
- An entry is created in `pn_comment_audit_log`: comment_id, deleted_by_user_id, deleted_at (UTC), original_display_name, original_text, original_ip
- The audit log entry is immutable (no delete/update endpoint exposed)

---

### US-05.07 — Held Comments Management

**As an** editor, **I want** to see a list of held comments in the CMS and approve or delete them, **so that** I can review flagged content and decide what to publish.

**Acceptance Criteria:**
- The CMS backoffice shows a "Held Comments" section listing all comments with `is_held = true` and `is_deleted = false`
- Each entry shows: article title, display name, comment text, held reason, and submission date
- Editor/Admin can approve a held comment (sets `is_held = false` — comment appears publicly) or delete it (soft delete + audit log)
- The held comments count is displayed on the editorial dashboard (EPIC-04)

---

### US-05.08 — Comment Count on Article Cards

**As a** visitor, **I want** to see the comment count on each article card, **so that** I can gauge community engagement before clicking into an article.

**Acceptance Criteria:**
- Article cards display the number of visible comments (not held, not deleted)
- The count is linked to the article's comments section anchor
- Articles with 0 comments show "0" or a comment icon without a count (design decision)

---

### US-05.09 — Display Name Cookie Persistence

**As a** visitor, **I want** my display name to be remembered across visits via a cookie, **so that** I don't have to re-type it every time I comment.

**Acceptance Criteria:**
- After a successful comment submission, the display name is saved in a browser cookie
- On subsequent visits, the "Име" field is pre-filled with the cookie value
- The cookie expires after 365 days
- If the cookie is cleared, the field is empty (no error)

## Open Questions

- **OQ1:** Should the comment delete button appear on the public article page for authenticated CMS users, or only in the backoffice? FS §7 (FR-CM-006) and Architecture §7.4 suggest both. This epic assumes both.
