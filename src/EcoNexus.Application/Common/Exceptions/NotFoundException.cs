namespace EcoNexus.Application.Common.Exceptions;

/// <summary>
/// Thrown when a requested resource (e.g., a station) does not exist.
/// The global exception handler maps this to HTTP 404.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
