using Erlang;
namespace Erlang.Tests;

internal static class EtsCursorEnd420Checks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(1);
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsCompiled419.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsCursor420.Register(runtime, runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            async ValueTask<Term> Call(string function, params Term[] arguments) => await runtime.Modules.Call(
                context,
                EtsCursorEnd420Names.Ets,
                function,
                arguments
            );
            Term table = await Call(EtsCursorEnd420Names.New, Term.A(EtsCursorEnd420Names.Name), Term.List(Term.A(EtsCursorEnd420Names.Ordered)));
            var rows = Enumerable.Range(1, 1002).Select(i => Term.Tuple(Term.Tuple(Term.A(EtsCursorEnd420Names.B), Term.I(i)), Term.I(i))).ToArray();
            _ = await Call(EtsCursorEnd420Names.Insert, table, Term.List(rows));
            Term program = await Call(
                EtsCursorEnd420Names.Compile,
                Term.List(Term.Tuple(Term.A(EtsCursorEnd420Names.Wildcard), Nil.Value, Term.List(Term.A(EtsCursorEnd420Names.Whole))))
            );
            Term raw = Term.Tuple(
                table,
                Term.Tuple(Term.A(EtsCursorEnd420Names.A), Term.I(0)),
                Term.Tuple(Term.A(EtsCursorEnd420Names.A), Term.A(EtsCursorEnd420Names.Wildcard)),
                Term.I(2000),
                program,
                Nil.Value,
                Term.I(0),
                Term.I(0)
            );
            Term result = await Call(EtsCursorEnd420Names.Select, raw);
            if (result is not TupleTerm { Items.Count: 2 } pair || !pair.Items[0].Equals(Term.List(rows.Take(1000).ToArray())) || !pair.Items[1].Equals(Term.A(EtsCursorEnd420Names.End))) throw new InvalidOperationException(EtsCursorEnd420Names.Failure);

            return true;
        });
    }
}
