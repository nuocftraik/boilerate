using Boilerate.Application.Common.Interfaces;

namespace Boilerate.Application.Identity.Roles;

/// <summary>
/// Service xử lý function management operations
/// </summary>
public interface IFunctionService : ITransientService
{
    /// <summary>
    /// Get list tất cả functions với actions
    /// </summary>
    Task<List<FunctionDto>> GetListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get function details by ID với actions
    /// </summary>
    Task<FunctionDto> GetByIdAsync(Guid id);

    /// <summary>
    /// Create hoặc update function với action assignments
    /// Returns function ID
    /// </summary>
    Task<string> CreateOrUpdateAsync(CreateOrUpdateFunctionRequest request);

    /// <summary>
    /// Delete function
    /// Cannot delete functions being used in Permission table
    /// </summary>
    Task<string> DeleteAsync(Guid id);
}
