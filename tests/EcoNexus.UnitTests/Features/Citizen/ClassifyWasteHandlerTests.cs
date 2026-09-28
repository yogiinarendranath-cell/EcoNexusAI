using EcoNexus.Application.Abstractions.AI;
using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Citizen.ClassifyWaste;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Citizen;

public sealed class ClassifyWasteHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly DateTimeOffset T0 = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);
    private const string TestImageUrl = "https://example.com/plastic-bottle.jpg";

    private readonly ICitizenProfileRepository _profiles = Substitute.For<ICitizenProfileRepository>();
    private readonly IWasteClassificationService _classifier = Substitute.For<IWasteClassificationService>();
    private readonly ClassifyWasteHandler _handler;

    public ClassifyWasteHandlerTests()
    {
        _classifier.ProviderName.Returns("Mock");
        _handler = new ClassifyWasteHandler(_profiles, _classifier);
    }

    // -------------------- Fixtures --------------------

    private static CitizenProfile NewProfile()
        => CitizenProfile.Create(UserId, "Test Citizen", T0);

    private static WasteClassificationResult ConfidentPlasticResult()
        => new(
            Category: WasteCategory.Plastic,
            Confidence: 0.92,
            IsRecyclable: true,
            IsCompostable: false,
            DisposalInstruction: "Rinse and place in the yellow recycling bin.");

    private static WasteClassificationResult LowConfidenceResult()
        => new(
            Category: WasteCategory.General,
            Confidence: 0.35,
            IsRecyclable: false,
            IsCompostable: false,
            DisposalInstruction: "Cannot determine material. Place in general waste.");

    private void ProfileExists(CitizenProfile profile)
        => _profiles.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
                    .Returns(profile);

    private void ClassifierReturns(WasteClassificationResult result)
        => _classifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                     .Returns(result);

    // -------------------- Profile guard --------------------

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _profiles.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
                 .Returns((CitizenProfile?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new ClassifyWasteCommand(UserId, TestImageUrl), CancellationToken.None));

        // Classifier never invoked.
        await _classifier.DidNotReceiveWithAnyArgs()
            .ClassifyAsync(default!, default);
    }

    // -------------------- Classifier failure --------------------

    [Fact]
    public async Task Handle_ClassifierThrows_ThrowsConflictException()
    {
        ProfileExists(NewProfile());
        _classifier.ClassifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                   .Returns<Task<WasteClassificationResult>>(_ =>
                       throw new InvalidOperationException("provider down"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new ClassifyWasteCommand(UserId, TestImageUrl), CancellationToken.None));

        await _profiles.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    // -------------------- Happy path --------------------

    [Fact]
    public async Task Handle_ConfidentResult_ReturnsResponseWithAllFields()
    {
        var profile = NewProfile();
        ProfileExists(profile);
        ClassifierReturns(ConfidentPlasticResult());

        var response = await _handler.Handle(
            new ClassifyWasteCommand(UserId, TestImageUrl),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.ClassificationId);
        Assert.Equal("Plastic", response.Category);
        Assert.Equal(0.92, response.Confidence, precision: 2);
        Assert.True(response.IsRecyclable);
        Assert.False(response.IsCompostable);
        Assert.Contains("yellow recycling bin", response.DisposalInstruction);
        Assert.True(response.IsConfident);  // 0.92 >= 0.6
        Assert.Equal("Mock", response.ProviderName);
    }

    [Fact]
    public async Task Handle_ConfidentResult_RecordsClassificationOnProfile()
    {
        var profile = NewProfile();
        ProfileExists(profile);
        ClassifierReturns(ConfidentPlasticResult());

        await _handler.Handle(
            new ClassifyWasteCommand(UserId, TestImageUrl),
            CancellationToken.None);

        Assert.Single(profile.Classifications);
        Assert.Equal(WasteCategory.Plastic, profile.Classifications.First().Category);
    }

    [Fact]
    public async Task Handle_ConfidentResult_PersistsChanges()
    {
        ProfileExists(NewProfile());
        ClassifierReturns(ConfidentPlasticResult());

        await _handler.Handle(
            new ClassifyWasteCommand(UserId, TestImageUrl),
            CancellationToken.None);

        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -------------------- Low confidence --------------------

    [Fact]
    public async Task Handle_LowConfidenceResult_MarksIsConfidentFalse()
    {
        ProfileExists(NewProfile());
        ClassifierReturns(LowConfidenceResult());

        var response = await _handler.Handle(
            new ClassifyWasteCommand(UserId, TestImageUrl),
            CancellationToken.None);

        Assert.False(response.IsConfident);  // 0.35 < 0.6
        Assert.Equal("General", response.Category);
    }

    // -------------------- Provider name passthrough --------------------

    [Fact]
    public async Task Handle_RecordsProviderNameFromService()
    {
        _classifier.ProviderName.Returns("Ollama");
        ProfileExists(NewProfile());
        ClassifierReturns(ConfidentPlasticResult());

        var response = await _handler.Handle(
            new ClassifyWasteCommand(UserId, TestImageUrl),
            CancellationToken.None);

        Assert.Equal("Ollama", response.ProviderName);
    }
}