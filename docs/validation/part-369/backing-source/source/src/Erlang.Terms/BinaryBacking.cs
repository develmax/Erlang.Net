namespace Erlang;

internal sealed class BinaryBacking
{
    private readonly object gate = new();
    private byte[] buffer;
    private object? writer;
    private int apparentBytes;
    internal bool IsReference { get; }
    internal byte[] Buffer => Volatile.Read(ref buffer);

    internal BinaryBacking(byte[] owned, bool reference)
    {
        buffer = owned;
        IsReference = reference;
        apparentBytes = owned.Length;
        ObservedPayloadStorage.Binary(owned);
    }

    internal static (BinaryBacking Backing, object Writer) FreshAppend(byte[] complete)
    {
        var allocated = new byte[Math.Max(BinaryBackingParameters.MinimumAppendBytes, Growth(complete.Length))];
        complete.CopyTo(allocated, 0);
        var result = new BinaryBacking(allocated, true);
        var token = new object();
        result.writer = token;
        result.apparentBytes = complete.Length;

        return (result, token);
    }

    internal (BinaryBacking Backing, object Writer) Append(object? token, byte[] complete)
    {
        lock (gate)
        {
            if (token is null || !ReferenceEquals(writer, token))
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
