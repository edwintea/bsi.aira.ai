using System.Collections.Generic;
using System.Threading.Tasks;

namespace ai.services.api.Interfaces
{
    public interface IAuditLogServices
    {
        Task CreateAuditLogAsync(string user, string action, string status, string details);
        Task DeleteAuditLogAsync(string partitionKey, string rowKey);
        IEnumerable<AuditLogEntity> ReadAllAuditLogs(string partitionKey = null);
        Task<AuditLogEntity> ReadAuditLogAsync(string partitionKey, string rowKey);
        Task UpdateAuditLogAsync(string partitionKey, string rowKey, string newDetails);
    }
}