namespace Erlang.Tests;

internal static class ContractReaudit487Names
{
    internal const string Test = "runtime/bif-contract487/whole84-values-and420-full-stacks";
    internal const string Subject = "contract_reaudit487";
    internal const string Prefix = "Erlang.Tests.ContractReaudit487.";
    internal const string Failure = "BIF contract re-audit resource is missing or differs";
    internal const int Values = 84;
    internal const int Profiles = 420;
    internal static readonly IReadOnlyDictionary<string, string> Digests = new Dictionary<string, string>
    {
        ["contract_reaudit487.beam"] = "F881049C686C98729F5C97A4043E7EA8957F55F5B8FCB700EF16C34A50E3713B",
        ["values.etf"] = "5CB532DFAA22E3C27CBE772F6417C0BC3C17A2C02475FB5E8DE310CBDF1B068B",
        ["stacks.etf"] = "0061C9DB219033EFD9B132169CDF716805ABDCF304E18970BE4126D332163507"
    };
}
