using Microsoft.AspNetCore.Mvc;

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

    public PaymentsController(IPaymentService paymentService, IPaymentRequestValidator paymentRequestValidator)
    {
        _paymentService = paymentService;
        _paymentRequestValidator = paymentRequestValidator;
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync(
        [FromBody] PostPaymentRequest request,
        CancellationToken cancellationToken)
    {
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
            return BadRequest(new HttpValidationProblemDetails(validationResult.Errors.ToDictionary(
                error => error.Key,
                error => error.Value)));
        }

        var payment = await _paymentService.ProcessPaymentAsync(command, cancellationToken);

        return Ok(new PostPaymentResponse
        {
            Id = payment.Id,
            Status = payment.Status,
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
        var payment = await _paymentService.GetPaymentAsync(new GetPaymentQuery
        {
            Id = id
        }, cancellationToken);

        if (payment is null)
        {
            return NotFound();
        }

        return Ok(new GetPaymentResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            LastFour = payment.LastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        });
    }
}