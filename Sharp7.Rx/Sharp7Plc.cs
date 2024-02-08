using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Basics;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Extensions;
using Sharp7.Rx.Interfaces;
using Sharp7.Rx.Settings;

namespace Sharp7.Rx;

public class Sharp7Plc : IPlc
{
    private readonly CompositeDisposable disposables = new();
    private readonly ConcurrentSubjectDictionary<string, byte[]> multiVariableSubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly List<long> performanceCoutner = new(1000);
    private readonly PlcConnectionSettings plcConnectionSettings;
    private readonly IS7VariableNameParser varaibleNameParser = new CacheVariableNameParser(new S7VariableNameParser());
    private bool disposed;
    private Sharp7Connector s7Connector;


    /// <summary>
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="rackNumber"></param>
    /// <param name="cpuMpiAddress"></param>
    /// <param name="port"></param>
    /// <param name="multiVarRequestCycleTime">
    ///     <para>
    ///         Polling interval used to read multi variable requests from PLC.
    ///     </para>
    ///     <para>
    ///         This is the wait time between two successive reads from PLC and determines the
    ///         time resolution for all variable reads reated with CreateNotification.
    ///     </para>
    ///     <para>
    ///         Default is 100 ms. The minimum supported time is 5 ms.
    ///     </para>
    /// </param>
    public Sharp7Plc(string ipAddress, int rackNumber, int cpuMpiAddress, int port = 102, TimeSpan? multiVarRequestCycleTime = null)
    {
        plcConnectionSettings = new PlcConnectionSettings {IpAddress = ipAddress, RackNumber = rackNumber, CpuMpiAddress = cpuMpiAddress, Port = port};
        s7Connector = new Sharp7Connector(plcConnectionSettings, varaibleNameParser);
        ConnectionState = s7Connector.ConnectionState;

        if (multiVarRequestCycleTime != null)
        {
            if (multiVarRequestCycleTime < TimeSpan.FromMilliseconds(5))
                MultiVarRequestCycleTime = TimeSpan.FromMilliseconds(5);
            else
                MultiVarRequestCycleTime = multiVarRequestCycleTime.Value;
        }
    }

    public IObservable<ConnectionState> ConnectionState { get; }

    public ILogger Logger
    {
        get => s7Connector.Logger;
        set => s7Connector.Logger = value;
    }

    public TimeSpan MultiVarRequestCycleTime { get; } = TimeSpan.FromSeconds(0.1);

    public int MultiVarRequestMaxItems { get; set; } = 16;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IObservable<TValue> CreateNotification<TValue>(string variableName, TransmissionMode transmissionMode)
    {
        return Observable.Create<TValue>(observer =>
        {
            var address = varaibleNameParser.Parse(variableName);
            if (address == null) throw new ArgumentException("Input variable name is not valid", nameof(variableName));

            var disp = new CompositeDisposable();
            var disposeableContainer = multiVariableSubscriptions.GetOrCreateObservable(variableName);
            disposeableContainer.AddDisposableTo(disp);

            var observable =
                // Directly read variable first.
                // This will propagate any errors due to reading from invalid addresses.
                Observable.FromAsync(() => GetValue<TValue>(variableName))
                    .Concat(
                        disposeableContainer.Observable
                            .Select(bytes => S7ValueConverter.ReadFromBuffer<TValue>(bytes, address))
                    );

            if (transmissionMode == TransmissionMode.OnChange)
                observable = observable.DistinctUntilChanged();

            observable.Subscribe(observer)
                .AddDisposableTo(disp);

            return disp;
        });
    }

    public Task<TValue> GetValue<TValue>(string variableName)
    {
        return GetValue<TValue>(variableName, CancellationToken.None);
    }


    public Task SetValue<TValue>(string variableName, TValue value)
    {
        return SetValue(variableName, value, CancellationToken.None);
    }


    public async Task<TValue> GetValue<TValue>(string variableName, CancellationToken token)
    {
        var address = varaibleNameParser.Parse(variableName);
        if (address == null) throw new ArgumentException("Input variable name is not valid", nameof(variableName));

        var data = await s7Connector.ReadBytes(address.Operand, address.Start, address.Length, address.DbNr, token);
        return S7ValueConverter.ReadFromBuffer<TValue>(data, address);
    }

    public async Task<bool> InitializeAsync()
    {
        await s7Connector.InitializeAsync();

#pragma warning disable 4014
        Task.Run(async () =>
        {
            try
            {
                await s7Connector.Connect();
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "Error while connecting to PLC");
            }
        });
#pragma warning restore 4014

        RunNotifications(s7Connector, MultiVarRequestCycleTime)
            .AddDisposableTo(disposables);

        return true;
    }

    public async Task SetValue<TValue>(string variableName, TValue value, CancellationToken token)
    {
        var address = varaibleNameParser.Parse(variableName);
        if (address == null) throw new ArgumentException("Input variable name is not valid", "variableName");

        if (typeof(TValue) == typeof(bool))
        {
            // Special handling for bools, which are written on a by-bit basis. Writing a complete byte would
            // overwrite other bits within this byte.

            if (address.Bit == null)
                throw new InvalidOperationException("Address must have a Bit to write a bool.");

            await s7Connector.WriteBit(address.Operand, address.Start, address.Bit.Value, (bool) (object) value, address.DbNr, token);
        }
        else
        {
            // TODO: Use ArrayPool.Rent() once we drop Framwework support
            var bytes = new byte[address.BufferLength];
            S7ValueConverter.WriteToBuffer(bytes, value, address);

            await s7Connector.WriteBytes(address.Operand, address.Start, bytes, address.DbNr, token);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        disposed = true;

        if (disposing)
        {
            disposables.Dispose();

            if (s7Connector != null)
            {
                s7Connector.Disconnect().Wait();
                s7Connector.Dispose();
                s7Connector = null;
            }

            multiVariableSubscriptions.Dispose();
        }
    }

    private async Task<Unit> GetAllValues(bool connected, IS7Connector connector)
    {
        if (!connected)
            return Unit.Default;

        if (multiVariableSubscriptions.ExistingKeys.IsEmpty())
            return Unit.Default;

        var stopWatch = Stopwatch.StartNew();
        foreach (var partsOfMultiVarRequest in multiVariableSubscriptions.ExistingKeys.Buffer(MultiVarRequestMaxItems))
        {
            var multiVarRequest = await connector.ExecuteMultiVarRequest(partsOfMultiVarRequest as IReadOnlyList<string>);

            foreach (var pair in multiVarRequest)
                if (multiVariableSubscriptions.TryGetObserver(pair.Key, out var subject))
                    subject.OnNext(pair.Value);
        }

        stopWatch.Stop();
        performanceCoutner.Add(stopWatch.ElapsedMilliseconds);

        PrintAndResetPerformanceStatistik();

        return Unit.Default;
    }

    private void PrintAndResetPerformanceStatistik()
    {
        if (performanceCoutner.Count == performanceCoutner.Capacity)
        {
            var average = performanceCoutner.Average();
            var min = performanceCoutner.Min();
            var max = performanceCoutner.Max();

            Logger?.LogTrace("Performance statistic during {0} elements of plc notification. Min: {1}, Max: {2}, Average: {3}, Plc: '{4}', Number of variables: {5}, Batch size: {6}",
                             performanceCoutner.Capacity, min, max, average, plcConnectionSettings.IpAddress,
                             multiVariableSubscriptions.ExistingKeys.Count(),
                             MultiVarRequestMaxItems);
            performanceCoutner.Clear();
        }
    }

    private IDisposable RunNotifications(IS7Connector connector, TimeSpan cycle)
    {
        return ConnectionState.FirstAsync()
            .Select(states => states == Enums.ConnectionState.Connected)
            .SelectMany(connected => GetAllValues(connected, connector))
            .RepeatAfterDelay(MultiVarRequestCycleTime)
            .LogAndRetryAfterDelay(Logger, cycle, "Error while getting batch notifications from plc")
            .Subscribe();
    }

    ~Sharp7Plc()
    {
        Dispose(false);
    }
}
