using System.Collections.Concurrent;
using Sharp7.Rx.Interfaces;

namespace Sharp7.Rx;

internal class CacheVariableNameParser : IVariableNameParser
{
    private static readonly ConcurrentDictionary<string, VariableAddress> addressCache = new ConcurrentDictionary<string, VariableAddress>(StringComparer.OrdinalIgnoreCase);

    private readonly IVariableNameParser inner;

    public CacheVariableNameParser(IVariableNameParser inner)
    {
        this.inner = inner;
    }

    public VariableAddress Parse(string input) => addressCache.GetOrAdd(input, inner.Parse);
}
