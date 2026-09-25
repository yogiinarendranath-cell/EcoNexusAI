namespace EcoNexus.Contracts.Citizen;

/// <summary>
/// Request body for POST /api/v1/citizen/classify. The image is
/// referenced by URL (or data URI); file upload arrives in a later phase.
/// </summary>
public sealed record ClassifyWasteRequest(string ImageUrl);
