namespace Erlang;

internal static class EtsCursorNames420
{
    internal const string Module = "ets", Select = "select", SelectReverse = "select_reverse", Match = "match", MatchObject = "match_object", ReverseHelper = "reverse";
    internal const string End = "$end_of_table", Foreign = "ETS cursor primitive requires its exact live runtime and registry", Conflict = "ETS cursor binding conflicts with an existing source or native owner", Invalid = "ETS cursor signature was not admitted";
    internal const int HashPublicArity = 6, HashTrapArity = 7, TreeArity = 8, ScanBudget = 1000, TreeReverseBudget = 40000, SmallPayloadBits = 60;
    internal static readonly (string Function, int Arity)[] Signatures = [(Match, 1), (Match, 3), (MatchObject, 1), (MatchObject, 3), (Select, 1), (Select, 3), (SelectReverse, 1), (SelectReverse, 3)];
}
