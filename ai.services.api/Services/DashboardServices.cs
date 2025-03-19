using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using ai.services.api.Interfaces;

namespace ai.services.api.Services
{
    public class DashboardServices : IDashboardServices
    {
        private readonly TableClient _tableClient;

        public DashboardServices(string connectionString, string tableName = "Logs")
        {
            // Initialize the TableServiceClient and TableClient
            var tableServiceClient = new TableServiceClient(connectionString);
            _tableClient = tableServiceClient.GetTableClient(tableName);
        }

        public int GetUniqueUsersPerDay(DateTimeOffset date)
        {
            var logs = _tableClient.Query<AuditLogEntity>(log => log.PerformedAt >= date && log.PerformedAt < date.AddDays(1));
            var uniqueUsers = logs.Select(log => log.User).Distinct().Count();
            return uniqueUsers;
        }

        public int GetUniqueUsersTotal()
        {
            var logs = _tableClient.Query<AuditLogEntity>();
            var uniqueUsers = logs.Select(log => log.User).Distinct().Count();
            return uniqueUsers;
        }

        public List<KeyValuePair<string, int>> GetMostActiveUsers()
        {
            var logs = _tableClient.Query<AuditLogEntity>();
            var userActions = logs.GroupBy(log => log.User)
                                  .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                                  .OrderByDescending(user => user.Value)
                                  .ToList();
            return userActions;
        }

        public List<KeyValuePair<string, int>> GetMostActiveUsersToday(DateTimeOffset date)
        {
            var logs = _tableClient.Query<AuditLogEntity>(log => log.PerformedAt >= date && log.PerformedAt < date.AddDays(1));
            var userActions = logs.GroupBy(log => log.User)
                                  .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                                  .OrderByDescending(user => user.Value)
                                  .ToList();
            return userActions;
        }

        public List<KeyValuePair<string, int>> GetMostPageAccess()
        {
            var logs = _tableClient.Query<AuditLogEntity>();
            var pageActions = logs.GroupBy(log => log.PartitionKey)
                                  .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                                  .OrderByDescending(user => user.Value)
                                  .ToList();
            return pageActions;
        }

        public List<KeyValuePair<string, int>> GetMostPageAccessToday(DateTimeOffset date)
        {
            var logs = _tableClient.Query<AuditLogEntity>(log => log.PerformedAt >= date && log.PerformedAt < date.AddDays(1));
            var userActions = logs.GroupBy(log => log.PartitionKey)
                                  .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                                  .OrderByDescending(user => user.Value)
                                  .ToList();
            return userActions;
        }

        public int GetTotalErrors()
        {
            var logs = _tableClient.Query<AuditLogEntity>(log => log.Status == "Error");
            return logs.Count();
        }

        public int GetErrorsPerDay(DateTimeOffset date)
        {
            var logs = _tableClient.Query<AuditLogEntity>(log => log.Status == "Error" && log.PerformedAt >= date && log.PerformedAt < date.AddDays(1));
            return logs.Count();
        }

        public List<AuditLogEntity> GetTotalErrorsList()
        {
            var logs = _tableClient.Query<AuditLogEntity>(log => log.Status == "Error");
            var logsList = logs.OrderByDescending(time => time.Timestamp)
                                  .ToList();
            return logsList;
        }

        public List<AuditLogEntity> GetTotalErrorsListToday(DateTimeOffset date)
        {
            var logs = _tableClient.Query<AuditLogEntity>(log => log.Status == "Error" && log.PerformedAt >= date && log.PerformedAt < date.AddDays(1));
            var logsList = logs.OrderByDescending(time => time.Timestamp)
                                  .ToList();
            return logsList;
        }

        public Dictionary<DateTime, int> GetErrorsByDateRange(DateTime startDate, DateTime endDate)
        {
            var logs = _tableClient.Query<AuditLogEntity>(
                log => log.Status == "Error" && log.PerformedAt >= startDate && log.PerformedAt < endDate.AddDays(1)).OrderByDescending(time => time.Timestamp);

            var errorsPerDay = logs
                .GroupBy(log => log.PerformedAt.Date)
                .ToDictionary(group => group.Key, group => group.Count());

            return errorsPerDay;
        }

        public Dictionary<DateTime, int> GetUniqueUserByDateRange(DateTime startDate, DateTime endDate)
        {
            var logs = _tableClient.Query<AuditLogEntity>(
                log => log.PerformedAt >= startDate && log.PerformedAt < endDate.AddDays(1)).OrderByDescending(time => time.Timestamp);

            var uniqueUsersPerDay = logs
                .GroupBy(log => log.PerformedAt.Date)
                .ToDictionary(
                    group => group.Key, // Group by day
                    group => group.Select(log => log.User).Distinct().Count()); // Count distinct users per day

            return uniqueUsersPerDay;
        }
    }
}