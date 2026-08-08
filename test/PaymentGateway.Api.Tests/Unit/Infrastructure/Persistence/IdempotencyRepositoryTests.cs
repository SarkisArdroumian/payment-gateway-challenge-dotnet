using PaymentGateway.Api.Application.Payments;
using PaymentGateway.Api.Infrastructure.Persistence;

namespace PaymentGateway.Api.Tests;

public class IdempotencyRepositoryTests
{
    [Fact]
    public void TryAddStoresRecordAndGetReturnsIt()
    {
        var repository = new IdempotencyRepository();
        var record = new IdempotencyRecord
        {
            IdempotencyKey = "merchant-payment-001",
            RequestFingerprint = "fingerprint-1",
            PaymentId = Guid.NewGuid()
        };

        var stored = repository.TryAdd(record);
        var retrieved = repository.Get(record.IdempotencyKey);

        Assert.True(stored);
        Assert.NotNull(retrieved);
        Assert.Equal(record.IdempotencyKey, retrieved.IdempotencyKey);
        Assert.Equal(record.RequestFingerprint, retrieved.RequestFingerprint);
        Assert.Equal(record.PaymentId, retrieved.PaymentId);
    }

    [Fact]
    public void TryAddReturnsFalseWhenIdempotencyKeyAlreadyExists()
    {
        var repository = new IdempotencyRepository();
        var key = "merchant-payment-002";

        repository.TryAdd(new IdempotencyRecord
        {
            IdempotencyKey = key,
            RequestFingerprint = "fingerprint-1",
            PaymentId = Guid.NewGuid()
        });

        var result = repository.TryAdd(new IdempotencyRecord
        {
            IdempotencyKey = key,
            RequestFingerprint = "fingerprint-2",
            PaymentId = Guid.NewGuid()
        });

        Assert.False(result);
    }
}
