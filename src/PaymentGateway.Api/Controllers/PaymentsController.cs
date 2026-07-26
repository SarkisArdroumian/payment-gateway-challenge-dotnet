using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using PaymentGateway.Api.Application.Abstractions;
using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Application.Payments.Queries;
using PaymentGateway.Api.Contracts.Requests;
using PaymentGateway.Api.Contracts.Responses;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentRequestValidator _paymentRequestValidator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IPaymentRequestValidator paymentRequestValidator,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _paymentRequestValidator = paymentRequestValidator;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync(
        [FromBody] PostPaymentRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing payment request for amount {Amount} {Currency}.",
            request.Amount,
            request.Currency);

        var command = new ProcessPaymentCommand
        {
            CardNumber = request.CardNumber,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv
        };

        var validationResult = _paymentRequestValidator.Validate(command);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Payment request validation failed with {ErrorCount} validation error fields.",
                validationResult.Errors.Count);

            return BadRequest(new HttpValidationProblemDetails(validationResult.Errors.ToDictionary(
                error => error.Key,
                error => error.Value)));
        }

        var payment = await _paymentService.ProcessPaymentAsync(command, cancellationToken);

        _logger.LogInformation(
            "Payment {PaymentId} processed with status {Status} for amount {Amount} {Currency}.",
            payment.Id,
            payment.Status,
            payment.Amount,
            payment.Currency);

        return Ok(new PostPaymentResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            AuthorizationCode = payment.AuthorizationCode,
            LastFour = payment.LastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPaymentResponse>> GetPaymentAsync(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving payment {PaymentId}.", id);

        var payment = await _paymentService.GetPaymentAsync(new GetPaymentQuery
        {
            Id = id
        }, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("Payment {PaymentId} was not found.", id);
            return NotFound();
        }

        _logger.LogInformation("Payment {PaymentId} retrieved successfully.", id);

        return Ok(new GetPaymentResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            AuthorizationCode = payment.AuthorizationCode,
            LastFour = payment.LastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        });
    }
}