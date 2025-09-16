namespace MyApp.Core;

public sealed class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Email { get; private set; }

    public User(string email) => Email = email;

    public void ChangeEmail(string newEmail) => Email = newEmail;
}
