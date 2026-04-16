using Boilerate.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Boilerate.Infrastructure.Auth.Permissions;

/// <summary>
/// Thuộc tính MustHavePermission (authorization khai báo)
/// Cách dùng: [MustHavePermission(AppAction.View, AppFunction.User)]
/// Tạo policy: "Permissions.User.View"
/// </summary>
public class MustHavePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Constructor với tham số action và function
    /// </summary>
    /// <param name="action">Action (VD: AppAction.View)</param>
    /// <param name="function">Function (VD: AppFunction.User)</param>
    public MustHavePermissionAttribute(string action, string function)
    {
        // Tạo tên policy: "Permissions.{Function}.{Action}"
        Policy = AppPermission.NameFor(action, function);
    }
}
