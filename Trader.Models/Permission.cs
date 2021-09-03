using System.Diagnostics.CodeAnalysis;

namespace Outcompute.Trader.Models
{
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Business Model")]
    public enum Permission
    {
        None,
        Spot,
        Margin,
        Leveraged
    }
}