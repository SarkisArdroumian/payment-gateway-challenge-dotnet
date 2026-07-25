using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Application;

public interface IPaymentRequestValidator
{
    PaymentValidationResult Validate(PostPaymentRequest request);
}
