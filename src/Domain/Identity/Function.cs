using Boilerate.Domain.Common.Contracts;

namespace Boilerate.Domain.Identity;

/// <summary>
/// Function entity (represents a module/feature)
/// Examples: Users, Products, Orders, Categories
/// </summary>
public class Function : BaseEntity
{
    /// <summary>
    /// Function name (e.g., "Users", "Products")
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Actions assigned to this function (many-to-many relationship)
    /// </summary>
    public virtual List<ActionInFunction> ActionInFunctions { get; set; } = new();

    public Function()
    {
    }

    /// <summary>
    /// Add an action to this function
    /// </summary>
    public void AddAction(Guid actionId)
    {
        ActionInFunctions.Add(new ActionInFunction(actionId, Id));
    }

    /// <summary>
    /// Update actions for this function (replace all)
    /// </summary>
    public void UpdateActions(List<Guid>? newActionIds)
    {
        if (newActionIds == null || newActionIds.Count == 0)
        {
            ActionInFunctions.Clear();
            return;
        }

        // Remove actions not in new list
        ActionInFunctions.RemoveAll(aif => !newActionIds.Contains(aif.ActionId));

        // Add new actions not yet in function
        var existingActionIds = ActionInFunctions.Select(aif => aif.ActionId).ToHashSet();
        foreach (var actionId in newActionIds)
        {
            if (!existingActionIds.Contains(actionId))
            {
                ActionInFunctions.Add(new ActionInFunction(actionId, Id));
            }
        }
    }
}
