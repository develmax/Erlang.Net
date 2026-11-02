namespace Erlang;

public sealed partial class ProcessContext
{
    internal ModuleLiteralLease800? ForkExecutingModuleLiteral800(Term root)
    {
        for (SourceExecution? current = sourceExecution.Value; current is not null; current = current.Parent)
            if (current.Literals800 is { } lease && lease.TryFork(root, out ModuleLiteralLease800? fork))
                return fork;

        return null;
    }
}
