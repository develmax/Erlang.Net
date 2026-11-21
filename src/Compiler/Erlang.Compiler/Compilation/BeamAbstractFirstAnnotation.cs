namespace Erlang.Compiler;

internal static class BeamAbstractFirstAnnotation
{
    internal static Term Read(Term form)
    {
        var root = ((TupleTerm)form).Items[1];
        if (!TryPosition(root, out var previous))
        {
            return root;
        }

        Term result = root;
        foreach (Term annotation in Walk(form))
        {
            if (!TryPosition(annotation, out var position))
            {
                return root;
            }

            // erl_parse:first_anno stops when pre-order locations start increasing.
            if (position.CompareTo(previous) > 0)
            {
                break;
            }

            result = annotation;
            previous = position;
        }

        return result;
    }

    private static IEnumerable<Term> Walk(Term value)
    {
        while (value is Cons list)
        {
            foreach (Term annotation in Walk(list.Head))
            {
                yield return annotation;
            }

            value = list.Tail;
        }

        if (value is not TupleTerm tuple || tuple.Items is not [Atom tag, _, ..])
        {
            yield break;
        }

        var items = tuple.Items;
        if (tag.Name == "function" && items.Count == 3)
        {
            yield break;
        }

        bool wrapper = tag.Name is "clauses" or "typed_record_field"
            || tag.Name == "function" && items.Count == 4;
        if (!wrapper)
        {
            yield return items[1];
        }

        int end = (tag.Name, items.Count) switch
        {
            ("record_field", 5) => 3,
            ("bin_element", 5) => 4,
            _ => items.Count
        };
        for (int index = wrapper ? 1 : 2; index < end; index++)
        {
            foreach (Term annotation in Walk(items[index]))
            {
                yield return annotation;
            }
        }
    }

    private static bool TryPosition(Term annotation, out (int Line, int Column) position)
    {
        if (annotation is Cons)
        {
            foreach (Term item in Cons.Items(annotation))
            {
                if (item is TupleTerm { Items: [Atom { Name: "location" }, var location] })
                {
                    return TryPosition(location, out position);
                }
            }
        }
        else if (annotation is TupleTerm { Items: [Integer line, Integer column] })
        {
            position = ((int)line.Value, (int)column.Value);

            return true;
        }
        else if (annotation is Integer lineOnly)
        {
            position = ((int)lineOnly.Value, 1);

            return true;
        }

        position = default;

        return false;
    }
}
