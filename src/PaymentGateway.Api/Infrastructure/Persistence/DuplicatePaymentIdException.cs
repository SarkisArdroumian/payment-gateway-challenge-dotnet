namespace PaymentGateway.Api.Infrastructure.Persistence;

public class DuplicatePaymentIdException : Exception
{
    public DuplicatePaymentIdException(Guid paymentId)
        : base($"A payment with id '{paymentId}' already exists.")
    {
    }
}
