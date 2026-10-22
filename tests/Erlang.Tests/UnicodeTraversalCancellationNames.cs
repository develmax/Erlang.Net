namespace Erlang.Tests;

internal static class UnicodeTraversalCancellationNames
{
    internal const string Test = "runtime/library392/unicode-real-traversal-cancellation-settles";
    internal const string Failure = "Unicode traversal cancellation/settlement control failed";
    internal const string UnicodeModule = "unicode";
    internal const string Function = "characters_to_binary";
    internal const string Encoding = "utf8";
    internal const string Kill = "kill";
    internal const string Sentinel = "unicode_cancel_sentinel392";
    internal const string Counter = "CumulativeReductions";
    internal const string Snapshot = "Snapshot";
    internal const string ResourcePrefix = "Erlang.Tests.LibraryDependencies392.";
    internal const int Depth = 16384;
    internal const int ChunkBytes = 65536;
    internal const int ChunkCount = 256;
    internal const long MinimumObservedWork = 131072;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);
}
