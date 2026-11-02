namespace Erlang;

public sealed partial class ModuleRegistry
{
    // Guest preparation waits for genuine source admission, but never acquires
    // the code writer: sealed native literal construction must remain outside M.
    internal async ValueTask<PreparedBeamImage> PrepareBeamForActor371(
        ProcessContext context,
        byte[] bytes,
        BeamDependencySelection? dependencies
    )
    {
        RequireAsyncSourceCaller();
        if (!ReferenceEquals(owningRuntime, context.Runtime))
            throw new InvalidOperationException(PreparedLoaderNames807.Invalid);
        context.Runtime.ThrowIfHalted();
        context.Runtime.RequireModuleLiteralPreparation800();
        using ModuleSourceMutationTicket ticket = await AcquireSourceMutationAsync(context).ConfigureAwait(false);
        using var source = new ModuleSourceMutationScope(ticket, false);
        context.Cancellation.ThrowIfCancellationRequested();
        ThrowIfSourceClosing();

        return dependencies is null ? PrepareBeam(bytes) : PrepareBeam(bytes, dependencies);
    }
}
