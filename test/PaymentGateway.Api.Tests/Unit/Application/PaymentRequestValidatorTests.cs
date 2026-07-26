using PaymentGateway.Api.Application.Payments;
using PaymentGateway.Api.Application.Payments.Commands;

namespace PaymentGateway.Api.Tests;

public class PaymentRequestValidatorTests
{
    private readonly PaymentRequestValidator _validator = new();

    [Fact]
    public void ValidRequestPassesValidation()
    {
        var request = CreateValidRequest();

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void InvalidCardNumberReturnsValidationError()
    {
        var request = CreateValidRequest();
        request.CardNumber = "1234abcd567890";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(ProcessPaymentCommand.CardNumber), result.Errors.Keys);
    }

    [Fact]
    public void ExpiredCardReturnsValidationError()
    {
        var now = DateTime.UtcNow;
        var request = CreateValidRequest();
        request.ExpiryMonth = now.AddMonths(-1).Month;
        request.ExpiryYear = now.AddMonths(-1).Year;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(ProcessPaymentCommand.ExpiryYear), result.Errors.Keys);
    }

    [Fact]
    public void UnsupportedCurrencyReturnsValidationError()
    {
        var request = CreateValidRequest();
        request.Currency = "CAD";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(ProcessPaymentCommand.Currency), result.Errors.Keys);
    }

    [Fact]
    public void NonPositiveAmountReturnsValidationError()
    {
        var request = CreateValidRequest();
        request.Amount = 0;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(ProcessPaymentCommand.Amount), result.Errors.Keys);
    }

    [Fact]
    public void InvalidCvvReturnsValidationError()
    {
        var request = CreateValidRequest();
        request.Cvv = "12a";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(ProcessPaymentCommand.Cvv), result.Errors.Keys);
    }

    private static ProcessPaymentCommand CreateValidRequest()
    {
        var expiryDate = DateTime.UtcNow.AddMonths(1);

        return new ProcessPaymentCommand
        {
            CardNumber = "42424242424242",
            ExpiryMonth = expiryDate.Month,
            ExpiryYear = expiryDate.Year,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
    }
}
