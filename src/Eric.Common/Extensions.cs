using System.Buffers.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyCart.Eric.Extensions;

public static class ArrayExtensions
{
    public static string ToBase64(this byte[] array) => Convert.ToBase64String(array);
    public static string ToUTF8String(this byte[] array) => TextUtil.UTF8NoBOM.GetString(array);
    public static string ToBase64(this ReadOnlySpan<byte> span) => Convert.ToBase64String(span);
    public static string ToUTF8String(this ReadOnlySpan<byte> span) => TextUtil.UTF8NoBOM.GetString(span);
    public static string ToBase64(this Span<byte> span) => Convert.ToBase64String(span);
    public static string ToUTF8String(this Span<byte> span) => TextUtil.UTF8NoBOM.GetString(span);
}

public static class EnumerableExtensions
{
    public static int GetSequenceHashCode<T>(this IEnumerable<T> array)
    {
        int hash = 17;
        foreach (T element in array)
        {
            if (element != null)
                hash = hash * 31 + EqualityComparer<T>.Default.GetHashCode(element);
        }
        return hash;
    }
}

public static class ObjectExtensions
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

public static class GuidExtensions
{
    public static string ToBase64(this Guid guid)
    {
        Span<byte> guidBytes = stackalloc byte[16];
        MemoryMarshal.Write(guidBytes, ref guid);
        return guidBytes.ToBase64();
    }

    public static Guid FromBase64(Span<byte> array) => MemoryMarshal.Read<Guid>(array);
}

public static class AsyncExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static CancellationTokenAwaiter GetAwaiter(this CancellationToken ct)
        => new CancellationTokenAwaiter(ct);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
    {
        public CancellationTokenAwaiter(CancellationToken cancellationToken)
            => _cancellationToken = cancellationToken;

        private readonly CancellationToken _cancellationToken;

        public object GetResult()
        {
            // this is called by compiler generated methods when the
            // task has completed. Instead of returning a result, we 
            // just throw an exception.
            if (IsCompleted)
                throw new OperationCanceledException();
            else
                throw new InvalidOperationException("The cancellation token has not yet been cancelled.");
        }

        // called by compiler generated/.net internals to check
        // if the task has completed.
        public bool IsCompleted => _cancellationToken.IsCancellationRequested;

        // The compiler will generate stuff that hooks in
        // here. We hook those methods directly into the
        // cancellation token.
        public void OnCompleted(Action continuation) => _cancellationToken.Register(continuation);
        public void UnsafeOnCompleted(Action continuation) => _cancellationToken.Register(continuation);
    }
}