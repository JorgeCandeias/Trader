using Outcompute.Trader.Trading.Algorithms.Positions;

namespace Outcompute.Trader.Trading.Tests;

public class AutoPositionResolverExceptionTests
{
    [Fact]
    public void Constructs1()
    {
        // act
        var ex = new AutoPositionResolverException();

        // assert
        Assert.NotEmpty(ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void Constructs2()
    {
        // arrange
        var message = "Message1";

        // act
        var ex = new AutoPositionResolverException(message);

        // assert
        Assert.Equal(message, ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void Constructs3()
    {
        // arrange
        var message = "Message1";
        var inner = new InvalidOperationException();

        // act
        var ex = new AutoPositionResolverException(message, inner);

        // assert
        Assert.Equal(message, ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}