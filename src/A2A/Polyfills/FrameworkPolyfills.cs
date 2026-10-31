// Fallbacks for framework APIs that netstandard2.0 does not have. Call sites use the
// modern API shape; on newer targets the framework members apply and these are not compiled.
#if NETSTANDARD2_0
#pragma warning disable CA1822, CA1849, CA2016, EA0014, RCS1141, VSTHRD003, VSTHRD103
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace System
{
    internal static class FrameworkPolyfills
    {
        extension(ArgumentNullException)
        {
            public static void ThrowIfNull(object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (argument is null)
                {
                    throw new ArgumentNullException(paramName);
                }
            }
        }

        extension(ArgumentException)
        {
            public static void ThrowIfNullOrEmpty(string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                ArgumentNullException.ThrowIfNull(argument, paramName);
                if (argument!.Length == 0)
                {
                    throw new ArgumentException("The value cannot be an empty string.", paramName);
                }
            }

            public static void ThrowIfNullOrWhiteSpace(string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                ArgumentNullException.ThrowIfNull(argument, paramName);
                if (string.IsNullOrWhiteSpace(argument))
                {
                    throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
                }
            }
        }

        extension(ObjectDisposedException)
        {
            public static void ThrowIf(bool condition, object instance)
            {
                if (condition)
                {
                    throw new ObjectDisposedException(instance?.GetType().FullName);
                }
            }
        }

        extension(Dns)
        {
            public static Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress, CancellationToken cancellationToken) =>
                Dns.GetHostAddressesAsync(hostNameOrAddress).WaitAsync(cancellationToken);
        }

        extension(Convert)
        {
            public static string ToHexString(byte[] inArray) => BitConverter.ToString(inArray).Replace("-", string.Empty);
        }
    }
}

namespace System.Security.Cryptography
{
    internal static class CryptographyPolyfills
    {
        extension(RandomNumberGenerator)
        {
            public static byte[] GetBytes(int count)
            {
                var data = new byte[count];
                using var generator = RandomNumberGenerator.Create();
                generator.GetBytes(data);
                return data;
            }
        }

        extension(SHA256)
        {
            public static byte[] HashData(byte[] source)
            {
                using var hash = SHA256.Create();
                return hash.ComputeHash(source);
            }
        }

        extension(HMACSHA256)
        {
            public static byte[] HashData(byte[] key, byte[] source)
            {
                using var hash = new HMACSHA256(key);
                return hash.ComputeHash(source);
            }
        }
    }

    /// <summary>Constant-time comparison, which is not available before .NET Core 2.1.</summary>
    internal static class CryptographicOperations
    {
        public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }
    }
}

namespace System.Threading.Tasks
{
    internal static class TaskPolyfills
    {
        /// <summary>Waits for the task, abandoning the wait when the token is canceled.</summary>
        public static async Task WaitAsync(this Task task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                return;
            }

            var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), canceled))
            {
                if (task != await Task.WhenAny(task, canceled.Task).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            await task.ConfigureAwait(false);
        }

        /// <summary>Waits for the task, abandoning the wait when the timeout elapses.</summary>
        public static async Task WaitAsync(this Task task, TimeSpan timeout)
        {
            using var timeoutSource = new CancellationTokenSource();
            var delay = Task.Delay(timeout, timeoutSource.Token);
            if (task != await Task.WhenAny(task, delay).ConfigureAwait(false))
            {
                throw new TimeoutException();
            }

            timeoutSource.Cancel();
            await task.ConfigureAwait(false);
        }

        /// <summary>Waits for the task, abandoning the wait when the timeout elapses or the token is canceled.</summary>
        public static Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken) =>
            task.WaitAsync(cancellationToken).WaitAsync(timeout);

        /// <summary>Waits for the task, abandoning the wait when the timeout elapses.</summary>
        public static async Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout)
        {
            await ((Task)task).WaitAsync(timeout).ConfigureAwait(false);
            return await task.ConfigureAwait(false);
        }

        /// <summary>Waits for the task, abandoning the wait when the token is canceled.</summary>
        public static async Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            await ((Task)task).WaitAsync(cancellationToken).ConfigureAwait(false);
            return await task.ConfigureAwait(false);
        }
    }
}

namespace System.Threading
{
    internal static class CancellationTokenSourcePolyfills
    {
        /// <summary>Requests cancellation without running the registered callbacks on the calling thread.</summary>
        public static Task CancelAsync(this CancellationTokenSource source) => Task.Run(source.Cancel);
    }
}

namespace System.Net.Http
{
    internal static class HttpPolyfills
    {
        extension(HttpContent content)
        {
            public Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken) =>
                content.ReadAsStreamAsync().WaitAsync(cancellationToken);

            public Task<string> ReadAsStringAsync(CancellationToken cancellationToken) =>
                content.ReadAsStringAsync().WaitAsync(cancellationToken);

            public Task LoadIntoBufferAsync(long maxBufferSize, CancellationToken cancellationToken) =>
                content.LoadIntoBufferAsync(maxBufferSize).WaitAsync(cancellationToken);
        }


        extension(HttpRequestException exception)
        {
            /// <summary>The status code is not captured before .NET 5.</summary>
            public HttpStatusCode? StatusCode => null;
        }
    }
}

namespace System.Collections.Generic
{
    internal static class CollectionPolyfills
    {
        public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) =>
            dictionary.TryGetValue(key, out var value) ? value : default;
    }
}
#endif
