namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal void RequireBinaryInfoCapture822()
    {
        if (System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(NativeBinaryInfo822Names.ModuleGate);
    }
}
