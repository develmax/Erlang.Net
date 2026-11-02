namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal bool RequiresDirectSequenceShutdown
        => System.Threading.Monitor.IsEntered(gate) || (codeModificationPermission?.IsOwned ?? false);
}
