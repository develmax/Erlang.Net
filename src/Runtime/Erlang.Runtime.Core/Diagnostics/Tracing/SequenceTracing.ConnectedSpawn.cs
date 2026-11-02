namespace Erlang;

internal sealed partial class SequenceTracing
{
    internal SequenceTraceToken? ConnectedSpawnSentPrepared(
        ProcessContext parent,
        Atom node,
        ReferenceTerm reference,
        Pid groupLeader,
        SequenceSpawnDescription description,
        Term creatorOptions,
        PreparedConnectedSequenceSend prepared
    )
    {
        lock (gate)
        {
            if (!prepared.IsCurrent(parent))
                throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
            Term header = Term.Tuple(
                Term.A(SequenceTraceNames.SpawnRequest),
                reference,
                parent.Self,
                groupLeader,
                description.Mfa,
                creatorOptions
            );
            SequenceTraceToken? first = Advance(parent);
            OutputPrepared(
                parent.Self,
                first,
                SequenceTraceFlags.Send,
                node,
                header,
                prepared.Header
            );
            SequenceTraceToken? second = Advance(parent);
            OutputPrepared(
                parent.Self,
                second,
                SequenceTraceFlags.Send,
                node,
                description.Arguments,
                prepared.Arguments
            );
            if (second is null) return null;
            // Encoding runs outside R after this method. Return covered portable
            // metadata, never the live mutable parent tuple that may be retired.
            SequenceTraceToken portable = prepared.Transport641?.Token
                ?? throw new InvalidOperationException(SequenceTraceRootNames.Invalid);

            return portable with { Flags = second.Flags, Serial = second.Serial, LastCount = second.LastCount };
        }
    }

    internal SequenceTraceToken? ConnectedSpawnCreated(
        ProcessContext child,
        ReferenceTerm reference,
        Pid parent,
        Pid groupLeader,
        TupleTerm mfa,
        Term options,
        PreparedSequenceTraceTransport641? prepared641,
        PreparedSequenceTraceOutput? headerPlan,
        PreparedSequenceTraceOutput? replyPlan
    )
    {
        if (prepared641 is null)
            return null;
        SequenceTraceToken token = prepared641.Token;
        lock (gate)
        {
            var headerToken = token with
            {
                Serial = SequenceTraceParameters.Signed(unchecked(SequenceTraceParameters.Unsigned(token.Serial) - 1))
            };
            Term header = Term.Tuple(
                Term.A(SequenceTraceNames.SpawnRequest),
                reference,
                parent,
                groupLeader,
                mfa,
                options
            );
            using var inherited641 = prepared641.ForkMetadata(
                headerToken.Flags,
                headerToken.Serial,
                headerToken.Sender,
                headerToken.LastCount
            );
            child.SequentialTrace.InstallPending(inherited641);
            child.SequentialTrace.LastCount = SequenceTraceParameters.Unsigned(headerToken.Serial);
            child.SequentialTrace.Clock = Math.Max(child.SequentialTrace.Clock, child.SequentialTrace.LastCount);
            OutputPrepared(
                child.Self,
                headerToken,
                SequenceTraceFlags.Receive,
                child.Self,
                header,
                headerPlan
            );
            SequenceTraceToken? reply = Advance(child);
            OutputPrepared(
                child.Self,
                reply,
                SequenceTraceFlags.Send,
                parent,
                Term.Tuple(
                    Term.A(SequenceTraceNames.SpawnReply),
                    reference,
                    Term.A(ProcessSpawnRequestNames.Success),
                    child.Self
                ),
                replyPlan
            );

            return reply;
        }
    }

    internal SequenceTraceToken? ConnectedSpawnFailed(
        ReferenceTerm reference,
        Pid parent,
        Pid groupLeader,
        TupleTerm mfa,
        Term options,
        Term error,
        PreparedSequenceTraceTransport641? prepared641,
        PreparedSequenceTraceOutput? headerPlan,
        PreparedSequenceTraceOutput? replyPlan
    )
    {
        if (prepared641 is null)
            return null;
        SequenceTraceToken token = prepared641.Token;
        lock (gate)
        {
            long headerSerial = SequenceTraceParameters.Signed(unchecked(SequenceTraceParameters.Unsigned(token.Serial) - 1));
            var headerToken = token with { Serial = headerSerial };
            Term header = Term.Tuple(
                Term.A(SequenceTraceNames.SpawnRequest),
                reference,
                parent,
                groupLeader,
                mfa,
                options
            );
            OutputPrepared(
                runtime.NodeAtom,
                headerToken,
                SequenceTraceFlags.Receive,
                runtime.NodeAtom,
                header,
                headerPlan
            );
            var replyToken = token with { Sender = runtime.NodeAtom, LastCount = headerSerial };
            OutputPrepared(
                runtime.NodeAtom,
                replyToken,
                SequenceTraceFlags.Send,
                parent,
                Term.Tuple(
                    Term.A(SequenceTraceNames.SpawnReply),
                    reference,
                    Term.A(ProcessSpawnRequestNames.Error),
                    error
                ),
                replyPlan
            );

            return replyToken;
        }
    }
}
