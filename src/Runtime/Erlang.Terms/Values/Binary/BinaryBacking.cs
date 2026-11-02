namespace Erlang;

internal sealed partial class BinaryBacking
{
    private readonly object gate = new();
    private byte[] buffer;
    private object? writer;
    private int apparentBytes;
    // Actual producer origin, not a transient writer-state/identity heuristic.
    // This family remains excluded until native writable append is implemented.
    private readonly bool writableOrigin;
    internal bool CanCaptureImmutableOrdinary => !writableOrigin && native524 is null;
    internal bool HasWritableOrigin => writableOrigin;

    internal WritableBinarySnapshot CaptureWritable(object? token)
    {
        lock (gate)
        {
            if (!writableOrigin || native524 is not null)
                throw new InvalidOperationException(NativeBinary524Names.ArrayRequired);

            return new(buffer.ToArray(), apparentBytes, token is not null && ReferenceEquals(writer, token));
        }
    }

    internal bool IsReference { get; }
    internal byte[] Buffer => native524 is null ? Volatile.Read(ref buffer)
        : throw new NotSupportedException(NativeBinary524Names.ArrayRequired);
    internal ReadOnlySpan<byte> ReadSpan524 => native524 is { } native ? native.Bytes524 : Buffer;
    internal int Length524 => native524?.DataLength524 ?? Buffer.Length;
    internal long ReferencedCapacity524 => native524?.AllocatedBytes524 ?? Buffer.LongLength;
    internal long ReferencedByteSize524 => native524?.ReferencedBytes524 ?? Buffer.LongLength;
    internal object? NativeIdentity524 => native524?.ResourceIdentity524;

    internal IDisposable? RetainNative524() => native524?.RetainRoot524();

    internal BinaryBacking(byte[] owned, bool reference) : this(owned, reference, false)
    {
    }

    private BinaryBacking(byte[] owned, bool reference, bool writableOrigin)
    {
        this.writableOrigin = writableOrigin;
        buffer = owned;
        IsReference = reference;
        apparentBytes = owned.Length;
        ObservedPayloadStorage.Binary(owned);
    }

    internal static (BinaryBacking Backing, object Writer) InitializeWritable(int initialBytes)
    {
        if (initialBytes < 0 || initialBytes > Array.MaxLength)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);
        var result = new BinaryBacking(new byte[initialBytes], true, true);
        var token = new object();
        result.writer = token;
        result.apparentBytes = 0;

        return (result, token);
    }

    internal static (BinaryBacking Backing, object Writer) FreshAppend(byte[] complete)
    {
        var allocated = new byte[Math.Max(BinaryBackingParameters.MinimumAppendBytes, Growth(complete.Length))];
        complete.CopyTo(allocated, 0);
        var result = new BinaryBacking(allocated, true, true);
        var token = new object();
        result.writer = token;
        result.apparentBytes = complete.Length;

        return (result, token);
    }

    internal (BinaryBacking Backing, object Writer) Append(object? token, byte[] complete)
    {
        lock (gate)
        {
            if (native524 is not null || token is null || !ReferenceEquals(writer, token))
                return FreshAppend(complete);
            if (complete.Length > buffer.Length)
            {
                var replacement = new byte[Growth(complete.Length)];
                complete.CopyTo(replacement, 0);
                ObservedPayloadStorage.Binary(replacement);
                Volatile.Write(ref buffer, replacement);
            }
            else
                complete.CopyTo(buffer, 0);
            apparentBytes = complete.Length;
            var next = new object();
            writer = next;

            return (this, next);
        }
    }

    internal void Pin()
    {
        if (native524 is INativeWritableBinaryStorage writable)
        {
            writable.PinWritable();

            return;
        }
        lock (gate)
        {
            if (writer is null)
                return;
            writer = null;
            if (buffer.Length - apparentBytes >= BinaryBackingParameters.PinMinimumSpareBytes)
            {
                var replacement = buffer.AsSpan(0, apparentBytes).ToArray();
                ObservedPayloadStorage.Binary(replacement);
                Volatile.Write(ref buffer, replacement);
            }
        }
    }

    private static int Growth(int bytes)
    {
        double desired = bytes <= BinaryBackingParameters.LargeGrowthThreshold
            ? (double)bytes * 2
            : bytes * BinaryBackingParameters.LargeGrowthFactor;
        if (desired > Array.MaxLength)
            throw new ErlangException(ErlangErrorReasons.SystemLimit);

        return (int)desired;
    }
}
