using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimOwnedConstructionCallFrames(ReadOnlyMemory<byte> code, int labelCount) =>
        TrimOwnedConstructionCallFramesCore(code, labelCount, checkedRecord: false);

    private byte[]? TrimOwnedCheckedRecordCallFrames(ReadOnlyMemory<byte> code, int labelCount) =>
        TrimOwnedConstructionCallFramesCore(code, labelCount, checkedRecord: true);

    private byte[]? TrimOwnedConstructionCallFramesCore(
        ReadOnlyMemory<byte> code,
        int labelCount,
        bool checkedRecord
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TestHeap)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        for (int boundary = 0; boundary < body.Count; boundary++)
        {
            if (body[boundary].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = boundary + 1;
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            if (body.Skip(start).Take(boundary - start).Any(instruction => instruction.Definition.Opcode == ExecutableLine))
            {
                continue;
            }

            for (int exit = start + 4; exit + 1 < boundary; exit++)
            {
                if (body[exit] is not { Definition.Opcode: Deallocate, Operands: [{ Value: var width }] }
                    || width < 2 || width > BeamOperandParameters.MaximumRegister
                    || body[exit - 1].Definition.Opcode != (checkedRecord ? UpdateRecord : PutTuple)
                    || body[exit + 1].Definition.Opcode != Return)
                {
                    continue;
                }

                int first = exit;
                int removed = (int)width;
                bool readsTuple = false;
                while (first > start && IsTerminalConstructor(first - 1))
                {
                    var instruction = body[--first];
                    readsTuple |= instruction.Definition.Opcode == GetTupleElement;
                    foreach (var operand in instruction.Operands.Where(operand => operand.Kind == BeamOperandParameters.Y))
                    {
                        removed = Math.Min(removed, (int)operand.Value);
                    }
                }

                int heap = first - 1;
                if ((checkedRecord ? removed != 1 : !readsTuple)
                    || removed <= 0 || removed >= width || heap <= start
                    || body[heap] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] })
                {
                    continue;
                }

                int call = heap - 1;
                if (body[call].Definition.Opcode == InitYRegisters)
                {
                    if (body[call].Operands.Skip(1).Any(operand => operand.Value >= removed))
                    {
                        continue;
                    }

                    call--;
                }

                if (call <= start || body[call].Definition.Opcode is not (LocalCall or ExternalCall))
                {
                    continue;
                }

                int site = call;
                while (site > start && body[site - 1].Definition.Opcode == Line)
                {
                    site--;
                }

                if (site > start && body[site - 1] is
                    {
                        Definition.Opcode: Move,
                        Operands: [
                        {
                            Kind: BeamOperandParameters.Atom or BeamOperandParameters.Nil
                            or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue
                        },
                        { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }]
                    })
                {
                    site--;
                }

                if (body[site].Offset < copied
                    || (checkedRecord && (call != heap - 1 || !HasCheckedConstructionCall(
                        body,
                        start,
                        site,
                        call
                    )))
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        boundary,
                        trackSiblingTrimEpochs: checkedRecord
                    )) is not { } frames
                    || !frames.Uses.TryGetValue(frames.Owners[site - start], out bool[]? slots) || slots.Length != width)
                {
                    continue;
                }

                // The unprotected terminal path reads only the surviving tail
                // of its frame after the call. Its removed slots need no kills.
                int remaining = (int)width - removed;
                output.Write(code.Span[copied..body[site].Offset]);
                output.WriteByte(TrimFrame);
                Unsigned(removed);
                Unsigned(remaining);
                output.Write(code.Span[body[site].Offset..body[call + 1].Offset]);
                int from = body[heap].Offset;
                int end = body[exit].Offset;
                foreach (var (operandStart, operandEnd, slot) in stackOperands)
                {
                    if (operandStart < from || operandStart >= end)
                    {
                        continue;
                    }

                    output.Write(code.Span[from..operandStart]);
                    BeamCompactWriter.Write(output, YRegisterTag, slot - removed);
                    from = operandEnd;
                }

                output.Write(code.Span[from..end]);
                output.WriteByte(Deallocate);
                Unsigned(remaining);
                copied = body[exit + 1].Offset;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, TrimFrame);
        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsTerminalConstructor(int index) => body[index] switch
        {
            { Definition.Opcode: GetTupleElement, Operands: [{ Kind: BeamOperandParameters.Y }, _, { Kind: BeamOperandParameters.X }] }
                => true,
            { Definition.Opcode: PutTuple, Operands: [{ Kind: BeamOperandParameters.X }, ..] } => true,
            { Definition.Opcode: PutList, Operands: [_, _, { Kind: BeamOperandParameters.X }] } => true,
            { Definition.Opcode: UpdateRecord, Operands: [_, _, _, { Kind: BeamOperandParameters.X }, ..] }
                => checkedRecord,
            _ => false
        };

        void Unsigned(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);
    }

    private static bool HasCheckedConstructionCall(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int site,
        int call
    )
    {
        if (site - 4 < start || !IsOwnedProjectionUnaryCall(body[call]))
        {
            return false;
        }

        int projection = site - 1;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        BeamOperand? savedComparison = null;
        if (body[projection] is
            {
                Definition.Opcode: Move,
                Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } retained]
            } && result == x1)
        {
            savedComparison = retained;
            projection--;
        }

        return projection - 3 >= start
            && body[projection] is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ Kind: BeamOperandParameters.Y, Value.Sign: > 0, TypeIndex: null } carrier, _, var argument]
            } && argument == x0 && (savedComparison is null || savedComparison == carrier)
            && body[projection - 1] is
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 }, var left, var right]
            } && SameProjectionRegister(left, x0) && right == x1
            && body[projection - 2] is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var comparedCarrier, _, var compared]
            } && comparedCarrier == carrier && compared == x1
            && IsOwnedProjectionUnaryCall(body[projection - 3]);
    }
}
