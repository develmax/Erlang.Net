namespace Erlang;

internal sealed partial class SequenceTracing
{
    // Outer caller has revalidated token/tracer/group-leader before allocating
    // the child or publishing any event. Description was normalized before R.
    internal void SpawnedPrepared(
        ProcessContext parent,
        ProcessContext child,
        PreparedLocalSpawnTrace641 prepared,
        ReferenceTerm? spawnReference
    )
    {
        lock (gate)
        {
            SequenceTraceToken? requestToken = Advance(parent);
            if (requestToken is null) return;
            if (prepared.Transport641 is null)
                throw new InvalidOperationException(SequenceTraceRootNames.Invalid);
            child.SequentialTrace.InstallPending(prepared.Transport641);
            child.SequentialTrace.Clock = SequenceTraceParameters.Unsigned(requestToken.Serial);
            child.SequentialTrace.LastCount = child.SequentialTrace.Clock;
            ReferenceTerm reference = spawnReference ?? runtime.MakeReference();
            SequenceSpawnDescription? description = prepared.Description;
            if (description is not null)
            {
                Term request = Term.Tuple(
                    Term.A(SequenceTraceNames.SpawnRequest),
                    reference,
                    parent.Self,
                    prepared.GroupLeader,
                    description.Mfa,
                    description.Options,
                    description.ReplyTag ?? Term.A(SequenceTraceNames.SpawnReply),
                    description.Arguments
                );
                OutputPrepared(
                    parent.Self,
                    requestToken,
                    SequenceTraceFlags.Send,
                    child.Self,
                    request,
                    prepared.RequestSend
                );
                OutputPrepared(
                    parent.Self,
                    requestToken,
                    SequenceTraceFlags.Receive,
                    child.Self,
                    request,
                    prepared.RequestReceive
                );
            }
            SequenceTraceToken? replyToken = Advance(child);
            if (replyToken is not null)
            {
                if (!prepared.Asynchronous)
                {
                    parent.SequentialTrace.LastCount = SequenceSpawnSerialLayout.TaggedWord(replyToken.Serial);
                    parent.SequentialTrace.Clock = parent.SequentialTrace.LastCount;
                }
                if (description is not null && prepared.SuccessReply)
                {
                    Term reply = Term.Tuple(
                        description.ReplyTag ?? Term.A(SequenceTraceNames.SpawnReply),
                        reference,
                        Term.A(SequenceTraceNames.Success),
                        child.Self
                    );
                    OutputPrepared(
                        parent.Self,
                        replyToken,
                        SequenceTraceFlags.Send,
                        parent.Self,
                        reply,
                        prepared.ReplySend
                    );
                    if (!prepared.Asynchronous)
                        OutputPrepared(
                            parent.Self,
                            replyToken,
                            SequenceTraceFlags.Receive,
                            parent.Self,
                            reply,
                            prepared.ReplyReceive
                        );
                }
            }
        }
    }
}
