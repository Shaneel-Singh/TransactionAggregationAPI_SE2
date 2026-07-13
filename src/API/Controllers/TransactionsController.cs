using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TransactionAggregationAPI.API.Models.Requests;
using TransactionAggregationAPI.API.Models.Responses;
using TransactionAggregationAPI.Application.Interfaces;

namespace TransactionAggregationAPI.API.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _service;
    private readonly IValidator<CreateTransactionRequest> _createValidator;
    private readonly IValidator<GetTransactionsRequest> _getValidator;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService service,
        IValidator<CreateTransactionRequest> createValidator,
        IValidator<GetTransactionsRequest> getValidator,
        ILogger<TransactionsController> logger)
    {
        _service = service;
        _createValidator = createValidator;
        _getValidator = getValidator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetTransactionsRequest req, CancellationToken ct)
    {
        var validation = await _getValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return BadRequest(BuildValidationProblem(validation));

        var (items, total) = await _service.GetAllAsync(
            req.Page, req.PageSize, req.SortBy, req.SortOrder,
            req.Category, req.From, req.To, ct);

        return Ok(PagedResponse<TransactionResponse>.Create(
            items.Select(TransactionResponse.FromDomain).ToList(),
            req.Page, req.PageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tx = await _service.GetByIdAsync(id, ct);
        if (tx is null) return NotFound(ProblemDetails(404, "Transaction not found", $"No transaction with ID {id}"));
        return Ok(TransactionResponse.FromDomain(tx));
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(string customerId, [FromQuery] GetTransactionsRequest req, CancellationToken ct)
    {
        var validation = await _getValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return BadRequest(BuildValidationProblem(validation));

        var (items, total) = await _service.GetByCustomerAsync(
            customerId, req.Page, req.PageSize, req.SortBy, req.SortOrder,
            req.Category, req.From, req.To, ct);

        return Ok(PagedResponse<TransactionResponse>.Create(
            items.Select(TransactionResponse.FromDomain).ToList(),
            req.Page, req.PageSize, total));
    }

    [HttpGet("customer/{customerId}/summary")]
    public async Task<IActionResult> GetSummary(
        string customerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var summary = await _service.GetSummaryAsync(customerId, from, to, ct);
        return Ok(new SummaryResponse
        {
            CustomerId = summary.CustomerId,
            TotalCount = summary.TotalCount,
            TotalAmount = summary.TotalAmount,
            AverageAmount = summary.AverageAmount,
            MaxAmount = summary.MaxAmount,
            MinAmount = summary.MinAmount,
            CountByCategory = summary.CountByCategory.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            AmountByCategory = summary.AmountByCategory.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            EarliestTransaction = summary.EarliestTransaction,
            LatestTransaction = summary.LatestTransaction
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest req, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(req, ct);
        if (!validation.IsValid)
            return BadRequest(BuildValidationProblem(validation));

        var apiKey = HttpContext.Request.Headers["X-API-Key"].ToString();
        var created = await _service.CreateAsync(
            req.CustomerId, req.AccountId, req.Amount, req.Currency,
            req.Description, req.MerchantName, req.TransactionType,
            req.TransactionDateUtc, $"api:{apiKey[..4]}", ct);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, TransactionResponse.FromDomain(created));
    }

    /// <summary>Soft-delete a transaction</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var apiKey = HttpContext.Request.Headers["X-API-Key"].ToString();
        var result = await _service.DeleteAsync(id, $"api:{apiKey[..4]}", ct);
        if (!result) return NotFound(ProblemDetails(404, "Transaction not found", $"No transaction with ID {id}"));
        return NoContent();
    }

    private ValidationProblemDetails BuildValidationProblem(FluentValidation.Results.ValidationResult result)
    {
        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return new ValidationProblemDetails(errors)
        {
            Status = 400,
            Title = "Validation failed",
            Type = "https://tools.ietf.org/html/rfc7807"
        };
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails ProblemDetails(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Type = "https://tools.ietf.org/html/rfc7807"
    };
}
