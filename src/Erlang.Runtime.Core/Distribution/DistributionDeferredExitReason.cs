namespace Erlang;

internal sealed class DistributionDeferredExitReason : IDisposable
{
    private ReadOnlyMemory<byte> payload;
    private readonly Func<bool> forbiddenGateHeld;
    private readonly Func<ReadOnlyMemory<byte>, EtfDecodedTerm> decode;
    private readonly TaskCompletionSource<EtfDecodedTerm?> completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int claimed;

    internal DistributionDeferredExitReason(
        ReadOnlyMemory<byte> payload,
        Func<bool> forbiddenGateHeld,
        Func<ReadOnlyMemory<byte>, EtfDecodedTerm> decode
    )
    {
        // Own the raw bytes; do not parse even an empty/malformed dropped reason.
        this.payload = payload.ToArray();
        this.forbiddenGateHeld = forbiddenGateHeld;
        this.decode = decode;
    }

    internal Task<EtfDecodedTerm?> Prepare(
        Func<bool> shouldDecode,
        Action<Exception> malformedReason
    )
    {
        if (forbiddenGateHeld())
            throw new InvalidOperationException(DistributionDeferredExitReasonDiagnostics.Locked);
        if (Volatile.Read(ref claimed) != 0)
            return completed.Task;
        if (Interlocked.CompareExchange(ref claimed, 1, 0) != 0)
            return completed.Task;
        bool eligible;
        try
        {
            // Caller commits native target/link drop/removal before reason decode.
            // No private lock is held while running either supplied callback.
            eligible = shouldDecode();
        }
        catch (Exception exception)
        {
            Finish(null, exception);

            return completed.Task;
        }
        if (!eligible)
        {
            Finish(null, null);

            return completed.Task;
        }
        try
        {
            if (forbiddenGateHeld())
                throw new InvalidOperationException(DistributionDeferredExitReasonDiagnostics.Locked);
            EtfDecodedTerm result = decode(payload);
            Finish(result, null);
        }
        catch (Exception exception)
        {
            if (IsMalformed(exception))
            {
                try
                {
                    // Caller closes only its captured connection generation.
                    malformedReason(exception);
                }
                catch (Exception closeFailure)
                {
                    Finish(null, new AggregateException(exception, closeFailure));

                    return completed.Task;
                }
            }
            Finish(null, exception);
        }

        return completed.Task;
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref claimed, 1, 0) != 0)
            return;
        Finish(null, null);
    }

    private void Finish(EtfDecodedTerm? result, Exception? exception)
    {
        payload = ReadOnlyMemory<byte>.Empty;
        if (exception is null)
            completed.TrySetResult(result);
        else
            completed.TrySetException(exception);
    }

    internal static bool IsMalformed(Exception exception)
        => exception is ArgumentException or EndOfStreamException or InvalidDataException or FormatException or OverflowException ||
            exception is ErlangException { Reason: Atom { Name: ErlangErrorReasons.BadArgument or ErlangErrorReasons.SystemLimit } };
}
