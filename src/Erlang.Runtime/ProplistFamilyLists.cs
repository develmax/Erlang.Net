namespace Erlang;

internal static class ProplistFamilyLists
{
    internal static bool Key(Term property, out Term key)
    {
        if (property is Atom)
        {
            key = property;

            return true;
        }
        if (property is TupleTerm { Items.Count: > 0 } tuple)
        {
            key = tuple.Items[0];

            return true;
        }
        key = Nil.Value;

        return false;
    }

    internal static bool Matches(Term property, Term key) => Key(property, out Term actual) && actual.Equals(key);

    internal static bool ListCell(Term value) => value is Nil or Cons;

    internal static async ValueTask<bool> IsProper(ProcessContext context, Term value)
    {
        while (value is Cons cell)
        {
            await context.ReduceAsync();
            value = cell.Tail;
        }

        return value is Nil;
    }

    internal static async ValueTask<List<Term>> Read(ProcessContext context, Term value, bool generator = false)
    {
        var items = new List<Term>();
        while (value is Cons cell)
        {
            await context.ReduceAsync();
            items.Add(cell.Head);
            value = cell.Tail;
        }
        End(value, generator);

        return items;
    }

    internal static void End(Term value, bool generator = false)
    {
        if (value is Nil) return;
        throw new ErlangException(generator
            ? Term.Tuple(Term.A(ProplistFamilyNames.BadGenerator), value)
            : Term.A(ErlangErrorReasons.FunctionClause));
    }

    internal static Term Append(Term prefix, Term tail)
    {
        var items = new List<Term>();
        while (prefix is Cons cell)
        {
            items.Add(cell.Head);
            prefix = cell.Tail;
        }
        if (prefix is not Nil) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return Cons.From(items, tail);
    }
}
