using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Interfaces
{
    [NoReorder]
    public interface IPlc : IDisposable
    {
        IObservable<TValue> CreateNotification<TValue>(string variableName, TransmissionMode transmissionMode);
        Task SetValue<TValue>(string variableName, TValue value);
        Task<TValue> GetValue<TValue>(string variableName);
		IObservable<ConnectionState> ConnectionState { get; }
        ILogger Logger { get; }
    }
}
