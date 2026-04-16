using Boilerate.Application.Identity.Roles;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace Boilerate.Host.Controllers.Identity;

/// <summary>
/// Role management APIs
/// </summary>
public class RoleController : BaseApiController
{
    private readonly IRoleService _roleService;
    private readonly IFunctionService _functionService;

    public RoleController(IRoleService roleService, IFunctionService functionService)
    {
        _roleService = roleService;
        _functionService = functionService;
    }

    /// <summary>
    /// Get list of all roles
    /// </summary>
    [HttpGet]
    [OpenApiOperation("Get a list of all roles.", "")]
    public Task<List<RoleDto>> GetListAsync(CancellationToken cancellationToken)
    {
        return _roleService.GetListAsync(cancellationToken);
    }

    /// <summary>
    /// Get role details by ID
    /// </summary>
    [HttpGet("{id}")]
    [OpenApiOperation("Get role details.", "")]
    public Task<RoleDto> GetByIdAsync(string id)
    {
        return _roleService.GetByIdAsync(id);
    }

    /// <summary>
    /// Get role details với permissions (for permission UI)
    /// Returns list of Functions with Actions marked as Selected or not
    /// </summary>
    [HttpGet("{id}/permissions")]
    [OpenApiOperation("Get role details with its permissions.", "")]
    public Task<List<FunctionDto>> GetByIdWithPermissionsAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return _roleService.GetByIdWithPermissionsAsync(id, cancellationToken);
    }

    /// <summary>
    /// Update role's permissions (table-based approach)
    /// </summary>
    [HttpPut("{id}/permissions")]
    [OpenApiOperation("Update a role's permissions.", "")]
    public async Task<ActionResult> UpdatePermissionsAsync(
        string id,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.RoleId)
        {
            return BadRequest();
        }

        var result = await _roleService.UpdatePermissionsAsync(request, cancellationToken);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Create hoặc update role
    /// </summary>
    [HttpPost("create/update")]
    [OpenApiOperation("Create or update a role.", "")]
    public async Task<ActionResult> RegisterRoleAsync(CreateOrUpdateRoleRequest request)
    {
        var result = await _roleService.CreateOrUpdateAsync(request);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("{id}")]
    [OpenApiOperation("Delete a role.", "")]
    public async Task<ActionResult> DeleteAsync(string id)
    {
        var result = await _roleService.DeleteAsync(id);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Get list of all functions (for permission UI)
    /// </summary>
    [HttpGet("functions")]
    [OpenApiOperation("Get a list of all functions.", "")]
    public Task<List<FunctionDto>> GetFunctionListAsync(CancellationToken cancellationToken)
    {
        return _functionService.GetListAsync(cancellationToken);
    }

    /// <summary>
    /// Get function details by ID
    /// </summary>
    [HttpGet("function/{id}")]
    [OpenApiOperation("Get function details.", "")]
    public Task<FunctionDto> GetFunctionByIdAsync(Guid id)
    {
        return _functionService.GetByIdAsync(id);
    }

    /// <summary>
    /// Create hoặc update function
    /// </summary>
    [HttpPost("function/create/update")]
    [OpenApiOperation("Create or update a function.", "")]
    public async Task<ActionResult> CreateUpdateFunctionAsync(CreateOrUpdateFunctionRequest request)
    {
        var result = await _functionService.CreateOrUpdateAsync(request);
        return Ok(new { message = result });
    }

    /// <summary>
    /// Delete function
    /// </summary>
    [HttpDelete("function/{id}")]
    [OpenApiOperation("Delete a function.", "")]
    public async Task<ActionResult> DeleteFunctionAsync(Guid id)
    {
        var result = await _functionService.DeleteAsync(id);
        return Ok(new { message = result });
    }
}
