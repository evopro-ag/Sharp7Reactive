using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Interfaces
{
    [NoReorder]
    internal interface IS7Connector : IDisposable
    {
        IObservable<ConnectionState> ConnectionState { get; }
        Task InitializeAsync();

        Task<bool> Connect();
        Task Disconnect();

        Task<byte[]> ReadBytes(Operand operand, ushort startByteAddress, ushort bytesToRead, ushort dBNr, CancellationToken token);

        Task<bool> WriteBit(Operand operand, ushort startByteAddress, byte bitAdress, bool value, ushort dbNr, CancellationToken token);
        Task<ushort> WriteBytes(Operand operand, ushort startByteAdress, byte[] data, ushort dBNr, CancellationToken token);

        Task<Dictionary<string, byte[]>> ExecuteMultiVarRequest(IReadOnlyList<string> variableNames);
    }
}