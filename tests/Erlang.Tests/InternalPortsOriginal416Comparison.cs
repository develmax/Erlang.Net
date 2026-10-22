using Erlang;
namespace Erlang.Tests;

internal static class InternalPortsOriginal416Comparison
{
    internal static int Profiles(
        Term expected,
        Term actual,
        List<object> differences,
        ref int examined
    )
    {
        Term[] wanted = Cons.Items(expected).ToArray();
        Term[] observed = actual is Cons or Nil ? Cons.Items(actual).ToArray() : [];
        InternalPortsOriginal416Access.Require(wanted.Length == InternalPortsOriginal416Names.Rows);
        int equal = 0;
        for (int group = 0; group < wanted.Length; group++)
        {
            var original = (TupleTerm)wanted[group];
            Term[] depths = Cons.Items(original.Items[1]).ToArray();
            Term[] current = group < observed.Length && observed[group] is TupleTerm { Items.Count: 2 } pair
                && pair.Items[1] is Cons or Nil ? Cons.Items(((TupleTerm)observed[group]).Items[1]).ToArray() : [];
            InternalPortsOriginal416Access.Require(depths.Length == 5);
            for (int depth = 0; depth < depths.Length; depth++)
            {
                examined++;
                if (depth < current.Length && depths[depth].Equals(current[depth])) equal++;
                else differences.Add(new
                {
                    Scope = InternalPortsOriginal416Names.StackScope,
                    Group = group,
                    Depth = depth,
                    Expected = OwnedFunctionOriginal407Terms.Typed(depths[depth]),
                    Actual = depth < current.Length ? OwnedFunctionOriginal407Terms.Typed(current[depth]) : null
                });
            }
        }

        return equal;
    }
}
