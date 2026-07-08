using FluentValidation;
using TransactionAggregationAPI.API.Models.Requests;
using TransactionAggregationAPI.Application.Domain;

namespace TransactionAggregationAPI.API.Validators;

public class GetTransactionsRequestValidator : AbstractValidator<GetTransactionsRequest>
{
    private static readonly string[] ValidSortFields = ["date", "amount", "category", "merchant"];
    private static readonly string[] ValidSortOrders = ["asc", "desc"];

    public GetTransactionsRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.SortBy)
            .Must(s => s is null || ValidSortFields.Contains(s.ToLowerInvariant()))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortFields)}.");

        RuleFor(x => x.SortOrder)
            .Must(s => s is null || ValidSortOrders.Contains(s.ToLowerInvariant()))
            .WithMessage("SortOrder must be 'asc' or 'desc'.");

        RuleFor(x => x.Category)
            .Must(c => c is null || Enum.TryParse<TransactionCategory>(c, true, out _))
            .WithMessage("Category is not a valid transaction category.");

        RuleFor(x => x)
            .Must(r => r.From is null || r.To is null || r.From <= r.To)
            .WithMessage("'from' date must be before or equal to 'to' date.");
    }
}
