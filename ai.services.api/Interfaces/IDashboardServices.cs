using System;
using System.Collections.Generic;

namespace ai.services.api.Interfaces
{
    public interface IDashboardServices
    {
        Dictionary<DateTime, int> GetErrorsByDateRange(DateTime startDate, DateTime endDate);
        int GetErrorsPerDay(DateTimeOffset date);
        List<KeyValuePair<string, int>> GetMostActiveUsers();
        List<KeyValuePair<string, int>> GetMostActiveUsersToday(DateTimeOffset date);
        List<KeyValuePair<string, int>> GetMostPageAccess();
        List<KeyValuePair<string, int>> GetMostPageAccessToday(DateTimeOffset date);
        int GetTotalErrors();
        List<AuditLogEntity> GetTotalErrorsList();
        List<AuditLogEntity> GetTotalErrorsListToday(DateTimeOffset date);
        Dictionary<DateTime, int> GetUniqueUserByDateRange(DateTime startDate, DateTime endDate);
        int GetUniqueUsersPerDay(DateTimeOffset date);
        int GetUniqueUsersTotal();
    }
}