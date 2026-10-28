namespace Erlang;

internal static class OwnedMapPayloadNames517
{
    internal const string HeaderMissing = "The map payload header is not active in its owning process heap.";
    internal const string InvalidShape = "The prepared map occurrence/topology view is inconsistent.";
    internal const string PreparationRequired = "Map shape/hash preparation must complete outside the process heap gate.";
    internal const string ConstructionRequired = "A live construction frame from the target process heap is required.";
    internal const string EmptyGlobal = "An empty map has no owned payload in this foundation; retain the existing global/portable representation.";
}
