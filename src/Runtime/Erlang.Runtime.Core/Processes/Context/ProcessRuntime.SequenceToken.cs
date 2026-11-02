namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void AssertSequenceTracePreparation()
    {
        if (System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(SequenceTraceRootNames.PreparationUnderRuntimeGate);
    }

    // First actor entry calls this outside R, before body/GC. A pending token
    // has real portable/native custody but does not pretend to be an allocated
    // process tuple. Only this successful import establishes physical state.
    internal void MaterializePendingSequenceTraceToken(ProcessContext context)
    {
        AssertSequenceTracePreparation();
        while (true)
        {
            PreparedSequenceTraceTransport641? expected;
            PreparedSequenceTraceTransport641? source;
            lock (gate)
            {
                expected = context.SequentialTrace.Pending;
                if (expected is null) return;
                SequenceTraceToken token = expected.Token;
                source = expected.ForkMetadata(
                    token.Flags,
                    token.Serial,
                    token.Sender,
                    token.LastCount
                );
            }
            using (source)
            {
                SequenceTraceStorage? prepared = SequenceTraceStorage.CreatePreparedWithLiteral814(context, source.Token, source.LiteralRoots814);
                try
                {
                    lock (gate)
                    {
                        if (!ReferenceEquals(expected, context.SequentialTrace.Pending)) continue;
                        ThrowIfHalted();
                        context.Cancellation.ThrowIfCancellationRequested();
                        context.SequentialTrace.InstallPrepared(prepared);
                        prepared = null;

                        return;
                    }
                }
                finally
                {
                    prepared?.Dispose();
                }
            }
        }
    }
}
