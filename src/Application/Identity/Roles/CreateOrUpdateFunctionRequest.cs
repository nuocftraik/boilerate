namespace Boilerate.Application.Identity.Roles;

/// <summary>
/// Request để tạo hoặc update function
/// </summary>
public class CreateOrUpdateFunctionRequest
{
    /// <summary>
    /// Function ID (null or Guid.Empty = create, not empty = update)
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Function name (required, unique)
    /// Examples: "Users", "Products", "Orders"
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// List of Action IDs to assign to this function
    /// Example: [ViewActionId, CreateActionId, UpdateActionId]
    /// </summary>
    public List<Guid>? ActionIds { get; set; }
}
