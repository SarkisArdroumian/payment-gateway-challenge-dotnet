using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Application.Payments;

public class AcquiringBankPaymentResult
{
    public PaymentStatus Status { get; set; }
    public string AuthorizationCode { get; set; } = string.Empty;
}
