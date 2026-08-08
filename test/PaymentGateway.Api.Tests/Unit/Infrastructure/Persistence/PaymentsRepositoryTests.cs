using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Infrastructure.Persistence;

namespace PaymentGateway.Api.Tests;

public class PaymentsRepositoryTests
{
    [Fact]
    public void AddStoresPaymentAndGetReturnsIt()
    {
        var repository = new PaymentsRepository();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized.ToString(),
            AuthorizationCode = "auth-123",
            LastFour = "4242",
            ExpiryMonth = 12,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100
        };

        repository.Add(payment);

        var storedPayment = repository.Get(payment.Id);

        Assert.NotNull(storedPayment);
        Assert.Equal(payment.Id, storedPayment.Id);
        Assert.Equal(payment.Status, storedPayment.Status);
        Assert.Equal(payment.AuthorizationCode, storedPayment.AuthorizationCode);
        Assert.Equal(payment.LastFour, storedPayment.LastFour);
        Assert.Equal(payment.ExpiryMonth, storedPayment.ExpiryMonth);
        Assert.Equal(payment.ExpiryYear, storedPayment.ExpiryYear);
        Assert.Equal(payment.Currency, storedPayment.Currency);
        Assert.Equal(payment.Amount, storedPayment.Amount);
    }

    [Fact]
    public void AddThrowsWhenPaymentIdAlreadyExists()
    {
        var repository = new PaymentsRepository();
        var id = Guid.NewGuid();

        repository.Add(new Payment
        {
            Id = id,
            Status = PaymentStatus.Authorized.ToString(),
            AuthorizationCode = "auth-1",
            LastFour = "4241",
            ExpiryMonth = 12,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100
        });

        var duplicatePayment = new Payment
        {
            Id = id,
            Status = PaymentStatus.Declined.ToString(),
            AuthorizationCode = string.Empty,
            LastFour = "4242",
            ExpiryMonth = 11,
            ExpiryYear = 2031,
            Currency = "USD",
            Amount = 200
        };

        var exception = Assert.Throws<DuplicatePaymentIdException>(() => repository.Add(duplicatePayment));

        Assert.Equal($"A payment with id '{id}' already exists.", exception.Message);
    }
}
