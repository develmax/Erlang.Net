namespace Erlang.Tests;

internal static class ProplistOwnCursorLifetimeHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            runtime.Modules.Register(
                ProplistOwnCursorNames.ExportModule,
                ProplistOwnCursorNames.ExportFunction,
                0,
                (_, _) => ValueTask.FromResult<Term>(Term.I(1))
            );
            var identity = new ExternalFunctionIdentity(Term.A(ProplistOwnCursorNames.ExportModule), Term.A(ProplistOwnCursorNames.ExportFunction), 0);
            FunctionTerm canonical = runtime.Functions.External(runtime, identity);
            int invoked = 0;
            var wrapper = new FunctionTerm(identity, (_, _) =>
            {
                invoked++;

                return ValueTask.FromResult<Term>(Nil.Value);
            });
            using (var scope = new ProplistOwnHashScope(runtime))
            {
                ProplistOwnCursorAssertions.Require(scope.Hash(canonical) == scope.Hash(wrapper), ProplistOwnCursorDiagnostics.Functions);
                MapTerm map = ProplistOwnCursorAssertions.WithKey(wrapper);
                Term before = await ProplistOwnCursorAssertions.RootSuffix(context, map, scope);
                runtime.Modules.DeleteModule(ProplistOwnCursorNames.ExportModule);
                runtime.Modules.Register(
                    ProplistOwnCursorNames.ExportModule,
                    ProplistOwnCursorNames.ExportFunction,
                    0,
                    (_, _) => ValueTask.FromResult<Term>(Term.I(2))
                );
                ProplistOwnCursorAssertions.Require(
                    ReferenceEquals(canonical, runtime.Functions.External(runtime, identity))
                    && scope.Hash(canonical) == scope.Hash(wrapper),
                    ProplistOwnCursorDiagnostics.Functions
                );
                Term after = await ProplistOwnCursorAssertions.RootSuffix(context, map, scope);
                ProplistOwnCursorAssertions.Require(before.Equals(after) && invoked == 0, ProplistOwnCursorDiagnostics.Functions);
            }
            var remote = new Pid(ProplistOwnCursorNames.Foreign, 9, 11);
            using var reservation = runtime.ReserveExternalTerm(remote);
            ulong firstHash;
            Term expected;
            MapTerm remoteMap = ProplistOwnCursorAssertions.WithKey(remote);
            using (var scope = new ProplistOwnHashScope(runtime))
            {
                firstHash = scope.Hash(remote);
                var alias = new Pid(remote.Node, remote.Id, remote.Creation);
                ProplistOwnCursorAssertions.Require(remote.Equals(alias) && firstHash == scope.Hash(alias), ProplistOwnCursorDiagnostics.Epochs);
                expected = await ProplistOwnCursorAssertions.RootSuffix(context, remoteMap, scope);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                ProplistOwnCursorAssertions.Require(firstHash == scope.Hash(alias), ProplistOwnCursorDiagnostics.Epochs);
            }
            using (var scope = new ProplistOwnHashScope(runtime))
            {
                ProplistOwnCursorAssertions.Require(firstHash == scope.Hash(remote), ProplistOwnCursorDiagnostics.Epochs);
                Term after = await ProplistOwnCursorAssertions.RootSuffix(context, remoteMap, scope);
                ProplistOwnCursorAssertions.Require(expected.Equals(after) && ReferenceEquals(reservation.Value, remote), ProplistOwnCursorDiagnostics.Epochs);
            }

            return Nil.Value;
        });
    }
}
