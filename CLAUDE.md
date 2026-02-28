# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PredelNews is a Bulgarian regional news website for Southwest Bulgaria (Blagoevgrad region). Built on **Umbraco 17.2.1 LTS** with **.NET 10**, deployed to a single Windows VPS with IIS and SQL Server Express.

Comprehensive specs live in `docs/`. A previous Umbraco 13 implementation is archived in `Legacy/src.zip`.

## Build & Run Commands

```bash
# Build entire solution
dotnet build PredelNews.slnx

# Run the web project
dotnet run --project src/Web/PredelNews.Web/PredelNews.Web.csproj

# Watch mode (auto-reload on changes)
dotnet watch run --project src/Web/PredelNews.Web/PredelNews.Web.csproj

# Run all tests
dotnet test PredelNews.slnx

# Run a specific test project
dotnet test src/Core/PredelNews.Core.Tests/PredelNews.Core.Tests.csproj
```

Umbraco backoffice is at `https://localhost:{port}/umbraco/` (HTTPS enforced).

## Solution Architecture

Five projects under `src/`, all C# on .NET 10:

| Project | Path | Purpose |
|---------|------|---------|
| `PredelNews.Web` | `src/Web/PredelNews.Web/` | ASP.NET Core host, Razor views/templates, Umbraco startup, setup composers |
| `PredelNews.Core` | `src/Core/PredelNews.Core/` | Domain models, interfaces, business logic, notification handlers |
| `PredelNews.Infrastructure` | `src/Infrastructure/PredelNews.Infrastructure/` | Database migrations (pn_* tables), Dapper repositories |
| `PredelNews.BackofficeExtensions` | `src/BackofficeExtensions/PredelNews.BackofficeExtensions/` | Custom Umbraco backoffice dashboards/API extensions |
| `PredelNews.Core.Tests` | `src/Core/PredelNews.Core.Tests/` | xUnit tests with NSubstitute + FluentAssertions |

**Dependency flow:** Web → Core, Infrastructure; Infrastructure → Core; BackofficeExtensions → Core

**Solution file:** `PredelNews.slnx` (XML format, .NET 10+)

## Key Technical Decisions

- **Server-side rendered** — all public pages use Umbraco Razor templates (SEO-first)
- **Search** — Umbraco Examine (Lucene-based, in-process), no external search service
- **Caching** — output cache with 60s staleness for public pages
- **Images** — WebP format with responsive srcsets via ImageSharp
- **Comments** — anonymous, immediate publish, soft-delete only, cookie-based dedup for polls
- **Email signup** — single opt-in (no confirmation email at MVP)
- **Ads** — Google AdSense + direct-sold banner placements
- **Logging** — Serilog with daily file rotation to `logs/predelnews-{date}.log`
- **Bulgarian language only** at MVP; no dark mode
- **File-scoped namespaces** throughout (enforced by `.editorconfig`)
- **Document types created programmatically** via `IContentTypeService` at startup (idempotent)
- **Custom tables** use `pn_` prefix, created via Umbraco `PackageMigrationPlan` + `AsyncMigrationBase`
- **SlugGenerator** in Core — pure business logic with Cyrillic-to-Latin transliteration

## Umbraco 17 API Patterns

Key differences from older Umbraco versions:

- `IContentTypeService.Get(string alias)` — sync method for lookup by alias
- `IContentTypeService.SaveAsync(ct, Constants.Security.SuperUserKey)` — requires performing user key
- `IContentPublishingService.PublishAsync()` — replaces old `SaveAndPublish()`
- `AsyncMigrationBase` — replaces deprecated `MigrationBase`
- `PackageMigrationPlan` — auto-discovered, uses `Guid` state IDs
- `Umbraco.Cms.Api.Management` — replaces old `Umbraco.Cms.Web.BackOffice` package
- `INotificationAsyncHandler<T>` — async notification handler pattern

## Git Workflow

- `main` — always deployable
- `feature/{short-name}` — short-lived branches (1-3 days), merged via PR
- No long-lived `develop` branch; 2-person team

## Documentation Reference

- **PRD & business goals:** `docs/business/prd.md`
- **Functional spec (feature IDs: FR-{MODULE}-{NNN}):** `docs/business/functional-specification.md`
- **Non-functional requirements:** `docs/business/non-functional-requirements.md`
- **Architecture & deployment:** `docs/technical/architecture.md`
- **Technical spec (content models, DB schema, API contracts):** `docs/technical/technical-specification.md`
- **Development plan (7 phases, epics):** `docs/planning/development-plan.md`
- **Individual epics:** `docs/planning/epics/EPIC-{01..10}-*.md`

## Deployment

Single Windows VPS → IIS with .NET hosting bundle → SQL Server Express 2022. HTTPS via Let's Encrypt/win-acme. No CDN, load balancer, or reverse proxy at MVP.

## Quality Targets

- Lighthouse performance >= 90
- Core Web Vitals "Good" (LCP <= 2.5s mobile)
- WCAG 2.1 AA compliance
- Sponsored content always labeled "Платена публикация" (template-enforced)
