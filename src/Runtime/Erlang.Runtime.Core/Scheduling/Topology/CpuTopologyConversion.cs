namespace Erlang;

public static class CpuTopologyConversion
{
    public static Term ToInternal(Term value)
    {
        if (value is Atom { Name: ProcessRegistryAtoms.Undefined })
            return value;
        var rows = new List<CpuTopologyEntry>();
        Convert(
            value,
            CpuTopologySettingParameters.InitialNodeCounter,
            CpuTopologySettingParameters.InitialProcessorCounter,
            CpuTopologyEntry.Empty,
            CpuTopologySettingParameters.RootLevel,
            ChildLevel(value, CpuTopologySettingParameters.RootLevel),
            rows
        );

        return Cons.From(rows.Select(row => row.ToTerm()));
    }

    private static CpuTopologyLevel ChildLevel(Term value, int parent)
    {
        if (value is TupleTerm { Items.Count: 2 } logical && logical.Items[0].Equals(Term.A(CpuTopologyNames.Logical)))
            return CpuTopologyLevel.Logical;
        if (value is not Cons { Head: TupleTerm tuple } || tuple.Items.Count == 0 || tuple.Items[0] is not Atom tag)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return tag.Name switch
        {
            CpuTopologyNames.Node when parent == CpuTopologySettingParameters.RootLevel => CpuTopologyLevel.Node,
            CpuTopologyNames.Node when parent == (int)CpuTopologyLevel.Processor => CpuTopologyLevel.ProcessorNode,
            CpuTopologyNames.Processor when parent == CpuTopologySettingParameters.RootLevel => CpuTopologyLevel.Node,
            CpuTopologyNames.Processor when parent == (int)CpuTopologyLevel.Node => CpuTopologyLevel.Processor,
            CpuTopologyNames.Core => CpuTopologyLevel.Core,
            CpuTopologyNames.Thread => CpuTopologyLevel.Thread,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };
    }

    private static void Convert(
        Term value,
        int nodeId,
        int processorId,
        CpuTopologyEntry cpu,
        int parent,
        CpuTopologyLevel level,
        List<CpuTopologyEntry> rows
    )
    {
        if (value is Nil)
            return;
        if (value is Cons)
        {
            Term remaining = value;
            while (remaining is Cons list)
            {
                Convert(
                    list.Head,
                    nodeId,
                    processorId,
                    cpu,
                    parent,
                    level,
                    rows
                );
                if (rows.Count != 0)
                {
                    cpu = rows[^1];
                    nodeId = Math.Max(cpu.Node, cpu.ProcessorNode);
                    processorId = cpu.Processor;
                }
                remaining = list.Tail;
            }
            if (remaining is not Nil)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            return;
        }
        if (value is not TupleTerm tuple || tuple.Items.Count is not (2 or 3) || tuple.Items[0] is not Atom tag)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (tuple.Items.Count == 3 && tuple.Items[1] is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Term children = tuple.Items[^1];
        int nextParent;
        CpuTopologyEntry next;
        switch (tag.Name)
        {
            case CpuTopologyNames.Node when parent == CpuTopologySettingParameters.RootLevel && level == CpuTopologyLevel.Node:
                nodeId++;
                next = CpuTopologyEntry.Empty with { Node = nodeId };
                nextParent = (int)CpuTopologyLevel.Node;
                break;
            case CpuTopologyNames.Processor when parent == CpuTopologySettingParameters.RootLevel && level == CpuTopologyLevel.Node:
                processorId++;
                next = CpuTopologyEntry.Empty with { Processor = processorId };
                nextParent = (int)CpuTopologyLevel.Processor;
                break;
            case CpuTopologyNames.Processor when parent < (int)CpuTopologyLevel.Processor && level <= CpuTopologyLevel.Processor:
                processorId++;
                next = cpu with { Processor = processorId, ProcessorNode = CpuTopologyParameters.UnspecifiedIdentifier, Core = CpuTopologyParameters.UnspecifiedIdentifier, Thread = CpuTopologyParameters.UnspecifiedIdentifier };
                nextParent = (int)CpuTopologyLevel.Processor;
                break;
            case CpuTopologyNames.Node when parent == (int)CpuTopologyLevel.Processor && level == CpuTopologyLevel.ProcessorNode:
                nodeId++;
                next = cpu with { ProcessorNode = nodeId };
                nextParent = (int)CpuTopologyLevel.ProcessorNode;
                break;
            case CpuTopologyNames.Core when parent < (int)CpuTopologyLevel.Core && level == CpuTopologyLevel.Core:
                next = cpu with { Core = cpu.Core + 1, Thread = CpuTopologyParameters.UnspecifiedIdentifier };
                nextParent = (int)CpuTopologyLevel.Core;
                break;
            case CpuTopologyNames.Thread when parent < (int)CpuTopologyLevel.Thread && level == CpuTopologyLevel.Thread:
                next = cpu with { Thread = cpu.Thread + 1 };
                nextParent = (int)CpuTopologyLevel.Thread;
                break;
            case CpuTopologyNames.Logical when tuple.Items.Count == 2 && parent < (int)CpuTopologyLevel.Logical && level == CpuTopologyLevel.Logical && children is Integer number && number.Value >= CpuTopologySettingParameters.MinimumLogicalIdentifier && number.Value <= CpuTopologySettingParameters.MaximumLogicalIdentifier:
                rows.Add(cpu with { Processor = cpu.Processor == CpuTopologyParameters.UnspecifiedIdentifier ? processorId + 1 : cpu.Processor, Core = cpu.Core == CpuTopologyParameters.UnspecifiedIdentifier ? CpuTopologySettingParameters.DefaultCoreIdentifier : cpu.Core, Thread = cpu.Thread == CpuTopologyParameters.UnspecifiedIdentifier ? CpuTopologySettingParameters.DefaultThreadIdentifier : cpu.Thread, Logical = (int)number.Value });

                return;
            default:
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        Convert(
            children,
            nodeId,
            processorId,
            next,
            nextParent,
            ChildLevel(children, nextParent),
            rows
        );
    }
}
