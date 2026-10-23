namespace Erlang;

internal static class RuntimeNativePrimitiveNames
{
    internal const RuntimeNativePrimitives PersistentProvider = RuntimeNativePrimitives.PersistentTermCore;
    internal const string ForeignRegistry = "Native primitive registration requires this runtime's exact registry";
    internal const string Displaced = "Native primitive registration refuses an existing or displaced module owner";
    internal const string Missing = "The selected native primitive implementation is unavailable";
    internal static readonly (string Function, int Arity)[] PersistentSignatures =
    [
        (PersistentTermNativeNames.Get, 0),
        (PersistentTermNativeNames.Get, 1),
        (PersistentTermNativeNames.Get, 2),
        (PersistentTermNativeNames.Put, 2),
        (PersistentTermNativeNames.PutNew, 2),
        (PersistentTermNativeNames.Erase, 1),
        (PersistentTermNativeNames.Info, 0)
    ];
    internal static readonly (string Function, int Arity)[] EtsSignatures =
    [
        (RuntimeEtsFacetNames407.New, 2),
        (RuntimeEtsFacetNames407.Whereis, 1),
        (RuntimeEtsFacetNames407.Insert, 2),
        (RuntimeEtsFacetNames407.Lookup, 2),
        (RuntimeEtsFacetNames407.Member, 2),
        (RuntimeEtsFacetNames407.Delete, 1),
        (RuntimeEtsFacetNames407.Delete, 2),
        (RuntimeEtsFacetNames407.Select, 2),
        (RuntimeEtsLookupNames411.Function, 3),
        (RuntimeEtsLookupNames411.Function, 4),
        (RuntimeEtsMatchNames411.Function, 2)
    ];
}
