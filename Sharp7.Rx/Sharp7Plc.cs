using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Extensions;
using Sharp7.Rx.Interfaces;
using Sharp7.Rx.Resources;

namespace Sharp7.Rx
{
    public class Sharp7Plc : IPlc
    {
        private readonly string ipAddress;
        private readonly int rackNumber;
        private readonly int cpuMpiAddress;
        private readonly S7VariableNameParser varaibleNameParser;
        private bool disposed;
        private ISubject<Unit> disposingSubject = new Subject<Unit>();
        private IS7Connector s7Connector;

        public Sharp7Plc(string ipAddress, int rackNumber, int cpuMpiAddress)
        {
            this.ipAddress = ipAddress;
            this.rackNumber = rackNumber;
            this.cpuMpiAddress = cpuMpiAddress;

            varaibleNameParser = new S7VariableNameParser();
        }

        public IObservable<ConnectionState> ConnectionState { get; private set; }

        public async Task<bool> InitializeAsync()
        {
            s7Connector = new Sharp7Connector(ipAddress, rackNumber, cpuMpiAddress);
            ConnectionState = s7Connector.ConnectionState;

            await s7Connector.InitializeAsync();

#pragma warning disable 4014
            Task.Run(async () =>
            {
                try
                {
                    await s7Connector.Connect();
                }
                catch (Exception)
                {

                }
            });
#pragma warning restore 4014

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

            if (typeof(TValue) == typeof(bool))
            {
                var b = await s7Connector.ReadBit(address.Operand, address.Start, address.Bit, address.DbNr, token);
                token.ThrowIfCancellationRequested();
                return (TValue)(object)b;
            }

            if (typeof(TValue) == typeof(int))
            {
                var b = await s7Connector.ReadBytes(address.Operand, address.Start, address.Length, address.DbNr, token);
                token.ThrowIfCancellationRequested();
                if (address.Length == 2)
                    return (TValue)(object)((b[0] << 8) + b[1]);
                if (address.Length == 4)
                {
                    Array.Reverse(b);
                    return (TValue)(object)Convert.ToInt32(b);
                }


                throw new InvalidOperationException($"length must be 2 or 4 but is {address.Length}");
            }

            if (typeof(TValue) == typeof(ulong))
            {
                var b = await s7Connector.ReadBytes(address.Operand, address.Start, address.Length, address.DbNr, token);
                token.ThrowIfCancellationRequested();
                Array.Reverse(b);
                return (TValue)(object)Convert.ToUInt64(b);
            }

            if (typeof(TValue) == typeof(short))
            {
                var b = await s7Connector.ReadBytes(address.Operand, address.Start, 2, address.DbNr, token);
                token.ThrowIfCancellationRequested();
                return (TValue)(object)(short)((b[0] << 8) + b[1]);
            }

            if (typeof(TValue) == typeof(byte) || typeof(TValue) == typeof(char))
            {
                var b = await s7Connector.ReadBytes(address.Operand, address.Start, 1, address.DbNr, token);
                token.ThrowIfCancellationRequested();

                return (TValue)(object)b[0];
            }

            if (typeof(TValue) == typeof(byte[]))
            {
                var b = await s7Connector.ReadBytes(address.Operand, address.Start, address.Length, address.DbNr, token);
                token.ThrowIfCancellationRequested();
                return (TValue)(object)b;
            }

            if (typeof(TValue) == typeof(double) || typeof(TValue) == typeof(float))
            {
                var bytes = await s7Connector.ReadBytes(address.Operand, address.Start, 4, address.DbNr, token);
                token.ThrowIfCancellationRequested();
                var d = Convert.ToSingle(bytes);
                return (TValue)(object)d;
            }

            if (typeof(TValue) == typeof(string))
            {
                if (address.Type == DbType.String)
                {
                    var bytes = await s7Connector.ReadBytes(address.Operand, address.Start, 2, address.DbNr, token);
                    token.ThrowIfCancellationRequested();
                    var stringLength = bytes[1];

                    var stringStartAddress = (ushort)(address.Start + 2);
                    var stringInBytes = await s7Connector.ReadBytes(address.Operand, stringStartAddress, stringLength, address.DbNr, token);
                    token.ThrowIfCancellationRequested();
                    return (TValue)(object)Encoding.ASCII.GetString(stringInBytes);
                }
                else
                {
                    var stringInBytes = await s7Connector.ReadBytes(address.Operand, address.Start, address.Length, address.DbNr, token);
                    token.ThrowIfCancellationRequested();
                    return (TValue)(object)Encoding.ASCII.GetString(stringInBytes).Trim();
                }
            }

            throw new InvalidOperationException(string.Format("type '{0}' not supported.", typeof(TValue)));
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

        public IObservable<TValue> CreateNotification<TValue>(string variableName, TransmissionMode transmissionMode, TimeSpan cycle)
        {
            var address = varaibleNameParser.Parse(variableName);
            if (address == null) throw new ArgumentException("Input variable name is not valid", nameof(variableName));

            if (cycle < TimeSpan.FromMilliseconds(100))
                cycle = TimeSpan.FromMilliseconds(100);

            var notification = ConnectionState.FirstAsync().Select(states => states == Enums.ConnectionState.Connected)
                .SelectMany(async connected =>
                {
                    var value = default(TValue);
                    if (connected)
                    {
                        value = await GetValue<TValue>(variableName, CancellationToken.None);
                    }

                    return new
                    {
                        HasValue = connected,
                        Value = value
                    };
                })
                .RepeatAfterDelay(cycle)
                .LogAndRetryAfterDelay(s7Connector.Logger, cycle, StringResources.StrLogErrorReadingDataFromPlc)
                .TakeUntil(disposingSubject)
                .Where(union => union.HasValue)
                .Select(union => union.Value);

            if (transmissionMode == TransmissionMode.Cyclic)
                return notification;

            if (transmissionMode == TransmissionMode.OnChange)
                return notification.DistinctUntilChanged();

            throw new ArgumentException("Transmission mode can either be Cyclic or OnChange", nameof(transmissionMode));
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (disposingSubject != null)
                    {
                        disposingSubject.OnNext(Unit.Default);
                        disposingSubject.OnCompleted();
                        var disposable = (disposingSubject as IDisposable);
                        if (disposable != null) disposable.Dispose();
                        disposingSubject = null;
                    }
                    if (s7Connector != null)
                    {
                        s7Connector.Disconnect().Wait();
                        s7Connector.Dispose();
                        s7Connector = null;
                    }
                }

                disposed = true;
            }
        }

        ~Sharp7Plc()
        {
            Dispose(false);
        }
    }
}
