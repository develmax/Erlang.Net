using Erlang.Differential;

namespace Erlang.Tests;

internal static class BoundedBifHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(BoundedBifHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BoundedBifHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), BoundedBifHostNames.Fixture);
        Term outcome = await CompiledModuleExecution.Run(artifact);
        Require(outcome is TupleTerm { Items.Count: 2 });
        var wrapper = (TupleTerm)outcome;
        Require(wrapper.Items[0].Equals(Term.A(BoundedBifHostNames.Success)) && wrapper.Items[1] is TupleTerm { Items.Count: 4 });
        var results = ((TupleTerm)wrapper.Items[1]).Items;
        Term[] rows = Cons.Items(results[0]).ToArray();
        Require(rows.Length == BoundedBifHostNames.FunctionCount * BoundedBifHostNames.DepthCount);
        var failures = new List<Exception>();
        for (int index = 0; index < rows.Length; index++)
        {
            try
            {
                CheckCapture(rows[index], index / BoundedBifHostNames.DepthCount, index % BoundedBifHostNames.DepthCount);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(BoundedBifHostNames.Failure, failures);
        Require(results[1].Equals(Cons.From(Enumerable.Range(0, BoundedBifHostNames.FunctionCount).SelectMany(_ => new Term[] { Term.A(ErlangBooleanAtoms.True), Term.A(ErlangBooleanAtoms.False) }))));
        Require(results[2] is TupleTerm { Items.Count: 3 });
        var identity = ((TupleTerm)results[2]).Items;
        Require(identity[0].Equals(identity[2]) && identity[1].Equals(identity[2]));
        await BoundedNativeIdentity379Checks.Run(artifact.Definition, outcome);
        Require(results[3].Equals(Term.Tuple(
            Term.A(ErlangBooleanAtoms.True),
            Term.A(ErlangBooleanAtoms.False),
            Term.A(ErlangBooleanAtoms.True),
            Term.A(ErlangBooleanAtoms.False),
            Term.A(ErlangBooleanAtoms.False)
        )));
    }

    private static void CheckCapture(Term value, int function, int depth)
    {
        Require(value is TupleTerm { Items.Count: 5 });
        var row = (TupleTerm)value;
        Require(row.Items[0].Equals(Term.A(BoundedBifHostNames.Callers[function])));
        Term selectedDepth = depth == 0 ? Term.A(BoundedBifHostNames.DefaultDepth) : Term.I(BoundedBifHostNames.ExplicitDepths[depth - 1]);
        Require(row.Items[1].Equals(selectedDepth));
        Require(row.Items[2].Equals(Term.A(BoundedBifHostNames.Error)) && row.Items[3].Equals(Term.A(BoundedBifHostNames.BadArg)));
        Term[] frames = Cons.Items(row.Items[4]).ToArray();
        Require(frames.Length > 0 && frames.Length <= BoundedBifHostNames.Limits[depth]);
        Require(frames.All(frame => frame is TupleTerm { Items.Count: 4 }));
        Term arguments = function == BoundedBifHostNames.FunctionCount - 1
            ? Term.List(Term.A(BoundedBifHostNames.Invalid), Term.A(BoundedBifHostNames.InvalidBound), Term.I(BoundedBifHostNames.RangeMaximum))
            : Term.List(Term.A(BoundedBifHostNames.Invalid));
        Term information = Term.List(Term.Tuple(
            Term.A(BoundedBifHostNames.ErrorInfo),
            new MapTerm(new Dictionary<Term, Term>
            {
                [Term.A(BoundedBifHostNames.FormatterKey)] = Term.A(BoundedBifHostNames.Formatter)
            })
        ));
        Require(frames[0].Equals(Term.Tuple(
            Term.A(BoundedBifHostNames.Erlang),
            Term.A(BoundedBifHostNames.Functions[function]),
            arguments,
            information
        )));
        if (BoundedBifHostNames.Limits[depth] == 1)
        {
            Require(frames.Length == 1);

            return;
        }
        Require(frames.Length >= 2);
        Require(frames[1].Equals(Term.Tuple(
            Term.A(BoundedBifHostNames.Module),
            Term.A(BoundedBifHostNames.Callers[function]),
            Term.I(BoundedBifHostNames.CallerArity),
            Term.List(
                Term.Tuple(Term.A(BoundedBifHostNames.File), Term.String(BoundedBifHostNames.Fixture)),
                Term.Tuple(Term.A(BoundedBifHostNames.Line), Term.I(BoundedBifHostNames.Lines[function]))
            )
        )));
        if (BoundedBifHostNames.Limits[depth] == 2)
            Require(frames.Length == 2);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BoundedBifHostNames.Failure);
    }
}
