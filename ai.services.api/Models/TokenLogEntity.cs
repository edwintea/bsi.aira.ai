using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ai.services.api.Models
{
    public class TokenLogEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Id { get; set; }
        public int Input { get; set; }
        public int Output { get; set; }
        public string PerformedBy { get; set; }
        public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;
        public ETag ETag { get; set; } = ETag.All;
        public DateTimeOffset? Timestamp { get; set; }

        public TokenLogEntity() { }

        public TokenLogEntity(string partitionKey, string rowKey, string id, int input, int output)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
            Id = id;
            Input = input;
            Output = output;
        }
    }
}
