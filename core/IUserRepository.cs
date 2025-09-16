namespace MyApp.Core;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User>  AddAsync(User user);
    Task UpdateEmailAsync(Guid id, string newEmail);
    Task DeleteAsync(Guid id);
}
