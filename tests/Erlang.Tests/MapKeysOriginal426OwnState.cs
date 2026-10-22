namespace Erlang.Tests;

internal static class MapKeysOriginal426OwnState
{
    internal static void Check(Term value)
    {
        Term[] rows = Cons.Items(value).ToArray();
        MapKeysOriginal426Access.Require(rows.Length == MapKeysOriginal426Names.OwnCases.Length);
        for (int index = 0; index < rows.Length; index++)
        {
            MapKeysOriginal426Access.Require(rows[index] is TupleTerm { Items.Count: 2 });
            var row = (TupleTerm)rows[index];
            MapKeysOriginal426Access.Require(row.Items[0].Equals(Term.A(MapKeysOriginal426Names.OwnCases[index]))
                && row.Items[1] is TupleTerm);
            var result = (TupleTerm)row.Items[1];
            MapKeysOriginal426Access.Require(result.Items.Count == (index == 0 ? 3 : 2)
                && result.Items[0].Equals(Term.A(MapKeysOriginal426Names.True)));
            if (index == 0)
                MapKeysOriginal426Access.Require(result.Items[1].Equals(result.Items[2]));
        }
    }
}
