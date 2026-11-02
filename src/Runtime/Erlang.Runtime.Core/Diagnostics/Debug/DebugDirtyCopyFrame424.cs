namespace Erlang;

internal sealed class DebugDirtyCopyFrame424
{
    internal DebugDirtyCopyFrame424(Term source)
    {
        Source = source;
        Children = ReadChildren(source);
        Copied = new Term[Children.Length];
    }

    internal static Term[] ReadChildren(Term source) => source switch
    {
        Cons list => [list.Head, list.Tail],
        TupleTerm tuple => tuple.Items.ToArray(),
        HamtBitmapTerm429 node => node.Children.ToArray(),
        MapTerm map => map.Entries.SelectMany(pair => new[] { pair.Key, pair.Value }).ToArray(),
        NativeRecordTerm record => record.Values.ToArray(),
        FunctionTerm function => function.Environment.ToArray(),
        _ => []
    };

    internal Term Source { get; }
    internal Term[] Children { get; }
    internal Term[] Copied { get; }
    internal int Index { get; set; }
}
