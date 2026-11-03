namespace Erlang;

// Prepares detached token transport and real trace-notification storage before
// R/M. Admission remains synchronous at each caller's original event point.
internal sealed class PreparedOrdinarySequenceSend : IAsyncDisposable
{
    private int committed;
    private readonly ProcessContext sender;
    private readonly Term destination;
    private readonly Term message;
    internal SequenceTraceSendPreview Preview { get; }
    internal PreparedSequenceTraceTransport641? Transport641 { get; }
    internal PreparedSequenceTraceOutput? Output { get; }
    internal SequenceTraceToken? Token => Transport641?.Token;

    private PreparedOrdinarySequenceSend(
        ProcessContext sender,
        Term destination,
        Term message,
        SequenceTraceSendPreview preview,
        PreparedSequenceTraceTransport641? transport,
        PreparedSequenceTraceOutput? output
    )
    {
        this.sender = sender;
        this.destination = destination;
        this.message = message;
        Preview = preview;
        Transport641 = transport;
        Output = output;
    }

    internal static async ValueTask<PreparedOrdinarySequenceSend> PrepareAsync(
        ProcessContext sender,
        Term destination,
        Term message
    )
    {
        ProcessRuntime runtime = sender.Runtime;
        runtime.AssertSequenceTracePreparation();
        runtime.CaptureSequenceSendPreparation(sender, out var preview, out var borrow);

        return await PrepareCapturedAsync(
            sender,
            destination,
            message,
            preview,
            borrow
        ).ConfigureAwait(false);
    }

    // A caller with a distinct existing acceptance/error order captures its
    // own preview+borrow atomically at that exact R point. This method adds no
    // cancellation/alive check that could change that accepted-signal contract.
    internal static async ValueTask<PreparedOrdinarySequenceSend> PrepareCapturedAsync(
        ProcessContext sender,
        Term destination,
        Term message,
        SequenceTraceSendPreview preview,
        ProcessTermBorrow506? borrow
    )
    {
        ProcessRuntime runtime = sender.Runtime;
        runtime.AssertSequenceTracePreparation();
        using (borrow)
        {
            PreparedSequenceTraceTransport641? transport = null;
            PreparedSequenceTraceOutput? output = null;
            try
            {
                if (preview.Next is { } token)
                {
                    transport = PreparedSequenceTraceTransport641.Capture(runtime, token);
                    if ((token.Flags & SequenceTraceFlags.Send) != 0)
                        output = await runtime.SequenceTracing.PrepareOutputAsync(
                            [token.Label, token.Sender, destination, message], sender.Self
                        ).ConfigureAwait(false);
                }

                return new(
                    sender,
                    destination,
                    message,
                    preview,
                    transport,
                    output
                );
            }
            catch (Exception primary)
            {
                var failures = new List<Exception> { primary };
                try
                {
                    if (output is not null) await output.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception cleanup)
                {
                    failures.Add(cleanup);
                }
                try
                {
                    transport?.Dispose();
                }
                catch (Exception cleanup)
                {
                    failures.Add(cleanup);
                }
                if (failures.Count > 1) throw new AggregateException(failures);
                throw;
            }
        }
    }

    internal bool IsCurrent()
    {
        if (Volatile.Read(ref committed) != 0) return false;
        SequenceTracing tracing = sender.Runtime.SequenceTracing;
        if (!tracing.IsCurrentSendPreview(sender, Preview)) return false;

        return Preview.Next is not { } token || tracing.IsCurrentOutputPlanOrSuppressed(
            Output,
            token,
            SequenceTraceFlags.Send,
            sender.Self
        );
    }

    internal void Commit()
    {
        // Caller holds R across its own recipient/generation validation and
        // this exact synchronous advance/output point; no callback preparation.
        if (!IsCurrent() || Interlocked.CompareExchange(ref committed, 1, 0) != 0)
            throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
        _ = sender.Runtime.SequenceTracing.SentPrepared(
            sender,
            destination,
            message,
            Output
        );
    }

    public async ValueTask DisposeAsync()
    {
        var failures = new List<Exception>();
        try
        {
            if (Output is not null) await Output.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            Transport641?.Dispose();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
