using PaymentGateway.Api.Application.Payments;
using PaymentGateway.Api.Application.Payments.Commands;

namespace PaymentGateway.Api.Application.Abstractions;

public interface IPaymentRequestValidator
{
    PaymentValidationResult Validate(ProcessPaymentCommand command);
}
