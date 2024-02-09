using JetBrains.Annotations;
using Sharp7.Rx.Enums;

namespace Sharp7.Rx.Interfaces;

[NoReorder]
internal interface IS7Connector : IDisposable
{
    IObservable<ConnectionState> ConnectionState { get; }
    Task InitializeAsync();

    Task<bool> Connect();
    Task Disconnect();

    Task<byte[]> ReadBytes(Operand operand, ushort startByteAddress, ushort bytesToRead, ushort dbNo, CancellationToken token);

    Task WriteBit(Operand operand, ushort startByteAddress, byte bitAdress, bool value, ushort dbNo, CancellationToken token);
    Task WriteBytes(Operand operand, ushort startByteAddress, byte[] data, ushort dbNo, CancellationToken token);

    Task<IReadOnlyDictionary<string, byte[]>> ExecuteMultiVarRequest(IReadOnlyList<string> variableNames);
}
