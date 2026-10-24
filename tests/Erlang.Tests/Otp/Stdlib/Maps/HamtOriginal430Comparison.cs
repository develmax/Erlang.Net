namespace Erlang.Tests;

internal static class HamtOriginal430Comparison
{
    internal static int Rows(
        Term expected,
        Term actual,
        object scope,
        List<object> differences,
        ref int examined,
        ref int unexpected
    )
    {
        Term[] left = Cons.Items(expected).ToArray(), right = Cons.Items(actual).ToArray();
        HamtNative430Access.Require(left.Length == HamtOriginal430Names.Rows && right.Length == left.Length);
        int equal = 0;
        for (int index = 0; index < left.Length; index++)
        {
            examined++;
            if (left[index].Equals(right[index]))
            {
                equal++;
                continue;
            }
            bool held = HeldPhysicalDifference(left[index], right[index]);
            if (!held) unexpected++;
            differences.Add(new
            {
                Scope = scope,
                Index = index,
                HeldPhysical = held,
                Projected = false,
                Original = OwnedFunctionOriginal407Terms.Typed(left[index]),
                Actual = OwnedFunctionOriginal407Terms.Typed(right[index]),
                OriginalWholeEtf = Convert.ToBase64String(ExternalTermFormat.Encode(left[index])),
                ActualWholeEtf = Convert.ToBase64String(ExternalTermFormat.Encode(right[index]))
            });
        }

        return equal;
    }

    private static bool HeldPhysicalDifference(Term expected, Term actual)
    {
        Term key = Term.Tuple(Term.A("type"), Term.A("small_reference_slice"));

        return expected is TupleTerm { Items.Count: 2 } original && actual is TupleTerm { Items.Count: 2 } own
            && original.Items[0].Equals(key) && own.Items[0].Equals(key)
            && original.Items[1].Equals(Term.Tuple(Term.A("value"), Term.A("heap_binary")))
            && own.Items[1].Equals(Term.Tuple(Term.A("value"), Term.A("sub_binary")));
    }
}
