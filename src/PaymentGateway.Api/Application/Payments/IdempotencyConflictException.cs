namespace PaymentGateway.Api.Application.Payments;

public class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException(string idempotencyKey)
        : base($"The idempotency key '{idempotencyKey}' was reused with a different payment request.")
    {
    }
}
