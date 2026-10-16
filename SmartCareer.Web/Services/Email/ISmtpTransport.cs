using MimeKit;

namespace SmartCareer.Web.Services.Email;

public interface ISmtpTransport
{
    Task SendAsync(MimeMessage message, SmtpOptions options, CancellationToken cancellationToken);
}
