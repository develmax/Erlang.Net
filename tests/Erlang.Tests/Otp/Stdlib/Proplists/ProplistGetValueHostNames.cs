namespace Erlang.Tests;

internal static class ProplistGetValueHostNames
{
    internal const string TestPrefix = "proplists-get-value-";
    internal const string CompiledTestPrefix = "oracle/compiled-module/proplists-get-value-";
    internal const string MfaTestPrefix = "mfa/proplists:get_value/";
    internal const string Module = "proplists";
    internal const string Function = "get_value";
    internal const string Key = "proplist_host_key";
    internal const string Other = "proplist_host_other";
    internal const string Kill = "kill";
    internal const string Killed = "killed";
    internal const string Failure = "Proplist get_value host contract failed.";
    internal const string ParseTest = "proplists-get-value-parse-all";
    internal const string IdentityTest = "proplists-get-value-object-identity";
    internal const string ProgressTest = "proplists-get-value-cooperative-progress";
    internal const string CancellationTest = "proplists-get-value-cooperative-cancellation";
    internal const int Cells = 12000;

    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
