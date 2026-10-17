namespace Erlang;

// Typed at native loading and ABI symbol-lookup sites; authorization, hashing and cleanup remain host failures.
internal sealed class NifNativeLoadStageException(string category, Exception failure)
    : InvalidOperationException(failure.Message, failure)
{
    internal string Category { get; } = category;
}
