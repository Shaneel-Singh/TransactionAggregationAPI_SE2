using FluentValidation;
using TransactionAggregationAPI.API.Models.Requests;

namespace TransactionAggregationAPI.API.Validators;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required.")
            .MaximumLength(64).WithMessage("CustomerId must not exceed 64 characters.");

        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("AccountId is required.")
            .MaximumLength(64).WithMessage("AccountId must not exceed 64 characters.");

        RuleFor(x => x.Amount)
            .NotEqual(0).WithMessage("Amount must not be zero.")
            .Must(a => a >= -9_999_999 && a <= 9_999_999).WithMessage("Amount is out of acceptable range.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be exactly 3 characters (ISO 4217).")
            .Matches("^[A-Z]{3}$").WithMessage("Currency must be uppercase ISO 4217 code (e.g. ZAR, USD).");

        RuleFor(x => x.Description)
            .MaximumLength(512).WithMessage("Description must not exceed 512 characters.");

        RuleFor(x => x.MerchantName)
            .MaximumLength(256).WithMessage("MerchantName must not exceed 256 characters.");

        RuleFor(x => x.TransactionType)
            .NotEmpty().WithMessage("TransactionType is required.")
            .Must(t => t is "debit" or "credit" or "fee" or "transfer")
            .WithMessage("TransactionType must be one of: debit, credit, fee, transfer.");

        RuleFor(x => x.TransactionDateUtc)
            .NotEmpty().WithMessage("TransactionDateUtc is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("TransactionDateUtc cannot be more than 1 day in the future.")
            .GreaterThan(new DateTime(2000, 1, 1)).WithMessage("TransactionDateUtc must be after year 2000.");
    }
}
