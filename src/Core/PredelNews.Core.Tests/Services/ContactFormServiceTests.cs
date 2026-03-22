using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Services;

namespace PredelNews.Core.Tests.Services;

public class ContactFormServiceTests
{
    private readonly IContactFormRepository _repository = Substitute.For<IContactFormRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly ISiteSettingsService _siteSettings = Substitute.For<ISiteSettingsService>();
    private readonly ILogger<ContactFormService> _logger = Substitute.For<ILogger<ContactFormService>>();
    private readonly ContactFormService _sut;

    public ContactFormServiceTests()
    {
        _siteSettings.GetSiteSettings().Returns(new PredelNews.Core.ViewModels.SiteSettingsViewModel
        {
            ContactRecipientEmail = "admin@predelnews.com"
        });
        _emailService.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _sut = new ContactFormService(_repository, _emailService, _siteSettings, _logger);
    }

    [Fact]
    public async Task SubmitAsync_ValidInput_InsertsToDbAndSendsEmail()
    {
        var (success, message) = await _sut.SubmitAsync("\u0418\u0432\u0430\u043d", "ivan@test.com", "\u0412\u044a\u043f\u0440\u043e\u0441", "\u0417\u0434\u0440\u0430\u0432\u0435\u0439\u0442\u0435", "127.0.0.1");

        success.Should().BeTrue();
        message.Should().Be("\u0421\u044a\u043e\u0431\u0449\u0435\u043d\u0438\u0435\u0442\u043e \u0432\u0438 \u0431\u0435\u0448\u0435 \u0438\u0437\u043f\u0440\u0430\u0442\u0435\u043d\u043e \u0443\u0441\u043f\u0435\u0448\u043d\u043e.");
        await _repository.Received(1).InsertAsync("\u0418\u0432\u0430\u043d", "ivan@test.com", "\u0412\u044a\u043f\u0440\u043e\u0441", "\u0417\u0434\u0440\u0430\u0432\u0435\u0439\u0442\u0435", "127.0.0.1");
        await _emailService.Received(1).SendAsync("admin@predelnews.com", Arg.Any<string>(), Arg.Any<string>());
    }

    [Theory]
    [InlineData("", "ivan@test.com", "\u0422\u0435\u043c\u0430", "\u0422\u0435\u043a\u0441\u0442", "\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0418\u043c\u0435\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.")]
    [InlineData("\u0418\u0432\u0430\u043d", "", "\u0422\u0435\u043c\u0430", "\u0422\u0435\u043a\u0441\u0442", "\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0418\u043c\u0435\u0439\u043b\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.")]
    [InlineData("\u0418\u0432\u0430\u043d", "not-an-email", "\u0422\u0435\u043c\u0430", "\u0422\u0435\u043a\u0441\u0442", "\u041d\u0435\u0432\u0430\u043b\u0438\u0434\u0435\u043d \u0438\u043c\u0435\u0439\u043b \u0430\u0434\u0440\u0435\u0441.")]
    [InlineData("\u0418\u0432\u0430\u043d", "ivan@test.com", "", "\u0422\u0435\u043a\u0441\u0442", "\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0422\u0435\u043c\u0430\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.")]
    [InlineData("\u0418\u0432\u0430\u043d", "ivan@test.com", "\u0422\u0435\u043c\u0430", "", "\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0421\u044a\u043e\u0431\u0449\u0435\u043d\u0438\u0435\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.")]
    public async Task SubmitAsync_InvalidInput_ReturnsValidationError(string name, string email, string subject, string msg, string expectedError)
    {
        var (success, message) = await _sut.SubmitAsync(name, email, subject, msg, "127.0.0.1");

        success.Should().BeFalse();
        message.Should().Be(expectedError);
        await _repository.DidNotReceive().InsertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task SubmitAsync_SmtpFails_StillReturnsSuccessAndInsertsToDb()
    {
        _emailService.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var (success, message) = await _sut.SubmitAsync("\u0418\u0432\u0430\u043d", "ivan@test.com", "\u0412\u044a\u043f\u0440\u043e\u0441", "\u0417\u0434\u0440\u0430\u0432\u0435\u0439\u0442\u0435", "127.0.0.1");

        success.Should().BeTrue();
        message.Should().Be("\u0421\u044a\u043e\u0431\u0449\u0435\u043d\u0438\u0435\u0442\u043e \u0432\u0438 \u0431\u0435\u0448\u0435 \u0438\u0437\u043f\u0440\u0430\u0442\u0435\u043d\u043e \u0443\u0441\u043f\u0435\u0448\u043d\u043e.");
        await _repository.Received(1).InsertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task SubmitAsync_SmtpThrows_StillReturnsSuccessAndInsertsToDb()
    {
        _emailService.SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).ThrowsAsync(new Exception("SMTP down"));

        var (success, message) = await _sut.SubmitAsync("\u0418\u0432\u0430\u043d", "ivan@test.com", "\u0412\u044a\u043f\u0440\u043e\u0441", "\u0417\u0434\u0440\u0430\u0432\u0435\u0439\u0442\u0435", "127.0.0.1");

        success.Should().BeTrue();
        message.Should().Be("\u0421\u044a\u043e\u0431\u0449\u0435\u043d\u0438\u0435\u0442\u043e \u0432\u0438 \u0431\u0435\u0448\u0435 \u0438\u0437\u043f\u0440\u0430\u0442\u0435\u043d\u043e \u0443\u0441\u043f\u0435\u0448\u043d\u043e.");
        await _repository.Received(1).InsertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task SubmitAsync_NoRecipientConfigured_StillInsertsToDb()
    {
        _siteSettings.GetSiteSettings().Returns(new PredelNews.Core.ViewModels.SiteSettingsViewModel
        {
            ContactRecipientEmail = null
        });
        var sut = new ContactFormService(_repository, _emailService, _siteSettings, _logger);

        var (success, message) = await sut.SubmitAsync("\u0418\u0432\u0430\u043d", "ivan@test.com", "\u0412\u044a\u043f\u0440\u043e\u0441", "\u0417\u0434\u0440\u0430\u0432\u0435\u0439\u0442\u0435", "127.0.0.1");

        success.Should().BeTrue();
        await _repository.Received(1).InsertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>());
        await _emailService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
