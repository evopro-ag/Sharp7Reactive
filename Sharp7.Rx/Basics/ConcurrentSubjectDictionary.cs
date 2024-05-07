using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Sharp7.Rx.Basics;

internal class ConcurrentSubjectDictionary<TKey, TValue> : IDisposable
{
    private readonly object dictionaryLock = new object();
    private readonly Func<TKey, TValue> valueFactory;
    private ConcurrentDictionary<TKey, SubjectWithRefCounter> dictionary;

    public ConcurrentSubjectDictionary()
    {
        dictionary = new ConcurrentDictionary<TKey, SubjectWithRefCounter>();
    }

    public ConcurrentSubjectDictionary(IEqualityComparer<TKey> comparer)
    {
        dictionary = new ConcurrentDictionary<TKey, SubjectWithRefCounter>(comparer);
    }

    public ConcurrentSubjectDictionary(TValue initialValue, IEqualityComparer<TKey> comparer)
    {
        valueFactory = _ => initialValue;
        dictionary = new ConcurrentDictionary<TKey, SubjectWithRefCounter>(comparer);
    }

    public ConcurrentSubjectDictionary(TValue initialValue)
    {
        valueFactory = _ => initialValue;
        dictionary = new ConcurrentDictionary<TKey, SubjectWithRefCounter>();
    }

    public ConcurrentSubjectDictionary(Func<TKey, TValue> valueFactory = null)
    {
        this.valueFactory = valueFactory;
        dictionary = new ConcurrentDictionary<TKey, SubjectWithRefCounter>();
    }

    public IEnumerable<TKey> ExistingKeys => dictionary.Keys;

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public DisposableItem<TValue> GetOrCreateObservable(TKey key)
    {
        lock (dictionaryLock)
        {
            var subject = dictionary.AddOrUpdate(
                key,
                k => new SubjectWithRefCounter(CreateSubject(k)),
                (_, subjectWithRefCounter) =>
                {
                    subjectWithRefCounter.IncreaseCount();
                    return subjectWithRefCounter;
                });

            return new DisposableItem<TValue>(subject.Subject.AsObservable(), () => RemoveIfNoLongerInUse(key));
        }
    }

    public bool TryGetObserver(TKey key, out IObserver<TValue> subject)
    {
        if (dictionary.TryGetValue(key, out var subjectWithRefCount))
        {
            subject = subjectWithRefCount.Subject.AsObserver();
            return true;
        }

        subject = null;
        return false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;
        if (disposing && dictionary != null)
        {
            foreach (var subjectWithRefCounter in dictionary)
                subjectWithRefCounter.Value.Subject.OnCompleted();
            dictionary.Clear();
            dictionary = null;
        }

        IsDisposed = true;
    }

    private ISubject<TValue> CreateSubject(TKey key)
    {
        if (valueFactory == null)
            return new Subject<TValue>();
        return new BehaviorSubject<TValue>(valueFactory(key));
    }

    private void RemoveIfNoLongerInUse(TKey variableName)
    {
        lock (dictionaryLock)
            if (dictionary.TryGetValue(variableName, out var subjectWithRefCount))
                if (subjectWithRefCount.DecreaseCount() < 1)
                    dictionary.TryRemove(variableName, out _);
    }

    ~ConcurrentSubjectDictionary()
    {
        Dispose(false);
    }

    class SubjectWithRefCounter
    {
        private int counter = 1;

        public SubjectWithRefCounter(ISubject<TValue> subject)
        {
            Subject = subject;
        }

        public ISubject<TValue> Subject { get; }

        public int DecreaseCount() => Interlocked.Decrement(ref counter);
        public int IncreaseCount() => Interlocked.Increment(ref counter);
    }
}
