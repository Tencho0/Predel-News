FILE: docs/planning/epics/EPIC-07-engagement-features.md

# EPIC-07 — Engagement Features (Polls, Email Signup, Contact Form)

## Goal / Outcome

Deliver three interactive visitor engagement features: a homepage poll widget, an email signup bar for future newsletter subscribers, and a contact form. These features drive community participation, build an email list from Day 1, and provide a channel for visitor inquiries.

## In Scope (MVP)

- **Poll widget:** Poll CRUD in CMS, single active poll enforcement, vote API endpoint, cookie-based deduplication, results display (percentage bar chart), open/close dates
- **Email signup:** Homepage signup bar, email validation, duplicate handling, GDPR consent checkbox, CSV export for Editor/Admin
- **Contact form:** Form submission with honeypot + rate limiting, SMTP delivery, DB persistence, graceful degradation on SMTP failure

## Out of Scope (MVP)

- Newsletter sending / campaign management (Phase 2)
- Multiple concurrent polls (Phase 2)
- Public poll archive (Phase 2)
- Double opt-in email confirmation
- reCAPTCHA on any form (Phase 2)

## Dependencies

- EPIC-01 (Foundation) — database migrations for `pn_polls`, `pn_poll_options`, `pn_email_subscribers`, `pn_contact_submissions`
- EPIC-02 (Content Model) — SiteSettings (contact recipient email, social links)
- EPIC-03 (Public Site Core) — homepage template (poll widget slot, email signup slot), contact page template

## High-Level Acceptance Criteria

- [ ] The poll widget displays on the homepage; a vote is recorded; results are shown; duplicate votes from the same browser are prevented
- [ ] Email signup stores the email with consent flag; duplicates are handled gracefully; Admin can export CSV
- [ ] Contact form submission is stored in the DB and emailed to the configured recipient
- [ ] All form endpoints validate CSRF tokens and apply rate limiting

---

## User Stories

### US-07.01 — Poll Creation & Management

**As an** editor, **I want** to create polls with a question and 2–4 answer options, activate one poll at a time, and view results, **so that** I can engage readers with quick opinion questions.

**Acceptance Criteria:**
- Editor/Admin can create a poll with a question (text) and 2–4 options
- Activating a new poll automatically deactivates any previously active poll (enforced by DB constraint + service logic)
- Editor can set open and close dates on a poll
- A closed poll (past close date or manually deactivated) shows final results but no longer accepts votes
- Poll results (vote counts and percentages) are visible to Editor/Admin in the CMS
- Past polls are listed in the CMS for reference (not publicly accessible in MVP)

---

### US-07.02 — Poll Widget on Homepage

**As a** visitor, **I want** to see the active poll on the homepage and vote with a single click, **so that** I can participate in community opinion questions.

**Acceptance Criteria:**
- The poll widget renders on the homepage when an active poll exists
- If no poll is active, the widget block is hidden (no empty placeholder)
- The widget displays the question and clickable answer options
- After voting, results are shown immediately as a percentage bar chart
- The vote is submitted via AJAX (no full page reload)
- A CSRF token is included in the vote request

---

### US-07.03 — Poll Vote Deduplication

**As a** developer, **I want** cookie-based vote deduplication to prevent a visitor from voting multiple times on the same poll, **so that** poll results are reasonably accurate.

**Acceptance Criteria:**
- After a vote, a cookie is set identifying the poll and the vote
- If the cookie exists for the current active poll, the widget shows results instead of vote options
- Clearing cookies or using incognito mode allows a re-vote (accepted limitation for MVP)
- The cookie does not contain personally identifiable information

---

### US-07.04 — Email Signup Bar

**As a** visitor, **I want** to enter my email address in a signup bar on the homepage, **so that** I can receive future newsletters about regional news.

**Acceptance Criteria:**
- The email signup bar is visible on the homepage (between Latest Articles and Footer, or as specified in the layout)
- The form has: email input field, GDPR consent checkbox (required, labeled in Bulgarian e.g., "Съгласен/а съм да получавам новини по имейл"), and a submit button
- On successful submission, a confirmation message appears (e.g., "Благодарим! Имейлът ви беше записан.")
- The form fields are cleared after successful submission
- A CSRF token is included and validated

---

### US-07.05 — Email Signup Validation & Storage

**As a** developer, **I want** submitted emails to be validated, deduplicated, and stored with a consent flag and timestamp, **so that** the email list is clean and GDPR-compliant.

**Acceptance Criteria:**
- Email format is validated server-side; invalid emails are rejected with a Bulgarian error message
- Duplicate email submissions are handled gracefully: no error shown to the visitor, no duplicate row stored
- Each email record stores: email, `signed_up_at` (UTC), `consent_flag = true`
- Rate limiting: max 5 signups per IP per 10 minutes
- The consent checkbox must be checked; submitting without consent is rejected with a validation error

---

### US-07.06 — Email List CSV Export

**As an** admin, **I want** to export the collected email list as a CSV file, **so that** I can import it into an email marketing platform when the newsletter launches.

**Acceptance Criteria:**
- Editor and Admin can trigger a CSV export from the CMS backoffice
- The CSV includes: email, signed_up_at (formatted date), consent_flag
- The export downloads immediately as a file (not displayed on-screen)
- Writers cannot access the email export

---

### US-07.07 — Contact Form Submission

**As a** visitor, **I want** to submit an inquiry via the contact form on the Контакти page, **so that** I can reach the editorial team.

**Acceptance Criteria:**
- The contact form has four required fields: Име (name), Email, Тема (subject), Съобщение (message)
- All fields show inline validation errors in Bulgarian for empty or malformed input
- A honeypot hidden field is present; submissions filling it are silently discarded
- Rate limiting: max 3 submissions per IP per 10 minutes; excess attempts show a Bulgarian message (e.g., "Моля, опитайте отново след няколко минути.")
- On successful submission, a confirmation message appears: "Съобщението ви беше изпратено успешно." and the form is cleared

---

### US-07.08 — Contact Form Email Delivery & Persistence

**As an** admin, **I want** contact form submissions to be emailed to a configurable address and stored in the database, **so that** no inquiry is lost even if email delivery fails.

**Acceptance Criteria:**
- Submission data is inserted into `pn_contact_submissions` (name, email, subject, message, submitted_at, IP) before attempting email delivery
- An email is sent via the configured SMTP provider to the recipient address from CMS Site Settings
- If SMTP delivery fails, the submission is still stored in the DB; the error is logged; the visitor sees the success message (graceful degradation)
- The recipient email is configurable by Admin in CMS Site Settings without code changes

---

### US-07.09 — CSRF Protection on All Form Endpoints

**As a** developer, **I want** anti-forgery tokens validated on all state-changing form endpoints (comments, polls, email signup, contact form), **so that** cross-site request forgery attacks are prevented.

**Acceptance Criteria:**
- All POST endpoints (`/api/comments`, `/api/poll/vote`, `/api/email-signup`, `/api/contact`) validate the anti-forgery token
- Requests without a valid token return HTTP 403
- The token is automatically included in all forms rendered by Razor templates

## Open Questions

- **OQ1:** The PRD says email signup is "single opt-in" with consent checkbox (Assumption A3 in FS). Should we add a confirmation message noting that no confirmation email will be sent, to set expectations?
