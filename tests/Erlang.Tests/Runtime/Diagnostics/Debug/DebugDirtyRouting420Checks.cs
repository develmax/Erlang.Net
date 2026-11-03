namespace Erlang.Tests;

internal static class DebugDirtyRouting420Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            var image = scope.Prepare(() => registry.PrepareBeam(DebugLocks419Access.Beam(), new BeamDependencySelection([DebugLocks419Names.Runtime])));

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                DirtyNative419Access.Require(image.NativePrimitiveExports.Count == 4 && registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                foreach (var entry in DebugLocks419Names.Signatures)
                    DirtyNative419Access.Require(ReferenceEquals(registry.SourceOwner(DebugLocks419Names.Module, entry.Function, entry.Arity), image));
            });
        }
        );
        await using var held = new RuntimeNodeFoundationHostActor(runtime);
        await using var clear = new RuntimeNodeFoundationHostActor(runtime);
        await using var collect = new RuntimeNodeFoundationHostActor(runtime);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Task<bool>? first = null;
        try
        {
            first = held.InvokeAsync(async context =>
         {
             await DirtyNative419Access.Schedule(
                 context,
                 DirtyNative419Names.Cpu,
                 [],
                 (_, _) =>
    {
        entered.Set();
        DirtyNative419Access.Require(release.Wait(DirtyNative419Names.Deadline));

        return Nil.Value;
    }
             );

             return true;
         });
            DirtyNative419Access.Require(entered.Wait(DirtyNative419Names.Deadline));
            Task<Term> clearWork = await clear.Invoke(context => runtime.Modules.Call(context, DebugLocks419Names.Module, DebugLocks419Names.Clear).AsTask());
            Task<Term> collectWork = await collect.Invoke(context => runtime.Modules.Call(context, DebugLocks419Names.Module, DebugLocks419Names.Collect).AsTask());
            object state = DirtyNative419Access.Snapshot(runtime);
            DirtyNative419Access.Require(!clearWork.IsCompleted && !collectWork.IsCompleted && DirtyNative419Access.Field<int>(state, DirtyNative419Names.Pending) == 2);
            release.Set();
            await first;
            DirtyNative419Access.Require((await clearWork).Equals(Term.A(DebugLocks419Names.Error)) && (await collectWork).Equals(Term.A(DebugLocks419Names.Error)));
        }
        finally
        {
            release.Set();
            if (first is not null) await first;
        }
    }
}
