using EcoNexus.Domain.Entities;

namespace EcoNexus.Application.Abstractions.Persistence;

/// <summary>
/// Persistence abstraction for the AssistantInteraction aggregate.
/// Recorded questions, tool choices, and answers live here for audit
/// and analysis.
/// </summary>
public interface IAssistantInteractionRepository
{
    /// <summary>Adds a new interaction and commits.</summary>
    Task AddAsync(AssistantInteraction interaction, CancellationToken cancellationToken = default);

    /// <summary>Loads a single interaction by id.</summary>
    Task<AssistantInteraction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Recent interactions for one user, newest first.</summary>
    Task<IReadOnlyList<AssistantInteraction>> GetRecentForUserAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes tracked changes. MUST translate EF Core's
    /// DbUpdateConcurrencyException into ConcurrencyConflictException.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
