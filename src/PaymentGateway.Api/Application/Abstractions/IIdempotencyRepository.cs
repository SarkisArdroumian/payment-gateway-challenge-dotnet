using PaymentGateway.Api.Application.Payments;

namespace PaymentGateway.Api.Application.Abstractions;

public interface IIdempotencyRepository
{
    IdempotencyRecord? Get(string idempotencyKey);
    bool TryAdd(IdempotencyRecord record);
}
