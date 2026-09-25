namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

/// <summary>
/// Helpers for reading typed values out of the loosely-typed parameter
/// dictionary that the LLM (or mock) provides.
/// </summary>
internal static class ToolParameterHelpers
{
    public static int GetInt(
        IReadOnlyDictionary<string, object?> parameters,
        string name,
        int defaultValue)
    {
        if (!parameters.TryGetValue(name, out var raw) || raw is null)
        {
            return defaultValue;
        }

        return raw switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => defaultValue,
        };
    }
}
