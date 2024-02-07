using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Basics;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Extensions;
using Sharp7.Rx.Interfaces;
using Sharp7.Rx.Settings;

namespace Sharp7.Rx
{
    public class Sharp7Plc : IPlc
    {
        private readonly IS7VariableNameParser varaibleNameParser = new CacheVariableNameParser(new S7VariableNameParser());
        private bool disposed;
        private IS7Connector s7Connector;
        private readonly PlcConnectionSettings plcConnectionSettings;
        private readonly ConcurrentSubjectDictionary<string, byte[]> multiVariableSubscriptions = new ConcurrentSubjectDictionary<string, byte[]>(StringComparer.InvariantCultureIgnoreCase);
        protected readonly CompositeDisposable Disposables = new CompositeDisposable();        
        private readonly List<long> performanceCoutner = new List<long>(1000);



        public Sharp7Plc(string ipAddress, int rackNumber, int cpuMpiAddress, int port = 102)
        {
            plcConnectionSettings = new PlcConnectionSettings(){IpAddress = ipAddress, RackNumber = rackNumber, CpuMpiAddress = cpuMpiAddress, Port = port};
        }

        public IObservable<ConnectionState> ConnectionState { get; private set; }
        public ILogger Logger { get; set; }

        public async Task<bool> InitializeAsync()
        {
            s7Connector = new Sharp7Connector(plcConnectionSettings, varaibleNameParser){Logger = Logger};
            ConnectionState = s7Connector.ConnectionState;

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

            RunNotifications(s7Connector, TimeSpan.FromMilliseconds(100))
                .AddDisposableTo(Disposables);
            
            return true;
        }

        public Task<TValue> GetValue<TValue>(string variableName)
        {
            return GetValue<TValue>(variableName, CancellationToken.None);
        }


        
        public async Task<TValue> GetValue<TValue>(string variableName, CancellationToken token)
        {
            var address = varaibleNameParser.Parse(variableName);
            if (address == null) throw new ArgumentException("Input variable name is not valid", nameof(variableName));

            var data = await s7Connector.ReadBytes(address.Operand, address.Start, address.Length, address.DbNr, token);
            return S7ValueConverter.ConvertToType<TValue>(data, address);
        }


        public Task SetValue<TValue>(string variableName, TValue value)
        {
            return SetValue(variableName, value, CancellationToken.None);
        }

        public async Task SetValue<TValue>(string variableName, TValue value, CancellationToken token)
        {
            var address = varaibleNameParser.Parse(variableName);
            if (address == null) throw new ArgumentException("Input variable name is not valid", "variableName");

            if (typeof(TValue) == typeof(bool))
            {
                await s7Connector.WriteBit(address.Operand, address.Start, address.Bit, (bool)(object)value, address.DbNr, token);
            }
            else if (typeof(TValue) == typeof(int) || typeof(TValue) == typeof(short))
            {
                byte[] bytes;
                if (address.Length == 4)
                    bytes = BitConverter.GetBytes((int)(object)value);
                else
                    bytes = BitConverter.GetBytes((short)(object)value);

                Array.Reverse(bytes);

                await s7Connector.WriteBytes(address.Operand, address.Start, bytes, address.DbNr, token);
            }
            else if (typeof(TValue) == typeof(byte) || typeof(TValue) == typeof(char))
            {
                var bytes = new[] { Convert.ToByte(value) };
                await s7Connector.WriteBytes(address.Operand, address.Start, bytes, address.DbNr, token);
            }
            else if (typeof(TValue) == typeof(byte[]))
            {
                await s7Connector.WriteBytes(address.Operand, address.Start, (byte[])(object)value, address.DbNr, token);
            }
            else if (typeof(TValue) == typeof(float))
            {
                var buffer = new byte[sizeof(float)];
                S7.SetRealAt(buffer, 0, (float)(object)value);
                await s7Connector.WriteBytes(address.Operand, address.Start, buffer, address.DbNr, token);
            }
            else if (typeof(TValue) == typeof(string))
            {
                var stringValue = value as string;
                if (stringValue == null) throw new ArgumentException("Value must be of type string", "value");

                var bytes = Encoding.ASCII.GetBytes(stringValue);
                Array.Resize(ref bytes, address.Length);

                if (address.Type == DbType.String)
                {
                    var bytesWritten = await s7Connector.WriteBytes(address.Operand, address.Start, new[] { (byte)address.Length, (byte)bytes.Length }, address.DbNr, token);
                    token.ThrowIfCancellationRequested();
                    if (bytesWritten == 2)
                    {
                        var stringStartAddress = (ushort)(address.Start + 2);
                        token.ThrowIfCancellationRequested();
                        await s7Connector.WriteBytes(address.Operand, stringStartAddress, bytes, address.DbNr, token);
                    }
                }
                else
                {
                    await s7Connector.WriteBytes(address.Operand, address.Start, bytes, address.DbNr, token);
                    token.ThrowIfCancellationRequested();
                }
            }
            else
            {
                throw new InvalidOperationException($"type '{typeof(TValue)}' not supported.");
            }
        }

        public IObservable<TValue> CreateNotification<TValue>(string variableName, TransmissionMode transmissionMode)
        {
            return Observable.Create<TValue>(observer =>
            {
                var address = varaibleNameParser.Parse(variableName);
                if (address == null) throw new ArgumentException("Input variable name is not valid", nameof(variableName));

                var disposables = new CompositeDisposable();
                var disposeableContainer = multiVariableSubscriptions.GetOrCreateObservable(variableName);
                disposeableContainer.AddDisposableTo(disposables);

                var observable = disposeableContainer.Observable
                    .Select(bytes => S7ValueConverter.ConvertToType<TValue>(bytes, address));

                if (transmissionMode == TransmissionMode.OnChange)
                    observable = observable.DistinctUntilChanged();

                observable.Subscribe(observer)
                    .AddDisposableTo(disposables);

                return disposables;
            });
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;

            if (disposing)
            {
                Disposables.Dispose();
                    
                if (s7Connector != null)
                {
                    s7Connector.Disconnect().Wait();
                    s7Connector.Dispose();
                    s7Connector = null;
                }

                multiVariableSubscriptions.Dispose();
            }
        }

        ~Sharp7Plc()
        {
            Dispose(false);
        }
        
        private IDisposable RunNotifications(IS7Connector connector, TimeSpan cycle)
        {
            return ConnectionState.FirstAsync()
                .Select(states => states == Enums.ConnectionState.Connected)
                .SelectMany(connected => GetAllValues(connected, connector))
                .RepeatAfterDelay(cycle)
                .LogAndRetryAfterDelay(Logger, cycle, "Error while getting batch notifications from plc")
                .Subscribe();
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

                Logger?.LogTrace("Performance statistic during {0} elements of plc notification. Min: {1}, Max: {2}, Average: {3}, Plc: '{4}', Number of variables: {5}, Batch size: {6}", performanceCoutner.Capacity, min, max, average, plcConnectionSettings.IpAddress, multiVariableSubscriptions.ExistingKeys.Count(),
                            MultiVarRequestMaxItems);
                performanceCoutner.Clear();
            }
        }

        public int MultiVarRequestMaxItems { get; set; } = 16;
    }
}
