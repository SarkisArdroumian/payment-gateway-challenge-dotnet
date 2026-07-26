namespace PaymentGateway.Api.Application.Payments;

public class PaymentValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
}
