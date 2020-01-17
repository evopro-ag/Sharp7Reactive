using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Sharp7.Rx.Enums;
using Sharp7.Rx.Interfaces;

namespace Sharp7.Rx.Extensions
{
    public static class PlcExtensions
    {
        public static IObservable<TReturn> CreateDatatransferWithHandshake<TReturn>(this IPlc plc, string triggerAddress, string ackTriggerAddress, Func<IPlc, Task<TReturn>> readData, bool initialTransfer)
        {
            return Observable.Create<TReturn>(async observer =>
            {
                var subscriptions = new CompositeDisposable();

                var notification = plc
                    .CreateNotification<bool>(triggerAddress, TransmissionMode.OnChange, TimeSpan.Zero)
                    .Publish()
                    .RefCount();

                if (initialTransfer)
                {
                    await plc.ConnectionState.FirstAsync(state => state == ConnectionState.Connected).ToTask();
                    var initialValue = await ReadData(plc, readData);
                    observer.OnNext(initialValue);
                }

                notification
                    .Where(trigger => trigger)
                    .SelectMany(_ => ReadDataAndAcknowlodge(plc, readData, ackTriggerAddress))
                    .Subscribe(observer)
                    .AddDisposableTo(subscriptions);

                notification
                    .Where(trigger => !trigger)
                    .SelectMany(async _ =>
                        {
                            await plc.SetValue(ackTriggerAddress, false);
                            return Unit.Default;
                        })
                    .Subscribe()
                    .AddDisposableTo(subscriptions);

                return subscriptions;
            });
        }

        public static IObservable<TReturn> CreateDatatransferWithHandshake<TReturn>(this IPlc plc, string triggerAddress, string ackTriggerAddress, Func<IPlc, Task<TReturn>> readData)
        {
            return CreateDatatransferWithHandshake(plc, triggerAddress, ackTriggerAddress, readData, false);
        }

        private static async Task<TReturn> ReadData<TReturn>(IPlc plc, Func<IPlc, Task<TReturn>> receiveData)
        {
            return await receiveData(plc);
        }

        private static async Task<TReturn> ReadDataAndAcknowlodge<TReturn>(IPlc plc, Func<IPlc, Task<TReturn>> readData, string ackTriggerAddress)
        {
            try
            {
                return await ReadData(plc, readData);
            }
            finally
            {
                await plc.SetValue(ackTriggerAddress, true);
            }
        }
    }
}
