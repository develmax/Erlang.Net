namespace Erlang;

internal sealed partial class RuntimeEtsStoredPayload
{
    internal void AddOwnedCensus420(EtsOwnedCensus420 census)
    {
        if (!census.Add(this)) return;
        var owned = Volatile.Read(ref nodes) ?? throw new ObjectDisposedException(nameof(RuntimeEtsStoredPayload));
        census.Array(owned);
        foreach (var node in owned)
        {
            census.Array(node.Bytes);
            census.Array(node.Children);
            census.Array(node.Names);
            // Names reference interned/borrowed atom strings; reservation values and factory recipes reference genuine shared runtime/image owners.
            // Only each newly allocated reservation capsule and external-entry recipe wrapper belongs to this copied object.
            census.Add(node.Identity);
            if (node.Function is ExternalStoredFunctionRecipe recipe) census.Add(recipe);
        }
    }
}
