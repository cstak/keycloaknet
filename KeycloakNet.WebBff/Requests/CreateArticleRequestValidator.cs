using FluentValidation;

namespace KeycloakNet.WebBff.Requests;

public class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Article title is required.")
            .MinimumLength(3).WithMessage("Article title must be at least 3 characters long.")
            .MaximumLength(100).WithMessage("Article title must not exceed 100 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Article content is required.")
            .MinimumLength(5).WithMessage("Article content must be at least 5 characters long.");
    }
}
