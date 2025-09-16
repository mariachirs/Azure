using NSubstitute.ExceptionExtensions; // for .ThrowsAsync()

namespace MyApp.Tests;

public class UserServiceTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly IEmailSender _email   = Substitute.For<IEmailSender>();

    private UserService CreateSut() => new(_repo, _email);

    [Fact]
    public async Task RegisterAsync_creates_user_and_sends_welcome()
    {
        // Arrange
        var inputEmail = "alice@example.com";
        _repo.AddAsync(Arg.Any<User>())
             .Returns(ci => {
                 var u = (User)ci[0]!;
                 return Task.FromResult(u);
             });

        var sut = CreateSut();

        // Act
        var created = await sut.RegisterAsync(inputEmail);

        // Assert
        Assert.Equal(inputEmail, created.Email);

        await _repo.Received(1).AddAsync(Arg.Is<User>(u => u.Email == inputEmail));
        await _email.Received(1).SendWelcomeAsync(inputEmail);
        await _email.DidNotReceive().SendChangedEmailNoticeAsync(default!, default!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("foo")]
    [InlineData("bar@")]
    public async Task RegisterAsync_invalid_email_throws(string badEmail)
    {
        var sut = CreateSut();
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.RegisterAsync(badEmail));
        Assert.Equal("email", ex.ParamName);
        await _repo.DidNotReceive().AddAsync(Arg.Any<User>());
        await _email.DidNotReceive().SendWelcomeAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetAsync_returns_null_when_not_found()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id).Returns((User?)null);
        var sut = CreateSut();

        var result = await sut.GetAsync(id);

        Assert.Null(result);
    }

    [Fact]
    public async Task ChangeEmailAsync_updates_repo_and_sends_notice_in_order()
    {
        var id = Guid.NewGuid();
        var oldEmail = "old@ex.com";
        var newEmail = "new@ex.com";

        _repo.GetByIdAsync(id).Returns(new User(oldEmail));

        var sut = CreateSut();

        await sut.ChangeEmailAsync(id, newEmail);

        await _repo.Received(1).UpdateEmailAsync(id, newEmail);
        await _email.Received(1).SendChangedEmailNoticeAsync(newEmail, oldEmail);

        Received.InOrder(async () =>
        {
            await _repo.UpdateEmailAsync(id, newEmail);
            await _email.SendChangedEmailNoticeAsync(newEmail, oldEmail);
        });
    }

    [Fact]
    public async Task ChangeEmailAsync_throws_if_user_not_found()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id).Returns((User?)null);
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ChangeEmailAsync(id, "x@y.com"));

        await _repo.DidNotReceive().UpdateEmailAsync(Arg.Any<Guid>(), Arg.Any<string>());
        await _email.DidNotReceive().SendChangedEmailNoticeAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteAsync_calls_repo_delete()
    {
        var id = Guid.NewGuid();
        var sut = CreateSut();

        await sut.DeleteAsync(id);

        await _repo.Received(1).DeleteAsync(id);
    }

    [Fact]
    public async Task RegisterAsync_propagates_sender_errors()
    {
        var email = "bob@example.com";
        _repo.AddAsync(Arg.Any<User>())
             .Returns(ci => (User)ci[0]!);

        _email.SendWelcomeAsync(email).ThrowsAsync(new IOException("SMTP down"));

        var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<IOException>(() => sut.RegisterAsync(email));
        Assert.Equal("SMTP down", ex.Message);
    }
}
