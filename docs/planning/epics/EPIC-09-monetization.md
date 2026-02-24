FILE: docs/planning/epics/EPIC-09-monetization.md

# EPIC-09 — Monetization (Ads & Sponsored Content)

## Goal / Outcome

Implement the full advertising and sponsored content system: 6 configurable ad slots (supporting both AdSense and direct-sold banners), sponsored article labeling ("Платена публикация"), and the admin tools for managing ad placements. This enables revenue generation from Day 1.

## In Scope (MVP)

- 6 ad slot positions: header leaderboard, sidebar 1, sidebar 2, article mid-content, article bottom, footer banner
- Two ad modes per slot: AdSense (auto) and Direct-sold (image + URL + date range)
- Direct-sold auto-revert to AdSense after end date
- "Реклама" label on all ad slots (template-enforced, global CSS)
- Sponsored article "Платена публикация" banners (top + bottom of article body, template-enforced)
- Sponsored badge on article cards in all listing contexts
- `rel="sponsored noopener"` on external links in sponsored articles
- Sponsor name display on sponsored articles
- Ad slot management section in CMS (Admin only)
- Ad slot responsive behavior (sidebar slots hidden on mobile, leaderboard adapts)
- Fixed-height CSS for ad containers (CLS prevention)

## Out of Scope (MVP)

- Direct ad reporting (impression/click counts) — Phase 2
- Programmatic ad header bidding
- Ad slot A/B testing
- Native ad formats beyond sponsored articles

## Dependencies

- EPIC-01 (Foundation) — database migration for `pn_ad_slots` with seed data
- EPIC-02 (Content Model) — `isSponsored` and `sponsorName` fields on Article, SiteSettings with AdSense script
- EPIC-03 (Public Site Core) — page templates where ad slots and sponsored labels render

## High-Level Acceptance Criteria

- [ ] All 6 ad slots render in correct positions on homepage and article page
- [ ] Admin can switch any slot to direct-sold mode; the banner displays; slot reverts to AdSense after end date
- [ ] All ad slots display the "Реклама" label
- [ ] Sponsored articles display "Платена публикация" at top and bottom of the article, and on all card instances
- [ ] External links in sponsored articles carry `rel="sponsored noopener"`
- [ ] Sidebar ad slots are hidden on mobile viewports

---

## User Stories

### US-09.01 — Ad Slot Rendering (AdSense Mode)

**As a** visitor, **I want** ads to load in designated positions without disrupting my reading experience, **so that** the site can generate revenue while I browse comfortably.

**Acceptance Criteria:**
- All 6 ad slots render in their designated positions: header leaderboard (below header, above content), sidebar 1 (right sidebar top), sidebar 2 (right sidebar mid), article mid (after 3rd paragraph), article bottom (below body, above comments), footer banner (above footer)
- In AdSense mode, each slot renders the configured AdSense ad unit code from `pn_ad_slots`
- AdSense JavaScript loads asynchronously and does not block page rendering
- If AdSense fails to load (ad blocker, network issue), the ad slot collapses gracefully (no blank space, no error)
- Ad containers have fixed CSS height to prevent Cumulative Layout Shift (CLS)

---

### US-09.02 — Ad Slot Rendering (Direct-Sold Mode)

**As a** visitor, **I want** direct-sold banner ads to display in designated positions with correct images and links, **so that** local advertisers reach me with relevant offers.

**Acceptance Criteria:**
- In direct-sold mode, the slot renders: banner image (`<img>` with alt text), destination URL (`<a>` with `rel="noopener"`), and optional impression tracking pixel
- The banner image is responsive (adapts to slot dimensions)
- Direct-sold mode takes priority over AdSense when active
- When the direct-sold end date passes, the slot automatically reverts to AdSense mode (checked on each page render)

---

### US-09.03 — Ad Slot Management (CMS)

**As an** admin, **I want** to manage all 6 ad slots from the CMS backoffice — switching between AdSense and direct-sold modes, uploading banners, and setting date ranges, **so that** I can sell and manage ad placements without developer help.

**Acceptance Criteria:**
- The CMS has an "Ad Management" section accessible to Admin only
- Each of the 6 slots is listed with its current mode (AdSense or Direct-sold) and status
- Admin can switch a slot to direct-sold mode by uploading a banner image, entering a destination URL, alt text, and start/end dates
- Admin can switch a slot back to AdSense mode manually
- Changes take effect on the public site within 60 seconds (cache refresh)
- Validation: start date must be before end date; image and URL are required for direct-sold mode

---

### US-09.04 — "Реклама" Label on Ad Slots

**As a** visitor, **I want** all ad slots to be clearly labeled "Реклама", **so that** I can distinguish advertising from editorial content.

**Acceptance Criteria:**
- Every rendered ad slot (both AdSense and direct-sold) displays a "Реклама" label
- The label is rendered by the Razor template (`_AdSlot.cshtml`), not injected by JavaScript
- The label styling is defined in the global CSS and cannot be overridden per-slot
- The label is visually distinct from editorial content (different background, smaller font, etc.)

---

### US-09.05 — Sponsored Article Labels ("Платена публикация")

**As a** visitor, **I want** sponsored articles to be clearly labeled "Платена публикация" at the top and bottom, **so that** I know when content is paid for by an external party.

**Acceptance Criteria:**
- If `isSponsored = true`, a "Платена публикация" banner renders above the headline and below the article body
- The sponsor name is displayed within or near the banner
- The banner is template-enforced: it is emitted by the Razor template and cannot be suppressed per-article via CMS fields
- The banner styling is defined in the global CSS (`_SponsoredBanner.cshtml`)
- The banner is visible on the article page at all viewport sizes

---

### US-09.06 — Sponsored Badge on Article Cards

**As a** visitor, **I want** to see a "Платена публикация" badge on article cards for sponsored content, **so that** I can identify paid content before clicking into it.

**Acceptance Criteria:**
- Article cards for sponsored articles display a "Платена публикация" badge
- The badge is visible in all card contexts: homepage blocks, archive pages, related articles, "All News" page
- The badge styling is consistent and non-removable (template-enforced)
- Non-sponsored articles do not display the badge

---

### US-09.07 — Sponsored Link Rewriting

**As a** developer, **I want** external links in sponsored article bodies to automatically receive `rel="sponsored noopener"`, **so that** the site complies with Google's guidelines for paid content links.

**Acceptance Criteria:**
- When `isSponsored = true`, all external (non-predelnews.com) links in the article body have `rel="sponsored noopener"` applied
- The rewriting happens at render time (or on save), not requiring the editor to manually set `rel` on each link
- Internal links (to other PredelNews pages) are not affected
- The rewriting is verified by inspecting the page source of a published sponsored article

---

### US-09.08 — Ad Slot Responsive Behavior

**As a** visitor on mobile, **I want** sidebar ad slots to be hidden and other ad slots to adapt to my screen size, **so that** ads don't overwhelm the mobile reading experience.

**Acceptance Criteria:**
- Sidebar slots (sidebar 1, sidebar 2) are hidden on viewports < 1024px (no `display`, no load)
- Header leaderboard adapts from 728×90 (desktop) to 320×100 (mobile) or collapses
- In-content and bottom ad slots render as responsive units
- Footer banner adapts to viewport width

---

### US-09.09 — Ad Slot CLS Prevention

**As a** developer, **I want** ad containers to have reserved CSS height, **so that** ad loading does not cause layout shifts that hurt Core Web Vitals.

**Acceptance Criteria:**
- Each ad slot container has a CSS `min-height` matching the expected ad dimensions
- Cumulative Layout Shift (CLS) contribution from ad slots is ≤ 0.05
- If an ad fails to load, the container collapses to 0 height after a timeout (no permanent blank space)

## Open Questions

- **OQ1:** Should the "Реклама" label be positioned above or inside the ad container? The NFR (NFR-UX-003) says "visually distinct" but doesn't specify exact placement. Recommend above the container for clarity.
