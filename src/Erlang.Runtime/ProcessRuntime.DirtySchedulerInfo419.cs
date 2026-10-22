namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal bool TryDirtySchedulerInfo419(ProcessContext context, Term query, out Term result)
    {
        result = Nil.Value;
        if (query is not Atom atom) return false;
        if (atom.Name == ProcessSchedulerNames.Identity
            && DirtyNativeThread419.Read(context) != DirtyNativeKind419.Normal)
        {
            result = Term.I(DirtyNativeSchedulerNames419.DirtySchedulerIdentity);

            return true;
        }
        if (atom.Name is not (DirtyNativeSchedulerNames419.Cpu
            or DirtyNativeSchedulerNames419.CpuOnline or DirtyNativeSchedulerNames419.Io)) return false;
        DirtyNativeOwner419 owner;
        lock (gate)
        {
            RequireEtsFoundationActor(context);
            context.Cancellation.ThrowIfCancellationRequested();
            owner = dirtyNative419 ??= new();
        }
        // Queries admit the configured physical lanes too. Counts therefore
        // never borrow normal dispatcher capacity or claim nonexistent lanes.
        owner.EnsureStarted();
        result = Term.I(DirtyNativeSchedulerNames419.WorkersPerKind);

        return true;
    }
}
