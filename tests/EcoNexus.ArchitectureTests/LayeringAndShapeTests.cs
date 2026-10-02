using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace EcoNexus.ArchitectureTests;

/// <summary>
/// Enforces layering and shape rules beyond the base dependency rules.
/// Covers Domain purity, DomainEvents/VO placement, aggregate sealing,
/// Application layering, and API/persistence separation.
/// </summary>
public sealed class LayeringAndShapeTests
{
    private static readonly Assembly DomainAssembly =
        typeof(Domain.AssemblyReference).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(Application.AssemblyReference).Assembly;

    private static readonly Assembly ApiAssembly =
        typeof(Api.AssemblyReference).Assembly;

    [Fact]
    public void Domain_ShouldNotReference_MediatR()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("MediatR")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Domain must not reference MediatR.");
    }

    [Fact]
    public void Domain_ShouldNotReference_MicrosoftExtensionsLogging()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Extensions.Logging")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Domain must not reference Microsoft.Extensions.Logging.");
    }

    [Fact]
    public void Domain_ShouldNotReference_Serilog()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Serilog")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Domain must not reference Serilog.");
    }

    [Fact]
    public void DomainEvents_ShouldImplement_IDomainEvent()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("EcoNexus.Domain.DomainEvents")
            .Should()
            .ImplementInterface(typeof(Domain.Abstractions.IDomainEvent))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every type in EcoNexus.Domain.DomainEvents must implement IDomainEvent.");
    }
    [Fact]
    public void DomainEvents_ShouldResideIn_DomainEventsNamespace()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(Domain.Abstractions.IDomainEvent))
            .Should()
            .ResideInNamespace("EcoNexus.Domain.DomainEvents")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every IDomainEvent must live in EcoNexus.Domain.DomainEvents.");
    }

    [Fact]
    public void ValueObjects_ShouldResideIn_ValueObjectsNamespace()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Domain.Abstractions.ValueObject))
            .Should()
            .ResideInNamespace("EcoNexus.Domain.ValueObjects")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every ValueObject must live in EcoNexus.Domain.ValueObjects.");
    }

    [Fact]
    public void AggregateRoots_ShouldBe_Sealed()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Domain.Abstractions.AggregateRoot))
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every aggregate root must be sealed.");
    }

    [Fact]
    public void Application_ShouldNotReference_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Application must not reference Entity Framework Core directly.");
    }
    [Fact]
    public void Commands_ShouldResideUnder_FeaturesNamespace()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Command")
            .Should()
            .ResideInNamespaceStartingWith("EcoNexus.Application.Features")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every Command must live under EcoNexus.Application.Features.");
    }

    [Fact]
    public void Queries_ShouldResideUnder_FeaturesNamespace()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Query")
            .Should()
            .ResideInNamespaceStartingWith("EcoNexus.Application.Features")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every Query must live under EcoNexus.Application.Features.");
    }

    [Fact]
    public void Controllers_ShouldNotReference_Persistence()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("EcoNexus.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("EcoNexus.Infrastructure.Persistence")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Controllers must not reference persistence directly.");
    }
}
