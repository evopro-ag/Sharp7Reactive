using System.Buffers;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Basics;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Extensions;
using Sharp7.Rx.Interfaces;
using Sharp7.Rx.Settings;
using Sharp7.Rx.Utils;

namespace Sharp7.Rx;

public class Sharp7Plc : IPlc
{
    private static readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;

    private static readonly MethodInfo getValueMethod = typeof(Sharp7Plc).GetMethods()
        .Single(m => m.Name == nameof(GetValue) && m.GetGenericArguments().Length == 1);

    private static readonly MethodInfo createNotificationMethod = typeof(Sharp7Plc).GetMethods()
        .Single(m => m.Name == nameof(CreateNotification) && m.GetGenericArguments().Length == 1);

    private readonly ConcurrentSubjectDictionary<string, byte[]> multiVariableSubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly List<long> performanceCounter = new(1000);
    private readonly PlcConnectionSettings plcConnectionSettings;
    private readonly CacheVariableNameParser variableNameParser = new(new VariableNameParser());
    private bool disposed;
    private int initialized;

    private IDisposable notificationSubscription;
    private Sharp7Connector s7Connector;

    /// <summary>
    /// </summary>
    /// <param name="ipAddress">IP address of S7.</param>
    /// <param name="rackNumber">See <see href="https://github.com/fbarresi/Sharp7/wiki/Connection#rack-and-slot">Sharp7 wiki</see></param>
    /// <param name="cpuMpiAddress">See <see href="https://github.com/fbarresi/Sharp7/wiki/Connection#rack-and-slot">Sharp7 wiki</see></param>
    /// <param name="port">TCP port for communication</param>
    /// <param name="multiVarRequestCycleTime">
    ///     <para>
    ///         Polling interval for multi variable read from PLC.
    ///     </para>
    ///     <para>
    ///         This is the wait time between two successive reads from PLC and determines the
    ///         time resolution for all variable reads related with CreateNotification.
    ///     </para>
    ///     <para>
    ///         Default is 100 ms. The minimum supported time is 5 ms.
    ///     </para>
    /// </param>
    public Sharp7Plc(string ipAddress, int rackNumber, int cpuMpiAddress, int port = 102, TimeSpan? multiVarRequestCycleTime = null)
    {
        plcConnectionSettings = new PlcConnectionSettings {IpAddress = ipAddress, RackNumber = rackNumber, CpuMpiAddress = cpuMpiAddress, Port = port};
        s7Connector = new Sharp7Connector(plcConnectionSettings, variableNameParser);
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

    /// <summary>
    ///     Create an Observable for a given variable. Multiple notifications are automatically combined into a multi-variable subscription to
    ///     reduce network trafic and PLC workload.
    /// </summary>
    public IObservable<TValue> CreateNotification<TValue>(string variableName, TransmissionMode transmissionMode)
    {
        return Observable.Create<TValue>(observer =>
        {
            var address = ParseAndVerify(variableName, typeof(TValue));

            var disp = new CompositeDisposable();
            var disposableContainer = multiVariableSubscriptions.GetOrCreateObservable(variableName);
            disposableContainer.AddDisposableTo(disp);

            var observable =
                ConnectionState
                    // Wait for connection to be established
                    .FirstAsync(c => c == Enums.ConnectionState.Connected)
                    // Read variable with GetValue first.
                    // This will propagate any errors due to reading from invalid addresses.
                    .SelectMany(_ => GetValue<TValue>(variableName))
                    // Output results from read loop
                    .Concat(
                        disposableContainer.Observable
                            .Select(bytes => ValueConverter.ReadFromBuffer<TValue>(bytes, address))
                    );

            if (transmissionMode == TransmissionMode.OnChange)
                observable = observable.DistinctUntilChanged();

            observable.Subscribe(observer)
                .AddDisposableTo(disp);

            return disp;
        });
    }

    /// <summary>
    ///     Creates an observable of object for a variable.
    ///     The return type is automatically infered from the variable name.
    /// </summary>
    /// <returns>The return type is infered from the variable name.</returns>
    public IObservable<object> CreateNotification(string variableName, TransmissionMode transmissionMode)
    {
        var address = variableNameParser.Parse(variableName);
        var clrType = address.GetClrType();

        var genericCreateNotification = createNotificationMethod!.MakeGenericMethod(clrType);

        var genericNotification = genericCreateNotification.Invoke(this, [variableName, transmissionMode]);

        return SignatureConverter.ConvertToObjectObservable(genericNotification, clrType);
    }

    /// <summary>
    ///     Read PLC variable as generic variable.
    ///     <para>
    ///         The method will fail with a <see cref="InvalidOperationException" />, if <see cref="ConnectionState" /> is not <see cref="ConnectionState.Connected" />.
    ///     </para>
    /// </summary>
    public async Task<TValue> GetValue<TValue>(string variableName, CancellationToken token = default)
    {
        var address = ParseAndVerify(variableName, typeof(TValue));

        var data = await s7Connector.ReadBytes(address.Operand, address.Start, address.BufferLength, address.DbNo, token);
        return ValueConverter.ReadFromBuffer<TValue>(data, address);
    }

    /// <summary>
    ///     Read PLC variable as object.
    ///     The return type is automatically infered from the variable name.
    ///     <para>
    ///         The method will fail with a <see cref="InvalidOperationException" />, if <see cref="ConnectionState" /> is not <see cref="ConnectionState.Connected" />.
    ///     </para>
    /// </summary>
    /// <returns>The actual return type is infered from the variable name.</returns>
    public async Task<object> GetValue(string variableName, CancellationToken token = default)
    {
        var address = variableNameParser.Parse(variableName);
        var clrType = address.GetClrType();

        var genericGetValue = getValueMethod!.MakeGenericMethod(clrType);

        var task = genericGetValue.Invoke(this, [variableName, token]) as Task;

        await task!;
        var taskType = typeof(Task<>).MakeGenericType(clrType);
        var propertyInfo = taskType.GetProperty(nameof(Task<object>.Result));
        var result = propertyInfo!.GetValue(task);

        return result;
    }

    /// <summary>
    ///     Write value to the PLC.
    ///     <para>
    ///         The method will fail with a <see cref="InvalidOperationException" />, if <see cref="ConnectionState" /> is not <see cref="ConnectionState.Connected" />.
    ///     </para>
    /// </summary>
    public async Task SetValue<TValue>(string variableName, TValue value, CancellationToken token = default)
    {
        var address = ParseAndVerify(variableName, typeof(TValue));

        if (typeof(TValue) == typeof(bool))
        {
            // Special handling for bools, which are written on a by-bit basis. Writing a complete byte would
            // overwrite other bits within this byte.

            await s7Connector.WriteBit(address.Operand, address.Start, address.Bit!.Value, (bool) (object) value, address.DbNo, token);
        }
        else
        {
            var buffer = arrayPool.Rent(address.BufferLength);
            try
            {
                ValueConverter.WriteToBuffer(buffer, value, address);

                await s7Connector.WriteBytes(address.Operand, address.Start, buffer, address.DbNo, address.BufferLength, token);
            }
            finally
            {
                arrayPool.Return(buffer);
            }
        }
    }

    /// <summary>
    ///     Trigger PLC connection and start notification loop.
    ///     <para>
    ///         This method returns immediately and does not wait for the connection to be established.
    ///     </para>
    /// </summary>
    /// <returns>Always true</returns>
    [Obsolete($"Use {nameof(InitializeConnection)} or {nameof(TriggerConnection)}.")]
    public Task<bool> InitializeAsync()
    {
        TriggerConnection();
        return Task.FromResult(true);
    }


    /// <summary>
    ///     Initialize PLC connection and wait for connection to be established (<see cref="ConnectionState" /> is <see cref="ConnectionState.Connected" />).
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task InitializeConnection(CancellationToken token = default)
    {
        DoInitializeConnection();
        await s7Connector.ConnectionState
            .FirstAsync(c => c == Enums.ConnectionState.Connected)
            .ToTask(token);
    }

    /// <summary>
    ///     Initialize PLC and trigger connection. This method will not wait for the connection to be established.
    ///     <para>
    ///         Without an established connection, it is safe to call <see cref="CreateNotification" />, but <see cref="GetValue{TValue}" />
    ///         and <see cref="SetValue{TValue}" /> will fail.
    ///     </para>
    /// </summary>
    /// <returns></returns>
    public void TriggerConnection() => DoInitializeConnection();

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        disposed = true;

        if (disposing)
        {
            notificationSubscription?.Dispose();
            notificationSubscription = null;

            if (s7Connector != null)
            {
                s7Connector.Disconnect().Wait();
                s7Connector.Dispose();
                s7Connector = null;
            }

            multiVariableSubscriptions.Dispose();
        }
    }

    private void DoInitializeConnection()
    {
        if (Interlocked.Exchange(ref initialized, 1) == 1)
            return;

        s7Connector.InitializeAsync();

        // Triger connection.
        // The initial connection might fail. In this case a reconnect is initiated.
        // So we ignore any errors and wait for ConnectionState Connected afterward.
        _ = Task.Run(async () =>
        {
            try
            {
                await s7Connector.Connect();
            }
            catch (Exception)
            {
                // Ignore. Exception is logged in the connector
            }
        });

        StartNotificationLoop();
    }

    private async Task<Unit> GetAllValues(Sharp7Connector connector)
    {
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
        performanceCounter.Add(stopWatch.ElapsedMilliseconds);

        PrintAndResetPerformanceStatistik();

        return Unit.Default;
    }

    private VariableAddress ParseAndVerify(string variableName, Type type)
    {
        var address = variableNameParser.Parse(variableName);
        if (!address.MatchesType(type))
            throw new DataTypeMissmatchException($"Address \"{variableName}\" does not match type {type}.", type, address);

        return address;
    }

    private void PrintAndResetPerformanceStatistik()
    {
        if (performanceCounter.Count != performanceCounter.Capacity) return;

        if (Logger.IsEnabled(LogLevel.Trace))
        {
            var average = performanceCounter.Average();

            var min = performanceCounter.Min();
            var max = performanceCounter.Max();

            Logger?.LogTrace("PLC {Plc} notification perf: {Elements} calls, min {Min}, max {Max}, avg {Avg}, variables {Vars}, batch size {BatchSize}",
                             plcConnectionSettings.IpAddress,
                             performanceCounter.Capacity, min, max, average,
                             multiVariableSubscriptions.ExistingKeys.Count(),
                             MultiVarRequestMaxItems);
        }

        performanceCounter.Clear();
    }

    private void StartNotificationLoop()
    {
        if (notificationSubscription != null)
            // notification loop already running
            return;

        var subscription =
            ConnectionState
                .FirstAsync(states => states == Enums.ConnectionState.Connected)
                .SelectMany(_ => GetAllValues(s7Connector))
                .RepeatAfterDelay(MultiVarRequestCycleTime)
                .LogAndRetryAfterDelay(Logger, MultiVarRequestCycleTime, $"Error while getting batch notifications from {s7Connector}")
                .Subscribe();

        if (Interlocked.CompareExchange(ref notificationSubscription, subscription, null) != null)
            // Subscription has already been created (race condition). Dispose new subscription.
            subscription.Dispose();
    }
}
