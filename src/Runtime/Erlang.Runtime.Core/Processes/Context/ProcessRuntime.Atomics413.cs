namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly RuntimeAtomicsOwner413 atomics413 = new();
    private Task? atomicsShutdown586;
    public RuntimeAtomicsReport413 AtomicsStorage413 => atomics413.Report;
    internal RuntimeAtomicsOwner413 AtomicsOwner413 => atomics413;

    internal void RequireAtomicsActor413(ProcessContext context)
    {
        lock (gate)
        {
            if (!ReferenceEquals(context.Runtime, this)) throw new ArgumentException(AtomicsNames413.Foreign, nameof(context));
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
        }
    }

    private Task CloseAtomics413()
    {
        lock (gate)
        {
            if (atomicsShutdown586 is not null)
                return atomicsShutdown586;
            atomics413.StopAdmission586();
            if (ExecutionContext.IsFlowSuppressed())
                return atomicsShutdown586 = Task.Run(CloseAtomicsAfterRoots586);

            using (ExecutionContext.SuppressFlow())
                return atomicsShutdown586 = Task.Run(CloseAtomicsAfterRoots586);
        }
    }

    private async Task CloseAtomicsAfterRoots586()
    {
        // Join the initiating runtime transition before touching heap roots.
        // No runtime/heap gate is held while waiting for their physical users.
        lock (gate)
        {
        }
        await DrainOwnedTermContexts506().ConfigureAwait(false);
        atomics413.Close();
    }

    private Task DrainAtomics586() => CloseAtomics413();
}
