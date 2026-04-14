using Microsoft.AspNetCore.Identity;

namespace Boilerate.Domain.Identity;

public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }
    
    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
        NormalizedName = roleName.ToUpperInvariant();
    }

    public string? Description { get; set; }
}
