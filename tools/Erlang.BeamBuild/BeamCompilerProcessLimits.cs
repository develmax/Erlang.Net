namespace Erlang.BeamBuild;

internal static class BeamCompilerProcessLimits
{
    internal const int TimeoutSeconds = 120;
    internal const int CleanupSeconds = 10;
    internal const int LogCharacters = 1_048_576;
    internal const int ReadBufferCharacters = 4096;
}
