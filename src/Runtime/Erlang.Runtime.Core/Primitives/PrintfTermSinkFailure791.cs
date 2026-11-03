namespace Erlang;

internal sealed class PrintfTermSinkFailure791(int result) : Exception
{
    internal int Result { get; } = result;
}
