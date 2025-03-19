using Azure;
using Azure.Data.Tables;
using System;

public class AuditLogEntity : ITableEntity
{
    public string PartitionKey { get; set; } // Could be "Audit" or by user/action
    public string RowKey { get; set; }       // Unique identifier for the log entry (e.g., GUID or timestamp)
    public string User { get; set; }       // Unique identifier for the log entry (e.g., GUID or timestamp)
    public string Action { get; set; }       // Action performed (e.g., "Created", "Updated", "Deleted")
    public string Status { get; set; }       // Status (e.g., "Success", "Failed", "Error")
    public string PerformedBy { get; set; }  // User who performed the action
    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow; // When the action was performed
    public string Details { get; set; }      // Additional details (e.g., what was changed)
    public ETag ETag { get; set; } = ETag.All;
    public DateTimeOffset? Timestamp { get; set; }

    public AuditLogEntity() { }

    public AuditLogEntity(string partitionKey, string rowKey, string user, string action, string status, string details)
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
        User = user;
        Action = action;
        Status = status;
        Details = details;
    }
}
