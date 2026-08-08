using System.Collections.Concurrent;

using PaymentGateway.Api.Application.Abstractions;
using PaymentGateway.Api.Application.Payments;

namespace PaymentGateway.Api.Infrastructure.Persistence;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _records = new(StringComparer.Ordinal);

    public IdempotencyRecord? Get(string idempotencyKey)
    {
        return _records.GetValueOrDefault(idempotencyKey);
    }

    public bool TryAdd(IdempotencyRecord record)
    {
        return _records.TryAdd(record.IdempotencyKey, record);
    }
}
