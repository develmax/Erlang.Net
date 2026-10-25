namespace Erlang;

internal static class NifNativeCapsuleDiagnostics
{
    internal const string InvalidDependency = "Native capsule dependency is empty or contains a zero byte";
    internal const string MissingDependencies = "Native capsule requires a bridge and target dependency";
}
