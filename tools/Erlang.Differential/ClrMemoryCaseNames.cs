namespace Erlang.Differential;

public static class ClrMemoryCaseNames
{
    public const string Prefix = "erlang-memory-profile-";
    public const string ModulePrefix = "-module(oracle_memory_profile). -export([run/0]). run()->";
    public const string ModuleSuffix = ".";
    public const string True = "true";
    public const string ErrorClass = "error";
    public const string BadArgument = "badarg";
    public const string FunctionClause = "function_clause";
    public const string SdkScope = "CLR SDK-observed payload profile v1, host-wide within the loaded SDK type universe";
    public const string OriginalScope = "Pinned ERTS dynamically allocated emulator memory; values are not an OS footprint";
    public const string RawSnapshotSource = "-module(oracle_memory_profile_raw). -export([run/0]). run()->{erlang:memory(),erlang:memory([total,processes,processes_used,system,atom,atom_used,binary,code,ets])}.";
    public const int OutcomeCount = 2;
    public const int OutcomeTag = 0;
    public const int OutcomeValue = 1;
    public const int SnapshotCount = 2;
    public const int AllSnapshot = 0;
    public const int SelectedSnapshot = 1;
    public const int KeyCount = 9;
    public const int PairCount = 2;
    public const int PairKey = 0;
    public const int PairValue = 1;
    public const int TotalIndex = 0;
    public const int ProcessesIndex = 1;
    public const int ProcessesUsedIndex = 2;
    public const int SystemIndex = 3;
    public const int AtomIndex = 4;
    public const int AtomUsedIndex = 5;
    public const int BinaryIndex = 6;
    public const int CodeIndex = 7;
    public const int EtsIndex = 8;
    public static IReadOnlyList<string> OrderedKeys { get; } = Array.AsReadOnly<string>(
        ["total", "processes", "processes_used", "system", "atom", "atom_used", "binary", "code", "ets"]
    );
    public const string AllShapeName = "all-nine-key-shape-partition";
    public const string AllShapeBody = "M=erlang:memory(),[{total,T},{processes,P},{processes_used,PU},{system,S},{atom,A},{atom_used,AU},{binary,B},{code,C},{ets,E}]=M,lists:all(fun({_,N})->is_integer(N) andalso N>=0 end,M) andalso T=:=P+S andalso PU=<P andalso AU=<A andalso S>=A+B+C+E";
    public const string ScalarName = "scalar-nonnegative";
    public const string ScalarBody = "N=erlang:memory(total),is_integer(N) andalso N>=0";
    public const string OrderDuplicatesName = "list-order-duplicate-one-snapshot";
    public const string OrderDuplicatesBody = "[{ets,E},{total,T},{ets,E},{processes,P},{total,T}]=erlang:memory([ets,total,ets,processes,total]),is_integer(E) andalso E>=0 andalso is_integer(T) andalso T>=0 andalso is_integer(P) andalso P>=0";
    public const string EmptyName = "empty-list";
    public const string EmptyBody = "erlang:memory([])";
    public const string WrongOuterName = "wrong-outer";
    public const string WrongOuterBody = "erlang:memory(42)";
    public const string UnknownName = "unknown-key";
    public const string UnknownBody = "erlang:memory(unknown_memory_profile317)";
    public const string MaximumName = "maximum-default";
    public const string MaximumBody = "erlang:memory(maximum)";
    public const string ValidImproperName = "valid-head-improper-tail";
    public const string ValidImproperBody = "erlang:memory([total|tail])";
    public const string InvalidImproperName = "invalid-head-before-improper-tail";
    public const string InvalidImproperBody = "erlang:memory([unknown_memory_profile317|tail])";
    public const string NonatomName = "nonatom-list-member";
    public const string NonatomBody = "erlang:memory([total,42])";
}
