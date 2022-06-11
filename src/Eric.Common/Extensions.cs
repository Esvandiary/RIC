using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TinyCart.Eric.Extensions;

public static class ArrayExtensions
{
    public static int GetSequenceHashCode<T>(this T[] array)
    {
        int hash = 17;
        foreach (T element in array)
        {
            if (element != null)
                hash = hash * 31 + EqualityComparer<T>.Default.GetHashCode(element);
        }
        return hash;
    }

    public static string ToBase64(this byte[] array) => Convert.ToBase64String(array);
    public static string ToUTF8String(this byte[] array) => TextUtil.UTF8NoBOM.GetString(array);
}

public static class EnumerableExtensions
{
    public static IEnumerable<T> Yield<T>(this T item)
    {
        yield return item;
    }
}

public static class StringExtensions
{
    public static byte[] ToUTF8Bytes(this string s) => TextUtil.UTF8NoBOM.GetBytes(s);
}

public static class AsyncExtensions
{
    /// <summary>
    /// Allows a cancellation token to be awaited.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static CancellationTokenAwaiter GetAwaiter(this CancellationToken ct)
    {
        // return our special awaiter
        return new CancellationTokenAwaiter
        {
            CancellationToken = ct
        };
    }

    /// <summary>
    /// The awaiter for cancellation tokens.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
    {
        public CancellationTokenAwaiter(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        internal CancellationToken CancellationToken;

        public object GetResult()
        {
            // this is called by compiler generated methods when the
            // task has completed. Instead of returning a result, we 
            // just throw an exception.
            if (IsCompleted) throw new OperationCanceledException();
            else throw new InvalidOperationException("The cancellation token has not yet been cancelled.");
        }

        // called by compiler generated/.net internals to check
        // if the task has completed.
        public bool IsCompleted => CancellationToken.IsCancellationRequested;

        // The compiler will generate stuff that hooks in
        // here. We hook those methods directly into the
        // cancellation token.
        public void OnCompleted(Action continuation) =>
            CancellationToken.Register(continuation);
        public void UnsafeOnCompleted(Action continuation) =>
            CancellationToken.Register(continuation);
    }
}