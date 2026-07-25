using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Application;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Infrastructure.Banking;

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
        var validationResult = _paymentRequestValidator.Validate(request);

        if (!validationResult.IsValid)
        {
            return BadRequest(new HttpValidationProblemDetails(validationResult.Errors.ToDictionary(
                error => error.Key,
                error => error.Value)));
        }

        try
        {
            var payment = await _paymentService.ProcessPaymentAsync(request, cancellationToken);

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
        catch (AcquiringBankUnavailableException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Title = "Acquiring bank unavailable",
                Status = StatusCodes.Status503ServiceUnavailable
            });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPaymentResponse>> GetPaymentAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentAsync(id, cancellationToken);

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