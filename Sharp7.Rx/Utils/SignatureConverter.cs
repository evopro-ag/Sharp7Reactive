using System.Reactive.Linq;
using System.Reflection;

namespace Sharp7.Rx.Utils;

internal static class SignatureConverter
{
    private static readonly MethodInfo convertToObjectObservableMethod =
        typeof(SignatureConverter)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(ConvertToObjectObservable) && m.GetGenericArguments().Length == 1);

    public static IObservable<object> ConvertToObjectObservable<T>(IObservable<T> obs) => obs.Select(o => (object) o);

    public static IObservable<object> ConvertToObjectObservable(object observable, Type sourceType)
    {
        var convertGeneric = convertToObjectObservableMethod.MakeGenericMethod(sourceType);

        return convertGeneric.Invoke(null, [observable]) as IObservable<object>;
    }
}
