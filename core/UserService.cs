using System.Text.RegularExpressions;

namespace MyApp.Core;

public sealed class UserService
{
    private readonly IUserRepository _repo;
    private readonly IEmailSender _email;

    public UserService(IUserRepository repo, IEmailSender email)
    {
        _repo  = repo;
        _email = email;
    }

    // Simple rule to keep tests straightforward
    public static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) &&
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    public async Task<User> RegisterAsync(string email)
    {
        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email.", nameof(email));

        var created = await _repo.AddAsync(new User(email));
        await _email.SendWelcomeAsync(created.Email);
        return created;
    }

    public Task<User?> GetAsync(Guid id) => _repo.GetByIdAsync(id);

    public async Task ChangeEmailAsync(Guid id, string newEmail)
    {
        if (!IsValidEmail(newEmail))
            throw new ArgumentException("Invalid email.", nameof(newEmail));

        var user = await _repo.GetByIdAsync(id) ??
                   throw new InvalidOperationException("User not found.");

        var old = user.Email;
        user.ChangeEmail(newEmail);

        await _repo.UpdateEmailAsync(id, newEmail);
        await _email.SendChangedEmailNoticeAsync(newEmail, old);
    }

    public Task DeleteAsync(Guid id) => _repo.DeleteAsync(id);
}
