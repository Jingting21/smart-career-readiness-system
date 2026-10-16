namespace SmartCareer.Web.Services.Email;

public interface IEmailService
{
    Task SendHtmlAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
