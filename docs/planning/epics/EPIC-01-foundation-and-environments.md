FILE: docs/planning/epics/EPIC-01-foundation-and-environments.md

# EPIC-01 — Foundation & Environments

## Goal / Outcome

Establish the project infrastructure so that both developers can write application code locally, deploy to a staging environment, and eventually provision the production VPS. This epic covers repository setup, Umbraco scaffolding, database provisioning, logging, coding standards, and environment configuration.

## In Scope (MVP)

- Git repository with agreed folder structure (`src/`, `docs/`, `CLAUDE.md`)
- Umbraco 17 LTS project scaffolded and building locally
- SQL Server Express running locally for each developer
- Serilog file-based logging configured (daily rotation, 30-day retention)
- `.editorconfig` and code formatting rules
- `CLAUDE.md` with project conventions
- Placeholder brand assets (logo, color palette, typography)
- Staging VPS provisioned (Windows Server, IIS, .NET 10, SQL Server Express)
- Production VPS provisioned (same stack + Let's Encrypt via win-acme)
- Domain `predelnews.com` acquired and DNS configured

## Out of Scope (MVP)

- CI/CD pipeline (post-launch enhancement)
- CDN / WAF (Phase 2)
- Container orchestration or auto-scaling
- Multi-environment automated deployment tooling

## Dependencies

- None (this is the first epic; all other epics depend on it)

## High-Level Acceptance Criteria

- [ ] Both developers can run the Umbraco backoffice locally at `https://localhost/umbraco/` and create a test content node persisted to SQL Server Express
- [ ] Serilog writes rotating log files to the configured path
- [ ] `.editorconfig` is committed and enforced in the IDE
- [ ] Staging VPS serves the Umbraco application over HTTPS
- [ ] Production VPS is provisioned with Let's Encrypt certificate, HTTPS enforcement, and HTTP→HTTPS redirect
- [ ] Domain `predelnews.com` resolves to the production VPS

---

## User Stories

### US-01.01 — Repository & Project Scaffold

**As a** developer, **I want** a Git repository with the agreed folder structure and a scaffolded Umbraco 17 LTS project, **so that** I can start writing application code immediately.

**Acceptance Criteria:**
- Repository contains `src/`, `docs/`, `CLAUDE.md`, `.editorconfig`, and `.gitignore`
- `dotnet build` succeeds for the Umbraco project
- The Umbraco backoffice is accessible at `https://localhost/umbraco/` after `dotnet run`

---

### US-01.02 — Local Database Setup

**As a** developer, **I want** SQL Server Express installed and configured locally with the Umbraco database created, **so that** all CMS data persists between sessions.

**Acceptance Criteria:**
- SQL Server Express is running locally
- The Umbraco application connects to the local SQL Server instance on startup
- Content created in the backoffice persists after an application restart

---

### US-01.03 — Structured Logging Configuration

**As a** developer, **I want** Serilog configured with a file sink (daily rotation, 30-day retention), **so that** application events are logged to disk for debugging and operational monitoring.

**Acceptance Criteria:**
- Serilog is registered in `Program.cs`
- Log files appear in the configured log directory (e.g., `/logs/`)
- Logs rotate daily; files older than 30 days are automatically cleaned up
- Log entries include timestamp, log level, source context, and message

---

### US-01.04 — Coding Standards & Conventions

**As a** developer, **I want** an `.editorconfig` file and a `CLAUDE.md` with project conventions committed to the repository, **so that** both developers and Claude Code produce consistent, reviewable code.

**Acceptance Criteria:**
- `.editorconfig` defines indentation, line endings, and C# formatting rules
- `CLAUDE.md` documents naming conventions, folder structure, branching strategy, and Umbraco-specific patterns
- Both files are committed to the `main` branch

---

### US-01.05 — Placeholder Brand Assets

**As a** developer, **I want** placeholder brand assets (logo, color palette, font choice) committed to the repository, **so that** templates can be built with realistic visuals before final branding is ready.

**Acceptance Criteria:**
- A placeholder logo image exists in `wwwroot/images/`
- CSS variables for the brand color palette are defined in `site.css`
- A web-safe font stack is configured as the default typography

**Notes:** Final brand assets are applied during Phase 6 (Content Loading). Placeholders must be clearly identifiable as non-final.

---

### US-01.06 — Staging VPS Provisioning

**As a** developer, **I want** a staging VPS provisioned with Windows Server, IIS, .NET 10, and SQL Server Express, **so that** I can deploy and test the application in a production-like environment.

**Acceptance Criteria:**
- Staging VPS is accessible via RDP
- IIS is configured with an ASP.NET Core site binding
- The Umbraco application deploys to staging and the backoffice is accessible over HTTPS
- SQL Server Express is running on the staging VPS with a dedicated database

---

### US-01.07 — Production VPS Provisioning

**As a** developer, **I want** the production VPS provisioned with the same stack as staging, plus Let's Encrypt HTTPS and the `predelnews.com` domain, **so that** the site is ready for content loading and launch.

**Acceptance Criteria:**
- Production VPS mirrors staging configuration (IIS, .NET 10, SQL Server Express)
- Let's Encrypt certificate is issued via win-acme and auto-renews
- HTTP requests to `predelnews.com` redirect (301) to HTTPS
- The Umbraco backoffice is accessible at `https://predelnews.com/umbraco/`

---

### US-01.08 — Base Layout Template

**As a** developer, **I want** a `_Layout.cshtml` with semantic HTML5 structure (`<header>`, `<nav>`, `<main>`, `<footer>`), a skip-to-content link, and placeholder CSS, **so that** all page templates inherit a consistent, accessible shell.

**Acceptance Criteria:**
- `_Layout.cshtml` renders `<header>`, `<nav>`, `<main>`, `<footer>` elements
- A skip-to-content link (`<a href="#main-content">`) is the first focusable element
- `color-scheme: light only` is enforced in the global CSS
- The layout is mobile-first and responsive (no horizontal scroll at 320px–1920px)

---

### US-01.09 — Custom Database Migration Framework

**As a** developer, **I want** the Umbraco `IMigrationPlan` / `PackageMigration` scaffolded to create all custom `pn_*` tables on application startup, **so that** all environments automatically have the required database schema.

**Acceptance Criteria:**
- A `PredelNewsMigrationPlan` class exists and runs on app startup
- All custom tables (`pn_comments`, `pn_comment_audit_log`, `pn_polls`, `pn_poll_options`, `pn_ad_slots`, `pn_email_subscribers`, `pn_contact_submissions`, `pn_audit_log`) are created
- Running the application a second time does not re-create or error on existing tables
- Ad slot seed data (6 rows) is inserted during the initial migration
