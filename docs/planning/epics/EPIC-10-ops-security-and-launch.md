FILE: docs/planning/epics/EPIC-10-ops-security-and-launch.md

# EPIC-10 — Ops, Security & Launch Readiness

## Goal / Outcome

Harden the system for production: implement security measures, optimize performance, configure monitoring and backups, execute load testing, and coordinate the content loading and soft launch phases. After this epic, the site is production-ready, content-loaded, and launched to the public.

## In Scope (MVP)

- **Security hardening:** HTTPS enforcement, HSTS, security headers (CSP report-only, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy), CMS account lockout (5 attempts / 15 min), strong password policy, session timeout (30 min), session cookie attributes
- **Secrets management:** All credentials in environment variables; zero secrets in source code or appsettings.json
- **Performance optimization:** Output caching (60s staleness, invalidation on publish), image optimization verification (WebP, srcset, lazy loading, ≤ 200 KB), critical CSS review, JS bundle ≤ 50 KB compressed, third-party scripts async/defer
- **Load testing:** 5× concurrent users for 30 minutes; zero 503s; TTFB ≤ 1500 ms at 95th percentile
- **Backup setup:** Daily automated DB + media backups to off-VPS storage; restore drill on staging
- **Monitoring:** Uptime monitoring (homepage HTTP check every 5 min), infrastructure alerts (CPU > 80%, disk > 80%, memory), Let's Encrypt certificate expiry monitoring
- **Content loading coordination:** Editorial team populates production CMS with 20+ articles, footer pages, poll, brand assets
- **Soft launch:** Limited audience (50–100 people), monitoring, bug triage, go/no-go decision
- **Deployment documentation:** Deployment and rollback procedures documented and tested

## Out of Scope (MVP)

- CI/CD pipeline (post-launch enhancement)
- CDN / WAF (Phase 2)
- MFA for CMS accounts (Phase 2)
- Real User Monitoring dashboard (Phase 2)
- Full GDPR consent management (Phase 2)

## Dependencies

- All prior epics (EPIC-01 through EPIC-09) must be feature-complete
- Production VPS provisioned (EPIC-01)

## High-Level Acceptance Criteria

- [ ] Lighthouse Performance score ≥ 90 on mobile for homepage, article page, and one archive page
- [ ] Core Web Vitals pass: LCP ≤ 2.5s, INP ≤ 200ms, CLS ≤ 0.1
- [ ] Load test passes with zero 503s
- [ ] SSL Labs grade ≥ A
- [ ] OWASP ZAP scan returns zero high-severity findings
- [ ] Database backup and restore drill completes successfully
- [ ] ≥ 20 real articles published, all footer pages populated, at least 1 active poll
- [ ] Soft launch completes with no critical bugs

---

## User Stories

### US-10.01 — HTTPS Enforcement & SSL Hardening

**As a** visitor, **I want** the site to always use HTTPS with a valid certificate, **so that** my connection is secure and my browser doesn't show warnings.

**Acceptance Criteria:**
- All HTTP requests redirect to HTTPS via 301 redirect
- HSTS header is set with `max-age=31536000; includeSubDomains`
- SSL Labs test returns grade ≥ A for `predelnews.com`
- Let's Encrypt certificate auto-renews via win-acme (verified by checking expiry date)

---

### US-10.02 — Security Headers

**As a** developer, **I want** all HTTP responses to include security headers, **so that** common web attacks (XSS, clickjacking, MIME sniffing) are mitigated.

**Acceptance Criteria:**
- All responses include: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy` (camera, microphone, geolocation denied)
- `Content-Security-Policy` is set in report-only mode at MVP (enforced in Phase 1.1)
- SecurityHeaders.com scan returns grade ≥ A
- Headers are applied via `SecurityHeadersMiddleware` (not per-page)

---

### US-10.03 — CMS Account Hardening

**As an** admin, **I want** CMS accounts protected by strong password policy, brute-force lockout, and session management, **so that** unauthorized access is prevented.

**Acceptance Criteria:**
- Password policy: ≥ 12 characters, mixed case, digit, special character
- Account lockout after 5 failed login attempts (15-minute lockout period)
- Session timeout: 30 minutes of inactivity
- Session cookies: `Secure`, `HttpOnly`, `SameSite=Lax`
- `/umbraco/` is excluded from `robots.txt`

---

### US-10.04 — Secrets Management

**As a** developer, **I want** all sensitive credentials stored in environment variables (not in source code or config files), **so that** secrets are not leaked through the repository.

**Acceptance Criteria:**
- Database connection string, SMTP credentials, and any API keys are read from environment variables
- `appsettings.json` contains no plaintext secrets
- A repository-wide grep for common secret patterns (passwords, connection strings, API keys) returns zero matches
- `appsettings.json` is not accessible via browser request

---

### US-10.05 — Output Caching Configuration

**As a** visitor, **I want** public pages to load quickly via server-side caching, **so that** I get fast response times even on a single-server deployment.

**Acceptance Criteria:**
- ASP.NET Core `OutputCache` middleware is configured with 60-second maximum staleness
- Cache is invalidated when content is published, updated, or unpublished (via Umbraco `ContentPublishedNotification`)
- CMS backoffice pages, preview URLs, and POST endpoints are excluded from caching
- Warm cache TTFB ≤ 50 ms for the homepage

---

### US-10.06 — Performance Optimization & Verification

**As a** developer, **I want** to verify that all performance targets are met (Lighthouse ≥ 90, Core Web Vitals "Good"), **so that** the site launches with a fast, high-quality user experience.

**Acceptance Criteria:**
- Lighthouse Performance score ≥ 90 on mobile for homepage, article page (with ads), and one archive page
- Core Web Vitals: LCP ≤ 2.5s, INP ≤ 200ms, CLS ≤ 0.1 on mobile simulation
- TTFB ≤ 600 ms (95th percentile) under normal conditions
- Total blocking JavaScript ≤ 50 KB (compressed)
- All content images serve WebP with responsive `srcset` and `loading="lazy"` below the fold
- Cover images on article pages use `<link rel="preload">`

---

### US-10.07 — Load Testing

**As a** developer, **I want** to run a load test simulating 5× concurrent users for 30 minutes, **so that** I'm confident the site survives traffic spikes from breaking news.

**Acceptance Criteria:**
- Load test tool (k6 or Artillery) configured to simulate 5× baseline concurrent users
- Test runs for 30 minutes against the staging environment
- Zero HTTP 503 errors during the test
- TTFB ≤ 1500 ms at 95th percentile during the spike
- Comment submission and contact form submission remain functional during load
- Test results are documented

---

### US-10.08 — Backup Setup & Restore Drill

**As an** admin, **I want** daily automated backups of the database and media files stored off-VPS, with a verified restore procedure, **so that** data can be recovered in case of VPS failure.

**Acceptance Criteria:**
- SQL Server database backup runs daily to off-VPS storage (separate volume or remote)
- Media files are backed up daily (file sync to off-VPS storage)
- Backup failure triggers an alert (email or monitoring notification)
- One full restore drill is completed on the staging environment
- Restore completes within 4 hours (RTO target)
- Restore procedure is documented in `docs/technical/deployment.md`

---

### US-10.09 — Uptime & Infrastructure Monitoring

**As an** admin, **I want** uptime monitoring for the homepage and infrastructure alerts for CPU, memory, and disk usage, **so that** I'm notified promptly if the site goes down or resources are exhausted.

**Acceptance Criteria:**
- UptimeRobot (or equivalent) checks the homepage every 5 minutes via HTTP
- Downtime alert is sent within 10 minutes of IIS becoming unresponsive
- Infrastructure monitoring alerts when: CPU > 80% sustained, disk > 80% used, memory > 85% used
- Let's Encrypt certificate expiry is monitored (alert ≥ 14 days before expiry)
- Monitoring dashboard/access is documented

---

### US-10.10 — Security Scan & XSS Verification

**As a** developer, **I want** to run an OWASP ZAP scan and manually test for XSS in user input fields, **so that** the site launches without known high-severity security vulnerabilities.

**Acceptance Criteria:**
- OWASP ZAP scan runs against all public pages and form endpoints
- Zero high-severity findings in the scan results
- `<script>alert('xss')</script>` entered in comment, search, and contact form fields is HTML-encoded on output (not executed)
- XSS payloads in URL parameters do not execute
- Any findings are documented and remediated before launch

---

### US-10.11 — Content Loading & Production Configuration

**As an** editor, **I want** to populate the production CMS with real articles, footer page content, and brand assets, **so that** the site looks and feels like a real news publication at launch.

**Acceptance Criteria:**
- ≥ 20 articles published across ≥ 3 categories and ≥ 2 regions with real content, proper images (with alt text), and correct metadata
- All 5 footer pages have real content (not placeholder text)
- Privacy policy is published at `/politika-za-poveritelnost/` and linked from footer and cookie banner
- At least 1 poll is active and functional
- Production logo and brand colors are applied across all pages (no placeholders)
- GA4 tracking fires on every page (verified in GA4 real-time report)
- AdSense codes configured or application submitted (slots collapse gracefully pending approval)
- Homepage Breaking News block is editorially curated with real articles
- `/sitemap.xml` includes all published content

---

### US-10.12 — Soft Launch & Go/No-Go

**As a** product owner, **I want** a soft launch with a limited audience (50–100 people) for 1 week, **so that** we can catch any remaining issues before opening to the public.

**Acceptance Criteria:**
- The site is shared with a controlled group (team, friends, local contacts)
- Monitoring is active; any errors or downtime are immediately triaged
- A bug triage process is in place: critical bugs are fixed immediately; non-critical are backlogged
- At the end of the soft launch week, a go/no-go decision is made based on: zero critical bugs, monitoring green, content quality confirmed by the editor
- Public launch proceeds only after go decision
