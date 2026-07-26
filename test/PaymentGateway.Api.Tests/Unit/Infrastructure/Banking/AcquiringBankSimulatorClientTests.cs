using System.Net;
using System.Net.Http.Json;

using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Infrastructure.Banking;
using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Tests;

public class AcquiringBankSimulatorClientTests
{
    [Fact]
    public async Task SendsExpectedRequestAndMapsAuthorizedResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    authorized = true,
                    authorization_code = Guid.NewGuid().ToString()
                })
            };
        });
        var client = CreateClient(handler);

        var result = await client.ProcessPaymentAsync(new ProcessPaymentCommand
        {
            CardNumber = "42424242424241",
            ExpiryMonth = 12,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        });

        Assert.Equal(PaymentStatus.Authorized, result);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal(new Uri("http://localhost:8080/payments"), capturedRequest.RequestUri);

        var body = await capturedRequest.Content!.ReadAsStringAsync();
        Assert.Contains("\"card_number\":\"42424242424241\"", body);
        Assert.Contains("\"expiry_date\":\"12/2030\"", body);
        Assert.Contains("\"currency\":\"GBP\"", body);
        Assert.Contains("\"amount\":100", body);
        Assert.Contains("\"cvv\":\"123\"", body);
    }

    [Fact]
    public async Task MapsDeclinedResponse()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                authorized = false,
                authorization_code = string.Empty
            })
        });
        var client = CreateClient(handler);

        var result = await client.ProcessPaymentAsync(CreateRequest());

        Assert.Equal(PaymentStatus.Declined, result);
    }

    [Fact]
    public async Task ThrowsWhenBankIsUnavailable()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<AcquiringBankUnavailableException>(() => client.ProcessPaymentAsync(CreateRequest()));
    }

    private static AcquiringBankSimulatorClient CreateClient(HttpMessageHandler handler)
    {
        return new AcquiringBankSimulatorClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080/")
        });
    }

    private static ProcessPaymentCommand CreateRequest()
    {
        return new ProcessPaymentCommand
        {
            CardNumber = "42424242424242",
            ExpiryMonth = 12,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
