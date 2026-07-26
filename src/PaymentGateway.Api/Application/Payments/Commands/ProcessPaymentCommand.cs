namespace PaymentGateway.Api.Application.Payments.Commands;

public class ProcessPaymentCommand
{
    public string CardNumber { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Cvv { get; set; } = string.Empty;
}
