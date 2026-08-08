namespace PaymentGateway.Api.Application.Payments;

public class IdempotencyRecord
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestFingerprint { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
}
