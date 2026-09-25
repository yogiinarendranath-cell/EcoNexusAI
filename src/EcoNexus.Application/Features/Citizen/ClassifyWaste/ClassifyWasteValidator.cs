using FluentValidation;

namespace EcoNexus.Application.Features.Citizen.ClassifyWaste;

internal sealed class ClassifyWasteValidator : AbstractValidator<ClassifyWasteCommand>
{
    public ClassifyWasteValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.ImageUrl)
            .NotEmpty()
            .MaximumLength(2048);
    }
}
