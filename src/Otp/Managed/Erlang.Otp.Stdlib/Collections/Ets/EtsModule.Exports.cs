namespace Erlang;

public static partial class EtsModule
{
    public static IReadOnlyList<(string Name, int Arity)> NativeExports { get; } = Array.AsReadOnly(new (string, int)[]
    {
        (EtsFunctionNames.New, 2), (EtsFunctionNames.Whereis, 1), (EtsFunctionNames.Insert, 2), (EtsFunctionNames.Lookup, 2), (EtsFunctionNames.Member, 2),
        (EtsFunctionNames.Delete, 1), (EtsFunctionNames.Delete, 2), (EtsFunctionNames.Select, 2), (EtsFunctionNames.LookupElement, 3), (EtsFunctionNames.LookupElement, 4),
        (EtsFunctionNames.Match, 2), (EtsFunctionNames.InternalSelectDelete, 2), (EtsFunctionNames.InternalDeleteAll, 2), (EtsFunctionNames.MatchObject, 2),
        (EtsFunctionNames.SelectCount, 2), (EtsFunctionNames.SelectReverse, 2), (EtsFunctionNames.InsertNew, 2), (EtsFunctionNames.Take, 2), (EtsFunctionNames.DeleteObject, 2),
        (EtsFunctionNames.UpdateElement, 3), (EtsFunctionNames.UpdateElement, 4), (EtsFunctionNames.First, 1), (EtsFunctionNames.Last, 1), (EtsFunctionNames.Next, 2), (EtsFunctionNames.Prev, 2),
        (EtsFunctionNames.FirstLookup, 1), (EtsFunctionNames.LastLookup, 1), (EtsFunctionNames.NextLookup, 2), (EtsFunctionNames.PrevLookup, 2),
        (EtsFunctionNames.UpdateCounter, 3), (EtsFunctionNames.UpdateCounter, 4), (EtsFunctionNames.Rename, 2), (EtsFunctionNames.Setopts, 2), (EtsFunctionNames.GiveAway, 3),
        (EtsFunctionNames.IsCompiledMs, 1), (EtsFunctionNames.MatchSpecCompile, 1), (EtsFunctionNames.MatchSpecRunR, 3), (EtsFunctionNames.InternalRequestAll, 0),
        (EtsFunctionNames.SafeFixtable, 2), (EtsFunctionNames.Slot, 2), (EtsFunctionNames.Match, 1), (EtsFunctionNames.Match, 3), (EtsFunctionNames.MatchObject, 1), (EtsFunctionNames.MatchObject, 3),
        (EtsFunctionNames.Select, 1), (EtsFunctionNames.Select, 3), (EtsFunctionNames.SelectReverse, 1), (EtsFunctionNames.SelectReverse, 3), (EtsFunctionNames.SelectReplace, 2),
        (EtsFunctionNames.Info, 1), (EtsFunctionNames.Info, 2)
    });

    public static IReadOnlyList<(string Name, int Arity)> Exports => Array.AsReadOnly(
        NativeExports.Concat(LibraryExports).OrderBy(export => export.Name, StringComparer.Ordinal).ThenBy(export => export.Arity).ToArray()
    );
}
