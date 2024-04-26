using JetBrains.Annotations;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Interfaces;

[NoReorder]
public interface IPlc : IDisposable
{
    IObservable<ConnectionState> ConnectionState { get; }

    Task SetValue<TValue>(string variableName, TValue value, CancellationToken token = default);

    Task<TValue> GetValue<TValue>(string variableName, CancellationToken token = default);
    Task<object> GetValue(string variableName, CancellationToken token = default);

    IObservable<TValue> CreateNotification<TValue>(string variableName, TransmissionMode transmissionMode);
    IObservable<object> CreateNotification(string variableName, TransmissionMode transmissionMode);
}
