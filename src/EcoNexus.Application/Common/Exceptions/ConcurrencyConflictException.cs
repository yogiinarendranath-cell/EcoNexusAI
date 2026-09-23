namespace EcoNexus.Application.Common.Exceptions;

/// <summary>
/// Raised by the persistence layer when an optimistic-concurrency conflict
/// occurs during SaveChanges. Translated from EF Core's
/// DbUpdateConcurrencyException so the Application layer does not need a
/// reference to EntityFrameworkCore.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) { }
    public ConcurrencyConflictException(string message, Exception inner)
        : base(message, inner) { }
}