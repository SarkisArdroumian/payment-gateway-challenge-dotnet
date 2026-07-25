using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

using Xunit.Abstractions;
using Xunit.Sdk;

namespace PaymentGateway.Api.Tests;

public class PaymentsBankSimulatorEndToEndTests(ITestOutputHelper output)
{
    private const string BankSimulatorBaseUrl = "http://localhost:8080/";
    private readonly ITestOutputHelper _output = output;

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task ProcessesAuthorizedPaymentThroughRealBankSimulator()
    {
        await EnsureBankSimulatorAvailableAsync();

        using var client = CreateClient();
        var request = CreateValidRequest(cardNumber: "42424242424241");

        var response = await client.PostAsJsonAsync("/api/payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("4241", paymentResponse.LastFour);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task ProcessesDeclinedPaymentThroughRealBankSimulator()
    {
        await EnsureBankSimulatorAvailableAsync();

        using var client = CreateClient();
        var request = CreateValidRequest(cardNumber: "42424242424242");

        var response = await client.PostAsJsonAsync("/api/payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal("4242", paymentResponse.LastFour);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task Returns503WhenRealBankSimulatorIsUnavailableForPayment()
    {
        await EnsureBankSimulatorAvailableAsync();

        using var client = CreateClient();
        var request = CreateValidRequest(cardNumber: "42424242424240");

        var response = await client.PostAsJsonAsync("/api/payments", request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private async Task EnsureBankSimulatorAvailableAsync()
    {
        try
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(BankSimulatorBaseUrl),
                Timeout = TimeSpan.FromSeconds(2)
            };

            using var response = await httpClient.GetAsync("payments");
            _output.WriteLine($"Bank simulator probe status: {(int)response.StatusCode}");
        }
        catch (Exception exception)
        {
            throw new XunitException($"Bank simulator is unavailable at {BankSimulatorBaseUrl}. Start it with 'docker compose up -d'. {exception.Message}");
        }
    }

    private static HttpClient CreateClient()
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();

        return webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AcquiringBank:BaseUrl"] = BankSimulatorBaseUrl
                });
            }))
            .CreateClient();
    }

    private static PostPaymentRequest CreateValidRequest(string cardNumber)
    {
        var expiryDate = DateTime.UtcNow.AddMonths(1);

        return new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = expiryDate.Month,
            ExpiryYear = expiryDate.Year,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
    }
}
