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

        await _sut.GetAllAsync();
        await _sut.UpdateSlotAsync(slot);
        await _sut.GetAllAsync();

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
