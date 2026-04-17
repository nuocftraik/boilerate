using Ardalis.Specification;
using Boilerate.Domain.Auditing;
using System;

namespace Boilerate.Application.Auditing;

public class AuditLogsExportSpec : Specification<Trail>
{
    public AuditLogsExportSpec(ExportAuditLogsRequest request)
    {
        Query.OrderByDescending(x => x.DateTime);

        if (request.StartDate.HasValue)
        {
            Query.Where(x => x.DateTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            Query.Where(x => x.DateTime <= request.EndDate.Value);
        }

        if (!string.IsNullOrEmpty(request.UserId) && Guid.TryParse(request.UserId, out var userIdGuid))
        {
            Query.Where(x => x.UserId == userIdGuid);
        }

        if (!string.IsNullOrEmpty(request.TableName))
        {
            Query.Where(x => x.TableName == request.TableName);
        }

        // Limit to 10,000 records for export
        Query.Take(10000);
    }
}
