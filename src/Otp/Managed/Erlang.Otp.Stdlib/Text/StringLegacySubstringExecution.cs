using System.Numerics;

namespace Erlang;

internal static class StringLegacySubstringExecution
{
    internal static Term Invoke(ProcessContext context, string function, IReadOnlyList<Term> a)
    {
        if (function == StringFunctionNames.SubString)
        {
            if (a.Count == 2)
            {
                return Invoke(context, StringFunctionNames.Substr, a);
            }
            if (a[1] is not Integer begin || a[2] is not Integer end)
            {
                throw StringSourceTrace.Clause(
                    context,
                    function,
                    a,
                    StringLegacySource.SubStringThree
                );
            }

            return Invoke(context, StringFunctionNames.Substr, [a[0], a[1], new Integer(end.Value - begin.Value + 1)]);
        }
        if (a.Count == 2)
        {
            if (a[1] is Integer { Value.IsOne: true } && a[0] is Cons or Nil)
            {
                return a[0];
            }
            if (a[1] is not Integer start || start.Value <= 1)
            {
                throw StringSourceTrace.Clause(
                    context,
                    function,
                    a,
                    StringLegacySource.SubstrTwo
                );
            }

            return Drop(context, a[0], start.Value);
        }
        if (a[1] is not Integer position || position.Value < 1 || a[2] is not Integer length || length.Value < 0)
        {
            throw StringSourceTrace.Clause(
                context,
                function,
                a,
                StringLegacySource.SubstrThree
            );
        }
        Term tail;
        using (StringSourceTrace.Scope(
            context,
            function,
            3,
            StringLegacySource.SubstrDropCall
        ))
        {
            tail = Drop(context, a[0], position.Value);
        }
        var selected = new List<Term>();
        BigInteger remaining = length.Value;
        while (remaining > 0 && tail is Cons cell)
        {
            selected.Add(cell.Head);
            tail = cell.Tail;
            remaining--;
        }
        if (tail is not (Cons or Nil))
        {
            throw StringSourceTrace.Clause(
                context,
                StringSourceNames.SubstrKeep,
                [tail, new Integer(remaining)],
                StringLegacySource.SubstrKeep,
                selected.Count == 0 ? [] : [StringSourceTrace.Frame(StringSourceNames.SubstrKeep, 2, StringLegacySource.SubstrKeep)]
            );
        }

        return Cons.From(selected);
    }

    private static Term Drop(ProcessContext context, Term input, BigInteger position)
    {
        while (position > 1 && input is Cons cell)
        {
            input = cell.Tail;
            position--;
        }
        if (position != 1 || input is not (Cons or Nil))
        {
            throw StringSourceTrace.Clause(
                context,
                StringSourceNames.SubstrDrop,
                [input, new Integer(position)],
                StringLegacySource.SubstrDrop
            );
        }

        return input;
    }
}
