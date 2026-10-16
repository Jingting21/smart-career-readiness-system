using MailKit;
using Microsoft.Extensions.Options;
using MimeKit;

namespace SmartCareer.Web.Services.Email;

public sealed class SmtpEmailService(
    IOptions<SmtpOptions> options,
    ISmtpTransport transport,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendHtmlAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(htmlBody);

        MimeMessage message;
        try
        {
            message = new MimeMessage
            {
                Subject = subject,
                Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody()
            };
            message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
            message.To.Add(MailboxAddress.Parse(recipientEmail));
        }
        catch (Exception exception) when (exception is FormatException or ParseException)
        {
            throw new ArgumentException("The recipient or configured sender email address is invalid.", nameof(recipientEmail), exception);
        }

        try
        {
            await transport.SendAsync(message, _options, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                "SMTP email delivery failed ({ExceptionType}).",
                exception.GetType().Name);
            throw new EmailDeliveryException("The email could not be delivered.", exception);
        }
    }
}
