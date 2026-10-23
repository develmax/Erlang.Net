namespace Erlang;

internal sealed record LocalFunctionInvocation(
    FunctionTerm OriginalFunction,
    LocalFunctionPublicationCell Cell,
    LocalFunctionPublication Publication
)
{
    internal bool IsCurrent => Cell.IsCurrent(Publication);
}
