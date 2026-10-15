namespace Erlang;

internal static class ModuleCodeModificationNames
{
    internal const string ScopeOrder = "Code modification scopes must close on their owning thread in reverse order.";
    internal const string NestedAsync = "An asynchronous code modification ticket cannot be acquired from an owned synchronous write scope.";
    internal const string GateOrder = "Code modification admission must precede the module registry monitor.";
    internal const string ReleasedTicket = "The code modification ticket has already been released.";
}
