namespace Erlang.BeamBuild;

internal static class BeamSourceBuildTaskNames
{
    internal const string Module = "Module";
    internal const string Source = "Source";
    internal const string InputSha = "SourceSha256";
    internal const string OutputSha = "OutputSha256";
    internal const string FullPath = "FullPath";
    internal const string MacroKind = "Kind";
    internal const string MacroText = "Text";
    internal const string RequestPrefix = ".request-";
    internal const string RequestExtension = ".json";
    internal const string Invalid = "Invalid admitted BEAM MSBuild task configuration or output";
    internal const string Failed = "Admitted BEAM compiler tool failed";
    internal const string Truncated = " [log truncated]";
    internal const int BuildSeconds = 600;
    internal const int CleanupSeconds = 10;
    internal const int LogCharacters = 65536;
    internal const int DrainBufferCharacters = 4096;
}
