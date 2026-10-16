using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using SmartCareer.Web.Services.Email;

namespace SmartCareer.Web.Tests;

public sealed class SmtpEmailServiceTests
{
    [Fact]
    public async Task SendHtmlAsyncBuildsEncodedHtmlMessageWithoutNetworkAccess()
    {
        var transport = new RecordingTransport();
        var service = CreateService(transport);

        await service.SendHtmlAsync(
            "student@example.test",
            "Career update — résumé ready",
            "<h1>Your résumé is ready</h1>");

        var message = Assert.IsType<MimeMessage>(transport.Message);
        Assert.Equal("Career update — résumé ready", message.Subject);
        Assert.Equal("SmartCareer Team", message.From.Mailboxes.Single().Name);
        Assert.Equal("sender@example.test", message.From.Mailboxes.Single().Address);
        Assert.Equal("student@example.test", message.To.Mailboxes.Single().Address);
        Assert.Equal("<h1>Your résumé is ready</h1>", message.HtmlBody);
        Assert.Equal(587, transport.Options!.Port);
    }

    [Fact]
    public async Task SendHtmlAsyncRejectsInvalidRecipientWithoutUsingTransport()
    {
        var transport = new RecordingTransport();
        var service = CreateService(transport);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SendHtmlAsync("not an address", "Subject", "<p>Body</p>"));

        Assert.Null(transport.Message);
    }

    [Fact]
    public async Task SendHtmlAsyncWrapsTransportFailureInSafeException()
    {
        var transport = new RecordingTransport
        {
            Exception = new InvalidOperationException("Simulated transport failure")
        };
        var service = CreateService(transport);

        var exception = await Assert.ThrowsAsync<EmailDeliveryException>(() =>
            service.SendHtmlAsync("student@example.test", "Private subject", "<p>Private body</p>"));

        Assert.Equal("The email could not be delivered.", exception.Message);
        Assert.DoesNotContain("Private subject", exception.Message);
        Assert.DoesNotContain("Private body", exception.Message);
        Assert.DoesNotContain("test-app-password", exception.Message);
    }

    private static SmtpEmailService CreateService(ISmtpTransport transport)
    {
        var options = Options.Create(new SmtpOptions
        {
            Host = "smtp.example.test",
            Port = 587,
            SenderName = "SmartCareer Team",
            SenderEmail = "sender@example.test",
            Username = "sender@example.test",
            Password = "test-app-password"
        });

        return new SmtpEmailService(
            options,
            transport,
            NullLogger<SmtpEmailService>.Instance);
    }

    private sealed class RecordingTransport : ISmtpTransport
    {
        public MimeMessage? Message { get; private set; }

        public SmtpOptions? Options { get; private set; }

        public Exception? Exception { get; init; }

        public Task SendAsync(
            MimeMessage message,
            SmtpOptions options,
            CancellationToken cancellationToken)
        {
            Message = message;
            Options = options;

            return Exception is null
                ? Task.CompletedTask
                : Task.FromException(Exception);
        }
    }
}
