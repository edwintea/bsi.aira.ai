using Azure.Data.Tables;
using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ai.services.api.Models;

namespace ai.services.api.Services
{
    public class TokenLogServices
    {
        private readonly TableClient _tableClient;

        public TokenLogServices(string connectionString, string tableName = "Tokens")
        {
            // Initialize the TableServiceClient and TableClient
            var tableServiceClient = new TableServiceClient(connectionString);
            _tableClient = tableServiceClient.GetTableClient(tableName);

            // Ensure the table exists
            //_tableClient.CreateIfNotExists();
        }

        // Create (Insert) Token Log
        public async Task CreateTokenLogAsync(string id, string action, int input, int output)
        {
            string partitionKey = action; // Use the action as the partition key
            string rowKey = Guid.NewGuid().ToString(); // Use a GUID as the row key for uniqueness

            var TokenLogEntity = new TokenLogEntity(partitionKey, rowKey, id, input, output);

            try
            {
                await _tableClient.AddEntityAsync(TokenLogEntity);
                Console.WriteLine("Token log created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating Token log: {ex.Message}");
            }
        }

        public async Task UpdateTokenLogAsync(string partitionKey, string rowKey, int newInput, int newOutput)
        {
            try
            {
                // Retrieve the existing entity
                var existingEntity = await _tableClient.GetEntityAsync<TokenLogEntity>(partitionKey, rowKey);

                if (existingEntity != null)
                {
                    // Update the fields
                    existingEntity.Value.Input = newInput;
                    existingEntity.Value.Output = newOutput;

                    // Update the entity in the table
                    await _tableClient.UpdateEntityAsync(existingEntity.Value, existingEntity.Value.ETag);

                    Console.WriteLine("Token log updated successfully.");
                }
                else
                {
                    Console.WriteLine("Entity not found.");
                }
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error updating Token log: {ex.Message}");
            }
        }

        // Read (Retrieve) Token Log
        public async Task<TokenLogEntity> ReadTokenLogAsync(string partitionKey, string rowKey)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<TokenLogEntity>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Console.WriteLine("Token log not found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading Token log: {ex.Message}");
                return null;
            }
        }

        // Delete Token Log
        public async Task DeleteTokenLogAsync(string partitionKey, string rowKey)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
                Console.WriteLine("Token log deleted successfully.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                Console.WriteLine("Token log not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting Token log: {ex.Message}");
            }
        }

        // Read All (Retrieve All Logs)
        public IEnumerable<TokenLogEntity> ReadAllTokenLogs(string partitionKey = null)
        {
            try
            {
                Pageable<TokenLogEntity> query;

                if (!string.IsNullOrEmpty(partitionKey))
                {
                    query = _tableClient.Query<TokenLogEntity>(e => e.PartitionKey == partitionKey);
                }
                else
                {
                    query = _tableClient.Query<TokenLogEntity>();
                }

                return query.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Token logs: {ex.Message}");
                return Enumerable.Empty<TokenLogEntity>();
            }
        }
    }
}
