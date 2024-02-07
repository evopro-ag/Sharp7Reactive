using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Basics;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Extensions;
using Sharp7.Rx.Interfaces;
using Sharp7.Rx.Resources;
using Sharp7.Rx.Settings;

namespace Sharp7.Rx;

internal class Sharp7Connector : IS7Connector
{
    private readonly BehaviorSubject<ConnectionState> connectionStateSubject = new BehaviorSubject<ConnectionState>(Enums.ConnectionState.Initial);
    private readonly int cpuSlotNr;

    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private readonly string ipAddress;
    private readonly int port;
    private readonly int rackNr;
    private readonly LimitedConcurrencyLevelTaskScheduler scheduler = new LimitedConcurrencyLevelTaskScheduler(maxDegreeOfParallelism: 1);
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
            throw new InvalidOperationException(StringResources.StrErrorS7DriverNotInitialized);

        try
        {
            var errorCode = await Task.Factory.StartNew(() => sharp7.ConnectTo(ipAddress, rackNr, cpuSlotNr), CancellationToken.None, TaskCreationOptions.None, scheduler);
            var success = EvaluateErrorCode(errorCode);
            if (success)
            {
                connectionStateSubject.OnNext(Enums.ConnectionState.Connected);
                return true;
            }
        }
        catch (Exception ex)
        {
            // TODO:
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
        if (result != 0)
        {
            EvaluateErrorCode(result);
            throw new InvalidOperationException($"Error in MultiVar request for variables: {string.Join(",", variableNames)}");
        }

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
            Logger?.LogError(ex, StringResources.StrErrorS7DriverCouldNotBeInitialized);
        }

        return Task.FromResult(true);
    }

    public async Task<byte[]> ReadBytes(Operand operand, ushort startByteAddress, ushort bytesToRead, ushort dBNr, CancellationToken token)
    {
        EnsureConnectionValid();

        var buffer = new byte[bytesToRead];


        var result =
            await Task.Factory.StartNew(() => sharp7.ReadArea(operand.ToArea(), dBNr, startByteAddress, bytesToRead, S7WordLength.Byte, buffer), token, TaskCreationOptions.None, scheduler);
        token.ThrowIfCancellationRequested();

        if (result != 0)
        {
            EvaluateErrorCode(result);
            var errorText = sharp7.ErrorText(result);
            throw new InvalidOperationException($"Error reading {operand}{dBNr}:{startByteAddress}->{bytesToRead} ({errorText})");
        }

        return buffer;
    }

    public async Task<bool> WriteBit(Operand operand, ushort startByteAddress, byte bitAdress, bool value, ushort dbNr, CancellationToken token)
    {
        EnsureConnectionValid();

        var buffer = new[] {value ? (byte) 0xff : (byte) 0};

        var offsetStart = (startByteAddress * 8) + bitAdress;

        var result = await Task.Factory.StartNew(() => sharp7.WriteArea(operand.ToArea(), dbNr, offsetStart, 1, S7WordLength.Bit, buffer), token, TaskCreationOptions.None, scheduler);
        token.ThrowIfCancellationRequested();

        if (result != 0)
        {
            EvaluateErrorCode(result);
            return (false);
        }

        return (true);
    }

    public async Task<ushort> WriteBytes(Operand operand, ushort startByteAdress, byte[] data, ushort dBNr, CancellationToken token)
    {
        EnsureConnectionValid();

        var result = await Task.Factory.StartNew(() => sharp7.WriteArea(operand.ToArea(), dBNr, startByteAdress, data.Length, S7WordLength.Byte, data), token, TaskCreationOptions.None, scheduler);
        token.ThrowIfCancellationRequested();

        if (result != 0)
        {
            EvaluateErrorCode(result);
            return 0;
        }

        return (ushort) (data.Length);
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

                connectionStateSubject?.OnCompleted();
                connectionStateSubject?.Dispose();
            }

            disposed = true;
        }
    }

    private async Task CloseConnection()
    {
        if (sharp7 == null)
            throw new InvalidOperationException(StringResources.StrErrorS7DriverNotInitialized);

        await Task.Factory.StartNew(() => sharp7.Disconnect(), CancellationToken.None, TaskCreationOptions.None, scheduler);
    }

    private void EnsureConnectionValid()
    {
        if (disposed)
            throw new ObjectDisposedException("S7Connector");

        if (sharp7 == null)
            throw new InvalidOperationException(StringResources.StrErrorS7DriverNotInitialized);

        if (!IsConnected)
            throw new InvalidOperationException("Plc is not connected");
    }

    private bool EvaluateErrorCode(int errorCode)
    {
        if (errorCode == 0)
            return true;

        if (sharp7 == null)
            throw new InvalidOperationException(StringResources.StrErrorS7DriverNotInitialized);

        var errorText = sharp7.ErrorText(errorCode);
        Logger?.LogError($"Error Code {errorCode} {errorText}");

        if (S7ErrorCodes.AssumeConnectionLost(errorCode))
            SetConnectionLostState();

        return false;
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
