namespace Erlang;

internal static class CollectionLibraryNames
{
    internal const string Sets = "sets";
    internal const string Dictionary = "dict";
    internal const string OrderedDictionary = "orddict";
    internal const string BalancedSets = "gb_sets";
    internal const string BalancedTrees = "gb_trees";

    internal static readonly string[] BeamOnly =
    [
        Sets,
        Dictionary,
        OrderedDictionary,
        BalancedSets,
        BalancedTrees
    ];
}
