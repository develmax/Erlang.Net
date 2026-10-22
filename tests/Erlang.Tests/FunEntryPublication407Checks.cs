namespace Erlang.Tests;

internal static class FunEntryPublication407Checks
{
    internal static async Task History()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        int before = FunEntryPublication407Access.FunctionEntries(runtime);
        PreparedBeamImage image = runtime.Modules.PrepareBeam(BeamStoredFunction406Access.Bytes());
        FunEntryPublication407Access.Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
        FunEntryPublication407Access.Require(FunEntryPublication407Access.FunctionEntries(runtime) == before);
        FunEntryPublication407Access.Purge(runtime, image.Name);
        FunEntryPublication407Access.Require(FunEntryPublication407Access.FunctionEntries(runtime) == before);
        FunEntryPublication407Access.PrimitiveArchive(runtime);
        await actor.InvokeAsync(async context =>
        {
            FunctionTerm restored = FunEntryPublication407Access.ImportForged(context);
            FunEntryPublication407Access.Metadata(restored, true);
            object storage = EtsFoundation405Access.Get(runtime);
            Term table = EtsFoundation405Access.Create(storage, context);
            FunctionTerm copy = GuestStoredFunction407Access.Copy(
                storage,
                context,
                table,
                restored
            );
            GuestStoredFunction407Access.Equal(restored, copy, context.Self);
            BeamStoredFunction406Access.Publish(runtime);
            FunctionTerm direct = (FunctionTerm)await runtime.Modules.Call(
                context,
                image.Name,
                BeamClosureHostNames.Make,
                Term.I(BeamClosureHostNames.Capture)
            );
            FunEntryPublication407Access.Metadata(direct, true);
            GuestStoredFunction407Access.Equal(direct, copy, context.Self);
            FunEntryPublication407Access.Require((await copy.Invoke(context, [Term.I(BeamClosureHostNames.Argument)])).Equals(Term.I(BeamClosureHostNames.Result)));
            EtsFoundation405Access.Invoke(
                storage,
                EtsFoundation405Names.Destroy,
                context,
                table
            );

            return true;
        });
    }

    internal static async Task Preflight()
    {
        foreach (bool duplicate in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime(1);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            PreparedBeamImage first = runtime.Modules.PrepareBeam(BeamStoredFunction406Access.Bytes());
            if (duplicate)
            {
                PreparedBeamImage second = runtime.Modules.PrepareBeam(BeamStoredFunction406Access.Bytes());
                FunEntryPublication407Access.Require(runtime.Modules.PublishPreparedBeam(first, second).Status == BeamPublicationStatus.Duplicated);
            }
            FunEntryPublication407Access.Require(!runtime.Modules.IsModuleLoaded(first.Name));
            await actor.InvokeAsync(async context =>
            {
                FunctionTerm restored = FunEntryPublication407Access.ImportForged(context);
                FunEntryPublication407Access.Metadata(restored, false);
                FunEntryPublication407Access.Require(runtime.Modules.PublishPreparedBeam(first).Status == BeamPublicationStatus.Published);
                FunctionTerm direct = (FunctionTerm)await runtime.Modules.Call(
                    context,
                    first.Name,
                    BeamClosureHostNames.Make,
                    Term.I(BeamClosureHostNames.Capture)
                );
                FunEntryPublication407Access.Metadata(direct, false);
                GuestStoredFunction407Access.Equal(restored, direct, context.Self);

                return true;
            });
        }
    }

    internal static async Task LockOrder()
    {
        await using var runtime = new ProcessRuntime(1);
        var invalidExternal = new ExternalFunctionIdentity(Term.A(ErlangModuleNames.Module), Term.A(FunEntryPublication407Names.Length), 0);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task lookups = Task.Run(async () =>
        {
            await start.Task;
            for (int index = 0; index < FunEntryPublication407Names.ExternalAttempts; index++)
            {
                bool rejected = false;
                try
                {
                    _ = runtime.Functions.External(runtime, invalidExternal, true);
                }
                catch (ErlangException failure)
                {
                    rejected = failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
                }
                FunEntryPublication407Access.Require(rejected);
            }
        });
        Task publication = Task.Run(async () =>
        {
            await start.Task;
            for (int index = 0; index < FunEntryPublication407Names.Iterations; index++)
            {
                PreparedBeamImage image = runtime.Modules.PrepareBeam(BeamStoredFunction406Access.Bytes());
                FunEntryPublication407Access.Require(runtime.Modules.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                FunEntryPublication407Access.Purge(runtime, image.Name);
            }
        });
        start.SetResult();
        await Task.WhenAll(lookups, publication).WaitAsync(BeamClosurePurgeHostNames.Deadline);
        FunEntryPublication407Access.PrimitiveArchive(runtime);
    }
}
