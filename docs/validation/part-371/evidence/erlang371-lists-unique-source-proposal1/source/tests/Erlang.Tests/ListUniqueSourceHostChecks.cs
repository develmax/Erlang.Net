using Erlang.Differential;

namespace Erlang.Tests;

internal static class ListUniqueSourceHostChecks
{
    internal static async Task Run()
    {
        var assembly = typeof(ListUniqueSourceHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(ListUniqueSourceHostNames.Fixture, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        using var artifact = GeneratedModuleCompiler.Compile(await reader.ReadToEndAsync(), ListUniqueSourceHostNames.Fixture);
        Term outcome = await CompiledModuleExecution.Run(artifact);
        Require(outcome is TupleTerm { Items.Count: 2 });
        var wrapper = (TupleTerm)outcome;
        Require(wrapper.Items[0].Equals(Term.A(ListUniqueSourceHostNames.Success)) && wrapper.Items[1] is TupleTerm { Items.Count: 5 });
        var results = ((TupleTerm)wrapper.Items[1]).Items;
        Require(results[0] is Nil);
        Require(results[1].Equals(Cons.From(Enumerable.Range(0, 5).Select(number => (Term)Term.I(number)))));
        Require(results[2].Equals(Term.List(Term.I(1), new FloatTerm(2))));
        Require(results[3].Equals(Cons.From(Enumerable.Range(1, ListUniqueSourceHostNames.UnionRange).Select(number => (Term)Term.I(number)))));
        Term[] errors = Cons.Items(results[4]).ToArray();
        Require(errors.Length == ListUniqueSourceHostNames.Functions.Length);
        Term[] arguments =
        [
            Term.List(new Cons(Term.A(ListUniqueSourceHostNames.AtomA), Term.A(ListUniqueSourceHostNames.Tail))),
            Term.List(Term.A(ListUniqueSourceHostNames.Raw), Nil.Value, Term.A(ListUniqueSourceHostNames.Ascending)),
            Term.List(Term.A(ListUniqueSourceHostNames.Left), Term.A(ListUniqueSourceHostNames.Right), Term.List(Term.I(1)), Term.I(1), Term.I(2))
        ];
        for (int index = 0; index < errors.Length; index++)
        {
            Require(errors[index] is TupleTerm { Items.Count: 3 });
            var error = ((TupleTerm)errors[index]).Items;
            Require(error[0].Equals(Term.A(ListUniqueSourceHostNames.Error)) && error[1].Equals(Term.A(ListUniqueSourceHostNames.FunctionClause)));
            Term[] frames = Cons.Items(error[2]).ToArray();
            Require(frames.Length > 0);
            Require(frames[0].Equals(Frame(ListUniqueSourceHostNames.Functions[index], arguments[index], ListUniqueSourceHostNames.Lines[index])));
            if (index == errors.Length - 1)
            {
                Require(frames.Length >= 2);
                Require(frames[1].Equals(Frame(ListUniqueSourceHostNames.MergeLoop, Term.I(3), ListUniqueSourceHostNames.MergeContinuationLine)));
            }
        }
    }

    private static Term Frame(string function, Term arguments, int line) => Term.Tuple(
        Term.A(ListUniqueSourceHostNames.Lists),
        Term.A(function),
        arguments,
        Term.List(
            Term.Tuple(Term.A(ListUniqueSourceHostNames.File), Term.String(ListUniqueSourceHostNames.ListsFile)),
            Term.Tuple(Term.A(ListUniqueSourceHostNames.Line), Term.I(line))
        )
    );

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ListUniqueSourceHostNames.Failure);
    }
}
