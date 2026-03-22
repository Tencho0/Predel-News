using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Services;

namespace PredelNews.Core.Tests.Services;

public class EmailSignupServiceTests
{
    private readonly IEmailSignupRepository _repository = Substitute.For<IEmailSignupRepository>();
    private readonly ILogger<EmailSignupService> _logger = Substitute.For<ILogger<EmailSignupService>>();
    private readonly EmailSignupService _sut;

    public EmailSignupServiceTests()
    {
        _repository.InsertIfNotExistsAsync(Arg.Any<string>()).Returns(true);
        _sut = new EmailSignupService(_repository, _logger);
    }

    [Fact]
    public async Task SignupAsync_ValidEmailWithConsent_ReturnsSuccess()
    {
        var (success, message) = await _sut.SignupAsync("test@example.com", true);

        success.Should().BeTrue();
        message.Should().Be("\u0411\u043b\u0430\u0433\u043e\u0434\u0430\u0440\u0438\u043c! \u0418\u043c\u0435\u0439\u043b\u044a\u0442 \u0432\u0438 \u0431\u0435\u0448\u0435 \u0437\u0430\u043f\u0438\u0441\u0430\u043d.");
        await _repository.Received(1).InsertIfNotExistsAsync("test@example.com");
    }

    [Fact]
    public async Task SignupAsync_InvalidEmail_ReturnsError()
    {
        var (success, message) = await _sut.SignupAsync("not-an-email", true);

        success.Should().BeFalse();
        message.Should().Be("\u041d\u0435\u0432\u0430\u043b\u0438\u0434\u0435\u043d \u0438\u043c\u0435\u0439\u043b \u0430\u0434\u0440\u0435\u0441.");
        await _repository.DidNotReceive().InsertIfNotExistsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SignupAsync_EmptyEmail_ReturnsError()
    {
        var (success, message) = await _sut.SignupAsync("", true);

        success.Should().BeFalse();
        message.Should().Be("\u041f\u043e\u043b\u0435\u0442\u043e \u201e\u0418\u043c\u0435\u0439\u043b\u201c \u0435 \u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e.");
    }

    [Fact]
    public async Task SignupAsync_ConsentNotChecked_ReturnsError()
    {
        var (success, message) = await _sut.SignupAsync("test@example.com", false);

        success.Should().BeFalse();
        message.Should().Be("\u041c\u043e\u043b\u044f, \u043e\u0442\u0431\u0435\u043b\u0435\u0436\u0435\u0442\u0435 \u0441\u044a\u0433\u043b\u0430\u0441\u0438\u0435\u0442\u043e \u0437\u0430 \u043f\u043e\u043b\u0443\u0447\u0430\u0432\u0430\u043d\u0435 \u043d\u0430 \u0438\u043c\u0435\u0439\u043b\u0438.");
        await _repository.DidNotReceive().InsertIfNotExistsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SignupAsync_DuplicateEmail_StillReturnsSuccess()
    {
        _repository.InsertIfNotExistsAsync(Arg.Any<string>()).Returns(false);

        var (success, message) = await _sut.SignupAsync("existing@example.com", true);

        success.Should().BeTrue();
        message.Should().Be("\u0411\u043b\u0430\u0433\u043e\u0434\u0430\u0440\u0438\u043c! \u0418\u043c\u0435\u0439\u043b\u044a\u0442 \u0432\u0438 \u0431\u0435\u0448\u0435 \u0437\u0430\u043f\u0438\u0441\u0430\u043d.");
    }
}
