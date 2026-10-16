namespace Erlang;

internal sealed class LocalFunctionPublicationChangedException(LocalFunctionInvocation invocation) : Exception
{
    internal LocalFunctionInvocation Invocation { get; } = invocation;
}
