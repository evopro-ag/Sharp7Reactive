namespace Sharp7.Rx.Basics;

internal class DisposableItem<TValue> : IDisposable
{
    private readonly Action disposeAction;

    bool disposed;

    public DisposableItem(IObservable<TValue> observable, Action disposeAction)
    {
        this.disposeAction = disposeAction;
        Observable = observable;
    }

    public IObservable<TValue> Observable { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;

        if (disposing)
        {
            disposeAction();
        }

        disposed = true;
    }
}
