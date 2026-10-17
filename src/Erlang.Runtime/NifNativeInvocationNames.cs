namespace Erlang;

internal static class NifNativeInvocationNames
{
    internal const string Unbound = "NIF atom construction requires a bound current invocation.";
    internal const string ForeignFailureState = "A NIF invocation binding must share its callback failure state.";
}
