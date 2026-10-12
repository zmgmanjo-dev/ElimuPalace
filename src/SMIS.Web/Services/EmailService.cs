using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SMIS.Web.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailWithAttachmentAsync(
        string toEmail, string toName, string subject,
        string bodyHtml, byte[] attachmentBytes, string attachmentFileName)
    {
        var settings = _config.GetSection("EmailSettings");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            settings["SenderName"], settings["SenderEmail"]));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = bodyHtml };
        builder.Attachments.Add(attachmentFileName, attachmentBytes,
            new ContentType("application", "pdf"));
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(
            settings["SmtpHost"],
            int.Parse(settings["SmtpPort"] ?? "587"),
            SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(
            settings["Username"], settings["Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}