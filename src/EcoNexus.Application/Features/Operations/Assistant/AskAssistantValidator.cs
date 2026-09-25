using FluentValidation;

namespace EcoNexus.Application.Features.Operations.Assistant;

internal sealed class AskAssistantValidator : AbstractValidator<AskAssistantCommand>
{
    public AskAssistantValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.Question)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
