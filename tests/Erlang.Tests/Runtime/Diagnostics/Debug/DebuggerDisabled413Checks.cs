namespace Erlang.Tests;

internal static class DebuggerDisabled413Checks
{
    internal static async Task Run()
    {
        var runtime = new ProcessRuntime();
        var foreign = new ProcessRuntime();
        ProcessContext? retained = null;
        try
        {
            PreparedBeamImage? image = null;
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                DebuggerDisabled413Access.Require(!registry.HasExport(DebuggerDisabled413Names.Module, "supported", 0));
                image = scope.Prepare(() => registry.PrepareBeam(DebuggerDisabled413Access.Beam(), new BeamDependencySelection(["erlang"])));
                DebuggerDisabled413Access.Require(image.Name == DebuggerDisabled413Names.Module
                    && image.NativePrimitiveExports.Count == DebuggerDisabled413Names.Signatures.Length
                    && new HashSet<BeamFunctionSignature>(DebuggerDisabled413Names.Signatures
                        .Select(s => new BeamFunctionSignature(s.Function, s.Arity))).SetEquals(image.NativePrimitiveExports));

                return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
                {
                    DebuggerDisabled413Access.Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                });
            }
            );
            await foreign.InitializeModulesAsync(_ => { });
            ErlangFunction captured = DebuggerDisabled413Access.Capture(runtime);
            await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
            {
                retained = await actor.Ready;
                await actor.InvokeAsync(async context =>
                {
                    foreach (var signature in DebuggerDisabled413Names.Signatures)
                    {
                        Term[] args = Enumerable.Repeat<Term>(Nil.Value, signature.Arity).ToArray();
                        try
                        {
                            Term result = await runtime.Modules.Call(
                                context,
                                DebuggerDisabled413Names.Module,
                                signature.Function,
                                args
                            );
                            DebuggerDisabled413Access.Require(signature.Function == "supported"
                                && result.Equals(Term.A(ErlangBooleanAtoms.False)));
                        }
                        catch (ErlangException error)
                        {
                            DebuggerDisabled413Access.Require(signature.Function != "supported"
                                && error.ExceptionClass == ErlangExceptionClasses.Error
                                && error.Reason.Equals(Term.A(ErlangErrorReasons.UndefinedFunction)));
                        }
                        DebuggerDisabled413Access.Require(ReferenceEquals(
                            runtime.Modules.SourceOwner(DebuggerDisabled413Names.Module, signature.Function, signature.Arity), image));
                    }

                    return true;
                });
                await using var other = new RuntimeNodeFoundationHostActor(foreign);
                await other.InvokeAsync(async context =>
                {
                    bool refused = false;
                    try
                    {
                        _ = await captured(context, []);
                    }
                    catch (ArgumentException)
                    {
                        refused = true;
                    }
                    DebuggerDisabled413Access.Require(refused);

                    return true;
                });
                await actor.InvokeAsync(async context =>
                {
                    DebuggerDisabled413Access.Cancel(context);
                    bool refused = false;
                    try
                    {
                        _ = await captured(context, []);
                    }
                    catch (OperationCanceledException)
                    {
                        refused = true;
                    }
                    DebuggerDisabled413Access.Require(refused);

                    return true;
                });
            }
            await runtime.DisposeAsync();
            bool closed = false;
            try
            {
                _ = await captured(retained!, []);
            }
            catch (ObjectDisposedException)
            {
                closed = true;
            }
            DebuggerDisabled413Access.Require(closed);
        }
        finally
        {
            try
            {
                await runtime.DisposeAsync();
            }
            finally
            {
                await foreign.DisposeAsync();
            }
        }
    }
}
