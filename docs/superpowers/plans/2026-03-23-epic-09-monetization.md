# EPIC-09 Monetization (Ads & Sponsored Content) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement 7 configurable ad slots (AdSense + direct-sold), sponsored article labels, and CMS admin tooling for ad slot management.

**Architecture:** Full layered — `AdSlot` domain model and `IAdSlotRepository` in Core/Infrastructure (Dapper), `AdSlotService` singleton with 60s `IMemoryCache`, `AdSlotViewComponent` rendering `_AdSlot.cshtml` partials, and `AdManagementApiController` in BackofficeExtensions. Article body processing (sponsored link rewriting + inline ad split) handled by `ArticleBodyProcessor` in Core, wrapped by an `IHtmlHelper` extension in Web.

**Tech Stack:** C# / .NET 10, Umbraco 17, Dapper, `Microsoft.Extensions.Caching.Memory`, HtmlAgilityPack, xUnit + NSubstitute + FluentAssertions.

---

## File Map

### Create
| File | Responsibility |
|---|---|
| `src/Core/PredelNews.Core/Models/AdSlot.cs` | Domain model with `IsDirectActive` computed property |
| `src/Core/PredelNews.Core/Interfaces/IAdSlotRepository.cs` | Repository interface |
| `src/Core/PredelNews.Core/Services/IAdSlotService.cs` | Service interface |
| `src/Core/PredelNews.Core/Services/AdSlotService.cs` | Singleton service with IMemoryCache + IServiceScopeFactory |
| `src/Core/PredelNews.Core/Services/ArticleBodyProcessor.cs` | Pure static class: link rewriting + body split at Nth paragraph |
| `src/Infrastructure/PredelNews.Infrastructure/Repositories/AdSlotRepository.cs` | Dapper implementation |
| `src/Infrastructure/PredelNews.Infrastructure/Migrations/AlterAdSlotsV3Migration.cs` | Seeds missing `article-bottom` row |
| `src/Web/PredelNews.Web/ViewComponents/AdSlotViewComponent.cs` | Async ViewComponent — fetches slot, returns partial |
| `src/Web/PredelNews.Web/Views/Shared/Components/AdSlot/_AdSlot.cshtml` | Ad slot template with Реклама label |
| `src/Web/PredelNews.Web/Helpers/ArticleBodyHelper.cs` | IHtmlHelper extension wrapping ArticleBodyProcessor |
| `src/BackofficeExtensions/PredelNews.BackofficeExtensions/Controllers/AdManagementApiController.cs` | REST API for ad slot management |
| `src/Core/PredelNews.Core.Tests/Services/AdSlotServiceTests.cs` | Unit tests for AdSlotService |
| `src/Core/PredelNews.Core.Tests/Services/ArticleBodyProcessorTests.cs` | Unit tests for ArticleBodyProcessor |

### Modify
| File | Change |
|---|---|
| `src/Core/PredelNews.Core/PredelNews.Core.csproj` | Add HtmlAgilityPack NuGet reference |
| `src/Core/PredelNews.Core/Extensions/ServiceCollectionExtensions.cs` | Register `IAdSlotService` → `AdSlotService` as `AddSingleton` |
| `src/Infrastructure/PredelNews.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Register `IAdSlotRepository` → `AdSlotRepository` as `AddScoped` |
| `src/Infrastructure/PredelNews.Infrastructure/Migrations/PredelNewsMigrationPlan.cs` | Add V3 migration step |
| `src/Web/PredelNews.Web/Views/Shared/_Layout.cshtml` | AdSense script, footer banner slot, mobile sticky slot |
| `src/Web/PredelNews.Web/Views/HomePage.cshtml` | Replace static ad divs with ViewComponent calls |
| `src/Web/PredelNews.Web/Views/Article.cshtml` | Replace static ad divs; use body helper; split for inline ad |
| `src/Web/PredelNews.Web/Views/Shared/_SponsoredBanner.cshtml` | Display sponsor name from ViewData |
| `src/Web/PredelNews.Web/wwwroot/css/site.css` | Replace `::before` label with `.pn-ad-label`, responsive, CLS min-heights |

---

## Task 1: Domain Model + HtmlAgilityPack

**Files:**
- Modify: `src/Core/PredelNews.Core/PredelNews.Core.csproj`
- Create: `src/Core/PredelNews.Core/Models/AdSlot.cs`
- Create: `src/Core/PredelNews.Core/Interfaces/IAdSlotRepository.cs`

- [ ] **Step 1: Add HtmlAgilityPack to Core**

Edit `src/Core/PredelNews.Core/PredelNews.Core.csproj` — add inside the existing `<ItemGroup>` with `Umbraco.Cms.Core`:
```xml
<PackageReference Include="HtmlAgilityPack" Version="1.11.*" />
```

- [ ] **Step 2: Create `AdSlot.cs`**

Create `src/Core/PredelNews.Core/Models/AdSlot.cs`:
```csharp
namespace PredelNews.Core.Models;

public class AdSlot
{
    public int Id { get; set; }
    public string SlotId { get; set; } = string.Empty;
    public string SlotName { get; set; } = string.Empty;
    public string Mode { get; set; } = "adsense";
    public string? AdsenseCode { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? BannerDestUrl { get; set; }
    public string? BannerAltText { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime UpdatedAt { get; set; }

    public bool IsDirectActive =>
        Mode == "direct" &&
        (StartDate == null || StartDate <= DateTime.UtcNow) &&
        (EndDate == null || EndDate > DateTime.UtcNow);
}
```

- [ ] **Step 3: Create `IAdSlotRepository.cs`**

Create `src/Core/PredelNews.Core/Interfaces/IAdSlotRepository.cs`:
```csharp
using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface IAdSlotRepository
{
    Task<IReadOnlyList<AdSlot>> GetAllAsync();
    Task UpdateAsync(AdSlot slot);
}
```

- [ ] **Step 4: Build to verify**
```
dotnet build PredelNews.slnx
```
Expected: Build succeeded with 0 errors.

- [ ] **Step 5: Commit**
```bash
git add src/Core/PredelNews.Core/PredelNews.Core.csproj \
        src/Core/PredelNews.Core/Models/AdSlot.cs \
        src/Core/PredelNews.Core/Interfaces/IAdSlotRepository.cs
git commit -m "feat(epic-09): add AdSlot domain model and IAdSlotRepository"
```

---

## Task 2: `AdSlotRepository` (Infrastructure)

**Files:**
- Create: `src/Infrastructure/PredelNews.Infrastructure/Repositories/AdSlotRepository.cs`
- Modify: `src/Infrastructure/PredelNews.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Create `AdSlotRepository.cs`**

Create `src/Infrastructure/PredelNews.Infrastructure/Repositories/AdSlotRepository.cs`:
```csharp
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;

namespace PredelNews.Infrastructure.Repositories;

public class AdSlotRepository : IAdSlotRepository
{
    private readonly string _connectionString;

    public AdSlotRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("umbracoDbDSN")
            ?? throw new InvalidOperationException("Connection string 'umbracoDbDSN' not found.");
    }

    public async Task<IReadOnlyList<AdSlot>> GetAllAsync()
    {
        const string sql = """
            SELECT id AS Id, slot_id AS SlotId, slot_name AS SlotName, mode AS Mode,
                   adsense_code AS AdsenseCode, banner_image_url AS BannerImageUrl,
                   banner_dest_url AS BannerDestUrl, banner_alt_text AS BannerAltText,
                   start_date AS StartDate, end_date AS EndDate, updated_at AS UpdatedAt
            FROM [pn_ad_slots]
            ORDER BY id
            """;
        await using var connection = new SqlConnection(_connectionString);
        var results = await connection.QueryAsync<AdSlot>(sql);
        return results.ToList().AsReadOnly();
    }

    public async Task UpdateAsync(AdSlot slot)
    {
        const string sql = """
            UPDATE [pn_ad_slots]
            SET mode = @Mode,
                adsense_code = @AdsenseCode,
                banner_image_url = @BannerImageUrl,
                banner_dest_url = @BannerDestUrl,
                banner_alt_text = @BannerAltText,
                start_date = @StartDate,
                end_date = @EndDate,
                updated_at = GETUTCDATE()
            WHERE slot_id = @SlotId
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, slot);
    }
}
```

- [ ] **Step 2: Register `AdSlotRepository`**

Edit `src/Infrastructure/PredelNews.Infrastructure/Extensions/ServiceCollectionExtensions.cs` — add one line:
```csharp
services.AddScoped<IAdSlotRepository, AdSlotRepository>();
```
Place it after the `IPollRepository` line. Also add the using:
```csharp
using PredelNews.Infrastructure.Repositories;
```
(It likely already has this using — check before adding.)

- [ ] **Step 3: Build**
```
dotnet build PredelNews.slnx
```
Expected: 0 errors.

- [ ] **Step 4: Commit**
```bash
git add src/Infrastructure/PredelNews.Infrastructure/Repositories/AdSlotRepository.cs \
        src/Infrastructure/PredelNews.Infrastructure/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(epic-09): implement AdSlotRepository with Dapper"
```

---

## Task 3: V3 Migration — Seed `article-bottom`

**Files:**
- Create: `src/Infrastructure/PredelNews.Infrastructure/Migrations/AlterAdSlotsV3Migration.cs`
- Modify: `src/Infrastructure/PredelNews.Infrastructure/Migrations/PredelNewsMigrationPlan.cs`

- [ ] **Step 1: Create `AlterAdSlotsV3Migration.cs`**

Create `src/Infrastructure/PredelNews.Infrastructure/Migrations/AlterAdSlotsV3Migration.cs`:
```csharp
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace PredelNews.Infrastructure.Migrations;

public class AlterAdSlotsV3Migration : AsyncMigrationBase
{
    public AlterAdSlotsV3Migration(IMigrationContext context) : base(context) { }

    protected override async Task MigrateAsync()
    {
        Logger.LogInformation("Running PredelNews v3 migration — seeding article-bottom ad slot");

        var count = Database.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM [pn_ad_slots] WHERE [slot_id] = 'article-bottom'");

        if (count == 0)
        {
            Database.Execute(
                "INSERT INTO [pn_ad_slots] ([slot_id], [slot_name], [mode], [updated_at]) VALUES (@0, @1, @2, GETUTCDATE())",
                "article-bottom", "Article Bottom (728x90)", "adsense");

            Logger.LogInformation("Seeded article-bottom ad slot");
        }

        await Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Register migration in plan**

Edit `src/Infrastructure/PredelNews.Infrastructure/Migrations/PredelNewsMigrationPlan.cs` — add one line to `DefinePlan()`:
```csharp
To<AlterAdSlotsV3Migration>(new Guid("6A1B2C3D-0003-4000-8000-000000000003"));
```
The file should look like:
```csharp
protected override void DefinePlan()
{
    To<CreateCustomTablesV1Migration>(new Guid("6A1B2C3D-0001-4000-8000-000000000001"));
    To<AlterPollTablesV2Migration>(new Guid("6A1B2C3D-0002-4000-8000-000000000002"));
    To<AlterAdSlotsV3Migration>(new Guid("6A1B2C3D-0003-4000-8000-000000000003"));
}
```

- [ ] **Step 3: Build**
```
dotnet build PredelNews.slnx
```
Expected: 0 errors.

- [ ] **Step 4: Commit**
```bash
git add src/Infrastructure/PredelNews.Infrastructure/Migrations/AlterAdSlotsV3Migration.cs \
        src/Infrastructure/PredelNews.Infrastructure/Migrations/PredelNewsMigrationPlan.cs
git commit -m "feat(epic-09): V3 migration seeds article-bottom ad slot"
```

---

## Task 4: `AdSlotService` — TDD

**Files:**
- Create: `src/Core/PredelNews.Core.Tests/Services/AdSlotServiceTests.cs`
- Create: `src/Core/PredelNews.Core/Services/IAdSlotService.cs`
- Create: `src/Core/PredelNews.Core/Services/AdSlotService.cs`
- Modify: `src/Core/PredelNews.Core/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Write failing tests**

Create `src/Core/PredelNews.Core.Tests/Services/AdSlotServiceTests.cs`:
```csharp
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;
using PredelNews.Core.Services;

namespace PredelNews.Core.Tests.Services;

public class AdSlotServiceTests
{
    private readonly IAdSlotRepository _repo;
    private readonly IMemoryCache _cache;
    private readonly AdSlotService _sut;

    public AdSlotServiceTests()
    {
        _repo = Substitute.For<IAdSlotRepository>();

        // Wire up IServiceScopeFactory to return the mock repository
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IAdSlotRepository)).Returns(_repo);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        _cache = new MemoryCache(new MemoryCacheOptions());
        _sut = new AdSlotService(scopeFactory, _cache);
    }

    [Fact]
    public async Task GetAllAsync_FirstCall_QueriesRepository()
    {
        _repo.GetAllAsync().Returns(new List<AdSlot>().AsReadOnly());

        await _sut.GetAllAsync();

        await _repo.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task GetAllAsync_SecondCall_UsesCacheNotRepository()
    {
        _repo.GetAllAsync().Returns(new List<AdSlot>().AsReadOnly());

        await _sut.GetAllAsync();
        await _sut.GetAllAsync();

        await _repo.Received(1).GetAllAsync();
    }

    [Fact]
    public async Task GetBySlotIdAsync_ReturnsMatchingSlot()
    {
        var slot = new AdSlot { SlotId = "header-leaderboard", Mode = "adsense" };
        _repo.GetAllAsync().Returns(new List<AdSlot> { slot }.AsReadOnly());

        var result = await _sut.GetBySlotIdAsync("header-leaderboard");

        result.Should().NotBeNull();
        result!.SlotId.Should().Be("header-leaderboard");
    }

    [Fact]
    public async Task GetBySlotIdAsync_UnknownSlotId_ReturnsNull()
    {
        _repo.GetAllAsync().Returns(new List<AdSlot>().AsReadOnly());

        var result = await _sut.GetBySlotIdAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSlotAsync_EvictsCache_SoNextCallHitsRepository()
    {
        var slot = new AdSlot { SlotId = "header-leaderboard", Mode = "adsense" };
        _repo.GetAllAsync().Returns(new List<AdSlot> { slot }.AsReadOnly());

        await _sut.GetAllAsync();          // Populates cache — repo called once
        await _sut.UpdateSlotAsync(slot);  // Evicts cache
        await _sut.GetAllAsync();          // Cache miss — repo called again

        await _repo.Received(2).GetAllAsync();
    }

    [Fact]
    public void AdSlot_IsDirectActive_WhenDirectModeAndNoDates_ReturnsTrue()
    {
        var slot = new AdSlot { Mode = "direct" };
        slot.IsDirectActive.Should().BeTrue();
    }

    [Fact]
    public void AdSlot_IsDirectActive_WhenEndDatePassed_ReturnsFalse()
    {
        var slot = new AdSlot { Mode = "direct", EndDate = DateTime.UtcNow.AddDays(-1) };
        slot.IsDirectActive.Should().BeFalse();
    }

    [Fact]
    public void AdSlot_IsDirectActive_WhenStartDateInFuture_ReturnsFalse()
    {
        var slot = new AdSlot { Mode = "direct", StartDate = DateTime.UtcNow.AddDays(1) };
        slot.IsDirectActive.Should().BeFalse();
    }

    [Fact]
    public void AdSlot_IsDirectActive_WhenAdSenseMode_ReturnsFalse()
    {
        var slot = new AdSlot { Mode = "adsense" };
        slot.IsDirectActive.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**
```
dotnet test src/Core/PredelNews.Core.Tests/PredelNews.Core.Tests.csproj --filter "AdSlotService" -v minimal
```
Expected: Build error — `AdSlotService` not found.

- [ ] **Step 3: Create `IAdSlotService.cs`**

Create `src/Core/PredelNews.Core/Services/IAdSlotService.cs`:
```csharp
using PredelNews.Core.Models;

namespace PredelNews.Core.Services;

public interface IAdSlotService
{
    Task<IReadOnlyList<AdSlot>> GetAllAsync();
    Task<AdSlot?> GetBySlotIdAsync(string slotId);
    Task UpdateSlotAsync(AdSlot slot);
}
```

- [ ] **Step 4: Create `AdSlotService.cs`**

Create `src/Core/PredelNews.Core/Services/AdSlotService.cs`:
```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;

namespace PredelNews.Core.Services;

public class AdSlotService : IAdSlotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "AdSlots:All";

    public AdSlotService(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<IReadOnlyList<AdSlot>> GetAllAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAdSlotRepository>();
            return await repo.GetAllAsync();
        }) ?? [];
    }

    public async Task<AdSlot?> GetBySlotIdAsync(string slotId)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(s => s.SlotId == slotId);
    }

    public async Task UpdateSlotAsync(AdSlot slot)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAdSlotRepository>();
        await repo.UpdateAsync(slot);
        _cache.Remove(CacheKey);
    }
}
```

- [ ] **Step 5: Register `AdSlotService` as Singleton**

Edit `src/Core/PredelNews.Core/Extensions/ServiceCollectionExtensions.cs` — add one line:
```csharp
services.AddSingleton<IAdSlotService, AdSlotService>();
```
Place it after `AddSingleton<ISlugGenerator, SlugGenerator>()`. The service is registered as Singleton (not Scoped) because it maintains a cross-request memory cache. The repository dependency is resolved via `IServiceScopeFactory` at call time to avoid the Scoped-inside-Singleton anti-pattern.

- [ ] **Step 6: Run tests — verify they pass**
```
dotnet test src/Core/PredelNews.Core.Tests/PredelNews.Core.Tests.csproj --filter "AdSlotService" -v minimal
```
Expected: All AdSlotService tests pass.

- [ ] **Step 7: Run full test suite**
```
dotnet test PredelNews.slnx
```
Expected: All tests pass.

- [ ] **Step 8: Commit**
```bash
git add src/Core/PredelNews.Core/Services/IAdSlotService.cs \
        src/Core/PredelNews.Core/Services/AdSlotService.cs \
        src/Core/PredelNews.Core/Extensions/ServiceCollectionExtensions.cs \
        src/Core/PredelNews.Core.Tests/Services/AdSlotServiceTests.cs
git commit -m "feat(epic-09): implement AdSlotService with 60s IMemoryCache (TDD)"
```

---

## Task 5: `ArticleBodyProcessor` — TDD

**Files:**
- Create: `src/Core/PredelNews.Core.Tests/Services/ArticleBodyProcessorTests.cs`
- Create: `src/Core/PredelNews.Core/Services/ArticleBodyProcessor.cs`

`ArticleBodyProcessor` is a pure static class. It does two things:
1. `Process(string body, bool isSponsored)` — rewrites external links to add `rel="sponsored noopener"` when `isSponsored = true`
2. `SplitAtParagraph(string body, int afterParagraph)` — splits HTML at the Nth closing `</p>`, returns `(Before, After)` tuple; returns `(body, "")` if fewer paragraphs than requested

- [ ] **Step 1: Write failing tests**

Create `src/Core/PredelNews.Core.Tests/Services/ArticleBodyProcessorTests.cs`:
```csharp
using FluentAssertions;
using PredelNews.Core.Services;

namespace PredelNews.Core.Tests.Services;

public class ArticleBodyProcessorTests
{
    // --- Process / link rewriting ---

    [Fact]
    public void Process_WhenSponsored_AddsRelToExternalLinks()
    {
        var html = "<p>See <a href=\"https://external.com\">this</a></p>";

        var result = ArticleBodyProcessor.Process(html, isSponsored: true);

        result.Should().Contain("rel=\"sponsored noopener\"");
    }

    [Fact]
    public void Process_WhenSponsored_SkipsInternalLinks()
    {
        var html = "<p>See <a href=\"https://predelnews.com/page\">internal</a></p>";

        var result = ArticleBodyProcessor.Process(html, isSponsored: true);

        result.Should().NotContain("sponsored");
    }

    [Fact]
    public void Process_WhenSponsored_SkipsRelativeLinks()
    {
        var html = "<p>See <a href=\"/page\">relative</a></p>";

        var result = ArticleBodyProcessor.Process(html, isSponsored: true);

        result.Should().NotContain("sponsored");
    }

    [Fact]
    public void Process_WhenNotSponsored_DoesNotModifyLinks()
    {
        var html = "<p>See <a href=\"https://external.com\">this</a></p>";

        var result = ArticleBodyProcessor.Process(html, isSponsored: false);

        result.Should().NotContain("sponsored");
        result.Should().Contain("href=\"https://external.com\"");
    }

    [Fact]
    public void Process_EmptyBody_ReturnsEmpty()
    {
        var result = ArticleBodyProcessor.Process(string.Empty, isSponsored: true);
        result.Should().BeEmpty();
    }

    // --- SplitAtParagraph ---

    [Fact]
    public void SplitAtParagraph_WhenEnoughParagraphs_SplitsAfterNth()
    {
        var html = "<p>One</p><p>Two</p><p>Three</p><p>Four</p>";

        var (before, after) = ArticleBodyProcessor.SplitAtParagraph(html, 3);

        before.Should().Be("<p>One</p><p>Two</p><p>Three</p>");
        after.Should().Be("<p>Four</p>");
    }

    [Fact]
    public void SplitAtParagraph_WhenFewerThanRequestedParagraphs_ReturnsFull()
    {
        var html = "<p>One</p><p>Two</p>";

        var (before, after) = ArticleBodyProcessor.SplitAtParagraph(html, 3);

        before.Should().Be("<p>One</p><p>Two</p>");
        after.Should().BeEmpty();
    }

    [Fact]
    public void SplitAtParagraph_WhenExactlyNParagraphs_AfterIsEmpty()
    {
        var html = "<p>One</p><p>Two</p><p>Three</p>";

        var (before, after) = ArticleBodyProcessor.SplitAtParagraph(html, 3);

        before.Should().Be("<p>One</p><p>Two</p><p>Three</p>");
        after.Should().BeEmpty();
    }

    [Fact]
    public void SplitAtParagraph_EmptyBody_ReturnsBothEmpty()
    {
        var (before, after) = ArticleBodyProcessor.SplitAtParagraph(string.Empty, 3);

        before.Should().BeEmpty();
        after.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**
```
dotnet test src/Core/PredelNews.Core.Tests/PredelNews.Core.Tests.csproj --filter "ArticleBodyProcessor" -v minimal
```
Expected: Build error — `ArticleBodyProcessor` not found.

- [ ] **Step 3: Create `ArticleBodyProcessor.cs`**

Create `src/Core/PredelNews.Core/Services/ArticleBodyProcessor.cs`:
```csharp
using HtmlAgilityPack;

namespace PredelNews.Core.Services;

public static class ArticleBodyProcessor
{
    private const string InternalDomain = "predelnews.com";

    /// <summary>
    /// When isSponsored is true, rewrites all external links to include rel="sponsored noopener".
    /// Internal links (predelnews.com) and relative links are not modified.
    /// </summary>
    public static string Process(string body, bool isSponsored)
    {
        if (string.IsNullOrEmpty(body)) return body;
        if (!isSponsored) return body;

        var doc = new HtmlDocument();
        doc.LoadHtml(body);

        foreach (var node in doc.DocumentNode.SelectNodes("//a[@href]") ?? [])
        {
            var href = node.GetAttributeValue("href", string.Empty);
            if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;
            if (href.Contains(InternalDomain, StringComparison.OrdinalIgnoreCase)) continue;

            node.SetAttributeValue("rel", "sponsored noopener");
        }

        return doc.DocumentNode.OuterHtml;
    }

    /// <summary>
    /// Splits HTML at the closing tag of the Nth paragraph.
    /// Returns (fullBody, "") if there are fewer than afterParagraph paragraphs.
    /// </summary>
    public static (string Before, string After) SplitAtParagraph(string body, int afterParagraph)
    {
        if (string.IsNullOrEmpty(body)) return (string.Empty, string.Empty);

        const string closeTag = "</p>";
        int found = 0;
        int searchFrom = 0;

        while (found < afterParagraph)
        {
            int idx = body.IndexOf(closeTag, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return (body, string.Empty);

            found++;
            searchFrom = idx + closeTag.Length;
        }

        return (body[..searchFrom], body[searchFrom..]);
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**
```
dotnet test src/Core/PredelNews.Core.Tests/PredelNews.Core.Tests.csproj --filter "ArticleBodyProcessor" -v minimal
```
Expected: All ArticleBodyProcessor tests pass.

- [ ] **Step 5: Run full suite**
```
dotnet test PredelNews.slnx
```
Expected: All tests pass.

- [ ] **Step 6: Commit**
```bash
git add src/Core/PredelNews.Core/Services/ArticleBodyProcessor.cs \
        src/Core/PredelNews.Core.Tests/Services/ArticleBodyProcessorTests.cs
git commit -m "feat(epic-09): implement ArticleBodyProcessor for link rewriting + body split (TDD)"
```

---

## Task 6: `AdSlotViewComponent` + Partial + `ArticleBodyHelper`

**Files:**
- Create: `src/Web/PredelNews.Web/ViewComponents/AdSlotViewComponent.cs`
- Create: `src/Web/PredelNews.Web/Views/Shared/Components/AdSlot/_AdSlot.cshtml`
- Create: `src/Web/PredelNews.Web/Helpers/ArticleBodyHelper.cs`

- [ ] **Step 1: Create `AdSlotViewComponent.cs`**

Create `src/Web/PredelNews.Web/ViewComponents/AdSlotViewComponent.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Models;
using PredelNews.Core.Services;

namespace PredelNews.Web.ViewComponents;

public class AdSlotViewComponent : ViewComponent
{
    private readonly IAdSlotService _adSlotService;

    public AdSlotViewComponent(IAdSlotService adSlotService)
    {
        _adSlotService = adSlotService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string slotId)
    {
        var slot = await _adSlotService.GetBySlotIdAsync(slotId)
                   ?? new AdSlot { SlotId = slotId, Mode = "adsense" };
        return View("_AdSlot", slot);
    }
}
```

- [ ] **Step 2: Create `_AdSlot.cshtml`**

Create directory `src/Web/PredelNews.Web/Views/Shared/Components/AdSlot/`.

Create `src/Web/PredelNews.Web/Views/Shared/Components/AdSlot/_AdSlot.cshtml`:
```html
@model PredelNews.Core.Models.AdSlot

<div class="pn-ad-slot pn-ad-@Model.SlotId">
  <span class="pn-ad-label">Реклама</span>
  @if (Model.IsDirectActive)
  {
    <a href="@Model.BannerDestUrl" rel="noopener" target="_blank">
      <img src="@Model.BannerImageUrl"
           alt="@(Model.BannerAltText ?? "Реклама")"
           class="img-fluid w-100">
    </a>
  }
  else if (!string.IsNullOrEmpty(Model.AdsenseCode))
  {
    @Html.Raw(Model.AdsenseCode)
  }
</div>
```

- [ ] **Step 3: Create `ArticleBodyHelper.cs`**

Create `src/Web/PredelNews.Web/Helpers/ArticleBodyHelper.cs`:
```csharp
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using PredelNews.Core.Services;

namespace PredelNews.Web.Helpers;

public static class ArticleBodyHelper
{
    /// <summary>
    /// Processes the article body for display: rewrites sponsored links when isSponsored is true.
    /// Returns IHtmlContent safe for @Html.RenderArticleBody(...) in Razor.
    /// </summary>
    public static IHtmlContent RenderArticleBody(
        this IHtmlHelper html,
        string body,
        bool isSponsored)
    {
        var processed = ArticleBodyProcessor.Process(body, isSponsored);
        return new HtmlString(processed);
    }
}
```

- [ ] **Step 4: Build**
```
dotnet build PredelNews.slnx
```
Expected: 0 errors.

- [ ] **Step 5: Commit**
```bash
git add src/Web/PredelNews.Web/ViewComponents/AdSlotViewComponent.cs \
        src/Web/PredelNews.Web/Views/Shared/Components/AdSlot/_AdSlot.cshtml \
        src/Web/PredelNews.Web/Helpers/ArticleBodyHelper.cs
git commit -m "feat(epic-09): add AdSlotViewComponent, _AdSlot partial, ArticleBodyHelper"
```

---

## Task 7: CSS Updates

**Files:**
- Modify: `src/Web/PredelNews.Web/wwwroot/css/site.css`

The ad slot section currently (lines ~187–201) has:
- `::before` pseudo-element that emits "Реклама" — **remove this**, replaced by `.pn-ad-label` in the template
- Old height classes (`pn-ad-leaderboard`, `pn-ad-sidebar`, etc.) using CSS names that don't match slot IDs — **replace** with classes matching the `pn-ad-{slotId}` pattern from `_AdSlot.cshtml`

- [ ] **Step 1: Replace the ad slot CSS block**

In `site.css`, find and replace the entire `/* -- AD SLOTS -- */` block (currently lines 187–201):

Old:
```css
/* -- AD SLOTS -- */
.pn-ad-slot {
  background: var(--pn-bg-surface); border: 1px dashed var(--pn-border);
  border-radius: .25rem; display: flex; align-items: center;
  justify-content: center; flex-direction: column; gap: .25rem;
  color: var(--pn-text-light); font-size: .75rem;
}
.pn-ad-slot::before {
  content: "Реклама"; text-transform: uppercase;
  letter-spacing: 1px; font-size: .625rem;
}
.pn-ad-leaderboard { height: 90px; }
.pn-ad-sidebar { height: 250px; }
.pn-ad-article-mid { height: 250px; }
.pn-ad-article-bottom { height: 90px; }
```

New:
```css
/* -- AD SLOTS -- */
.pn-ad-slot {
  background: var(--pn-bg-surface); border: 1px dashed var(--pn-border);
  border-radius: .25rem; display: flex; align-items: center;
  justify-content: center; flex-direction: column; gap: .25rem;
  color: var(--pn-text-light); font-size: .75rem; overflow: hidden;
}
.pn-ad-label {
  text-transform: uppercase; letter-spacing: 1px;
  font-size: .625rem; font-weight: 600; color: var(--pn-text-light);
}

/* Slot-specific min-heights (CLS prevention) */
.pn-ad-header-leaderboard { min-height: 90px; }
.pn-ad-sidebar-top { min-height: 250px; }
.pn-ad-sidebar-bottom { min-height: 250px; }
.pn-ad-article-inline { min-height: 90px; }
.pn-ad-article-bottom { min-height: 90px; }
.pn-ad-footer-banner { min-height: 90px; }

/* Mobile sticky — fixed bottom bar, mobile-only */
.pn-ad-mobile-sticky {
  position: fixed; bottom: 0; left: 0; right: 0;
  min-height: 50px; z-index: 1050; background: #fff;
  border-top: 1px solid var(--pn-border);
}

/* Responsive: hide sidebar slots on mobile */
@media (max-width: 1023px) {
  .pn-ad-sidebar-top,
  .pn-ad-sidebar-bottom { display: none; }
}

/* Mobile sticky: hide on desktop */
@media (min-width: 1024px) {
  .pn-ad-mobile-sticky { display: none; }
}
```

- [ ] **Step 2: Build**
```
dotnet build PredelNews.slnx
```

- [ ] **Step 3: Commit**
```bash
git add src/Web/PredelNews.Web/wwwroot/css/site.css
git commit -m "feat(epic-09): update ad slot CSS — slot-ID classes, pn-ad-label, responsive, mobile-sticky"
```

---

## Task 8: `_Layout.cshtml` — AdSense Script + Global Slots

**Files:**
- Modify: `src/Web/PredelNews.Web/Views/Shared/_Layout.cshtml`

Three changes to `_Layout.cshtml`:
1. **`<head>`**: inject AdSense script from SiteSettings
2. **Above `</footer>`**: footer banner slot
3. **Bottom of `<body>` (before `</body>`)**: mobile sticky slot

- [ ] **Step 1: Add `@inject` and AdSense script to `_Layout.cshtml`**

At the very top of `_Layout.cshtml` (before `<!DOCTYPE html>`), add:
```razor
@inject PredelNews.Core.Services.ISiteSettingsService SiteSettingsService
@{
    var _adsenseTag = SiteSettingsService.GetSiteSettings().AdsenseScriptTag;
}
```

Inside `<head>`, after `@await Html.PartialAsync("_GoogleAnalytics")`, add:
```razor
@if (!string.IsNullOrEmpty(_adsenseTag))
{
  @Html.Raw(_adsenseTag)
}
```

- [ ] **Step 2: Add footer banner slot**

In `_Layout.cshtml`, find the `<footer class="pn-footer ...">` opening tag. Add the footer banner **immediately before** the `<footer` tag:
```razor
<!-- Ad: Footer Banner -->
<div class="container my-3">
  @await Component.InvokeAsync("AdSlot", new { slotId = "footer-banner" })
</div>
```

- [ ] **Step 3: Add mobile sticky slot**

Find the closing `</body>` tag. Add immediately before it:
```razor
<!-- Ad: Mobile Sticky -->
@await Component.InvokeAsync("AdSlot", new { slotId = "mobile-sticky" })
```

- [ ] **Step 4: Build**
```
dotnet build PredelNews.slnx
```
Expected: 0 errors.

- [ ] **Step 5: Commit**
```bash
git add src/Web/PredelNews.Web/Views/Shared/_Layout.cshtml
git commit -m "feat(epic-09): add AdSense script + footer-banner + mobile-sticky to layout"
```

---

## Task 9: `HomePage.cshtml` — Wire Up Ad Slots

**Files:**
- Modify: `src/Web/PredelNews.Web/Views/HomePage.cshtml`

Replace the two static ad slot divs with live ViewComponent calls.

- [ ] **Step 1: Replace leaderboard div**

Find:
```html
<!-- Ad: Header Leaderboard -->
<div class="container my-3">
  <div class="pn-ad-slot pn-ad-leaderboard"><span>728 &times; 90</span></div>
</div>
```

Replace with:
```razor
<!-- Ad: Header Leaderboard -->
<div class="container my-3">
  @await Component.InvokeAsync("AdSlot", new { slotId = "header-leaderboard" })
</div>
```

- [ ] **Step 2: Replace sidebar div**

Find:
```html
<div class="pn-ad-slot pn-ad-sidebar mb-4"><span>300 &times; 250</span></div>
```

Replace with:
```razor
@await Component.InvokeAsync("AdSlot", new { slotId = "sidebar-top" })
<div class="mb-4"></div>
@await Component.InvokeAsync("AdSlot", new { slotId = "sidebar-bottom" })
<div class="mb-4"></div>
```

- [ ] **Step 3: Build**
```
dotnet build PredelNews.slnx
```

- [ ] **Step 4: Commit**
```bash
git add src/Web/PredelNews.Web/Views/HomePage.cshtml
git commit -m "feat(epic-09): wire ad slots in HomePage via AdSlotViewComponent"
```

---

## Task 10: `Article.cshtml` + `_SponsoredBanner.cshtml`

**Files:**
- Modify: `src/Web/PredelNews.Web/Views/Article.cshtml`
- Modify: `src/Web/PredelNews.Web/Views/Shared/_SponsoredBanner.cshtml`

- [ ] **Step 1: Add `@using` for `ArticleBodyHelper`**

Add to `src/Web/PredelNews.Web/Views/_ViewImports.cshtml` (this namespace is not yet present):
```razor
@using PredelNews.Web.Helpers
```

- [ ] **Step 2: Replace leaderboard in `Article.cshtml`**

Find:
```html
<!-- Ad: Header Leaderboard -->
<div class="container my-3">
  <div class="pn-ad-slot pn-ad-leaderboard"><span>728 &times; 90</span></div>
</div>
```

Replace with:
```razor
<!-- Ad: Header Leaderboard -->
<div class="container my-3">
  @await Component.InvokeAsync("AdSlot", new { slotId = "header-leaderboard" })
</div>
```

- [ ] **Step 3: Replace article body + inject inline ad**

Find:
```razor
<div class="pn-article-body mt-4">
  @Html.Raw(Model.Body)
</div>
```

Replace with:
```razor
@{
    var (bodyBefore, bodyAfter) = PredelNews.Core.Services.ArticleBodyProcessor.SplitAtParagraph(Model.Body, 3);
}
<div class="pn-article-body mt-4">
  @Html.RenderArticleBody(bodyBefore, Model.IsSponsored)
  @if (!string.IsNullOrEmpty(bodyAfter))
  {
    <div class="my-3">
      @await Component.InvokeAsync("AdSlot", new { slotId = "article-inline" })
    </div>
    @Html.RenderArticleBody(bodyAfter, Model.IsSponsored)
  }
</div>
```

- [ ] **Step 4: Replace article-bottom slot**

Find:
```html
<div class="pn-ad-slot pn-ad-article-bottom my-4"><span>728 &times; 90</span></div>
```

Replace with:
```razor
<div class="my-4">
  @await Component.InvokeAsync("AdSlot", new { slotId = "article-bottom" })
</div>
```

- [ ] **Step 5: Replace sidebar slot + add second sidebar**

Find:
```html
<div class="pn-ad-slot pn-ad-sidebar mb-4"><span>300 &times; 250</span></div>
```

Replace with:
```razor
@await Component.InvokeAsync("AdSlot", new { slotId = "sidebar-top" })
<div class="mb-4"></div>
@await Component.InvokeAsync("AdSlot", new { slotId = "sidebar-bottom" })
<div class="mb-4"></div>
```

- [ ] **Step 6: Update `_SponsoredBanner.cshtml` to show sponsor name**

Open `src/Web/PredelNews.Web/Views/Shared/_SponsoredBanner.cshtml`.

Current content:
```html
<div class="pn-sponsored-banner d-flex align-items-center gap-2">
    <i class="bi bi-info-circle"></i> Платена публикация
</div>
```

Replace with:
```html
@{
    var sponsorName = ViewData["SponsorName"] as string;
}
<div class="pn-sponsored-banner d-flex align-items-center gap-2">
    <i class="bi bi-info-circle"></i>
    <span>Платена публикация@(!string.IsNullOrEmpty(sponsorName) ? $" от {sponsorName}" : "")</span>
</div>
```

- [ ] **Step 7: Pass SponsorName when calling `_SponsoredBanner` in `Article.cshtml`**

Find all calls to `Html.PartialAsync("_SponsoredBanner")` in `Article.cshtml`. There are two (top and bottom). Replace both with:
```razor
@await Html.PartialAsync("_SponsoredBanner", null, new ViewDataDictionary(ViewData) { { "SponsorName", Model.SponsorName } })
```

- [ ] **Step 8: Build**
```
dotnet build PredelNews.slnx
```
Expected: 0 errors.

- [ ] **Step 9: Run all tests**
```
dotnet test PredelNews.slnx
```
Expected: All tests pass.

- [ ] **Step 10: Commit**
```bash
git add src/Web/PredelNews.Web/Views/Article.cshtml \
        src/Web/PredelNews.Web/Views/Shared/_SponsoredBanner.cshtml \
        src/Web/PredelNews.Web/Views/_ViewImports.cshtml
git commit -m "feat(epic-09): wire ad slots in Article.cshtml, inline ad split, sponsor name in banner"
```

---

## Task 11: `AdManagementApiController`

**Files:**
- Create: `src/BackofficeExtensions/PredelNews.BackofficeExtensions/Controllers/AdManagementApiController.cs`

This follows the exact pattern of `EngagementApiController`. Read that file first to confirm pattern before implementing.

- [ ] **Step 1: Create `AdManagementApiController.cs`**

Create `src/BackofficeExtensions/PredelNews.BackofficeExtensions/Controllers/AdManagementApiController.cs`:
```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Models;
using PredelNews.Core.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace PredelNews.BackofficeExtensions.Controllers;

public record UpdateAdSlotRequest(
    string Mode,
    string? AdsenseCode,
    string? BannerImageUrl,
    string? BannerDestUrl,
    string? BannerAltText,
    DateTime? StartDate,
    DateTime? EndDate
);

[VersionedApiBackOfficeRoute("ads")]
[ApiExplorerSettings(GroupName = "Ads")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
public class AdManagementApiController : ManagementApiControllerBase
{
    private readonly IAdSlotService _adSlotService;
    private readonly IUserService _userService;

    public AdManagementApiController(IAdSlotService adSlotService, IUserService userService)
    {
        _adSlotService = adSlotService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSlots()
    {
        var slots = await _adSlotService.GetAllAsync();
        return Ok(slots);
    }

    [HttpGet("{slotId}")]
    public async Task<IActionResult> GetSlot(string slotId)
    {
        var slot = await _adSlotService.GetBySlotIdAsync(slotId);
        if (slot == null) return NotFound();
        return Ok(slot);
    }

    [HttpPut("{slotId}")]
    public async Task<IActionResult> UpdateSlot(string slotId, [FromBody] UpdateAdSlotRequest request)
    {
        if (!await IsAdminAsync()) return Forbid();

        // Validate
        if (request.Mode == "direct")
        {
            if (string.IsNullOrWhiteSpace(request.BannerImageUrl))
                return BadRequest(new { status = "error", message = "Изображението на банера е задължително в директен режим." });
            if (string.IsNullOrWhiteSpace(request.BannerDestUrl))
                return BadRequest(new { status = "error", message = "URL адресът на банера е задължителен в директен режим." });
        }
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate >= request.EndDate)
            return BadRequest(new { status = "error", message = "Началната дата трябва да е преди крайната." });

        var slot = await _adSlotService.GetBySlotIdAsync(slotId);
        if (slot == null) return NotFound();

        slot.Mode = request.Mode;
        slot.AdsenseCode = request.AdsenseCode;
        slot.BannerImageUrl = request.BannerImageUrl;
        slot.BannerDestUrl = request.BannerDestUrl;
        slot.BannerAltText = request.BannerAltText;
        slot.StartDate = request.StartDate;
        slot.EndDate = request.EndDate;

        await _adSlotService.UpdateSlotAsync(slot);
        return Ok(new { status = "updated" });
    }

    [HttpPost("{slotId}/reset")]
    public async Task<IActionResult> ResetToAdSense(string slotId)
    {
        if (!await IsAdminAsync()) return Forbid();

        var slot = await _adSlotService.GetBySlotIdAsync(slotId);
        if (slot == null) return NotFound();

        slot.Mode = "adsense";
        slot.BannerImageUrl = null;
        slot.BannerDestUrl = null;
        slot.BannerAltText = null;
        slot.StartDate = null;
        slot.EndDate = null;

        await _adSlotService.UpdateSlotAsync(slot);
        return Ok(new { status = "reset" });
    }

    private async Task<bool> IsAdminAsync()
    {
        var userKeyStr = User.FindFirst(Umbraco.Cms.Core.Constants.Security.OpenIdDictSubClaimType)?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userKeyStr == null || !Guid.TryParse(userKeyStr, out var userKey))
            return false;

        var user = await _userService.GetAsync(userKey);
        var groups = user?.Groups.Select(g => g.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        return groups.Contains("admin", StringComparer.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Build**
```
dotnet build PredelNews.slnx
```
Expected: 0 errors.

- [ ] **Step 3: Run all tests**
```
dotnet test PredelNews.slnx
```
Expected: All tests pass.

- [ ] **Step 4: Commit**
```bash
git add src/BackofficeExtensions/PredelNews.BackofficeExtensions/Controllers/AdManagementApiController.cs
git commit -m "feat(epic-09): implement AdManagementApiController for CMS ad slot management"
```

---

## Task 12: Final Verification

- [ ] **Step 1: Full build**
```
dotnet build PredelNews.slnx
```
Expected: 0 errors, 0 warnings (or only pre-existing warnings).

- [ ] **Step 2: Full test run**
```
dotnet test PredelNews.slnx -v minimal
```
Expected: All tests pass. Note new tests: `AdSlotServiceTests` (8 tests), `ArticleBodyProcessorTests` (8 tests).

- [ ] **Step 3: Manual smoke test checklist**

Start the site with `dotnet run --project src/Web/PredelNews.Web/PredelNews.Web.csproj` and verify:
- [ ] Homepage loads with no errors; ad slot containers appear where static divs were
- [ ] Article page loads; "Реклама" label appears in each ad slot (from `<span class="pn-ad-label">`)
- [ ] A sponsored article shows "Платена публикация от [SponsorName]" at top and bottom of the article body
- [ ] On a sponsored article, external links in the body have `rel="sponsored noopener"` in page source
- [ ] Resize to mobile (<1024px) — sidebar ad slots are hidden, mobile sticky appears
- [ ] Umbraco backoffice → `/umbraco/swagger` → "Ads" group has 4 endpoints (GET all, GET by slotId, PUT, POST reset)
- [ ] DB check: `SELECT slot_id FROM pn_ad_slots` returns 7 rows including `article-bottom`

- [ ] **Step 4: Final commit (if any fixups needed)**
```bash
git add -p   # stage only relevant changes
git commit -m "fix(epic-09): address smoke test findings"
```

---

## Acceptance Criteria Traceability

| EPIC AC | Covered by |
|---|---|
| All 6 ad slots render in correct positions | Tasks 8, 9, 10 (ViewComponent calls in Layout, HomePage, Article) |
| Admin can switch slot to direct-sold; banner displays; reverts after end date | Tasks 4 (IsDirectActive), 11 (controller), 6 (_AdSlot.cshtml) |
| All ad slots display "Реклама" label | Task 6 (`<span class="pn-ad-label">` in _AdSlot.cshtml) |
| Sponsored articles show "Платена публикация" at top and bottom | Task 10 (_SponsoredBanner with SponsorName) |
| External links in sponsored articles have rel="sponsored noopener" | Task 5 (ArticleBodyProcessor.Process) |
| Sidebar slots hidden on mobile | Task 7 (CSS @media) |
