using Azure.Data.Tables;
using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ai.services.api.Interfaces;

namespace ai.services.api.Services
{
    public class AuditLogServices : IAuditLogServices
    {
        private readonly TableClient _tableClient;

        public AuditLogServices(string connectionString, string tableName = "Logs")
        {
            // Initialize the TableServiceClient and TableClient
            var tableServiceClient = new TableServiceClient(connectionString);
            _tableClient = tableServiceClient.GetTableClient(tableName);

            // Ensure the table exists
            //_tableClient.CreateIfNotExists();
        }

        // Create (Insert) Audit Log
        public async Task CreateAuditLogAsync(string user, string action, string status, string details)
        {
            string partitionKey = action; // Use the action as the partition key
            string rowKey = Guid.NewGuid().ToString(); // Use a GUID as the row key for uniqueness

            var auditLogEntity = new AuditLogEntity(partitionKey, rowKey, user, action, status, details);

            try
            {
                await _tableClient.AddEntityAsync(auditLogEntity);
                Console.WriteLine("Audit log created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating audit log: {ex.Message}");
            }
        }

        // Read (Retrieve) Audit Log
        public async Task<AuditLogEntity> ReadAuditLogAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<AuditLogEntity>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Console.WriteLine("Audit log not found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading audit log: {ex.Message}");
                return null;
            }
        }

        // Update Audit Log
        public async Task UpdateAuditLogAsync(string partitionKey, string rowKey, string newDetails)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<AuditLogEntity>(partitionKey, rowKey);
                var auditLogEntity = response.Value;

                auditLogEntity.Details = newDetails;

                await _tableClient.UpdateEntityAsync(auditLogEntity, auditLogEntity.ETag);
                Console.WriteLine("Audit log updated successfully.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Console.WriteLine("Audit log not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating audit log: {ex.Message}");
            }
        }

        // Delete Audit Log
        public async Task DeleteAuditLogAsync(string partitionKey, string rowKey)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
                Console.WriteLine("Audit log deleted successfully.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Console.WriteLine("Audit log not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting audit log: {ex.Message}");
            }
        }

        // Read All (Retrieve All Logs)
        public IEnumerable<AuditLogEntity> ReadAllAuditLogs(string partitionKey = null)
        {
            try
            {
                Pageable<AuditLogEntity> query;

                if (!string.IsNullOrEmpty(partitionKey))
                {
                    query = _tableClient.Query<AuditLogEntity>(e => e.PartitionKey == partitionKey);
                }
                else
                {
                    query = _tableClient.Query<AuditLogEntity>();
                }

                return query.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving audit logs: {ex.Message}");
                return Enumerable.Empty<AuditLogEntity>();
            }
        }
    }
}