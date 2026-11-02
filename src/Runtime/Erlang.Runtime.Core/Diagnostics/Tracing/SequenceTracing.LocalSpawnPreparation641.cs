namespace Erlang;

internal sealed partial class SequenceTracing
{
    internal PreparedLocalSpawnTrace641 PrepareLocalSpawnTrace641(
        ProcessContext parent,
        SequenceSpawnDescription? description,
        bool asynchronous,
        bool successReply
    )
    {
        runtime.AssertSequenceTracePreparation();
        SequenceTraceSendPreview preview;
        ProcessTermBorrow506? borrow;
        Pid groupLeader;
        lock (gate)
        {
            preview = PreviewSent(parent);
            groupLeader = parent.SnapshotGroupLeader();
            borrow = parent.SequentialTrace.BorrowPhysical();
        }
        using (borrow)
            return PrepareLocalSpawnSnapshot641(
                parent,
                preview,
                description,
                groupLeader,
                asynchronous,
                successReply,
                false
            );
    }

    internal PreparedLocalSpawnTrace641[] PrepareLocalSpawnBatch641(
        ProcessContext parent,
        SequenceSpawnDescription description,
        int count
    )
    {
        runtime.AssertSequenceTracePreparation();
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var result = new PreparedLocalSpawnTrace641[count];
        var failures = new List<Exception>(checked(count + 1));
        SequenceTraceSendPreview preview;
        ProcessTermBorrow506? borrow;
        Pid groupLeader;
        lock (gate)
        {
            preview = PreviewSent(parent);
            groupLeader = parent.SnapshotGroupLeader();
            borrow = parent.SequentialTrace.BorrowPhysical();
        }
        using (borrow)
        {
            try
            {
                for (int index = 0; index < count; index++)
                {
                    result[index] = PrepareLocalSpawnSnapshot641(
                        parent,
                        preview,
                        description,
                        groupLeader,
                        false,
                        true,
                        index != 0
                    );
                    preview = PredictFollowingLocalSpawn641(parent, preview);
                }

                return result;
            }
            catch (Exception original)
            {
                failures.Add(original);
                foreach (PreparedLocalSpawnTrace641? item in result)
                {
                    try
                    {
                        item?.Dispose();
                    }
                    catch (Exception cleanup)
                    {
                        failures.Add(cleanup);
                    }
                }
                if (failures.Count != 1) throw new AggregateException(failures);
                throw;
            }
        }
    }

    private static SequenceTraceSendPreview PredictFollowingLocalSpawn641(ProcessContext parent, SequenceTraceSendPreview previous)
    {
        if (previous.Next is not { } sent)
            return previous;
        var original = new SequenceTraceToken(
            sent.Flags,
            sent.Label,
            sent.Serial,
            sent.Sender,
            sent.LastCount
        );
        long replySerial = SequenceTraceParameters.Signed(unchecked(SequenceTraceParameters.Unsigned(sent.Serial) + 1));
        ulong clock = SequenceSpawnSerialLayout.TaggedWord(replySerial);
        SequenceTraceToken next = previous.SelfTracer ? original : original with
        {
            Sender = parent.Self,
            Serial = SequenceTraceParameters.Signed(unchecked(clock + 1)),
            LastCount = SequenceTraceParameters.Signed(clock)
        };

        return new(
            original,
            clock,
            clock,
            previous.SelfTracer,
            next
        );
    }

    internal bool IsCurrentLocalSpawnPreview641(ProcessContext parent, SequenceTraceSendPreview preview, bool predicted)
    {
        if (!predicted) return IsCurrentSendPreview(parent, preview);
        lock (gate)
        {
            SequenceTraceState state = parent.SequentialTrace;
            SequenceTraceToken? actual = state.Token, expected = preview.Original;
            bool same = actual is null ? expected is null : expected is not null
                && actual.Flags == expected.Flags && ReferenceEquals(actual.Label, expected.Label)
                && actual.Serial == expected.Serial && ReferenceEquals(actual.Sender, expected.Sender)
                && actual.LastCount == expected.LastCount;

            return same && state.Clock == preview.Clock && state.LastCount == preview.LastCount
                && Tracer().Equals(parent.Self) == preview.SelfTracer;
        }
    }

    private PreparedLocalSpawnTrace641 PrepareLocalSpawnSnapshot641(
        ProcessContext parent,
        SequenceTraceSendPreview preview,
        SequenceSpawnDescription? description,
        Pid groupLeader,
        bool asynchronous,
        bool successReply,
        bool predicted
    )
    {
        SequenceSpawnDescription? normalized = asynchronous ? description?.Asynchronous() : description;
        var result = new PreparedLocalSpawnTrace641(
            this,
            parent,
            preview,
            normalized,
            groupLeader,
            asynchronous,
            successReply,
            predicted
        );
        try
        {
            if (preview.Next is not null)
                result.Transport641 = PreparedSequenceTraceTransport641.Capture(runtime, preview.Next);
            if (preview.Next is { } request && result.Transport641 is not null && normalized is not null)
            {
                Term replyTag = normalized.ReplyTag ?? Term.A(SequenceTraceNames.SpawnReply);
                Term[] requestRoots = [request.Label, groupLeader, normalized.Mfa, normalized.Options, replyTag, normalized.Arguments];
                Term[] replyRoots = [result.Transport641.Token.Label, replyTag];
                if ((request.Flags & SequenceTraceFlags.Send) != 0)
                    result.RequestSend = PrepareOutputAsync(requestRoots, parent.Self).AsTask().GetAwaiter().GetResult();
                if ((request.Flags & SequenceTraceFlags.Receive) != 0)
                    result.RequestReceive = PrepareOutputAsync(requestRoots, parent.Self).AsTask().GetAwaiter().GetResult();
                if (successReply)
                {
                    if ((request.Flags & SequenceTraceFlags.Send) != 0)
                        result.ReplySend = PrepareOutputAsync(replyRoots, parent.Self).AsTask().GetAwaiter().GetResult();
                    if (!asynchronous && (request.Flags & SequenceTraceFlags.Receive) != 0)
                        result.ReplyReceive = PrepareOutputAsync(replyRoots, parent.Self).AsTask().GetAwaiter().GetResult();
                }
            }

            return result;
        }
        catch (Exception original)
        {
            try
            {
                result.Dispose();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }
}
