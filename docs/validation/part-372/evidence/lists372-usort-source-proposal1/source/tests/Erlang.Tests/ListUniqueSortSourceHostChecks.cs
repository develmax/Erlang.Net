namespace Erlang.Tests;

internal static class ListUniqueSortSourceHostChecks
{
    internal static async Task Identity()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term one = Term.I(1);
        Term two = Term.I(2);
        Term three = Term.I(3);
        foreach (Term list in new[] { Nil.Value, Term.List(one), Term.List(one, two), Term.List(one, two, three) })
            Require(ReferenceEquals(await Sort(actor, runtime, list), list));
        Term first = new FloatTerm(1);
        Term equal = Term.I(1);
        Term result = await Sort(actor, runtime, Term.List(first, equal, new FloatTerm(1)));
        Require(result is Cons { Tail: Nil } selected && ReferenceEquals(selected.Head, first));
        Term large = new FloatTerm(2);
        Term small = Term.I(0);
        Term middle = Term.I(1);
        result = await Sort(actor, runtime, Term.List(large, small, Term.I(2), middle, new FloatTerm(0)));
        Term[] items = Cons.Items(result).ToArray();
        Require(items.Length == 3 && ReferenceEquals(items[0], small) && ReferenceEquals(items[1], middle) && ReferenceEquals(items[2], large));
        Term duplicate = Term.I(2);
        result = await actor.InvokeAsync(context => runtime.Modules.Call(context, ListUniqueSortSourceHostNames.Lists, ListUniqueSortSourceHostNames.Sort, Term.List(large, middle, duplicate)).AsTask());
        items = Cons.Items(result).ToArray();
        Require(items.Length == 3 && ReferenceEquals(items[0], middle) && ReferenceEquals(items[1], large) && ReferenceEquals(items[2], duplicate));
    }

    internal static async Task Malformed()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term tail = Term.A(ListUniqueSortSourceHostNames.Tail);
        Term[][] prefixes = [[Term.I(1)], [Term.I(1), Term.I(1), Term.I(1)], [Term.I(1), Term.I(3)], [Term.I(3), Term.I(1)], [Term.I(1), Term.I(3), Term.I(0), Term.I(-1)], [Term.I(3), Term.I(1), Term.I(4), Term.I(5)]];
        Term[] inputs = prefixes.Select(prefix => Cons.From(prefix, tail)).ToArray();
        Term[] expectedArguments =
        [
            Term.List(inputs[0]),
            Term.List(Term.I(1), tail),
            Term.List(Term.I(1), Term.I(3), tail, Nil.Value, Nil.Value),
            Term.List(Term.I(3), Term.I(1), tail, Nil.Value, Nil.Value),
            Term.List(Term.I(1), Term.I(3), tail, Term.List(Term.I(0)), Nil.Value, Term.I(-1)),
            Term.List(Term.I(3), Term.I(1), tail, Term.List(Term.I(4)), Nil.Value, Term.I(5))
        ];
        for (int index = 0; index < inputs.Length; index++)
        {
            bool failed = false;
            try
            {
                await Sort(actor, runtime, inputs[index]);
            }
            catch (ErlangException error)
            {
                failed = true;
                Require(error.Reason.Equals(Term.A(ListUniqueSortSourceHostNames.FunctionClause)));
                Term expected = Term.Tuple(Term.A(ListUniqueSortSourceHostNames.Lists), Term.A(ListUniqueSortSourceHostNames.ErrorFunctions[index]), expectedArguments[index], Term.List(Term.Tuple(Term.A(ListUniqueSortSourceHostNames.File), Term.String(ListUniqueSortSourceHostNames.Source)), Term.Tuple(Term.A(ListUniqueSortSourceHostNames.Line), Term.I(ListUniqueSortSourceHostNames.ErrorLines[index]))));
                Require(Cons.Items(error.StackTraceTerm).First().Equals(expected));
            }
            Require(failed);
        }
    }

    internal static async Task Deep()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term input = Nil.Value;
        for (int index = 0; index < ListUniqueSortSourceHostNames.Cells; index++)
            input = new Cons(Term.I(index * ListUniqueSortSourceHostNames.Step % ListUniqueSortSourceHostNames.Modulus), input);
        Term result = await Sort(actor, runtime, input);
        Term[] ordered = Cons.Items(result).ToArray();
        Require(ordered.Length == ListUniqueSortSourceHostNames.Modulus);
        for (int index = 0; index < ordered.Length; index++)
            Require(ordered[index].Equals(Term.I(index)));
        Term tail = Term.A(ListUniqueSortSourceHostNames.Tail);
        input = tail;
        Term first = new FloatTerm(1);
        for (int index = 0; index < ListUniqueSortSourceHostNames.Cells; index++)
            input = new Cons(first, input);
        bool failed = false;
        try
        {
            await Sort(actor, runtime, input);
        }
        catch (ErlangException error)
        {
            failed = true;
            Require(error.Reason.Equals(Term.A(ListUniqueSortSourceHostNames.FunctionClause)));
            TupleTerm frame = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
            Require(frame.Items[1].Equals(Term.A(ListUniqueSortSourceHostNames.UsortHelper)) && frame.Items[2].Equals(Term.List(first, tail)));
        }
        Require(failed);
    }

    private static Task<Term> Sort(RuntimeNodeFoundationHostActor actor, ProcessRuntime runtime, Term list)
        => actor.InvokeAsync(context => runtime.Modules.Call(context, ListUniqueSortSourceHostNames.Lists, ListUniqueSortSourceHostNames.UniqueSort, list).AsTask());

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(ListUniqueSortSourceHostNames.Failure);
    }
}
