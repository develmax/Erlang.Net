namespace Erlang;

internal static class ErlangFunctionParameters
{
    public const long MinimumQueryArity = -(1L << 59);
    public const long MaximumQueryArity = (1L << 59) - 1;
    public const int MaximumFunctionArity = 255;
}
