namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ReadOnlyMemory<byte> ReuseLateAtomicProjectionSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        // Keep logical projection identities available to the earlier shape passes.
        if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded
            || !decoded.Body.Any(IsAtomicStackAliasCandidate))
        {
            return code;
        }

        var (body, stackOperands) = decoded;
        if (RewriteReusedStackSlots(
            code,
            body,
            stackOperands,
            useValueLiveness: false,
            closureEntries: null,
            atomicOnly: true
        ) is { } reused)
        {
            code = reused;
            if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { } updated)
            {
                return code;
            }

            (body, stackOperands) = updated;
        }

        return RewriteDisjointFrames(
            code,
            body,
            stackOperands,
            atomicOnly: true
        ) is { } compacted ? compacted : code;
    }

    private sealed record LateProjectionDecode(
        BeamCodeHeader Header,
        int AtomCount,
        int LiteralCount,
        (IReadOnlyList<BeamInstruction> Body, IReadOnlyList<(int Start, int End, int Slot)> StackOperands) Value
    );

    private LateProjectionDecode? lateProjectionDecode;

    private (IReadOnlyList<BeamInstruction> Body, IReadOnlyList<(int Start, int End, int Slot)> StackOperands)?
        DecodeLateAtomicProjectionSlots(ReadOnlyMemory<byte> code, int labelCount)
    {
        int functionCount = exports.Count + locals.Count;
        int atomCount = atoms.Count + 1;
        int literalCount = literalBytes.Count;
        // Within EmitCode, rewrites either retain the input range or return new bytes.
        // Memory equality requires the same storage, offset and length.
        if (lateProjectionDecode is { } cached && cached.Header.Instructions.Equals(code)
            && cached.Header.Version == BeamWire.FormatVersion
            && cached.Header.MaximumOpcode == highestOpcode && cached.Header.LabelCount == labelCount
            && cached.Header.FunctionCount == functionCount && cached.AtomCount == atomCount
            && cached.LiteralCount == literalCount)
        {
            return cached.Value;
        }

        lateProjectionDecode = null;
        var stackOperands = new List<(int Start, int End, int Slot)>();
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                functionCount,
                code
            );
            var body = BeamInstructionReader.Read(
                header,
                atomCount,
                literalCount,
                recordStackOperand: (start, end, slot) => stackOperands.Add((start, end, slot))
            );

            IReadOnlyList<(int Start, int End, int Slot)> offsets = stackOperands.AsReadOnly();
            var value = (Body: body, StackOperands: offsets);
            lateProjectionDecode = new LateProjectionDecode(
                header,
                atomCount,
                literalCount,
                value
            );

            return value;
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }
    }

    private static int FirstStackOperandAtOrAfter(
        IReadOnlyList<(int Start, int End, int Slot)> operands,
        int offset
    )
    {
        // The reader records operand starts in strictly increasing byte order.
        int first = 0;
        int end = operands.Count;
        while (first < end)
        {
            int middle = first + (end - first) / 2;
            if (operands[middle].Start < offset)
            {
                first = middle + 1;
            }
            else
            {
                end = middle;
            }
        }

        return first;
    }
}
