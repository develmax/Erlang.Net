namespace Erlang.Tests;

internal static class DebugLocks419Checks
{
    internal static async Task Source()
    {
        await using var runtime = new ProcessRuntime();
        PreparedBeamImage? image = null;
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            DebugLocks419Access.Require(!registry.HasExport(DebugLocks419Names.Module, DebugLocks419Names.Clear, 0));
            image = scope.Prepare(() => registry.PrepareBeam(DebugLocks419Access.Beam(), new BeamDependencySelection([DebugLocks419Names.Runtime])));
            DebugLocks419Access.Require(image.Name == DebugLocks419Names.Module && image.NativePrimitiveExports.Count == DebugLocks419Names.Signatures.Length);
            foreach (var s in DebugLocks419Names.Signatures) DebugLocks419Access.Require(image.NativePrimitiveExports.Contains(new BeamFunctionSignature(s.Function, s.Arity)));

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                DebugLocks419Access.Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                var exports = registry.Exports.Where(e => e.Module == DebugLocks419Names.Module).ToArray();
                DebugLocks419Access.Require(exports.Length == 6);
                foreach (var e in exports) DebugLocks419Access.Require(ReferenceEquals(registry.SourceOwner(e.Module, e.Function, e.Arity), image));
            });
        }
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term profile = await runtime.Modules.Call(
                context,
                DebugLocks419Names.Runtime,
                DebugLocks419Names.SystemInfo,
                Term.A(DebugLocks419Names.LockCounting)
            );
            DebugLocks419Access.Require(profile.Equals(Term.A(DebugLocks419Names.False)));
            foreach (var s in DebugLocks419Names.Signatures)
            {
                Term[] args = Enumerable.Repeat<Term>(Nil.Value, s.Arity).ToArray();
                bool badarg = false;
                try
                {
                    Term value = await runtime.Modules.Call(
                        context,
                        DebugLocks419Names.Module,
                        s.Function,
                        args
                    );
                    DebugLocks419Access.Require(s.Function != DebugLocks419Names.Control && value.Equals(Term.A(DebugLocks419Names.Error)));
                }
                catch (ErlangException error)
                {
                    if (error.StackTraceTerm is Cons { Head: TupleTerm { Items.Count: 4 } frame }) badarg = s.Function == DebugLocks419Names.Control && error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)) && frame.Items[0].Equals(Term.A(DebugLocks419Names.Module)) && frame.Items[1].Equals(Term.A(s.Function)) && frame.Items[2].Equals(Cons.From(args));
                    DebugLocks419Access.Require(badarg);
                }
            }

            return true;
        });
    }

    internal static async Task Owner()
    {
        var runtime = new ProcessRuntime();
        await using var foreign = new ProcessRuntime();
        ProcessContext? saved = null;
        try
        {
            await runtime.InitializeModulesAsync(_ => { });
            await foreign.InitializeModulesAsync(_ => { });
            ErlangFunction body = DebugLocks419Access.Capture(runtime, DebugLocks419Names.Collect, 0);
            await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
            {
                saved = await actor.Ready;
                await using var other = new RuntimeNodeFoundationHostActor(foreign);
                await other.InvokeAsync(async context =>
            {
                bool refused = false;
                try
                {
                    _ = await body(context, []);
                }
                catch (ArgumentException)
                {
                    refused = true;
                }
                DebugLocks419Access.Require(refused);

                return true;
            });
                await actor.InvokeAsync(async context =>
            {
                DebugLocks419Access.Cancel(context);
                bool refused = false;
                try
                {
                    _ = await body(context, []);
                }
                catch (OperationCanceledException)
                {
                    refused = true;
                }
                DebugLocks419Access.Require(refused);

                return true;
            });
            }
            await runtime.DisposeAsync();
            bool closed = false;
            try
            {
                _ = await body(saved!, []);
            }
            catch (ObjectDisposedException)
            {
                closed = true;
            }
            DebugLocks419Access.Require(closed);
        }
        finally
        {
            await runtime.DisposeAsync();
        }
    }
}
