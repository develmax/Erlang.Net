namespace Erlang.Tests;

internal static class BeamLoaderPaddedReferenceNames
{
    internal const uint MagicMarker = 1u << 17;
    internal const uint InvalidPidTag = 0;
    internal const string Failure = "Live padded magic reference recovery failed";
    internal const string ForeignNode = "loader_foreign@node";
    internal const uint DifferentCreation = 1;
    internal const uint NonzeroSuffix = 1;
}
