using System;
using System.Collections.Concurrent;
using Sharp7.Rx.Interfaces;

namespace Sharp7.Rx
{
    internal class CacheVariableNameParser : IS7VariableNameParser
    {
        private static readonly ConcurrentDictionary<string, S7VariableAddress> addressCache = new ConcurrentDictionary<string, S7VariableAddress>(StringComparer.OrdinalIgnoreCase);

        private readonly IS7VariableNameParser inner;

        public CacheVariableNameParser(IS7VariableNameParser inner)
        {
            this.inner = inner;
        }

        public S7VariableAddress Parse(string input) => addressCache.GetOrAdd(input, inner.Parse);
    }
}