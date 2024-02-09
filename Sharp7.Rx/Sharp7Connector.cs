using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Basics;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Extensions;
using Sharp7.Rx.Interfaces;
using Sharp7.Rx.Settings;

namespace Sharp7.Rx;

internal class Sharp7Connector : IS7Connector
{
    private readonly BehaviorSubject<ConnectionState> connectionStateSubject = new(Enums.ConnectionState.Initial);
    private readonly int cpuSlotNr;

    private readonly CompositeDisposable disposables = new();
    private readonly string ipAddress;
    private readonly int port;
    private readonly int rackNr;
    private readonly LimitedConcurrencyLevelTaskScheduler scheduler = new(maxDegreeOfParallelism: 1);
    private readonly IS7VariableNameParser variableNameParser;
    private bool disposed;

    private S7Client sharp7;


    public Sharp7Connector(PlcConnectionSettings settings, IS7VariableNameParser variableNameParser)
    {
        this.variableNameParser = variableNameParser;
        ipAddress = settings.IpAddress;
        cpuSlotNr = settings.CpuMpiAddress;
        port = settings.Port;
        rackNr = settings.RackNumber;

        ReconnectDelay = TimeSpan.FromSeconds(5);
    }

    public IObservable<ConnectionState> ConnectionState => connectionStateSubject.DistinctUntilChanged().AsObservable();

    public ILogger Logger { get; set; }

    public TimeSpan ReconnectDelay { get; set; }

    private bool IsConnected => connectionStateSubject.Value == Enums.ConnectionState.Connected;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<bool> Connect()
    {
        if (sharp7 == null)
            throw new InvalidOperationException("S7 driver is not initialized.");

        try
        {
            var errorCode = await Task.Factory.StartNew(() => sharp7.ConnectTo(ipAddress, rackNr, cpuSlotNr), CancellationToken.None, TaskCreationOptions.None, scheduler);
            if (errorCode == 0)
            {
                connectionStateSubject.OnNext(Enums.ConnectionState.Connected);
                return true;
            }
            else
            {
                var errorText = EvaluateErrorCode(errorCode);
                Logger.LogError("Failed to establish initial connection: {Error}", errorText);
            }
        }
        catch (Exception ex)
        {
            connectionStateSubject.OnNext(Enums.ConnectionState.ConnectionLost);
            Logger.LogError(ex, "Failed to establish initial connection.");
        }

        return false;
    }


    public async Task Disconnect()
    {
        connectionStateSubject.OnNext(Enums.ConnectionState.DisconnectedByUser);
        await CloseConnection();
    }

    public async Task<Dictionary<string, byte[]>> ExecuteMultiVarRequest(IReadOnlyList<string> variableNames)
    {
        if (variableNames.IsEmpty())
            return new Dictionary<string, byte[]>();

        var s7MultiVar = new S7MultiVar(sharp7);

        var buffers = variableNames
            .Select(key => new {VariableName = key, Address = variableNameParser.Parse(key)})
            .Select(x =>
            {
                var buffer = new byte[x.Address.Length];
                s7MultiVar.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, x.Address.DbNr, x.Address.Start, x.Address.Length, ref buffer);
                return new {x.VariableName, Buffer = buffer};
            })
            .ToArray();

        var result = await Task.Factory.StartNew(() => s7MultiVar.Read(), CancellationToken.None, TaskCreationOptions.None, scheduler);

        EnsureSuccessOrThrow(result, $"Error in MultiVar request for variables: {string.Join(",", variableNames)}");

        return buffers.ToDictionary(arg => arg.VariableName, arg => arg.Buffer);
    }

    public Task InitializeAsync()
    {
        try
        {
            sharp7 = new S7Client();
            sharp7.PLCPort = port;

            var subscription =
                ConnectionState
                    .Where(state => state == Enums.ConnectionState.ConnectionLost)
                    .Take(1)
                    .SelectMany(_ => Reconnect())
                    .RepeatAfterDelay(ReconnectDelay)
                    .LogAndRetry(Logger, "Error while reconnecting to S7.")
                    .Subscribe();

            disposables.Add(subscription);
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "S7 driver could not be initialized");
        }

        return Task.FromResult(true);
    }

    public async Task<byte[]> ReadBytes(Operand operand, ushort startByteAddress, ushort bytesToRead, ushort dbNo, CancellationToken token)
    {
        EnsureConnectionValid();

        var buffer = new byte[bytesToRead];


        var result =
            await Task.Factory.StartNew(() => sharp7.ReadArea(operand.ToArea(), dbNo, startByteAddress, bytesToRead, S7WordLength.Byte, buffer), token, TaskCreationOptions.None, scheduler);
        token.ThrowIfCancellationRequested();

        EnsureSuccessOrThrow(result, $"Error reading {operand}{dbNo}:{startByteAddress}->{bytesToRead}");

        return buffer;
    }

    public async Task WriteBit(Operand operand, ushort startByteAddress, byte bitAdress, bool value, ushort dbNo, CancellationToken token)
    {
        EnsureConnectionValid();

        var buffer = new[] {value ? (byte) 0xff : (byte) 0};

        var offsetStart = (startByteAddress * 8) + bitAdress;

        var result = await Task.Factory.StartNew(() => sharp7.WriteArea(operand.ToArea(), dbNo, offsetStart, 1, S7WordLength.Bit, buffer), token, TaskCreationOptions.None, scheduler);
        token.ThrowIfCancellationRequested();

        EnsureSuccessOrThrow(result, $"Error writing {operand}{dbNo}:{startByteAddress} bit {bitAdress}");
    }

    public async Task WriteBytes(Operand operand, ushort startByteAddress, byte[] data, ushort dbNo, CancellationToken token)
    {
        EnsureConnectionValid();

        var result = await Task.Factory.StartNew(() => sharp7.WriteArea(operand.ToArea(), dbNo, startByteAddress, data.Length, S7WordLength.Byte, data), token, TaskCreationOptions.None, scheduler);
        token.ThrowIfCancellationRequested();

        EnsureSuccessOrThrow(result, $"Error writing {operand}{dbNo}:{startByteAddress}.{data.Length}");
    }

    private void EnsureSuccessOrThrow(int result, string message)
    {
        if (result == 0) return;

        var errorText = EvaluateErrorCode(result);
        // 0x40000: Maybe the DB is optimized or PUT/GET communication is not enabled.
        throw new S7CommunicationException($"{message} ({errorText})", result, errorText);
    }


    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                disposables.Dispose();

                if (sharp7 != null)
                {
                    sharp7.Disconnect();
                    sharp7 = null;
                }

                connectionStateSubject?.OnNext(Enums.ConnectionState.Disposed);
                connectionStateSubject?.OnCompleted();
                connectionStateSubject?.Dispose();
            }

            disposed = true;
        }
    }

    private async Task CloseConnection()
    {
        if (sharp7 == null)
            throw new InvalidOperationException("S7 driver is not initialized.");

        await Task.Factory.StartNew(() => sharp7.Disconnect(), CancellationToken.None, TaskCreationOptions.None, scheduler);
    }

    private void EnsureConnectionValid()
    {
        if (disposed)
            throw new ObjectDisposedException("S7Connector");

        if (sharp7 == null)
            throw new InvalidOperationException("S7 driver is not initialized.");

        if (!IsConnected)
            throw new InvalidOperationException("Plc is not connected");
    }

    private string EvaluateErrorCode(int errorCode)
    {
        if (errorCode == 0)
            return null;

        if (sharp7 == null)
            throw new InvalidOperationException("S7 driver is not initialized.");

        var errorText = $"0x{errorCode:X}: {sharp7.ErrorText(errorCode)}";
        Logger?.LogError($"S7 Error {errorText}");

        if (S7ErrorCodes.AssumeConnectionLost(errorCode))
            SetConnectionLostState();

        return errorText;
    }

    private async Task<bool> Reconnect()
    {
        await CloseConnection();

        return await Connect();
    }

    private void SetConnectionLostState()
    {
        if (connectionStateSubject.Value == Enums.ConnectionState.ConnectionLost) return;

        connectionStateSubject.OnNext(Enums.ConnectionState.ConnectionLost);
    }

    ~Sharp7Connector()
    {
        Dispose(false);
    }
}
