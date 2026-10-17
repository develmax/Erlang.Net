namespace Erlang;

internal static class CpuTopologyStateRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            CpuTopologyStateNames.Set,
            1,
            (context, arguments) => ValueTask.FromResult(CpuTopologyFormatting.Format(context.Runtime.CpuTopology.Set(CpuTopologyConversion.ToInternal(arguments[0]))))
        );
    }

    public static bool TrySystemFlag(
        ProcessContext context,
        Term flag,
        Term value,
        out Term result
    )
    {
        if (flag is Atom { Name: CpuTopologyStateNames.Internal })
        {
            result = context.Runtime.CpuTopology.Set(value);

            return true;
        }
        result = null!;

        return false;
    }

    public static bool TrySystemInfo(ProcessContext context, Term query, out Term result)
    {
        string selector = CpuTopologyStateNames.Used;
        bool external;
        if (query is Atom { Name: CpuTopologyStateNames.External })
            external = true;
        else if (query is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom { Name: CpuTopologyStateNames.Internal or CpuTopologyStateNames.External } type)
        {
            external = type.Name == CpuTopologyStateNames.External;
            if (tuple.Items[1] is not Atom which)
            {
                if (external)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);

                throw InvalidInternalSelector();
            }
            selector = which.Name;
        }
        else
        {
            result = null!;

            return false;
        }
        if (!external && selector is not (CpuTopologyStateNames.Used or CpuTopologyStateNames.Defined or CpuTopologyStateNames.Detected))
            throw InvalidInternalSelector();
        Term topology = context.Runtime.CpuTopology.Get(selector);
        result = external ? CpuTopologyFormatting.Format(topology) : topology;

        return true;
    }

    private static ErlangException InvalidInternalSelector() => new(Nil.Value)
    {
        ExplicitStackTrace = true
    };
}
