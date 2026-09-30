using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Citizen.ListClassifications;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Citizen;

public sealed class ListClassificationsHandlerTests
{
    private readonly ICitizenProfileRepository _profileRepository =
        Substitute.For<ICitizenProfileRepository>();

    private readonly ListClassificationsHandler _handler;

    public ListClassificationsHandlerTests()
    {
        _handler = new ListClassificationsHandler(_profileRepository);
    }

    private static CitizenProfile NewProfile()
        => CitizenProfile.Create(
            Guid.NewGuid(),
            "Ada Lovelace",
            DateTimeOffset.UtcNow);

    private static WasteClassification AddClassification(
        CitizenProfile profile,
        WasteCategory category,
        double confidence,
        DateTimeOffset at)
    {
        var result = new WasteClassificationResult(
            category,
            confidence,
            IsRecyclable: true,
            IsCompostable: false,
            DisposalInstruction: $"Dispose {category} in the appropriate bin.");

        return profile.RecordClassification(
            result,
            imageReference: $"https://example.com/img-{Guid.NewGuid():N}.jpg",
            providerName: "Mock",
            classifiedAt: at);
    }

    [Fact]
    public async Task Handle_ProfileWithClassifications_ReturnsMappedListNewestFirst()
    {
        var profile = NewProfile();
        var baseTime = DateTimeOffset.UtcNow;

        var older = AddClassification(profile, WasteCategory.Plastic, 0.92, baseTime);
        var newer = AddClassification(profile, WasteCategory.Glass,   0.88, baseTime.AddHours(1));

        _profileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var response = await _handler.Handle(
            new ListClassificationsQuery(profile.UserId),
            CancellationToken.None);

        Assert.Equal(2, response.Count);

        Assert.Equal(newer.Id,                   response[0].Id);
        Assert.Equal("Glass",                    response[0].Category);
        Assert.Equal(0.88,                       response[0].Confidence, 3);
        Assert.True(response[0].IsRecyclable);
        Assert.False(response[0].IsCompostable);
        Assert.Equal(newer.DisposalInstruction,  response[0].DisposalInstruction);
        Assert.Equal(newer.ImageReference,       response[0].ImageReference);
        Assert.Equal("Mock",                     response[0].ProviderName);
        Assert.Equal(newer.ClassifiedAt,         response[0].ClassifiedAt);

        Assert.Equal(older.Id,                   response[1].Id);
        Assert.Equal("Plastic",                  response[1].Category);
    }

    [Fact]
    public async Task Handle_ProfileWithNoClassifications_ReturnsEmptyList()
    {
        var profile = NewProfile();

        _profileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var response = await _handler.Handle(
            new ListClassificationsQuery(profile.UserId),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task Handle_ProfileDoesNotExist_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();

        _profileRepository
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((CitizenProfile?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new ListClassificationsQuery(userId),
                CancellationToken.None));

        Assert.Contains(userId.ToString(), ex.Message);
    }

    [Fact]
    public async Task Handle_QueriesRepositoryWithExactUserId()
    {
        var profile = NewProfile();

        _profileRepository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        await _handler.Handle(
            new ListClassificationsQuery(profile.UserId),
            CancellationToken.None);

        await _profileRepository
            .Received(1)
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>());
    }
}