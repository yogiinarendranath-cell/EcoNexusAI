using NetArchTest.Rules;
using Xunit;

namespace EcoNexus.ArchitectureTests;

/// <summary>
/// Enforces coding conventions that keep the codebase navigable and
/// consistent as it grows:
///
///   - Handlers/validators/controllers follow naming conventions.
///   - Domain does not reference Entity Framework Core or ASP.NET Core.
///   - FluentValidation validators inherit from AbstractValidator.
///   - Entities live in the Entities namespace.
///
/// Any violation fails the build, same as the dependency rules in
/// <see cref="DependencyRulesTests"/>.
/// </summary>
public sealed class ConventionTests
{
    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(Domain.AssemblyReference).Assembly;

    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(Application.AssemblyReference).Assembly;

    private static readonly System.Reflection.Assembly ApiAssembly =
        typeof(Api.AssemblyReference).Assembly;

    // ---- Domain purity ----

    [Fact]
    public void Domain_ShouldNotReference_EntityFrameworkCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Domain must not reference Entity Framework Core.");
    }

    [Fact]
    public void Domain_ShouldNotReference_AspNetCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "EcoNexus.Domain must not reference ASP.NET Core.");
    }

    // ---- Naming conventions ----

    [Fact]
    public void Handlers_ShouldHave_NameEndingWith_Handler()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(MediatR.IRequestHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every MediatR handler must have a name ending with 'Handler'.");
    }

    [Fact]
    public void Validators_ShouldInheritFrom_AbstractValidator()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every class named '...Validator' must inherit from AbstractValidator.");
    }

    [Fact]
    public void Controllers_ShouldHave_NameEndingWith_Controller()
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace("EcoNexus.Api.Controllers")
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every type in the Controllers namespace must end with 'Controller'.");
    }

       [Fact]
    public void DomainEntities_ShouldResideIn_EntitiesNamespace()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Domain.Abstractions.Entity))
            .And()
            .DoNotResideInNamespace("EcoNexus.Domain.Abstractions")
            .Should()
            .ResideInNamespace("EcoNexus.Domain.Entities")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Every concrete entity inheriting from Entity must live in EcoNexus.Domain.Entities.");
    }
}