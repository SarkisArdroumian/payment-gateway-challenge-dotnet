using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Application.Abstractions;
using PaymentGateway.Api.Application.Payments;
using PaymentGateway.Api.Application.Payments.Commands;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Contracts.Requests;
using PaymentGateway.Api.Contracts.Responses;
using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Infrastructure.Banking;
using PaymentGateway.Api.Infrastructure.Persistence;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();

    [Fact]
    public async Task ProcessesAuthorizedPaymentSuccessfully()
    {
        var client = CreateClient(acquiringBankClient: new FakeAcquiringBankClient(new AcquiringBankPaymentResult
        {
            Status = PaymentStatus.Authorized.ToString(),
            AuthorizationCode = "auth-4241"
        }));
        var request = CreateValidRequest(cardNumber: "42424242424241");

        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Authorized.ToString(), paymentResponse.Status);
        Assert.Equal("auth-4241", paymentResponse.AuthorizationCode);
        Assert.Equal("4241", paymentResponse.LastFour);
        Assert.Equal(request.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(request.Currency, paymentResponse.Currency);
        Assert.Equal(request.Amount, paymentResponse.Amount);
    }

    [Fact]
    public async Task ProcessesDeclinedPaymentSuccessfully()
    {
        var client = CreateClient(acquiringBankClient: new FakeAcquiringBankClient(new AcquiringBankPaymentResult
        {
            Status = PaymentStatus.Declined.ToString(),
            AuthorizationCode = string.Empty
        }));
        var request = CreateValidRequest(cardNumber: "42424242424242");

        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Declined.ToString(), paymentResponse.Status);
        Assert.Equal(string.Empty, paymentResponse.AuthorizationCode);
        Assert.Equal("4242", paymentResponse.LastFour);
    }

    [Fact]
    public async Task Returns400ForInvalidPaymentRequest()
    {
        var client = CreateClient();
        var request = CreateValidRequest();
        request.CardNumber = "123";

        var response = await client.PostAsJsonAsync("/api/Payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns503WhenAcquiringBankIsUnavailable()
    {
        var client = CreateClient(acquiringBankClient: new FakeAcquiringBankClient(static (_, _) => throw new AcquiringBankUnavailableException()));
        var request = CreateValidRequest(cardNumber: "42424242424240");

        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal("Acquiring bank unavailable", problemDetails.Title);
        Assert.True(problemDetails.Extensions.TryGetValue("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId?.ToString()));
    }

    [Fact]
    public async Task Returns500ForUnexpectedFailure()
    {
        var client = CreateClient(acquiringBankClient: new FakeAcquiringBankClient(static (_, _) => throw new InvalidOperationException("Unexpected failure")));
        var request = CreateValidRequest();

        var response = await client.PostAsJsonAsync("/api/Payments", request);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal("An unexpected error occurred", problemDetails.Title);
        Assert.True(problemDetails.Extensions.TryGetValue("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId?.ToString()));
    }

    [Fact]
    public async Task RetrievesProcessedPaymentSuccessfully()
    {
        var client = CreateClient(acquiringBankClient: new FakeAcquiringBankClient(new AcquiringBankPaymentResult
        {
            Status = PaymentStatus.Authorized.ToString(),
            AuthorizationCode = "auth-processed"
        }));
        var request = CreateValidRequest(cardNumber: "42424242424241");

        var postResponse = await client.PostAsJsonAsync("/api/Payments", request);
        var createdPayment = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();

        var getResponse = await client.GetAsync($"/api/Payments/{createdPayment!.Id}");
        var paymentResponse = await getResponse.Content.ReadFromJsonAsync<GetPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(createdPayment.Id, paymentResponse.Id);
        Assert.Equal(createdPayment.Status, paymentResponse.Status);
        Assert.Equal(createdPayment.AuthorizationCode, paymentResponse.AuthorizationCode);
        Assert.Equal(createdPayment.LastFour, paymentResponse.LastFour);
        Assert.Equal(createdPayment.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(createdPayment.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(createdPayment.Currency, paymentResponse.Currency);
        Assert.Equal(createdPayment.Amount, paymentResponse.Amount);
    }

    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Authorized.ToString(),
            AuthorizationCode = "auth-existing",
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            LastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP"
        };

        IPaymentsRepository paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var client = CreateClient(paymentsRepository: paymentsRepository);

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(payment.Id, paymentResponse.Id);
        Assert.Equal(payment.Status, paymentResponse.Status);
        Assert.Equal(payment.AuthorizationCode, paymentResponse.AuthorizationCode);
        Assert.Equal(payment.LastFour, paymentResponse.LastFour);
        Assert.Equal(payment.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(payment.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(payment.Currency, paymentResponse.Currency);
        Assert.Equal(payment.Amount, paymentResponse.Amount);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static PostPaymentRequest CreateValidRequest(string cardNumber = "42424242424241")
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

    private static HttpClient CreateClient(
        IPaymentsRepository? paymentsRepository = null,
        IAcquiringBankClient? acquiringBankClient = null)
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();

        return webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                if (paymentsRepository is not null)
                {
                    services.AddSingleton(paymentsRepository);
                }

                if (acquiringBankClient is not null)
                {
                    services.AddSingleton(acquiringBankClient);
                }
            }))
            .CreateClient();
    }

    private sealed class FakeAcquiringBankClient : IAcquiringBankClient
    {
        private readonly Func<ProcessPaymentCommand, CancellationToken, Task<AcquiringBankPaymentResult>> _processPayment;

        public FakeAcquiringBankClient(AcquiringBankPaymentResult result)
            : this((_, _) => Task.FromResult(result))
        {
        }

        public FakeAcquiringBankClient(Func<ProcessPaymentCommand, CancellationToken, Task<AcquiringBankPaymentResult>> processPayment)
        {
            _processPayment = processPayment;
        }

        public Task<AcquiringBankPaymentResult> ProcessPaymentAsync(ProcessPaymentCommand command, CancellationToken cancellationToken = default)
        {
            return _processPayment(command, cancellationToken);
        }
    }
}
