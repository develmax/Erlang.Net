namespace Erlang;

internal static class NifNativeArrayNames
{
    internal const string NullArray = "Native term array cannot be null when its count is positive";
    internal static readonly Atom SystemLimit = Term.A(ErlangErrorReasons.SystemLimit);
}
