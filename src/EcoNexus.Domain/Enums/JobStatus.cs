namespace EcoNexus.Domain.Enums;

/// <summary>
/// The lifecycle state of a collection job.
/// </summary>
public enum JobStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
