namespace Erlang.Tests;

internal static class DebugDirtyOriginal424OwnState
{
    private static void Require(bool value) => DebugDirtyOriginal424Access.Require(value);

    private static TupleTerm Tuple(Term value, int arity)
    {
        Require(value is TupleTerm tuple && tuple.Items.Count == arity);

        return (TupleTerm)value;
    }

    internal static void Check(Term value)
    {
        Term[] rows = Cons.Items(value).ToArray();
        Require(rows.Length == 12);
        int index = 0;
        foreach (string route in new[] { "cpu", "io", "normal" })
        {
            TupleTerm row = Tuple(rows[index++], 3), result = Tuple(row.Items[2], 4);
            Require(row.Items[0].Equals(Term.A("send")) && row.Items[1].Equals(Term.A(route)));
            Require(result.Items[0].Equals(Term.A("value")) && result.Items[1].Equals(Term.A("ok")) && result.Items[3] is Pid);
            Require(result.Items[2].Equals(Term.Tuple(Term.A("ok"), result.Items[3])));
        }
        foreach (string route in new[] { "cpu", "io", "normal" })
        {
            TupleTerm row = Tuple(rows[index++], 3), result = Tuple(row.Items[2], 6);
            Require(row.Items[0].Equals(Term.A("ready")) && row.Items[1].Equals(Term.A(route)));
            Require(result.Items[0].Equals(Term.A("value")) && result.Items[1].Equals(Term.A("ok")) && result.Items[4].Equals(Term.A("normal")) && result.Items[5] is Pid);
            Require(result.Items[2].Equals(Term.Tuple(Term.A("ready"), result.Items[5])) && result.Items[3].Equals(Term.Tuple(Term.A("done"), result.Items[5])));
        }
        foreach (string route in new[] { "cpu", "io" })
            foreach (string operation in new[] { "alive_waitexiting", "alive_waitexitingonly" })
            {
                TupleTerm row = Tuple(rows[index++], 4), result = Tuple(row.Items[3], 4);
                Require(row.Items[0].Equals(Term.A("alive")) && row.Items[1].Equals(Term.A(route)) && row.Items[2].Equals(Term.A(operation)));
                Require(result.Items[0].Equals(Term.A("value")) && result.Items[2].Equals(Term.A("killed")) && result.Items[3] is Pid);
                Require(result.Items[1].Equals(Term.Tuple(Term.A("alive"), result.Items[3])));
            }
        TupleTerm magic = Tuple(rows[index++], 2), magicResult = Tuple(magic.Items[1], 5);
        Require(magic.Items[0].Equals(Term.A("magic_copy")) && magicResult.Items[0].Equals(Term.A("value")));
        Term[] copies = Cons.Items(magicResult.Items[1]).ToArray();
        Require(copies.Length == 1000 && copies.All(copy => copy.Equals(magicResult.Items[2])));
        Require(magicResult.Items[3].Equals(Term.I(123)) && magicResult.Items[4].Equals(Term.A("false")));
        TupleTerm heap = Tuple(rows[index], 2), heapResult = Tuple(heap.Items[1], 5);
        Require(heap.Items[0].Equals(Term.A("heap_copy_sharing")) && heapResult.Items[0].Equals(Term.A("value")));
        Require(Cons.Items(heapResult.Items[1]).Count() == 1000 && heapResult.Items.Skip(2).All(item => item.Equals(Term.A("false"))));
    }
}
