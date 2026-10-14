namespace Erlang;

internal static class BeamLoaderDiagnostics
{
    internal const string UnexpectedPublication = "Unexpected prepared BEAM publication status";
    internal const string UnexpectedLoadingResult = "Unexpected prepared BEAM loading result";
    internal const string ConcurrentPending = "A pending on_load image already exists for this module";
}
