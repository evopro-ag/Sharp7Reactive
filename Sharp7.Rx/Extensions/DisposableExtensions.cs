using System.Reactive.Disposables;

namespace Sharp7.Rx.Extensions;

internal static class DisposableExtensions
{
    public static void AddDisposableTo(this IDisposable disposable, CompositeDisposable compositeDisposable)
    {
        compositeDisposable.Add(disposable);
    }
}
