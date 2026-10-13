namespace Erlang;

internal static class CpuTopologyFormatting
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        CpuTopologyNames.Format,
        1,
        (context, arguments) =>
        {
            try
            {
                Term result = Format(arguments[0]);

                return ValueTask.FromResult(result);
            }
            catch (ErlangException)
            {
                throw new ErlangException(CpuTopologyNames.InternalError)
                {
                    ExplicitStackTrace = true,
                    StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), null, CpuTopologyNames.Format)
                };
            }
        }
    );

    internal static Term Format(Term rows) => rows is Atom { Name: ProcessRegistryAtoms.Undefined } ? rows : Convert(rows, true, CpuTopologyLevel.Node);

    private static Term Convert(Term rows, bool first, CpuTopologyLevel level)
    {
        if (rows is Nil)
            return Nil.Value;
        if (rows is not Cons list || list.Head is not TupleTerm tuple || tuple.Items.Count != CpuTopologyParameters.RecordArity || tuple.Items[0] is not Atom { Name: CpuTopologyNames.Cpu })
            throw new ErlangException(ErlangErrorReasons.FunctionClause);
        if (level == CpuTopologyLevel.Logical)
            return Term.Tuple(Term.A(CpuTopologyNames.Logical), tuple.Items[(int)CpuTopologyLevel.Logical]);
        Term value = Element(tuple, level);
        Term remaining = list.Tail;
        Term same = Term.List(tuple);
        while (remaining is Cons next && value.Equals(Element(next.Head, level)))
        {
            same = new Cons(next.Head, same);
            remaining = next.Tail;
        }
        bool unspecified = value is Integer number && number.Value == CpuTopologyParameters.UnspecifiedIdentifier;
        Term group = Reverse(same);
        if (unspecified && first && remaining is Nil || first && remaining is Nil && level is not CpuTopologyLevel.Processor and not CpuTopologyLevel.ProcessorNode)
            return Convert(group, true, level + 1);
        if (unspecified && level == CpuTopologyLevel.Node)
            return Append(Convert(group, true, CpuTopologyLevel.Processor), Convert(remaining, false, level));

        return new Cons(Term.Tuple(Term.A(Tag(level)), Convert(group, true, level + 1)), Convert(remaining, false, level));
    }

    private static Term Element(Term term, CpuTopologyLevel level) => term is TupleTerm tuple && tuple.Items.Count > (int)level ? tuple.Items[(int)level] : throw new ErlangException(ErlangErrorReasons.BadArgument);

    private static Term Reverse(Term rows)
    {
        Term result = Nil.Value;
        foreach (Term row in Cons.Items(rows))
            result = new Cons(row, result);

        return result;
    }

    private static Term Append(Term left, Term right) => Cons.From(Cons.Items(left), right);

    private static string Tag(CpuTopologyLevel level) => level switch
    {
        CpuTopologyLevel.Node or CpuTopologyLevel.ProcessorNode => CpuTopologyNames.Node,
        CpuTopologyLevel.Processor => CpuTopologyNames.Processor,
        CpuTopologyLevel.Core => CpuTopologyNames.Core,
        CpuTopologyLevel.Thread => CpuTopologyNames.Thread,
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    };
}
