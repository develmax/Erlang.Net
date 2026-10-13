namespace Erlang;

internal sealed record CpuTopologyEntry(
    int Node,
    int Processor,
    int ProcessorNode,
    int Core,
    int Thread,
    int Logical
)
{
    public static CpuTopologyEntry Empty { get; } = new(
        CpuTopologyParameters.UnspecifiedIdentifier,
        CpuTopologyParameters.UnspecifiedIdentifier,
        CpuTopologyParameters.UnspecifiedIdentifier,
        CpuTopologyParameters.UnspecifiedIdentifier,
        CpuTopologyParameters.UnspecifiedIdentifier,
        CpuTopologyParameters.UnspecifiedIdentifier
    );

    public Term ToTerm() => Term.Tuple(
        Term.A(CpuTopologyNames.Cpu),
        Term.I(Node),
        Term.I(Processor),
        Term.I(ProcessorNode),
        Term.I(Core),
        Term.I(Thread),
        Term.I(Logical)
    );

    public static CpuTopologyEntry Read(Term value)
    {
        if (value is not TupleTerm tuple || tuple.Items.Count != CpuTopologyParameters.RecordArity || !tuple.Items[0].Equals(Term.A(CpuTopologyNames.Cpu)))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var fields = tuple.Items.Skip(1).Select(Identifier).ToArray();

        return new(
            fields[0],
            fields[1],
            fields[2],
            fields[3],
            fields[4],
            fields[5]
        );
    }

    private static int Identifier(Term value) => value is Integer number && number.Value >= CpuTopologySettingParameters.MinimumInternalIdentifier && number.Value <= CpuTopologySettingParameters.MaximumInternalIdentifier ? (int)number.Value : throw new ErlangException(ErlangErrorReasons.BadArgument);
}
