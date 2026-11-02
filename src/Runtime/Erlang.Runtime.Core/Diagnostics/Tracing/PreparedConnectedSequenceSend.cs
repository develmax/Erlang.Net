namespace Erlang;

internal sealed class PreparedConnectedSequenceSend : IAsyncDisposable
{
    internal SequenceTraceSendPreview Preview { get; }
    internal PreparedSequenceTraceTransport641? Transport641 { get; }
    internal PreparedSequenceTraceOutput? Header { get; }
    internal PreparedSequenceTraceOutput? Arguments { get; }

    private PreparedConnectedSequenceSend(
        SequenceTraceSendPreview preview,
        PreparedSequenceTraceTransport641? transport,
        PreparedSequenceTraceOutput? header,
        PreparedSequenceTraceOutput? arguments
    )
    {
        Preview = preview;
        Transport641 = transport;
        Header = header;
        Arguments = arguments;
    }

    internal static async ValueTask<PreparedConnectedSequenceSend> PrepareAsync(
        ProcessContext parent,
        ReferenceTerm reference,
        Pid groupLeader,
        SequenceSpawnDescription description,
        Term creatorOptions
    )
    {
        ProcessRuntime runtime = parent.Runtime;
        runtime.AssertSequenceTracePreparation();
        SequenceTraceSendPreview preview;
        ProcessTermBorrow506? borrow;
        runtime.CaptureSequenceSendPreparation(parent, out preview, out borrow);
        using (borrow)
        {
            PreparedSequenceTraceTransport641? transport = null;
            PreparedSequenceTraceOutput? header = null, arguments = null;
            try
            {
                if (preview.Next is { } token)
                {
                    transport = PreparedSequenceTraceTransport641.Capture(runtime, token);
                    if ((token.Flags & SequenceTraceFlags.Send) != 0)
                    {
                        header = await runtime.SequenceTracing.PrepareOutputAsync(
                            [token.Label, token.Sender, reference, groupLeader, description.Mfa, creatorOptions], parent.Self
                        ).ConfigureAwait(false);
                        arguments = await runtime.SequenceTracing.PrepareOutputAsync(
                            [token.Label, token.Sender, description.Arguments], parent.Self
                        ).ConfigureAwait(false);
                    }
                }

                return new(
                    preview,
                    transport,
                    header,
                    arguments
                );
            }
            catch (Exception primary)
            {
                var failures = new List<Exception> { primary };
                try
                {
                    if (header is not null) await header.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception cleanup)
                {
                    failures.Add(cleanup);
                }
                try
                {
                    if (arguments is not null) await arguments.DisposeAsync().ConfigureAwait(false);
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

    internal bool IsCurrent(ProcessContext parent)
    {
        SequenceTracing tracing = parent.Runtime.SequenceTracing;
        if (!tracing.IsCurrentSendPreview(parent, Preview)) return false;
        if (Preview.Next is not { } token) return true;

        return tracing.IsCurrentOutputPlanOrSuppressed(
            Header,
            token,
            SequenceTraceFlags.Send,
            parent.Self
        )
            && tracing.IsCurrentOutputPlanOrSuppressed(
                Arguments,
                token,
                SequenceTraceFlags.Send,
                parent.Self
            );
    }

    public async ValueTask DisposeAsync()
    {
        var failures = new List<Exception>();
        try
        {
            if (Header is not null) await Header.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        try
        {
            if (Arguments is not null) await Arguments.DisposeAsync().ConfigureAwait(false);
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
