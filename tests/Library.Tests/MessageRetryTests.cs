using System.Text;
using Library.Contracts.Messaging;
using RabbitMQ.Client;

namespace Library.Tests;

public class MessageRetryTests
{
    [Fact]
    public void GetRetryCount_NullProperties_ReturnsZero()
    {
        Assert.Equal(0, MessageRetry.GetRetryCount(null));
    }

    [Fact]
    public void GetRetryCount_MissingHeader_ReturnsZero()
    {
        Assert.Equal(0, MessageRetry.GetRetryCount(new BasicProperties()));
    }

    [Fact]
    public void GetRetryCount_IntHeader_ReturnsValue()
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { [MessageRetry.HeaderName] = 3 }
        };

        Assert.Equal(3, MessageRetry.GetRetryCount(properties));
    }

    [Fact]
    public void GetRetryCount_ByteArrayHeader_ReturnsParsedValue()
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { [MessageRetry.HeaderName] = Encoding.UTF8.GetBytes("5") }
        };

        Assert.Equal(5, MessageRetry.GetRetryCount(properties));
    }

    [Fact]
    public void GetRetryCount_StringHeader_ReturnsParsedValue()
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { [MessageRetry.HeaderName] = "2" }
        };

        Assert.Equal(2, MessageRetry.GetRetryCount(properties));
    }

    [Fact]
    public void GetRetryCount_InvalidValue_ReturnsZero()
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { [MessageRetry.HeaderName] = "abc" }
        };

        Assert.Equal(0, MessageRetry.GetRetryCount(properties));
    }
}
