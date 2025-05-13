using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Options;
using LLMGateway.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Net.Http;

namespace LLMGateway.Tests.Core.Services;

public class RetryPolicyServiceTests
{
    private readonly Mock<ILogger<RetryPolicyService>> _loggerMock;
    private readonly Mock<IOptions<RetryPolicyOptions>> _optionsMock;
    private readonly RetryPolicyOptions _options;
    private readonly RetryPolicyService _service;

    public RetryPolicyServiceTests()
    {
        _loggerMock = new Mock<ILogger<RetryPolicyService>>();
        _options = new RetryPolicyOptions
        {
            MaxRetryAttempts = 3,
            MaxProviderRetryAttempts = 2,
            BaseRetryIntervalSeconds = 0.01 // Use a small value for faster tests
        };
        _optionsMock = new Mock<IOptions<RetryPolicyOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_options);
        _service = new RetryPolicyService(_loggerMock.Object, _optionsMock.Object);
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_ShouldRetryOnTransientErrors()
    {
        // Arrange
        var policy = _service.CreateAsyncRetryPolicy("TestOperation");
        var attemptCount = 0;

        // Act & Assert
        await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Transient error", null, HttpStatusCode.ServiceUnavailable);
            }
            // On the third attempt, it should succeed
            await Task.CompletedTask;
        });

        // Assert
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_ShouldRetryOnProviderUnavailableException()
    {
        // Arrange
        var policy = _service.CreateAsyncRetryPolicy("TestOperation");
        var attemptCount = 0;

        // Act & Assert
        await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new ProviderUnavailableException("TestProvider", "Provider unavailable");
            }
            // On the third attempt, it should succeed
            await Task.CompletedTask;
        });

        // Assert
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public async Task CreateAsyncRetryPolicy_ShouldNotRetryOnNonTransientErrors()
    {
        // Arrange
        var policy = _service.CreateAsyncRetryPolicy("TestOperation");

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                await Task.CompletedTask;
                throw new HttpRequestException("Non-transient error", null, HttpStatusCode.BadRequest);
            });
        });
    }

    [Fact]
    public async Task CreateAsyncRetryPolicyWithGenericType_ShouldRetryOnTransientErrors()
    {
        // Arrange
        var policy = _service.CreateAsyncRetryPolicy<string>("TestOperation");
        var attemptCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Transient error", null, HttpStatusCode.ServiceUnavailable);
            }
            // On the third attempt, it should succeed
            await Task.CompletedTask;
            return "Success";
        });

        // Assert
        Assert.Equal(3, attemptCount);
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task CreateProviderRetryPolicy_ShouldUseMaxProviderRetryAttempts()
    {
        // Arrange
        var policy = _service.CreateProviderRetryPolicy("TestProvider");
        var attemptCount = 0;

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await policy.ExecuteAsync(async () =>
            {
                attemptCount++;
                await Task.CompletedTask;
                throw new HttpRequestException("Transient error", null, HttpStatusCode.ServiceUnavailable);
            });
        });

        // Assert
        // MaxProviderRetryAttempts is 2, so we should have 3 attempts total (initial + 2 retries)
        Assert.Equal(3, attemptCount);
    }
}
