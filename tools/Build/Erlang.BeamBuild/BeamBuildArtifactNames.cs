namespace Erlang.BeamBuild;

internal static class BeamBuildArtifactNames
{
    internal const string StagePrefix = ".work-";
    internal const string CompilerLogStem = "compiler";
    internal const string StdoutLogSuffix = ".stdout.log";
    internal const string StderrLogSuffix = ".stderr.log";
    internal const string ProcessReceiptSuffix = ".process.json";
    internal const string CompletionFile = "completion.json";
    internal const string BuildOnlyStatus = "PASS_BUILD_ONLY";
    internal const string ManagedDependencyGlob = "*.dll";
    internal const string GuidCompactFormat = "N";
    internal const string TemporarySuffix = ".tmp";
    internal const string TemporarySeparator = ".";
}
