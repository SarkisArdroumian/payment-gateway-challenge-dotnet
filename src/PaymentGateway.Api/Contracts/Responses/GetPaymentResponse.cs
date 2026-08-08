using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Contracts.Responses;

public class GetPaymentResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; }
    public string AuthorizationCode { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Amount { get; set; }
}
