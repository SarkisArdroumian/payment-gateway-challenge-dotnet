using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

using PaymentGateway.Api.Application.Abstractions;
using PaymentGateway.Api.Application.Payments;
using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Infrastructure.Banking;

public class AcquiringBankSimulatorClient(HttpClient httpClient, ILogger<AcquiringBankSimulatorClient> logger) : IAcquiringBankClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AcquiringBankSimulatorClient> _logger = logger;

    public async Task<AcquiringBankPaymentResult> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
    {
        var lastFour = command.CardNumber[^4..];
        var normalizedCurrency = command.Currency.Trim().ToUpperInvariant();

        _logger.LogInformation(
            "Sending payment request to acquiring bank for amount {Amount} {Currency} with card ending {LastFour}.",
            command.Amount,
            normalizedCurrency,
            lastFour);

        using var response = await _httpClient.PostAsJsonAsync(
            "payments",
            new BankPaymentRequest
            {
                CardNumber = command.CardNumber,
                ExpiryDate = $"{command.ExpiryMonth:D2}/{command.ExpiryYear:D4}",
                Currency = command.Currency,
                Amount = command.Amount,
                Cvv = command.Cvv
            },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogWarning(
                "Acquiring bank returned status code {StatusCode} for card ending {LastFour}.",
                (int)response.StatusCode,
                lastFour);

            throw new AcquiringBankUnavailableException();
        }

        response.EnsureSuccessStatusCode();

        var bankResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The acquiring bank returned an invalid response.");

        string paymentStatus = bankResponse.Authorized
            ? PaymentStatus.Authorized.ToString()
            : PaymentStatus.Declined.ToString();

        _logger.LogInformation(
            "Acquiring bank responded with status {PaymentStatus} for card ending {LastFour}.",
            paymentStatus,
            lastFour);

        return new AcquiringBankPaymentResult
        {
            Status = paymentStatus,
            AuthorizationCode = bankResponse.AuthorizationCode
        };
    }

    private sealed class BankPaymentRequest
    {
        [JsonPropertyName("card_number")]
        public string CardNumber { get; set; } = string.Empty;

        [JsonPropertyName("expiry_date")]
        public string ExpiryDate { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public int Amount { get; set; }

        [JsonPropertyName("cvv")]
        public string Cvv { get; set; } = string.Empty;
    }

    private sealed class BankPaymentResponse
    {
        [JsonPropertyName("authorized")]
        public bool Authorized { get; set; }

        [JsonPropertyName("authorization_code")]
        public string AuthorizationCode { get; set; } = string.Empty;
    }
}
