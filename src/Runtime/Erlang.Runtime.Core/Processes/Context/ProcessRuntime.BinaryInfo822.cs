namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireBinaryInfoCapture822()
    {
        if (System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(NativeBinaryInfo822Names.RuntimeGate);
        Modules.RequireBinaryInfoCapture822();
    }
}
