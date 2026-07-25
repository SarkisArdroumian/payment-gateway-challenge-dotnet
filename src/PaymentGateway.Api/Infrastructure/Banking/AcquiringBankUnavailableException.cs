namespace PaymentGateway.Api.Infrastructure.Banking;

public class AcquiringBankUnavailableException : Exception
{
    public AcquiringBankUnavailableException()
        : base("The acquiring bank is unavailable.")
    {
    }
}
