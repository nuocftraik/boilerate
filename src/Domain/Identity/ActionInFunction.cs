namespace Boilerate.Domain.Identity;

/// <summary>
/// Bảng giao (junction) giữa Action và Function.
/// Mỗi Function có thể chứa nhiều Actions (VD: Users module có View, Create, Update, Delete).
/// Sử dụng composite key (ActionId, FunctionId).
/// </summary>
public class ActionInFunction
{
    public Guid ActionId { get; set; }
    public Guid FunctionId { get; set; }

    public virtual Action Action { get; set; } = default!;
    public virtual Function Function { get; set; } = default!;

    public ActionInFunction() { }

    public ActionInFunction(Guid actionId, Guid functionId)
    {
        ActionId = actionId;
        FunctionId = functionId;
    }
}
