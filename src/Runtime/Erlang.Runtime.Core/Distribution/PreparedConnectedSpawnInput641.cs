namespace Erlang;

// The decoded packet and token children are detached before R. This owner has
// no sender ProcessContext: an accepted child cannot postpone sender Done.
internal sealed class PreparedConnectedSpawnInput641 : IDisposable
{
    private RuntimeProcessPayload641? payload;
    private readonly System.Runtime.ExceptionServices.ExceptionDispatchInfo? tokenFailure;
    private readonly bool argumentsValid;
    internal PreparedSequenceTraceTransport641? Trace641 { get; }
    internal RuntimeProcessPayload641 Payload641 => payload
        ?? throw new ObjectDisposedException(nameof(PreparedConnectedSpawnInput641));
    internal TupleTerm Control => (TupleTerm)((TupleTerm)Payload641.Value641).Items[0];
    internal Term Arguments => ((TupleTerm)Payload641.Value641).Items[1];

    private PreparedConnectedSpawnInput641(RuntimeProcessPayload641 payload)
    {
        this.payload = payload;
        try
        {
            _ = Cons.Items(Arguments).ToArray();
            argumentsValid = true;
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            argumentsValid = false;
        }
        if (((Integer)Control.Items[0]).Value == DistributionControlOpcodes.SpawnRequestTrace)
        {
            Term raw = Control.Items[DistributionSpawnLayout.RequestTokenIndex];
            try
            {
                if (DistributionSequenceTraceToken.Decode(raw).Token is not null)
                    Trace641 = PreparedSequenceTraceTransport641.FromPayload641(payload, (TupleTerm)raw);
            }
            catch (NotSupportedException error)
            {
                tokenFailure = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error);
            }
        }
    }

    internal void RequireToken() => tokenFailure?.Throw();

    internal void RequireArguments()
    {
        if (!argumentsValid)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
    }

    internal static PreparedConnectedSpawnInput641 Capture(ProcessRuntime runtime, TupleTerm control, Term arguments)
    {
        runtime.AssertSequenceTracePreparation();
        RuntimeProcessPayload641 payload = RuntimeProcessPayload641.Prepare641(runtime, null, Term.Tuple(control, arguments));
        try
        {
            return new(payload);
        }
        catch (Exception original)
        {
            try
            {
                payload.Close641();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    internal PreparedConnectedSpawnOutputs PrepareOutputs(ProcessRuntime runtime)
    {
        runtime.AssertSequenceTracePreparation();
        if (Trace641 is null)
            return new(
                null,
                null,
                false,
                false
            );
        SequenceTraceToken token = Trace641.Token;
        Term[] headerRoots = [
            token.Label,
            token.Sender,
            Control.Items[DistributionSpawnLayout.ReferenceIndex],
            Control.Items[DistributionSpawnLayout.ParentIndex],
            Control.Items[DistributionSpawnLayout.GroupLeaderIndex],
            Control.Items[DistributionSpawnLayout.MfaIndex],
            Control.Items[DistributionSpawnLayout.OptionsIndex]
        ];
        Term[] replyRoots = [token.Label, Control.Items[DistributionSpawnLayout.ReferenceIndex], Control.Items[DistributionSpawnLayout.ParentIndex]];
        bool receive = (token.Flags & SequenceTraceFlags.Receive) != 0;
        bool send = (token.Flags & SequenceTraceFlags.Send) != 0;
        PreparedSequenceTraceOutput? header = receive
            ? runtime.SequenceTracing.PrepareOutputAsync(headerRoots).AsTask().GetAwaiter().GetResult() : null;
        try
        {
            PreparedSequenceTraceOutput? reply = send
                ? runtime.SequenceTracing.PrepareOutputAsync(replyRoots).AsTask().GetAwaiter().GetResult() : null;

            return new(
                header,
                reply,
                receive,
                send
            );
        }
        catch (Exception original)
        {
            try
            {
                if (header is not null) header.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }

    public void Dispose()
    {
        Exception? traceFailure = null;
        try
        {
            Trace641?.Dispose();
        }
        catch (Exception error)
        {
            traceFailure = error;
        }
        try
        {
            Interlocked.Exchange(ref payload, null)?.Close641();
        }
        catch (Exception error)
        {
            if (traceFailure is not null) throw new AggregateException(traceFailure, error);
            throw;
        }
        if (traceFailure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(traceFailure).Throw();
    }
}
