namespace Erlang.Tests;

internal static class ProplistOwnCursorNames
{
    internal const string DomainsTest = "runtime/proplists/from-map-own-key-domains";
    internal const string LifetimesTest = "runtime/proplists/from-map-own-entry-epochs";
    internal const string CollisionsTest = "runtime/proplists/from-map-own-real-collisions";
    internal const string CompilerTest = "compiler/proplists/from-map-own-context-routes";
    internal const string AliasTest = "runtime/proplists/from-map-own-reference-stored-counts";
    internal const string Proplists = "proplists";
    internal const string FromMap = "from_map";
    internal const string Erlang = "erlang";
    internal const string Alias = "alias";
    internal const string Unalias = "unalias";
    internal const string BadMap = "badmap";
    internal const string ErrorClass = "error";
    internal const int HashLevels = 16;
    internal const int HashNibbleBits = 4;
    internal const string Maps = "maps";
    internal const string IteratorValid = "is_iterator_valid";
    internal const string Foreign = "proplist_cursor_foreign@node";
    internal const string Module = "proplist_own_cursor_calls";
    internal const string SourceFile = "proplist_own_cursor_calls.erl";
    internal const string ExportModule = "proplist_own_cursor_exports";
    internal const string ExportFunction = "value";
    internal const string LocalFactory = "local";
    internal const string Read = "read";
    internal const string Generate = "generate";
    internal const string Zip = "zip";
    internal const string FirstAtom = "proplist_own_cursor_zeta";
    internal const string SecondAtom = "proplist_own_cursor_alpha";
    internal const string RecordModule = "proplist_cursor_records";
    internal const string RecordName = "entry";
    internal const string RecordField = "payload";
    internal const string AliasReport = "artifacts/proplists-own-cursor-reference-alias-observation.json";
    internal const int ScalarKeys = 32;
    internal const int RootSlot = 1;
    internal const int RootNibbleMask = 15;
    internal const int LargeEnvironment = 256;
    internal const string CollisionFirstHex = "0000000000000000000000000000000000000000000000000000000000000000";
    internal const string CollisionSecondHex = "01000000000000000000000000000000BF7EEA966E6982A33D4ADAE8A9853E61";
    internal const string Source = """
        -module(proplist_own_cursor_calls).
        -export([local/1,read/1,generate/1,zip/2]).
        local(Value) -> fun() -> Value end.
        read(Input) -> proplists:from_map(Input).
        generate(Input) -> [{K,V} || K := V <- Input].
        zip(Input,Rows) -> [{K,V} || K := V <- Input && _ <- Rows].
        """;
}
