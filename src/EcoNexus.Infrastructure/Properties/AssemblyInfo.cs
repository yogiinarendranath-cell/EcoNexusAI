using System.Runtime.CompilerServices;

// Allow the unit-test assembly to see `internal` types (e.g. the
// MockWasteClassificationService). This is the standard pattern for
// testing infrastructure implementations without exposing them publicly.
[assembly: InternalsVisibleTo("EcoNexus.UnitTests")]
[assembly: InternalsVisibleTo("EcoNexus.IntegrationTests")]
