using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using PaymentGateway.Api.Application.Abstractions;
using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Infrastructure.Banking;

public class AcquiringBankSimulatorClient(HttpClient httpClient) : IAcquiringBankClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<PaymentStatus> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
    {
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
            throw new AcquiringBankUnavailableException();
        }

        response.EnsureSuccessStatusCode();

        var bankResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The acquiring bank returned an invalid response.");

        return bankResponse.Authorized
            ? PaymentStatus.Authorized
            : PaymentStatus.Declined;
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
    }
}
