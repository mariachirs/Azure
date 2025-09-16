namespace MyApp.Core;

public interface IEmailSender
{
    Task SendWelcomeAsync(string to);
    Task SendChangedEmailNoticeAsync(string to, string oldEmail);
}
