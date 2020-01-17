using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

namespace Sharp7.Rx.Extensions
{
    internal static class DisposableExtensions
    {
        public static void AddDisposableTo(this IDisposable disposable, CompositeDisposable compositeDisposable)
        {
            compositeDisposable.Add(disposable);
        }
        
        public static void DisposeItems(this IEnumerable<object> disposables)
        {
            foreach (IDisposable disposable in disposables.OfType<IDisposable>())
                disposable?.Dispose();
        }
    }
}
