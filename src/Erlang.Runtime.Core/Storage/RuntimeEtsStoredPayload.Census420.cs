namespace Erlang;

internal sealed partial class RuntimeEtsStoredPayload
{
    internal void AddOwnedCensus420(EtsOwnedCensus420 census)
    {
        if (!census.Add(this)) return;
        if (Volatile.Read(ref compressed438) is { } compressed)
        {
            compressed.AddOwnedCensus(census);

            return;
        }
        var owned = Volatile.Read(ref nodes) ?? throw new ObjectDisposedException(nameof(RuntimeEtsStoredPayload));
        census.Array(owned);
        foreach (var node in owned)
        {
            census.Array(node.Bytes);
            census.Array(node.Children);
            census.Array(node.Names);
            // Names reference interned/borrowed atom strings; reservation values and factory recipes reference genuine shared runtime/image owners.
            // New reservation capsules and external/local BEAM recipe wrappers belong to this copied object; their actual shared bindings/code/runtime owners remain borrowed.
            census.Add(node.Identity);
            if (node.Function is ExternalStoredFunctionRecipe or BeamStoredFunctionRecipe) census.Add(node.Function);
        }
    }
}
