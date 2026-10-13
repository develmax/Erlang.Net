namespace Erlang;

internal static class CpuTopologyValidation
{
    public static Term Read(Term value)
    {
        if (value is Atom { Name: ProcessRegistryAtoms.Undefined })
            return value;
        if (value is not Cons)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var rows = Cons.Items(value).Select(CpuTopologyEntry.Read).OrderBy(row => row.Processor).ThenBy(row => row.Node).ThenBy(row => row.ProcessorNode).ThenBy(row => row.Core).ThenBy(row => row.Thread).ToArray();
        if (rows.Select(row => row.Logical).Distinct().Count() != rows.Length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        for (int index = 1; index < rows.Length; index++)
        {
            var previous = rows[index - 1];
            var current = rows[index];
            if (previous.Processor == current.Processor && previous.Node == current.Node && previous.ProcessorNode == current.ProcessorNode && previous.Core == current.Core && previous.Thread == current.Thread)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        int node = rows[0].Node;
        int processor = rows[0].Processor;
        bool noNodes = node < 0 && rows[0].ProcessorNode < 0;
        for (int index = 1; index < rows.Length; index++)
        {
            var current = rows[index];
            if (noNodes)
            {
                if (current.Node >= 0 || current.ProcessorNode >= 0)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            else
            {
                if (current.Processor == processor && current.Node != node)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                node = current.Node;
                processor = current.Processor;
                if (node >= 0 && current.ProcessorNode >= 0 || node < 0 && current.ProcessorNode < 0)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }

        return Cons.From(rows.Select(row => row.ToTerm()));
    }
}
