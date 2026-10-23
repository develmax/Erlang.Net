namespace Erlang;

internal sealed class TraceValidation425(string? cause) : Exception
{
    internal string? Cause { get; } = cause;
}
