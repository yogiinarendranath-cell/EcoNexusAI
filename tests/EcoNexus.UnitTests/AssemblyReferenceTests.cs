using EcoNexus.Domain;
using Xunit;

namespace EcoNexus.UnitTests;

public class AssemblyReferenceTests
{
    [Fact]
    public void DomainAssemblyReference_Exists()
    {
        // The anchor type proves the Domain assembly is loaded and referenced.
        Assert.NotNull(typeof(AssemblyReference));
    }
}
