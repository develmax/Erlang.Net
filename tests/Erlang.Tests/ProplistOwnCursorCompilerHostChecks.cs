using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProplistOwnCursorCompilerHostChecks
{
    internal static async Task Run()
    {
        using var compiled = GeneratedModuleCompiler.Compile(ProplistOwnCursorNames.Source, ProplistOwnCursorNames.SourceFile);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            using var reservation = runtime.ReserveExternalTerm(new Pid(ProplistOwnCursorNames.Foreign, 5, 17));
            MapTerm map = ProplistOwnCursorAssertions.WithKey(reservation.Value);
            using var scope = new ProplistOwnHashScope(runtime);
            Term expected = await ProplistOwnCursorAssertions.RootSuffix(context, map, scope);
            Term cursor = new Cons(Term.I(ProplistOwnCursorNames.RootSlot), map);
            Term valid = await runtime.Modules.Call(
                context,
                ProplistOwnCursorNames.Maps,
                ProplistOwnCursorNames.IteratorValid,
                cursor
            );
            ProplistOwnCursorAssertions.Require(valid.Equals(Term.A(ErlangBooleanAtoms.True)), ProplistOwnCursorDiagnostics.Compiler);
            foreach (string function in new[] { ProplistOwnCursorNames.Read, ProplistOwnCursorNames.Generate })
            {
                Term result = await Execution.InvokeAsync(
                    compiled.Definition,
                    function,
                    context,
                    [cursor]
                );
                ProplistOwnCursorAssertions.Require(result.Equals(expected), ProplistOwnCursorDiagnostics.Compiler);
            }
            Term rows = Cons.From(Enumerable.Repeat<Term>(Nil.Value, Cons.Items(expected).Count()));
            Term zipped = await Execution.InvokeAsync(
                compiled.Definition,
                ProplistOwnCursorNames.Zip,
                context,
                [cursor, rows]
            );
            ProplistOwnCursorAssertions.Require(zipped.Equals(expected), ProplistOwnCursorDiagnostics.Compiler);

            return Nil.Value;
        });
    }
}
