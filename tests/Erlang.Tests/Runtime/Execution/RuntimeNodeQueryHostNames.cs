namespace Erlang.Tests;

internal static class RuntimeNodeQueryHostNames
{
    public const string Test = "runtime/known-nodes-weak-lifetime-isolation-and-named-identity";
    public const string Node = "named@host";
    public const string Foreign = "host@foreign";
    public const string Retained = "retained@foreign";
    public const string Failure = "Owned node query contract failed";
    public const string Keep = "keep_node";
    public const string Decode = "binary_to_term";
}
