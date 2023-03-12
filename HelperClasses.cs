using SpotifyAPI.Web;
using SpotifyAPI.Web.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace spotify_playlist_generator
{
    class WaitPaginator : SpotifyAPI.Web.IPaginator
    {
        public SpotifyAPI.Web.SimplePaginator BasePaginator { get; set; }
        public int WaitTime { get; set; }

        public WaitPaginator(int WaitTime)
        {
            this.WaitTime = WaitTime;
            this.BasePaginator = new SimplePaginator();
        }

        public IAsyncEnumerable<T> Paginate<T>(IPaginatable<T> firstPage, IAPIConnector connector, CancellationToken cancel = default)
        {
            System.Threading.Thread.Sleep(WaitTime);
            return this.BasePaginator.Paginate(firstPage, connector, cancel);
        }

        public IAsyncEnumerable<T> Paginate<T, TNext>(IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper, IAPIConnector connector, CancellationToken cancel = default)
        {
            System.Threading.Thread.Sleep(WaitTime);
            return this.BasePaginator.Paginate(firstPage, mapper, connector, cancel);
        }

        public Task<IList<T>> PaginateAll<T>(IPaginatable<T> firstPage, IAPIConnector connector, CancellationToken cancellationToken)
        {
            System.Threading.Thread.Sleep(WaitTime);
            return this.BasePaginator.PaginateAll(firstPage, connector, cancellationToken);
        }

        public Task<IList<T>> PaginateAll<T, TNext>(IPaginatable<T, TNext> firstPage, Func<TNext, IPaginatable<T, TNext>> mapper, IAPIConnector connector, CancellationToken cancellationToken)
        {
            System.Threading.Thread.Sleep(WaitTime);
            return this.BasePaginator.PaginateAll(firstPage, mapper, connector, cancellationToken);
        }
    }

    //copied from the API library for slightly more control
    //https://github.com/JohnnyCrazy/SpotifyAPI-NET/blob/master/SpotifyAPI.Web/RetryHandlers/SimpleRetryHandler.cs
    public class CustomRetryHandler : IRetryHandler
    {
        private readonly Func<TimeSpan, Task> _sleep;

        /// <summary>
        ///     Specifies after how many miliseconds should a failed request be retried.
        /// </summary>
        public TimeSpan RetryAfter { get; set; }

        /// <summary>
        ///     Maximum number of tries for one failed request.
        /// </summary>
        public int RetryTimes { get; set; }

        /// <summary>
        ///     Whether a failure of type "Too Many Requests" should use up one of the allocated retry attempts.
        /// </summary>
        public bool TooManyRequestsConsumesARetry { get; set; }

        /// <summary>
        ///     Error codes that will trigger auto-retry
        /// </summary>
        public IEnumerable<HttpStatusCode> RetryErrorCodes { get; set; }

        /// <summary>
        ///   A simple retry handler which retries a request based on status codes with a fixed sleep interval.
        ///   It also supports Retry-After headers sent by spotify. The execution will be delayed by the amount in
        ///   the Retry-After header
        /// </summary>
        /// <returns></returns>
        public CustomRetryHandler() : this(Task.Delay) { }
        public CustomRetryHandler(Func<TimeSpan, Task> sleep)
        {
            _sleep = sleep;
            RetryAfter = TimeSpan.FromMilliseconds(50);
            RetryTimes = 10;
            TooManyRequestsConsumesARetry = false;
            RetryErrorCodes = new[] {
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable
      };
        }

        private static TimeSpan? ParseTooManyRetries(IResponse response)
        {
            if (response.StatusCode != (HttpStatusCode)429)
            {
                return null;
            }
            if (
              (response.Headers.ContainsKey("Retry-After") && int.TryParse(response.Headers["Retry-After"], out int secondsToWait))
              || (response.Headers.ContainsKey("retry-after") && int.TryParse(response.Headers["retry-after"], out secondsToWait)))
            {
                //throw in a buffer
                return TimeSpan.FromSeconds(secondsToWait * 1.1);
            }

            throw new APIException("429 received, but unable to parse Retry-After Header. This should not happen!");
        }

        public Task<IResponse> HandleRetry(IRequest request, IResponse response, IRetryHandler.RetryFunc retry, CancellationToken cancellationToken)
        {
            //Ensure.ArgumentNotNull(response, nameof(response));
            //Ensure.ArgumentNotNull(retry, nameof(retry));

            return HandleRetryInternally(request, response, retry, RetryTimes);
        }

        private async Task<IResponse> HandleRetryInternally(
          IRequest request,
          IResponse response,
          IRetryHandler.RetryFunc retry,
          int triesLeft)
        {
            var secondsToWait = ParseTooManyRetries(response);
            if (secondsToWait != null && (!TooManyRequestsConsumesARetry || triesLeft > 0))
            {
                var destTime = DateTime.Now.Add(secondsToWait.Value);
                
                Console.WriteLine();

                if (secondsToWait.Value.TotalHours <= 1)
                    Console.WriteLine("Received 429 error, waiting " + secondsToWait.Value.ToHumanTimeString() + " before retry.");
                else if (DateTime.Now.Date == destTime.Date)
                    Console.WriteLine("Received 429 error, waiting until " + destTime.ToShortTimeString());
                else
                    Console.WriteLine("Received 429 error, waiting until " + destTime.ToShortDateTimeString());


                await _sleep(secondsToWait.Value).ConfigureAwait(false);
                response = await retry(request).ConfigureAwait(false);
                var newTriesLeft = TooManyRequestsConsumesARetry ? triesLeft - 1 : triesLeft;
                return await HandleRetryInternally(request, response, retry, newTriesLeft).ConfigureAwait(false);
            }

            while (RetryErrorCodes.Contains(response.StatusCode) && triesLeft > 0)
            {
                Console.WriteLine("Retrying http request...");
                await _sleep(RetryAfter).ConfigureAwait(false);
                response = await retry(request).ConfigureAwait(false);
                return await HandleRetryInternally(request, response, retry, triesLeft - 1).ConfigureAwait(false);
            }

            return response;
        }
    }

    public class ProgressPrinter
    {
        private System.Diagnostics.Stopwatch _sw;
        private double _progress = 0;
        private int _total = 0;
        private Action<string, string> _Update;

        public ProgressPrinter(int Total, Action<string, string> Update)
        {
            _total = Total;
            _Update = Update;
            this.Start();
        }

        public ProgressPrinter(int Total, Action<string> Update)
        {
            _total = Total;
            _Update = (x, y) => Update(x);
            this.Start();
        }

        private void Start()
        {
            _sw = System.Diagnostics.Stopwatch.StartNew();
            _Update("0%", "");
        }

        public bool PrintProgress()
        {
            _progress += 1;
            var percentProgress = _progress / _total;
            var ticksRemaining = ((1 - percentProgress) * _sw.Elapsed.Ticks) / percentProgress;
            var tsRemaining = double.IsInfinity(ticksRemaining) || double.IsNaN(ticksRemaining)
                ? new TimeSpan(0)
                : new TimeSpan(System.Convert.ToInt64(ticksRemaining));

            _Update(string.Format("{0:0.00%}", percentProgress), tsRemaining.ToHumanTimeString());

            return true;
        }


    }

    // https://stackoverflow.com/a/1563234
    public static class Retry
    {

        //report on nothing, return nothing
        public static void Do(
            Action action,
            int retryIntervalMilliseconds = 30 * 1000,
            int maxAttemptCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryIntervalMilliseconds, maxAttemptCount);
        }

        //report on exceptions, return nothing
        public static void Do(
            Action<Exception> action,
            int retryIntervalMilliseconds = 30 * 1000,
            int maxAttemptCount = 3)
        {
            Do<object>((x, y) =>
            {
                action(y);
                return null;
            }, retryIntervalMilliseconds, maxAttemptCount);
        }

        //report on attempts, return nothing
        public static void Do(
            Action<int> action,
            int retryIntervalMilliseconds = 30 * 1000,
            int maxAttemptCount = 3)
        {
            Do<object>((x, y) =>
            {
               action(x);
               return null;
            }, retryIntervalMilliseconds, maxAttemptCount);
        }

        //report on attempts and exceptions, return nothing
        public static void Do(
            Action<int, Exception> action,
            int retryIntervalMilliseconds = 30 * 1000,
            int maxAttemptCount = 3)
        {
            Do<object>((x, y) =>
            {
                action(x, y);
                return null;
            }, retryIntervalMilliseconds, maxAttemptCount);
        }


        //report on nothing, return T
        public static T Do<T>(
            Func<T> action,
            int retryIntervalMilliseconds = 30 * 1000,
            int maxAttemptCount = 3)
        {
            return Do((x, y) =>
            {
                return action();
            }, retryIntervalMilliseconds, maxAttemptCount);
        }

        //report on exceptions, return T
        public static T Do<T>(
            Func<Exception, T> action,
            int retryIntervalMilliseconds = 30 * 1000,
            int maxAttemptCount = 3)
        {
            return Do((x, y) =>
            {
                return action(y);
            }, retryIntervalMilliseconds, maxAttemptCount);
        }

        //report on attempts, return T
        public static T Do<T>(
            Func<int, T> action,
            int retryIntervalMilliseconds = 30 * 1000,
            int maxAttemptCount = 3)
        {
            return Do((x, y) =>
            {
                return action(x);
            }, retryIntervalMilliseconds, maxAttemptCount);
        }

        //report on attempts and exceptions, return T
        public static T Do<T>(
            Func<int, Exception, T> action,
            int retryIntervalMilliseconds = 30 * 1000,
            int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryIntervalMilliseconds);
                    }
                    return action(attempted, exceptions.LastOrDefault());
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}
