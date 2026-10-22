namespace Erlang.Tests;

internal static class StrictChain411Names
{
    internal const string Ordinary = "chain411_ordinary";
    internal const string A = "chain411_a";
    internal const string B = "chain411_b";
    internal const string Failure = "chain411_failure";
    internal const string Held = "chain411_held";
    internal const string Observer = "chain411_observer";
    internal const string HeldNotice = "callback_held";
    internal const string Run = "run";
    internal const string Erlang = "erlang";
    internal const string ReadyA = "a_ready";
    internal const string ResourcePrefix = "Erlang.Tests.Resources.StrictChain411.";
    internal const string Manifest = "manifest.json";
    internal const string OrderTest = "HOST/library411/explicit-order-mixed-source-imports";
    internal const string RefusalTest = "HOST/library411/exact-order-default-and-import-refusals";
    internal const string PreCanceledTest = "HOST/library411/pre-canceled-factory";
    internal const string HeldTest = "HOST/library411/actual-held-callback-cancel-dispose";
    internal const string Error = "Strict library explicit on-load chain control failed";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
