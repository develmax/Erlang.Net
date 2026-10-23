namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly RuntimeAtomicsOwner413 atomics413 = new();
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

    private void CloseAtomics413() => atomics413.Close();
}
