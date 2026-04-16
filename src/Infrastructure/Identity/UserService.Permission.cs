namespace Boilerate.Infrastructure.Identity;

internal partial class UserService
{
    public Task<List<string>> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
    {
        // To be implemented in Step 20C/D
        return Task.FromResult(new List<string>());
    }

    public Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
    {
        // To be implemented in Step 20C/D
        return Task.FromResult(false);
    }
}
