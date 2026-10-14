namespace Erlang;

internal sealed class HaltParseException(bool badOptions) : Exception
{
    public bool BadOptions { get; } = badOptions;
}
