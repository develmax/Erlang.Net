namespace Erlang;

internal static class DebugSharingGraph425
{
    internal static Term[] Children(Term source) => source switch
    {
        Cons list => [list.Head, list.Tail],
        TupleTerm tuple => tuple.Items.ToArray(),
        HamtBitmapTerm429 node => node.Children.ToArray(),
        MapTerm { FlatKeys426: { } keys } map => new Term[] { keys }.Concat(keys.Items.Select(map.Get)).ToArray(),
        MapTerm map => map.Entries.SelectMany(pair => new[] { pair.Key, pair.Value }).ToArray(),
        NativeRecordTerm record => record.Values.ToArray(),
        FunctionTerm function => function.Environment.ToArray(),
        _ => []
    };
}
