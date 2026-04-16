using Boilerate.Application.Common.Exceptions;
using Boilerate.Application.Identity.Roles;
using Boilerate.Domain.Identity;
using Boilerate.Infrastructure.Persistence.Context;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Boilerate.Infrastructure.Identity;

/// <summary>
/// Service xử lý function management operations
/// </summary>
public class FunctionService : IFunctionService
{
    private readonly ApplicationDbContext _db;

    public FunctionService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Get list tất cả functions với actions
    /// </summary>
    public async Task<List<FunctionDto>> GetListAsync(CancellationToken cancellationToken)
    {
        var functions = await _db.Functions
            .Include(f => f.ActionInFunctions)
            .ThenInclude(aif => aif.Action)
            .ToListAsync(cancellationToken);

        return functions.Adapt<List<FunctionDto>>();
    }

    /// <summary>
    /// Get function details by ID với actions
    /// </summary>
    public async Task<FunctionDto> GetByIdAsync(Guid id)
    {
        var function = await _db.Functions
              .Include(f => f.ActionInFunctions)
              .ThenInclude(aif => aif.Action)
              .FirstOrDefaultAsync(f => f.Id == id);

        if (function == null)
        {
            throw new NotFoundException("Function not found");
        }

        return function.Adapt<FunctionDto>();
    }

    /// <summary>
    /// Create hoặc update function với action assignments
    /// </summary>
    public async Task<string> CreateOrUpdateAsync(CreateOrUpdateFunctionRequest request)
    {
        if (request.Id == null || request.Id == Guid.Empty)
        {
            // Create new function
            var function = new Function { Name = request.Name };

            // Add actions to function
            if (request.ActionIds != null)
            {
                foreach (var actionId in request.ActionIds)
                {
                    function.AddAction(actionId);
                }
            }

            _db.Functions.Add(function);
            await _db.SaveChangesAsync();

            return function.Id.ToString();
        }
        else
        {
            // Update existing function
            var function = await _db.Functions
                .Include(f => f.ActionInFunctions)
                .FirstOrDefaultAsync(f => f.Id == request.Id);

            if (function == null)
            {
                throw new NotFoundException("Function not found");
            }

            // Update name
            function.Name = request.Name;

            // Update actions (replace all)
            function.UpdateActions(request.ActionIds);

            await _db.SaveChangesAsync();

            return function.Id.ToString();
        }
    }

    /// <summary>
    /// Delete function
    /// Cannot delete functions being used in Permission table
    /// </summary>
    public async Task<string> DeleteAsync(Guid id)
    {
        var function = await _db.Functions.FirstOrDefaultAsync(f => f.Id == id);

        if (function == null)
        {
            throw new NotFoundException("Function not found");
        }

        // Check if function is being used in Permission table
        var isUsedInPermissions = await _db.Permissions
            .AnyAsync(p => p.FunctionId == id);

        if (isUsedInPermissions)
        {
            throw new ConflictException(
                $"Cannot delete function '{function.Name}' as it is being used in permissions.");
        }

        _db.Functions.Remove(function);
        await _db.SaveChangesAsync();

        return id.ToString();
    }
}
