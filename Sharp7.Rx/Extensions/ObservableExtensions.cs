using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sharp7.Rx.Resources;

namespace Sharp7.Rx.Extensions
{
	internal static class ObservableExtensions
	{
		public static IObservable<Unit> Select<TSource>(this IObservable<TSource> source, Func<TSource, Task> selector)
		{
			return source
				.Select(x => Observable.FromAsync(async () => await selector(x)))
				.Concat();
		}

		public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, Task<TResult>> selector)
		{
			return source
				.Select(x => Observable.FromAsync(async () => await selector(x)))
				.Concat();
		}

		public static IObservable<T> LogAndRetry<T>(this IObservable<T> source, ILogger logger, string message)
		{
			return source
				.Do(
					_ => { },
					ex => logger?.LogError(ex, message))
				.Retry();
		}

		public static IObservable<T> RetryAfterDelay<T>(
			this IObservable<T> source,
			TimeSpan retryDelay,
			int retryCount = -1,
			IScheduler scheduler = null)
		{
			return RedoAfterDelay(source, retryDelay, retryCount, scheduler, Observable.Retry, Observable.Retry);
		}

		public static IObservable<T> RepeatAfterDelay<T>(
			this IObservable<T> source,
			TimeSpan retryDelay,
			int repeatCount = -1,
			IScheduler scheduler = null)
		{
			return RedoAfterDelay(source, retryDelay, repeatCount, scheduler, Observable.Repeat, Observable.Repeat);
		}

		public static IObservable<T> LogAndRetryAfterDelay<T>(
			this IObservable<T> source,
			ILogger logger,
			TimeSpan retryDelay,
			string message,
			int retryCount = -1,
			IScheduler scheduler = null)
		{
			var sourceLogged =
				source
					.Do(
						_ => { },
						ex => logger?.LogError(ex, message));

			return RetryAfterDelay(sourceLogged, retryDelay, retryCount, scheduler);
		}

		private static IObservable<T> RedoAfterDelay<T>(IObservable<T> source, TimeSpan retryDelay, int retryCount, IScheduler scheduler, Func<IObservable<T>, IObservable<T>> reDo,
			Func<IObservable<T>, int, IObservable<T>> reDoCount)
		{
			scheduler = scheduler ?? TaskPoolScheduler.Default;
			var attempt = 0;

			var deferedObs = Observable.Defer(() => ((++attempt == 1) ? source : source.DelaySubscription(retryDelay, scheduler)));
			return retryCount > 0 ? reDoCount(deferedObs, retryCount) : reDo(deferedObs);
		}

		public static IObservable<T> DisposeMany<T>(this IObservable<T> source)
		{
			return Observable.Create<T>(obs =>
			{
				var serialDisposable = new SerialDisposable();
				var subscription =
					source.Subscribe(
						item =>
						{
							serialDisposable.Disposable = item as IDisposable;
							obs.OnNext(item);
						},
						obs.OnError,
						obs.OnCompleted);
				return new CompositeDisposable(serialDisposable, subscription);
			});
		}

		public static IObservable<Unit> SelectMany<T>(this IObservable<T> source, Func<T, Task> selector)
		{
			return source.SelectMany(async item =>
			{
				await selector(item);
				return Unit.Default;
			});
		}
	}
}