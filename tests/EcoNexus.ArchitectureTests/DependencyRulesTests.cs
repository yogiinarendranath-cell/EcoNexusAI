using NetArchTest.Rules;
using Xunit;

namespace EcoNexus.ArchitectureTests;

/// <summary>
/// Enforces the Clean Architecture layering rules defined in ADR-0004.
/// Any violation fails the build.
/// </summary>
public sealed class DependencyRulesTests
{
    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(Domain.AssemblyReference).Assembly;

    // ---- Domain ----

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("EcoNexus.Application")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Domain must not depend on EcoNexus.Application.");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("EcoNexus.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Domain must not depend on EcoNexus.Infrastructure.");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("EcoNexus.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Domain must not depend on EcoNexus.Api.");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Contracts()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("EcoNexus.Contracts")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Domain must not depend on EcoNexus.Contracts.");
    }

    // ---- Application ----

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var applicationAssembly = typeof(EcoNexus.Application.AssemblyReference).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("EcoNexus.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Application must not depend on EcoNexus.Infrastructure.");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Api()
    {
        var applicationAssembly = typeof(EcoNexus.Application.AssemblyReference).Assembly;

        var result = Types.InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("EcoNexus.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Application must not depend on EcoNexus.Api.");
    }

    // ---- Contracts ----

    [Fact]
    public void Contracts_ShouldNotDependOn_Domain()
    {
        var contractsAssembly = typeof(Contracts.AssemblyReference).Assembly;

        var result = Types.InAssembly(contractsAssembly)
            .ShouldNot()
            .HaveDependencyOn("EcoNexus.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Contracts must not depend on EcoNexus.Domain.");
    }

    [Fact]
    public void Contracts_ShouldNotDependOn_Application()
    {
        var contractsAssembly = typeof(Contracts.AssemblyReference).Assembly;

        var result = Types.InAssembly(contractsAssembly)
            .ShouldNot()
            .HaveDependencyOn("EcoNexus.Application")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Contracts must not depend on EcoNexus.Application.");
    }
}
