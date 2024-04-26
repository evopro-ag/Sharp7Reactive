using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Interfaces;

[NoReorder]
public interface IPlc : IDisposable
{
    IObservable<TValue> CreateNotification<TValue>(string variableName, TransmissionMode transmissionMode);
    Task SetValue<TValue>(string variableName, TValue value, CancellationToken token = default);
    Task<TValue> GetValue<TValue>(string variableName, CancellationToken token = default);
    IObservable<ConnectionState> ConnectionState { get; }

    Task<object> GetValue(string variableName, CancellationToken token = default);

    ILogger Logger { get; }
}
