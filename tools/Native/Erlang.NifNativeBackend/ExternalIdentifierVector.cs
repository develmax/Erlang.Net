namespace Erlang.NifNativeBackend;

internal sealed record ExternalIdentifierVector(
    Term Input,
    NativeOffheapKind576 Kind,
    nuint Header,
    nuint[] Data
)
{
    // Independent pinned little-endian ARCH_64 ExternalThing raw vectors.
    internal static readonly ExternalIdentifierVector[] Values =
    [
        new(
            new Pid(ExternalIdentifierCaseNames.Node, ExternalIdentifierCaseNames.PidId, ExternalIdentifierCaseNames.Creation),
            NativeOffheapKind576.ExternalPid,
            0xF0,
            [unchecked((nuint)0x1122334455667788UL)]
        ),
        new(
            new PortTerm(ExternalIdentifierCaseNames.Node, ExternalIdentifierCaseNames.PortId, ExternalIdentifierCaseNames.Creation),
            NativeOffheapKind576.ExternalPort,
            0xF4,
            [unchecked((nuint)0x8877665544332211UL)]
        ),
        new(
            Reference([0x11]),
            NativeOffheapKind576.ExternalReference,
            0xF8,
            [unchecked((nuint)0x1100000001UL)]
        ),
        new(
            Reference([0x11,0x22]),
            NativeOffheapKind576.ExternalReference,
            0x138,
            [unchecked((nuint)0x1100000002UL),0x22]
        ),
        new(
            Reference([0x11,0x22,0x33]),
            NativeOffheapKind576.ExternalReference,
            0x138,
            [unchecked((nuint)0x1100000003UL),unchecked((nuint)0x3300000022UL)]
        ),
        new(
            Reference([0x11,0x22,0x33,0x44]),
            NativeOffheapKind576.ExternalReference,
            0x178,
            [unchecked((nuint)0x1100000004UL),unchecked((nuint)0x3300000022UL),0x44]
        ),
        new(
            Reference([0x11,0x22,0x33,0x44,0x55]),
            NativeOffheapKind576.ExternalReference,
            0x178,
            [unchecked((nuint)0x1100000005UL),unchecked((nuint)0x3300000022UL),unchecked((nuint)0x5500000044UL)]
        )
    ];

    private static ReferenceTerm Reference(uint[] numbers) => new(ExternalIdentifierCaseNames.Node, numbers, ExternalIdentifierCaseNames.Creation);
}
