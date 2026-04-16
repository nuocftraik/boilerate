using Boilerate.Application.Identity.Roles;
using Boilerate.Infrastructure.Auth.Permissions;
using Boilerate.Shared.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace Boilerate.Host.Controllers.Identity;

/// <summary>
/// Role Controller - Role and Function management APIs
/// Endpoints: Role CRUD, Permission management, Function CRUD
/// </summary>
public class RoleController : BaseApiController
{
    private readonly IRoleService _roleService;
    private readonly IFunctionService _functionService;

    public RoleController(
        IRoleService roleService,
        IFunctionService functionService)
    {
        _roleService = roleService;
        _functionService = functionService;
    }

    #region Role Management

    /// <summary>
    /// Lấy danh sách tất cả roles
    /// Requires: Roles.View permission
    /// </summary>
    [HttpGet]
    [MustHavePermission(AppAction.View, AppFunction.Role)]
    [OpenApiOperation("Get a list of all roles.", "")]
    public Task<List<RoleDto>> GetListAsync(CancellationToken cancellationToken)
    {
        return _roleService.GetListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy chi tiết role theo ID
    /// Requires: Roles.View permission
    /// </summary>
    [HttpGet("{id}")]
    [MustHavePermission(AppAction.View, AppFunction.Role)]
    [OpenApiOperation("Get role details.", "")]
    public Task<RoleDto> GetByIdAsync(string id)
    {
        return _roleService.GetByIdAsync(id);
    }

    /// <summary>
    /// Lấy role với danh sách permissions
    /// Requires: Roles.View permission
    /// </summary>
    [HttpGet("{id}/permissions")]
    [MustHavePermission(AppAction.View, AppFunction.Role)]
    [OpenApiOperation("Get role details with its permissions.", "")]
    public Task<List<FunctionDto>> GetByIdWithPermissionsAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return _roleService.GetByIdWithPermissionsAsync(id, cancellationToken);
    }

    /// <summary>
    /// Cập nhật permissions cho role
    /// Requires: Roles.Update permission
    /// </summary>
    [HttpPut("{id}/permissions")]
    [MustHavePermission(AppAction.Update, AppFunction.Role)]
    [OpenApiOperation("Update a role's permissions.", "")]
    public async Task<ActionResult> UpdatePermissionsAsync(
        string id,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        // Validate ID match
        if (id != request.RoleId)
        {
            return BadRequest("ID mismatch");
        }

        var result = await _roleService.UpdatePermissionsAsync(request, cancellationToken);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Tạo hoặc cập nhật role
    /// Requires: Roles.Create hoặc Roles.Update permission
    /// </summary>
    [HttpPost("create/update")]
    [MustHavePermission(AppAction.Create, AppFunction.Role)]
    [OpenApiOperation("Create or update a role.", "")]
    public async Task<ActionResult> RegisterRoleAsync(CreateOrUpdateRoleRequest request)
    {
        var result = await _roleService.CreateOrUpdateAsync(request);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Xóa role
    /// Requires: Roles.Delete permission
    /// </summary>
    [HttpDelete("{id}")]
    [MustHavePermission(AppAction.Delete, AppFunction.Role)]
    [OpenApiOperation("Delete a role.", "")]
    public async Task<ActionResult> DeleteAsync(string id)
    {
        var result = await _roleService.DeleteAsync(id);
        return Ok(new { message = result });
    }

    #endregion

    #region Function Management

    /// <summary>
    /// Lấy danh sách tất cả functions
    /// Requires: Functions.View permission
    /// </summary>
    [HttpGet("functions")]
    [MustHavePermission(AppAction.View, AppFunction.Role)]
    [OpenApiOperation("Get a list of all functions.", "")]
    public Task<List<FunctionDto>> GetFunctionListAsync(
 CancellationToken cancellationToken)
    {
        return _functionService.GetListAsync(cancellationToken);
    }

    /// <summary>
    /// Lấy chi tiết function theo ID
    /// Requires: Functions.View permission
    /// </summary>
    [HttpGet("function/{id}")]
    [MustHavePermission(AppAction.View, AppFunction.Role)]
    [OpenApiOperation("Get function details.", "")]
    public Task<FunctionDto> GetFunctionByIdAsync(Guid id)
    {
        return _functionService.GetByIdAsync(id);
    }

    /// <summary>
    /// Tạo hoặc cập nhật function
    /// Requires: Functions.Create permission
    /// </summary>
    [HttpPost("function/create/update")]
    [MustHavePermission(AppAction.Create, AppFunction.Role)]
    [OpenApiOperation("Create or update a function.", "")]
    public async Task<ActionResult> CreateUpdateFunctionAsync(
        CreateOrUpdateFunctionRequest request)
    {
        var result = await _functionService.CreateOrUpdateAsync(request);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Xóa function
    /// Requires: Functions.Delete permission
    /// </summary>
    [HttpDelete("function/{id}")]
    [MustHavePermission(AppAction.Delete, AppFunction.Role)]
    [OpenApiOperation("Delete a function.", "")]
    public async Task<ActionResult> DeleteFunctionAsync(Guid id)
    {
        var result = await _functionService.DeleteAsync(id);
        return Ok(new { message = result });
    }

    #endregion
}
