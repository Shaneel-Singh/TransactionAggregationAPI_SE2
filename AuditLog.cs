namespace TransactionAggregationAPI.Application.Domain;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAtUtc { get; set; }
    public string? CorrelationId { get; set; }
}
