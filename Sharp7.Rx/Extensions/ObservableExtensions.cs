using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Sharp7.Rx.Extensions
{
    internal static class ObservableExtensions
    {
        public static IObservable<Unit> Select<TSource>(this IObservable<TSource> source, Func<TSource, Task> selector)
        {
            return source
                .Select(x => Observable.FromAsync(async () => await selector(x)))
                .Concat();
        }

        public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, Task<TResult>> selector)
        {
            return source
                .Select(x => Observable.FromAsync(async () => await selector(x)))
                .Concat();
        }
    }
}
