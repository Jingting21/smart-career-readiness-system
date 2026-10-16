using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SmartCareer.Web.Services.Email;

public sealed class MailKitSmtpTransport : ISmtpTransport
{
    public async Task SendAsync(
        MimeMessage message,
        SmtpOptions options,
        CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                options.Host,
                options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);
            await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken, progress: null);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(quit: true, cancellationToken);
            }
        }
    }
}
