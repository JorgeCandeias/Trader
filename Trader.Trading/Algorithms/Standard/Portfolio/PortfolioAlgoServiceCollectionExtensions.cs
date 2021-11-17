﻿using Outcompute.Trader.Trading.Algorithms;
using Outcompute.Trader.Trading.Algorithms.Standard.Portfolio;

namespace Microsoft.Extensions.DependencyInjection;

public static class PortfolioAlgoServiceCollectionExtensions
{
    internal const string AlgoTypeName = "Portfolio";

    internal static IServiceCollection AddPortfolioAlgoType(this IServiceCollection services)
    {
        return services
            .AddAlgoType<PortfolioAlgo>(AlgoTypeName)
            .AddOptionsType<PortfolioAlgoOptions>()
            .Services;
    }

    public static IAlgoBuilder<IAlgo, PortfolioAlgoOptions> AddPortfolioAlgo(this IServiceCollection services, string name)
    {
        return services.AddAlgo<IAlgo, PortfolioAlgoOptions>(name, AlgoTypeName);
    }
}