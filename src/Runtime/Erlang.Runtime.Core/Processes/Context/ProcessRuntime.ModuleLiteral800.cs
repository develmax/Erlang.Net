namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireModuleLiteralPreparation800()
    {
        if (System.Threading.Monitor.IsEntered(gate))
            throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
    }
}
