internal static class MathCompatibilityNames
{
    internal const string Managed = "managed";
    internal const string Beam = "beam";
    internal const string Module = "math";
    internal const string ModuleInformation = "module_info";
    internal const string RuntimeModule = "erlang";
    internal const string Observer = "beam_math_observer392";
    internal const string Run = "run";
    internal const string Stacks = "run_stacks";
    internal const string OriginalRows = "original.etf";
    internal const string OriginalExports = "exports.etf";
    internal const string OriginalMetadata = "metadata.etf";
    internal const string OracleIdentity = "oracle-identity.json";
    internal const string Version = "29.1.1";
    internal const string OriginalImage = "math.beam";
    internal const string ObserverImage = Observer + ".beam";
    internal const string OriginalValues = "values.etf";
    internal const string OriginalStacks = "stacks.etf";
    internal const int ExpectedRows = 502772;
    internal static readonly string[] RequiredFiles =
    [
        OriginalRows,
        OriginalExports,
        OriginalMetadata,
        OriginalImage,
        ObserverImage,
        OriginalValues,
        OriginalStacks
    ];
    internal const int ExpectedExports = 26;
    internal const int ExpectedPrimitives = 24;
    internal const int SamplesPerFunction = 5;
    internal const string Ok = "ok";
    internal const string Error = "error";
    internal static readonly TimeSpan Deadline = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan CleanupDeadline = TimeSpan.FromSeconds(30);
}
