namespace Erlang.Tests;

internal static class PreloadedModuleHostDiagnostics
{
    public const string Ownership = "Preloaded catalogue does not match actual embedded code ownership";
    public const string Snapshot = "Preloaded snapshot changed after publication";
    public const string Rollback = "Failed embedded installation leaked code or catalogue entries";
    public const string Concurrency = "Reader observed partial embedded installation";
    public const string Boot = "Actual runtime init/erlang code is absent from the boot catalogue";
}
