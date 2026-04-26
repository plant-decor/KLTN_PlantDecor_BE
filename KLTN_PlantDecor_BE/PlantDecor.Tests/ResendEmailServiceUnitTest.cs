using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Services;
using Resend;
using System.Net;

namespace PlantDecor.Tests;

public class ResendEmailServiceUnitTest
{
    private static IConfiguration CreateConfig(string fromEmail = "no-reply@test.local", string fromName = "PlantDecor")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resend:FromEmail"] = fromEmail,
                ["Resend:FromName"] = fromName
            })
            .Build();
    }

    [Fact]
    public async Task SendEmailAsync_ShouldSendEmail_WithCorrectFields_Normal()
    {
        var config = CreateConfig(fromEmail: "no-reply@test.local", fromName: "PlantDecor");
        var resend = new Mock<IResend>(MockBehavior.Strict);

        EmailMessage? sentMessage = null;

        resend.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => sentMessage = msg)
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), new ResendRateLimit()));

        var sut = new ResendEmailService(config, resend.Object);

        await sut.SendEmailAsync(new EmailRequest
        {
            To = "user@example.com",
            Subject = "Subject",
            Body = "<b>Hello</b>"
        }, CancellationToken.None);

        sentMessage.Should().NotBeNull();
        sentMessage!.From.ToString().Should().Be("PlantDecor <no-reply@test.local>");
        sentMessage.To.Should().ContainSingle("user@example.com");
        sentMessage.Subject.Should().Be("Subject");
        sentMessage.HtmlBody.Should().Be("<b>Hello</b>");

        resend.Verify(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldPassCancellationToken_Normal()
    {
        var config = CreateConfig();
        var resend = new Mock<IResend>(MockBehavior.Strict);

        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        resend.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), token))
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), new ResendRateLimit()));

        var sut = new ResendEmailService(config, resend.Object);

        await sut.SendEmailAsync(new EmailRequest { To = "a@b.com", Subject = "s", Body = "b" }, token);

        resend.Verify(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), token), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldSend_WhenSubjectEmpty_Normal()
    {
        var config = CreateConfig();
        var resend = new Mock<IResend>(MockBehavior.Strict);

        resend.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), new ResendRateLimit()));

        var sut = new ResendEmailService(config, resend.Object);

        await sut.SendEmailAsync(new EmailRequest { To = "a@b.com", Subject = "", Body = "b" }, CancellationToken.None);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldSend_WhenBodyEmpty_Boundary()
    {
        var config = CreateConfig();
        var resend = new Mock<IResend>(MockBehavior.Strict);

        resend.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), new ResendRateLimit()));

        var sut = new ResendEmailService(config, resend.Object);

        await sut.SendEmailAsync(new EmailRequest { To = "a@b.com", Subject = "s", Body = "" }, CancellationToken.None);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldSend_WhenFromConfigMissing_Boundary()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var resend = new Mock<IResend>(MockBehavior.Strict);

        EmailMessage? sentMessage = null;
        resend.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((msg, _) => sentMessage = msg)
            .ReturnsAsync(new ResendResponse<Guid>(Guid.NewGuid(), new ResendRateLimit()));

        var sut = new ResendEmailService(config, resend.Object);

        await sut.SendEmailAsync(new EmailRequest { To = "a@b.com", Subject = "s", Body = "b" }, CancellationToken.None);

        sentMessage.Should().NotBeNull();
        sentMessage!.From.ToString().Should().Be(" <>");
    }

    [Fact]
    public async Task SendEmailAsync_ShouldThrow_WhenResendReturnsFailure_Abnormal()
    {
        var config = CreateConfig();
        var resend = new Mock<IResend>(MockBehavior.Strict);

        var response = new ResendResponse<Guid>(
            new ResendException(HttpStatusCode.BadRequest, ErrorType.ValidationError, "fail", new ResendRateLimit()),
            new ResendRateLimit());

        resend.Setup(r => r.EmailSendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var sut = new ResendEmailService(config, resend.Object);

        var act = () => sut.SendEmailAsync(new EmailRequest { To = "a@b.com", Subject = "s", Body = "b" }, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("Error sending email via Resend: Failed to send email via Resend: BadRequest");
    }
}

