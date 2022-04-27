﻿using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators;

public class AbsLossTests
{
    [Fact]
    public void YieldsPositiveChanges()
    {
        // act
        using var indicator = Indicator.Identity<decimal?>(1, 1, 2, 3, 5).AbsLoss();

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Equal(0, x),
            x => Assert.Equal(0, x),
            x => Assert.Equal(0, x),
            x => Assert.Equal(0, x));
    }

    [Fact]
    public void YieldsNegativeChanges()
    {
        // act
        using var indicator = Indicator.Identity<decimal?>(144, 89, 55, 34, 21).AbsLoss();

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Equal(55, x),
            x => Assert.Equal(34, x),
            x => Assert.Equal(21, x),
            x => Assert.Equal(13, x));
    }

    [Fact]
    public void YieldsMixedChanges()
    {
        // act
        using var indicator = Indicator.Identity<decimal?>(1, 2, 1, 5, 3).AbsLoss();

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Equal(0, x),
            x => Assert.Equal(1, x),
            x => Assert.Equal(0, x),
            x => Assert.Equal(2, x));
    }

    [Fact]
    public void UpdatesFromSource()
    {
        // arrange
        using var source = Indicator.Identity<decimal?>(1, 2, 1, 5, 3);
        using var indicator = source.AbsLoss();

        // act
        source.Update(2, 0);

        // assert
        Assert.Collection(indicator,
            x => Assert.Null(x),
            x => Assert.Equal(0, x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(0, x),
            x => Assert.Equal(2, x));
    }
}