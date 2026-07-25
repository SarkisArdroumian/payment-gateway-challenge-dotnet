using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Application;

public class PaymentRequestValidator : IPaymentRequestValidator
{
    private static readonly HashSet<string> SupportedCurrencies = ["USD", "EUR", "GBP"];

    public PaymentValidationResult Validate(PostPaymentRequest request)
    {
        Dictionary<string, List<string>> errors = new();

        ValidateCardNumber(request.CardNumber, errors);
        ValidateExpiryDate(request.ExpiryMonth, request.ExpiryYear, errors);
        ValidateCurrency(request.Currency, errors);
        ValidateAmount(request.Amount, errors);
        ValidateCvv(request.Cvv, errors);

        return new PaymentValidationResult
        {
            Errors = errors.ToDictionary(
                error => error.Key,
                error => error.Value.ToArray())
        };
    }

    private static void ValidateCardNumber(string cardNumber, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            AddError(errors, nameof(PostPaymentRequest.CardNumber), "Card number is required.");
            return;
        }

        if (!cardNumber.All(char.IsDigit))
        {
            AddError(errors, nameof(PostPaymentRequest.CardNumber), "Card number must contain only digits.");
        }

        if (cardNumber.Length < 14 || cardNumber.Length > 19)
        {
            AddError(errors, nameof(PostPaymentRequest.CardNumber), "Card number must be between 14 and 19 digits.");
        }
    }

    private static void ValidateExpiryDate(int expiryMonth, int expiryYear, Dictionary<string, List<string>> errors)
    {
        if (expiryMonth < 1 || expiryMonth > 12)
        {
            AddError(errors, nameof(PostPaymentRequest.ExpiryMonth), "Expiry month must be between 1 and 12.");
            return;
        }

        if (expiryYear < 1)
        {
            AddError(errors, nameof(PostPaymentRequest.ExpiryYear), "Expiry year must be greater than zero.");
            return;
        }

        var now = DateTime.UtcNow;
        var lastValidDate = new DateTime(expiryYear, expiryMonth, DateTime.DaysInMonth(expiryYear, expiryMonth));

        if (lastValidDate.Date < now.Date)
        {
            AddError(errors, nameof(PostPaymentRequest.ExpiryYear), "Expiry date must not be in the past.");
        }
    }

    private static void ValidateCurrency(string currency, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            AddError(errors, nameof(PostPaymentRequest.Currency), "Currency is required.");
            return;
        }

        if (!SupportedCurrencies.Contains(currency.Trim().ToUpperInvariant()))
        {
            AddError(errors, nameof(PostPaymentRequest.Currency), "Currency must be one of USD, EUR or GBP.");
        }
    }

    private static void ValidateAmount(int amount, Dictionary<string, List<string>> errors)
    {
        if (amount <= 0)
        {
            AddError(errors, nameof(PostPaymentRequest.Amount), "Amount must be greater than zero.");
        }
    }

    private static void ValidateCvv(string cvv, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrWhiteSpace(cvv))
        {
            AddError(errors, nameof(PostPaymentRequest.Cvv), "CVV is required.");
            return;
        }

        if (!cvv.All(char.IsDigit))
        {
            AddError(errors, nameof(PostPaymentRequest.Cvv), "CVV must contain only digits.");
        }

        if (cvv.Length is < 3 or > 4)
        {
            AddError(errors, nameof(PostPaymentRequest.Cvv), "CVV must be 3 or 4 digits.");
        }
    }

    private static void AddError(Dictionary<string, List<string>> errors, string key, string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = [];
            errors[key] = messages;
        }

        messages.Add(message);
    }
}
