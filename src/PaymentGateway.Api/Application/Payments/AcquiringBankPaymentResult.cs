using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Application.Payments;

public class AcquiringBankPaymentResult
{
    public string Status { get; set; }
    public string AuthorizationCode { get; set; } = string.Empty;
}
