using Erlang.Differential;

namespace Erlang.Tests;

internal static class SizeSourceLocationHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(SizeSourceLocationHostChecks).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(SizeSourceLocationHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), SizeSourceLocationHostNames.Fixture);
        Term value = await CompiledModuleExecution.Run(artifact);
        Require(value is TupleTerm { Items.Count: 2 });
        var pair = (TupleTerm)value;
        Require(pair.Items[1].Equals(Cons.From(Enumerable.Repeat<Term>(Term.A(ErlangBooleanAtoms.False), SizeSourceLocationHostNames.FunctionCount))));
        Term[] rows = Cons.Items(pair.Items[0]).ToArray();
        Require(rows.Length == SizeSourceLocationHostNames.FunctionCount * SizeSourceLocationHostNames.DepthCount);
        var failures = new List<Exception>();
        for (int index = 0; index < rows.Length; index++)
        {
            try
            {
                Check(rows[index], index / SizeSourceLocationHostNames.DepthCount, index % SizeSourceLocationHostNames.DepthCount);
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count != 0)
            throw new AggregateException(SizeSourceLocationHostNames.Failure, failures);
    }

    private static void Check(Term value, int function, int depth)
    {
        Require(value is TupleTerm { Items.Count: 5 });
        var row = (TupleTerm)value;
        Require(row.Items[0].Equals(Term.A(SizeSourceLocationHostNames.Callers[function])));
        Require(row.Items[2].Equals(Term.A(SizeSourceLocationHostNames.Error)));
        Term reason = function == SizeSourceLocationHostNames.FunctionCount - 1
            ? Term.Tuple(Term.A(SizeSourceLocationHostNames.BadMap), Term.A(SizeSourceLocationHostNames.Invalid))
            : Term.A(SizeSourceLocationHostNames.BadArg);
        Require(row.Items[3].Equals(reason));
        Term[] frames = Cons.Items(row.Items[4]).ToArray();
        Term information = Term.List(Term.Tuple(Term.A(SizeSourceLocationHostNames.ErrorInfo), new MapTerm(new Dictionary<Term, Term>
        {
            [Term.A(SizeSourceLocationHostNames.FormatterKey)] = Term.A(SizeSourceLocationHostNames.Formatter)
        })));
        Require(frames[0].Equals(Term.Tuple(
            Term.A(SizeSourceLocationHostNames.Erlang),
            Term.A(SizeSourceLocationHostNames.Functions[function]),
            Term.List(Term.A(SizeSourceLocationHostNames.Invalid)),
            information
        )));
        if (depth is 1 or 2)
        {
            Require(frames.Length == 1);
            return;
        }
        Require(frames.Length >= 2);
        Require(frames[1].Equals(Term.Tuple(
            Term.A(SizeSourceLocationHostNames.Module),
            Term.A(SizeSourceLocationHostNames.Callers[function]),
            Term.I(SizeSourceLocationHostNames.CallerArity),
            Term.List(
                Term.Tuple(Term.A(SizeSourceLocationHostNames.File), Term.String(SizeSourceLocationHostNames.Fixture)),
                Term.Tuple(Term.A(SizeSourceLocationHostNames.Line), Term.I(SizeSourceLocationHostNames.Lines[function]))
            )
        )));
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SizeSourceLocationHostNames.Failure);
    }
}
