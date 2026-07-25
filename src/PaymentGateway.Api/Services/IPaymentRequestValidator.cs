using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Services;

public interface IPaymentRequestValidator
{
    PaymentValidationResult Validate(PostPaymentRequest request);
}
