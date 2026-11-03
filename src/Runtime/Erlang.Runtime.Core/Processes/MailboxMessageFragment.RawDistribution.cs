namespace Erlang;

internal sealed partial class MailboxMessageFragment
{
    internal static async ValueTask<MailboxMessageFragment> CreatePreparedRawDistributionAsync(
        ProcessRuntime runtime,
        ProcessContext? target,
        Term portableMessage,
        Term portableRaw,
        ModuleLiteralRoots801? carriedLiteral801 = null
    )
    {
        ProcessTermHeap506 heap = target?.TermHeap506 ?? new ProcessTermHeap506(runtime);
        bool ownsHeap = target is null;
        ProcessTermFrame506? frame = null;
        try
        {
            frame = heap.Enter();
            int valueSlot = frame.Reserve();
            int rawSlot = frame.Reserve();
            frame.SetPreparedWithLiteral801(valueSlot, portableMessage, carriedLiteral801);
            // Classification has no callback, state mutation or diagnostic
            // publication. Invalid raw values still import unchanged and fail
            // only at the original receive/decode point. A known-valid token
            // gets exactly its final mutable tuple, never an imported shadow.
            SequenceTraceToken? decoded = null;
            try
            {
                decoded = DistributionSequenceTraceToken.Decode(portableRaw).Token;
            }
            catch (NotSupportedException) { }
            SequenceTraceToken? token = null;
            if (decoded is null) frame.SetPreparedWithLiteral801(rawSlot, portableRaw, carriedLiteral801);
            else
            {
                TupleTerm actual = frame.PrepareMutableSequenceTokenWithLiteral814(rawSlot, decoded, carriedLiteral801);
                token = SequenceTraceToken.FromPhysical(actual);
            }

            return new(
                heap,
                frame,
                frame.Read(valueSlot),
                ownsHeap,
                token
            );
        }
        catch (Exception primary)
        {
            var failures = new List<Exception> { primary };
            try
            {
                frame?.Dispose();
            }
            catch (Exception cleanup)
            {
                failures.Add(cleanup);
            }
            try
            {
                if (ownsHeap) await heap.RetireAsync().ConfigureAwait(false);
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
