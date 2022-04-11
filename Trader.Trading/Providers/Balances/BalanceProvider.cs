using AutoMapper;
using Outcompute.Trader.Data;

namespace Outcompute.Trader.Trading.Providers.Balances;

internal class BalanceProvider : IBalanceProvider
{
    private readonly IGrainFactory _factory;
    private readonly ITradingRepository _repository;
    private readonly IMapper _mapper;

    public BalanceProvider(IGrainFactory factory, ITradingRepository repository, IMapper mapper)
    {
        _factory = factory;
        _repository = repository;
        _mapper = mapper;
    }

    public ValueTask<Balance?> TryGetBalanceAsync(string asset, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(asset, nameof(asset));

        return _factory.GetBalanceProviderReplicaGrain(asset).TryGetBalanceAsync();
    }

    public async ValueTask SetBalancesAsync(IEnumerable<Balance> balances, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(balances, nameof(balances));

        // todo: move this to the grain
        await _repository.SetBalancesAsync(balances, cancellationToken).ConfigureAwait(false);

        await balances
            .Select(balance => _factory.GetBalanceProviderGrain(balance.Asset).SetBalanceAsync(balance))
            .WhenAll()
            .ConfigureAwait(false);
    }

    public ValueTask SetBalancesAsync(AccountInfo accountInfo, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(accountInfo, nameof(accountInfo));

        var balances = _mapper.Map<IEnumerable<Balance>>(accountInfo);

        return SetBalancesAsync(balances, cancellationToken);
    }

    public ValueTask<IEnumerable<Balance>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        // todo: get this from replica grains
        return _repository.GetBalancesAsync(cancellationToken);
    }
}