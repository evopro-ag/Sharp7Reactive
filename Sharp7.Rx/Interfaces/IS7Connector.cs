using System;
using System.Threading;
using System.Threading.Tasks;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Interfaces
{
    internal interface IS7Connector : IDisposable
    {
        IObservable<ConnectionState> ConnectionState { get; }
        Task InitializeAsync();

        Task<bool> Connect();
        Task Disconnect();

        Task<bool> ReadBit(Operand operand, ushort byteAddress, byte bitAdress, ushort dbNr, CancellationToken token);
        Task<byte[]> ReadBytes(Operand operand, ushort startByteAddress, ushort bytesToRead, ushort dBNr, CancellationToken token);

        Task<bool> WriteBit(Operand operand, ushort startByteAddress, byte bitAdress, bool value, ushort dbNr, CancellationToken token);
        Task<ushort> WriteBytes(Operand operand, ushort startByteAdress, byte[] data, ushort dBNr, CancellationToken token);
    }
}