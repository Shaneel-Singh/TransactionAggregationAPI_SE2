using FluentAssertions;
using FluentValidation.TestHelper;
using TransactionAggregationAPI.API.Models.Requests;
using TransactionAggregationAPI.API.Validators;
using Xunit;

namespace TransactionAggregationAPI.UnitTests;

public class ValidationTests
{
    private readonly CreateTransactionRequestValidator _createValidator;
    private readonly GetTransactionsRequestValidator _getValidator;

    public ValidationTests()
    {
        _createValidator = new CreateTransactionRequestValidator();
        _getValidator = new GetTransactionsRequestValidator();
    }

    [Fact]
    public void CreateTransactionRequest_ValidRequest_PassesValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            Description = "Test transaction",
            MerchantName = "Test Merchant",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateTransactionRequest_EmptyCustomerId_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.CustomerId)
            .WithErrorMessage("CustomerId is required.");
    }

    [Fact]
    public void CreateTransactionRequest_CustomerIdTooLong_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = new string('A', 65),
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.CustomerId)
            .WithErrorMessage("CustomerId must not exceed 64 characters.");
    }

    [Fact]
    public void CreateTransactionRequest_EmptyAccountId_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "",
            Amount = 100m,
            Currency = "ZAR",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.AccountId)
            .WithErrorMessage("AccountId is required.");
    }

    [Fact]
    public void CreateTransactionRequest_ZeroAmount_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 0m,
            Currency = "ZAR",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Amount)
            .WithErrorMessage("Amount must not be zero.");
    }

    [Fact]
    public void CreateTransactionRequest_LowercaseCurrency_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "zar",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Currency);
    }

    [Fact]
    public void CreateTransactionRequest_CurrencyTwoChars_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZA",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Currency)
            .WithErrorMessage("Currency must be exactly 3 characters (ISO 4217).");
    }

    [Fact]
    public void CreateTransactionRequest_CurrencyFourChars_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZARR",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Currency)
            .WithErrorMessage("Currency must be exactly 3 characters (ISO 4217).");
    }

    [Fact]
    public void CreateTransactionRequest_ValidCurrencyZAR_PassesValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.Currency);
    }

    [Fact]
    public void CreateTransactionRequest_InvalidTransactionTypeBuy_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            TransactionType = "buy",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.TransactionType)
            .WithErrorMessage("TransactionType must be one of: debit, credit, fee, transfer.");
    }

    [Fact]
    public void CreateTransactionRequest_ValidTransactionTypeDebit_PassesValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.TransactionType);
    }

    [Fact]
    public void CreateTransactionRequest_TransactionDateFarPast_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            TransactionType = "debit",
            TransactionDateUtc = new DateTime(1999, 1, 1)
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.TransactionDateUtc)
            .WithErrorMessage("TransactionDateUtc must be after year 2000.");
    }

    [Fact]
    public void CreateTransactionRequest_TransactionDateInFuture_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            TransactionType = "debit",
            TransactionDateUtc = DateTime.UtcNow.AddDays(2)
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.TransactionDateUtc)
            .WithErrorMessage("TransactionDateUtc cannot be more than 1 day in the future.");
    }

    [Fact]
    public void GetTransactionsRequest_ValidDefaults_PassesValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 20
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void GetTransactionsRequest_PageZero_FailsValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 0,
            PageSize = 20
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Page)
            .WithErrorMessage("Page must be at least 1.");
    }

    [Fact]
    public void GetTransactionsRequest_PageSize101_FailsValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 101
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.PageSize)
            .WithErrorMessage("PageSize must be between 1 and 100.");
    }

    [Fact]
    public void GetTransactionsRequest_PageSize100_PassesValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 100
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.PageSize);
    }

    [Fact]
    public void GetTransactionsRequest_PageSize1_PassesValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 1
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.PageSize);
    }

    [Fact]
    public void GetTransactionsRequest_InvalidCategory_FailsValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 20,
            Category = "InvalidCategory"
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.Category)
            .WithErrorMessage("Category is not a valid transaction category.");
    }

    [Fact]
    public void GetTransactionsRequest_FromGreaterThanTo_FailsValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 20,
            From = DateTime.UtcNow,
            To = DateTime.UtcNow.AddDays(-1)
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r)
            .WithErrorMessage("'from' date must be before or equal to 'to' date.");
    }

    [Fact]
    public void GetTransactionsRequest_ValidSortByDate_PassesValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 20,
            SortBy = "date"
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.SortBy);
    }

    [Fact]
    public void GetTransactionsRequest_InvalidSortBy_FailsValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 20,
            SortBy = "xyz"
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(r => r.SortBy);
    }

    [Fact]
    public void CreateTransactionRequest_ValidCurrencyUSD_PassesValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "USD",
            TransactionType = "credit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.Currency);
    }

    [Fact]
    public void CreateTransactionRequest_ValidTransactionTypeCredit_PassesValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 100m,
            Currency = "ZAR",
            TransactionType = "credit",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.TransactionType);
    }

    [Fact]
    public void CreateTransactionRequest_ValidTransactionTypeFee_PassesValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 10m,
            Currency = "ZAR",
            TransactionType = "fee",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.TransactionType);
    }

    [Fact]
    public void CreateTransactionRequest_ValidTransactionTypeTransfer_PassesValidation()
    {
        var request = new CreateTransactionRequest
        {
            CustomerId = "C001",
            AccountId = "ACC001",
            Amount = 500m,
            Currency = "ZAR",
            TransactionType = "transfer",
            TransactionDateUtc = DateTime.UtcNow
        };

        var result = _createValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.TransactionType);
    }

    [Fact]
    public void GetTransactionsRequest_ValidCategoryFood_PassesValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 20,
            Category = "Food"
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.Category);
    }

    [Fact]
    public void GetTransactionsRequest_ValidSortByAmount_PassesValidation()
    {
        var request = new GetTransactionsRequest
        {
            Page = 1,
            PageSize = 20,
            SortBy = "amount"
        };

        var result = _getValidator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(r => r.SortBy);
    }
}
