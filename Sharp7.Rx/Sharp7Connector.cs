﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Basics;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Extensions;
using Sharp7.Rx.Interfaces;
using Sharp7.Rx.Resources;
using Sharp7.Rx.Settings;

namespace Sharp7.Rx
{
    internal class Sharp7Connector : IS7Connector
    {
        private readonly IS7VariableNameParser variableNameParser;
        private readonly BehaviorSubject<ConnectionState> connectionStateSubject = new BehaviorSubject<ConnectionState>(Enums.ConnectionState.Initial);
        private ConcurrentDictionary<string, S7VariableAddress> s7VariableAddresses = new ConcurrentDictionary<string, S7VariableAddress>();

        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly LimitedConcurrencyLevelTaskScheduler scheduler = new LimitedConcurrencyLevelTaskScheduler(maxDegreeOfParallelism:1);
        private readonly string ipAddress;
        private readonly int rackNr;
        private readonly int cpuSlotNr;
	    private readonly int port;

	    private S7Client sharp7;
        private bool disposed;

		public ILogger Logger { get; set; }
        public async Task<Dictionary<string, byte[]>> ExecuteMultiVarRequest(IEnumerable<string> variableNames)
        {
            var enumerable = variableNames as string[] ?? variableNames.ToArray();

            if (enumerable.IsEmpty())
                return new Dictionary<string, byte[]>();

            var s7MultiVar = new S7MultiVar(sharp7);

            var buffers = enumerable.Select(key => new {VariableName = key, Address = s7VariableAddresses.GetOrAdd(key, s => variableNameParser.Parse(s))})
                .Select(x =>
                {
                    var buffer = new byte[x.Address.Length];
                    s7MultiVar.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, x.Address.DbNr, x.Address.Start,x.Address.Length, ref buffer);
                    return new { x.VariableName, Buffer = buffer};
                })
                .ToArray();

            var result = await Task.Factory.StartNew(() => s7MultiVar.Read(), CancellationToken.None, TaskCreationOptions.None, scheduler);
            if (result != 0)
            {
                await EvaluateErrorCode(result);
                throw new InvalidOperationException($"Error in MultiVar request for variables: {string.Join(",", enumerable)}");
            }

            return buffers.ToDictionary(arg => arg.VariableName, arg => arg.Buffer);
        }

        
        
        public Sharp7Connector(PlcConnectionSettings settings, IS7VariableNameParser variableNameParser)
		{
            this.variableNameParser = variableNameParser;
            this.ipAddress = settings.IpAddress;
            this.cpuSlotNr = settings.CpuMpiAddress;
			this.port = settings.Port;
			this.rackNr = settings.RackNumber;

			ReconnectDelay = TimeSpan.FromSeconds(5);
        }

        public TimeSpan ReconnectDelay { get; set; }

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
                var success = await EvaluateErrorCode(errorCode);
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

        public IObservable<ConnectionState> ConnectionState => connectionStateSubject.DistinctUntilChanged().AsObservable();


        public async Task Disconnect()
        {
            connectionStateSubject.OnNext(Enums.ConnectionState.DisconnectedByUser);
            await CloseConnection();
        }

        public Task InitializeAsync()
        {
            try
            {
                sharp7 = new S7Client();
	            sharp7.PLCPort = this.port;

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

        private async Task<bool> EvaluateErrorCode(int errorCode)
        {
            if (errorCode == 0)
                return true;

            if (sharp7 == null)
                throw new InvalidOperationException(StringResources.StrErrorS7DriverNotInitialized);

            var errorText = sharp7.ErrorText(errorCode);
            Logger?.LogError($"Error Code {errorCode} {errorText}");
            await SetConnectionLostState();

            return false;
        }

        private async Task<bool> Reconnect()
        {
            await CloseConnection();

            return await Connect();
        }

        private async Task SetConnectionLostState()
        {
            var state = await connectionStateSubject.FirstAsync();
            if (state == Enums.ConnectionState.ConnectionLost) return;

            connectionStateSubject.OnNext(Enums.ConnectionState.ConnectionLost);
        }

        ~Sharp7Connector()
        {
            Dispose(false);
        }

        private bool IsConnected => connectionStateSubject.Value == Enums.ConnectionState.Connected;

        public async Task<byte[]> ReadBytes(Operand operand, ushort startByteAddress, ushort bytesToRead, ushort dBNr, CancellationToken token)
        {
            EnsureConnectionValid();

            var buffer = new byte[bytesToRead];

            var area = FromOperand(operand);

            var result =
                await Task.Factory.StartNew(() => sharp7.ReadArea(area, dBNr, startByteAddress, bytesToRead, S7Consts.S7WLByte, buffer), token, TaskCreationOptions.None, scheduler);
            token.ThrowIfCancellationRequested();

            if (result != 0)
            {
                await EvaluateErrorCode(result);
                var errorText = this.sharp7.ErrorText(result);
                throw new InvalidOperationException($"Error reading {operand}{dBNr}:{startByteAddress}->{bytesToRead} ({errorText})");
            }

            var retBuffer = new byte[bytesToRead];
            Array.Copy(buffer, 0, retBuffer, 0, bytesToRead);
            return (retBuffer);
        }

        private int FromOperand(Operand operand)
        {
            switch (operand)
            {
                case Operand.Input:
                    return S7Consts.S7AreaPE;
                case Operand.Output:
                    return S7Consts.S7AreaPA;
                case Operand.Marker:
                    return S7Consts.S7AreaMK;
                case Operand.Db:
                    return S7Consts.S7AreaDB;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operand), operand, null);
            }
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

        public async Task<ushort> WriteBytes(Operand operand, ushort startByteAdress, byte[] data, ushort dBNr, CancellationToken token)
        {
            EnsureConnectionValid();

            var result = await Task.Factory.StartNew(() => sharp7.WriteArea(FromOperand(operand), dBNr, startByteAdress, data.Length, S7Consts.S7WLByte, data), token, TaskCreationOptions.None, scheduler);
            token.ThrowIfCancellationRequested();

            if (result != 0)
            {
                await EvaluateErrorCode(result);
                return (0);
            }
            return (ushort)(data.Length);
        }


        public async Task<bool> ReadBit(Operand operand, ushort byteAddress, byte bitAdress, ushort dbNr, CancellationToken token)
        {
            EnsureConnectionValid();

            var byteValue = await ReadBytes(operand, byteAddress, 1, dbNr, token);
            token.ThrowIfCancellationRequested();

            if (byteValue.Length != 1)
                throw new InvalidOperationException("Read bytes does not have length 1");

            return Convert.ToBoolean(byteValue[0] & (1 << bitAdress));
        }

        public async Task<bool> WriteBit(Operand operand, ushort startByteAddress, byte bitAdress, bool value, ushort dbNr, CancellationToken token)
        {
            EnsureConnectionValid();

            var buffer = new byte[] { value ? (byte)0xff : (byte)0 };

            var offsetStart = (startByteAddress * 8) + bitAdress;

            var result = await Task.Factory.StartNew(() => sharp7.WriteArea(FromOperand(operand), dbNr, offsetStart, 1, S7Consts.S7WLBit, buffer), token, TaskCreationOptions.None, scheduler);
            token.ThrowIfCancellationRequested();

            if (result != 0)
            {
                await EvaluateErrorCode(result);
                return (false);
            }
            return (true);
        }
    }
}