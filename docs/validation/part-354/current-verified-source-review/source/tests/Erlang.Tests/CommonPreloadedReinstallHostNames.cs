namespace Erlang.Tests;

internal static class CommonPreloadedReinstallHostNames
{
    internal const string Module = "trusted_preloaded_reinstall_probe";
    internal const string Function = "fail";
    internal const string Fault = "trusted_reinstall_fault";
    internal const string Rollback = "intentional_nested_reinstall_rollback";
    internal const string MissingRollback = "Expected nested reinstall rollback";
    internal const string MissingFailure = "Expected trusted wrapper failure";
    internal const string Broken = "Trusted reinstall/current metadata/old snapshot preservation failed";
    internal const string True = "true";
    internal const string BodyProperty = "Body";
    internal const string ImageProperty = "Image";
    internal const string BindingsField = "runtimeErrorBindings";
    internal const string OldCodeField = "oldCode";
    internal const string EntriesProperty = "Entries";
    internal const string Item2Field = "Item2";
}
