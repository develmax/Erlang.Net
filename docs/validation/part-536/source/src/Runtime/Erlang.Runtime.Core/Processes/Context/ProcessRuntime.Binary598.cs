namespace Erlang;

public sealed partial class ProcessRuntime
{
    private BinaryProgramOwner598? binaryPrograms598;
    private Task? binaryShutdown598;
    private bool binaryClosing598;

    internal BinaryProgramOwner598 BinaryPrograms598(ProcessContext context)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(NativeResource524Names.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(binaryClosing598, this);

            return binaryPrograms598 ??= new BinaryProgramOwner598(this);
        }
    }

    private Task CloseBinaryPrograms598()
    {
        lock (gate)
        {
            if (binaryShutdown598 is not null) return binaryShutdown598;
            binaryClosing598 = true;
            binaryPrograms598?.StopAdmission598();
            if (ExecutionContext.IsFlowSuppressed()) return binaryShutdown598 = Task.Run(CloseBinaryAfterRoots598);
            using (ExecutionContext.SuppressFlow()) return binaryShutdown598 = Task.Run(CloseBinaryAfterRoots598);
        }
    }

    private async Task CloseBinaryAfterRoots598()
    {
        lock (gate) { }
        await DrainOwnedTermContexts506().ConfigureAwait(false);
        BinaryProgramOwner598? selected;
        lock (gate) selected = binaryPrograms598;
        if (selected is not null) await selected.Drain598().ConfigureAwait(false);
    }
}
