namespace Erlang;

internal sealed class OwnedPayloadCopyFrame506
{
    internal Term Source { get; }
    internal PreparedOwnedMap517? MapPlan { get; }
    internal PreparedOwnedMapNode526? NodePlan526 { get; }
    internal PreparedHamtBitmap785? BitmapPlan785 { get; }
    internal PreparedHeapRecord? RecordPlan { get; }
    internal Term[] Children { get; }
    internal Term[] Copied { get; }
    internal int Index { get; set; }

    internal OwnedPayloadCopyFrame506(Term source, PreparedMapGraph517 maps)
    {
        Source = source;
        MapPlan = source is MapTerm map ? maps.Find(map) : null;
        NodePlan526 = maps.FindNode526(source);
        BitmapPlan785 = NodePlan526 is null ? maps.FindBitmap785(source) : null;
        RecordPlan = source is NativeRecordTerm record ? maps.FindRecord(record) : null;
        Children = RecordPlan is { } recordPlan ? recordPlan.Children
            : NodePlan526 is { } nodePlan ? nodePlan.Children526()
            : BitmapPlan785 is { } bitmapPlan ? bitmapPlan.Children
            : MapPlan is { } plan ? plan.Children.ToArray() : source switch
            {
                Cons cons => [cons.Head, cons.Tail],
                TupleTerm tuple => tuple.Items.ToArray(),
                FunctionTerm function => function.Environment.ToArray(),
                _ => []
            };
        Copied = new Term[Children.Length];
    }
}
