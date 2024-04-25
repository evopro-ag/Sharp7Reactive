﻿using System.Buffers;
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
    private readonly List<long> performanceCounter = new(1000);
    private readonly PlcConnectionSettings plcConnectionSettings;
    private readonly CacheVariableNameParser variableNameParser = new CacheVariableNameParser(new VariableNameParser());
    private bool disposed;
    private Sharp7Connector s7Connector;
    private static readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Shared;


    /// <summary>
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="rackNumber"></param>
    /// <param name="cpuMpiAddress"></param>
    /// <param name="port"></param>
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

    public IObservable<TValue> CreateNotification<TValue>(string variableName, TransmissionMode transmissionMode)
    {
        return Observable.Create<TValue>(observer =>
        {
            var address = ParseAndVerify(variableName, typeof(TValue));

            var disp = new CompositeDisposable();
            var disposableContainer = multiVariableSubscriptions.GetOrCreateObservable(variableName);
            disposableContainer.AddDisposableTo(disp);

            var observable =
                // Read variable with GetValue first.
                // This will propagate any errors due to reading from invalid addresses.
                Observable.FromAsync(() => GetValue<TValue>(variableName))
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
        var address = ParseAndVerify(variableName, typeof(TValue));

        var data = await s7Connector.ReadBytes(address.Operand, address.Start, address.BufferLength, address.DbNo, token);
        return ValueConverter.ReadFromBuffer<TValue>(data, address);
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

                await s7Connector.WriteBytes(address.Operand, address.Start, buffer, address.DbNo, token);
            }
            finally
            {
                ArrayPool<Byte>.Shared.Return(buffer);                
            }
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
        if (performanceCounter.Count == performanceCounter.Capacity)
        {
            var average = performanceCounter.Average();
            var min = performanceCounter.Min();
            var max = performanceCounter.Max();

            Logger?.LogTrace("PLC {Plc} notification perf: {Elements} calls, min {Min}, max {Max}, avg {Avg}, variables {Vars}, batch size {BatchSize}",
                             plcConnectionSettings.IpAddress,
                             performanceCounter.Capacity, min, max, average, 
                             multiVariableSubscriptions.ExistingKeys.Count(),
                             MultiVarRequestMaxItems);
            performanceCounter.Clear();
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
