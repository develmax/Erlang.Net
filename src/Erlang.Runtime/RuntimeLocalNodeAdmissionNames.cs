namespace Erlang;

internal static class RuntimeLocalNodeAdmissionNames
{
    internal const string Creation = "creation";
    internal const string NodeUp = "nodeup";
    internal const string NodeDown = RuntimeNodeControlNames.NodeDown;
    internal const string Closed = "The local node event table is closed.";
    internal const string Owner = "The node event subscriber is not a live owned process.";
    internal const int MonitorTrapArity = 3;
}
