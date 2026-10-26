namespace Erlang;

internal sealed partial class SequenceTracing
{
    internal SequenceTraceToken? ConnectedSpawnSent(
        ProcessContext parent,
        Atom node,
        ReferenceTerm reference,
        Pid groupLeader,
        SequenceSpawnDescription description,
        Term creatorOptions
    )
    {
        lock (gate)
        {
            Term header = Term.Tuple(
                Term.A(SequenceTraceNames.SpawnRequest),
                reference,
                parent.Self,
                groupLeader,
                description.Mfa,
                creatorOptions
            );
            _ = Sent(parent, node, header);

            return Sent(parent, node, description.Arguments);
        }
    }

    internal SequenceTraceToken? ConnectedSpawnCreated(
        ProcessContext child,
        ReferenceTerm reference,
        Pid parent,
        Pid groupLeader,
        TupleTerm mfa,
        Term options,
        SequenceTraceToken? token
    )
    {
        if (token is null)
            return null;
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
            Received(
                child,
                header,
                headerToken,
                false
            );

            return Sent(
                child,
                parent,
                Term.Tuple(
                    Term.A(SequenceTraceNames.SpawnReply),
                    reference,
                    Term.A(ProcessSpawnRequestNames.Success),
                    child.Self
                )
            );
        }
    }

    internal SequenceTraceToken? ConnectedSpawnFailed(
        ReferenceTerm reference,
        Pid parent,
        Pid groupLeader,
        TupleTerm mfa,
        Term options,
        Term error,
        SequenceTraceToken? token
    )
    {
        if (token is null)
            return null;
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
            Output(
                runtime.NodeAtom,
                headerToken,
                SequenceTraceFlags.Receive,
                runtime.NodeAtom,
                header
            );
            var replyToken = token with { Sender = runtime.NodeAtom, LastCount = headerSerial };
            Output(
                runtime.NodeAtom,
                replyToken,
                SequenceTraceFlags.Send,
                parent,
                Term.Tuple(
                    Term.A(SequenceTraceNames.SpawnReply),
                    reference,
                    Term.A(ProcessSpawnRequestNames.Error),
                    error
                )
            );

            return replyToken;
        }
    }
}
