namespace Erlang;

// Typed only at the two platform-loader sites; never wraps authorization, hashing or cleanup.
internal sealed class NifNativeLoadStageException(string category, Exception failure)
    : InvalidOperationException(failure.Message, failure)
{
    internal string Category { get; } = category;
}
