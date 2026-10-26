namespace Erlang.Tests;

internal static class NumericContract487Names
{
    internal const string Test = "runtime/bif-contract487/numeric252-values-and1260-full-stacks";
    internal const string Subject = "numeric_contract487";
    internal const string Prefix = "Erlang.Tests.NumericContract487.";
    internal const string Failure = "Numeric BIF re-audit resource is missing or differs";
    internal const int Values = 252;
    internal const int Profiles = 1260;
    internal const int BudgetSeconds = 30;
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["numeric_contract487.beam"] = "F5C0688DB96E7AD49C3FF3B21EDF604AEB4E87734024C4A7174C810FC286A0F3",
        ["values.etf"] = "AD2938336E345B6A7A97DDAE8E6BCBA28F86F04AFC6B0144193BAF6DA9F5CB32",
        ["stacks.etf"] = "DEA8EA2184832E2E8BDE0747103A7BBFE06A5FE5DFF3220D5F452029142E75C6"
    };
}
