using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class ProplistStackHostChecks
{
    internal static async Task Run()
    {
        using var compiled = GeneratedModuleCompiler.Compile(ProplistGetValueObservationSource.Source, ProplistGetValueObservationSource.File);
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        Term actual = await owner.InvokeAsync(context => Execution.InvokeAsync(
            compiled.Definition,
            ProplistStackHostNames.Observe,
            context,
            []
        ).AsTask()).WaitAsync(ProplistGetValueHostNames.Deadline);
        var rows = new List<Term>();
        foreach (int arity in new[] { 2, 3 })
            foreach (string mode in new[] { ProplistStackHostNames.Tail, ProplistStackHostNames.NonTail })
                foreach (int depth in new[] { 0, 1, 2 })
                {
                    Term first = Term.Tuple(
                        Term.A(ProplistGetValueHostNames.Module),
                        Term.A(ProplistGetValueHostNames.Function),
                        Term.List(
                            Term.A(ProplistStackHostNames.PropertyKey),
                            Term.A(ProplistStackHostNames.ImproperTail),
                            Term.A(arity == 2 ? ProplistStackHostNames.Undefined : ProplistStackHostNames.Fallback)
                        ),
                        Location(ProplistStackHostNames.ProplistFile, ProplistStackHostNames.ProplistLine)
                    );
                    bool tail = mode == ProplistStackHostNames.Tail;
                    int line = tail ? ProplistStackHostNames.TailLine : arity == 2 ? ProplistStackHostNames.NonTailTwoLine : ProplistStackHostNames.NonTailThreeLine;
                    Term frames = depth < 2 ? Term.List(first) : Term.List(
                        first,
                        Term.Tuple(
                            Term.A(ProplistStackHostNames.Module),
                            Term.A(tail ? ProplistStackHostNames.TailCaller : ProplistStackHostNames.NonTailCaller),
                            Term.I(ProplistStackHostNames.CallerArity),
                            Location(ProplistGetValueObservationSource.File, line)
                        )
                    );
                    rows.Add(Term.Tuple(
                        Term.I(arity),
                        Term.A(mode),
                        Term.I(depth),
                        Term.Tuple(Term.A(ErlangExceptionClasses.Error), Term.A(ErlangErrorReasons.FunctionClause), frames)
                    ));
                }
        if (!actual.Equals(Cons.From(rows)))
            throw new InvalidOperationException(ProplistStackHostNames.Failure + actual);
    }

    private static Term Location(string file, int line) => Term.List(
        Term.Tuple(Term.A(ProplistStackHostNames.File), Term.String(file)),
        Term.Tuple(Term.A(ProplistStackHostNames.Line), Term.I(line))
    );
}
