namespace Erlang;

internal sealed class CpuTopologyState
{
    private readonly object gate = new();
    private Term defined = Term.A(ProcessRegistryAtoms.Undefined);

    public Term Set(Term value)
    {
        Term validated = CpuTopologyValidation.Read(value);
        lock (gate)
        {
            Term previous = defined;
            defined = validated;

            return previous;
        }
    }

    public Term Get(string selector)
    {
        lock (gate)
        {
            return selector switch
            {
                CpuTopologyStateNames.Used or CpuTopologyStateNames.Defined => defined,
                CpuTopologyStateNames.Detected => Term.A(ProcessRegistryAtoms.Undefined),
                _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
            };
        }
    }
}
