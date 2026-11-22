using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleTwoResultFrameTrims(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TrimFrame)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, operands) = decoded;
        using var output = new MemoryStream();
        int copied = 0;
        int first = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = first;
            first = end + 1;
            if (entry + 1 >= end || body[entry].Definition.Opcode != Label
                || body[entry + 1] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value: var width }, _]
                } || width != 2 || !ReadCallFrame(
                    body,
                    entry + 2,
                    end,
                    2,
                    out var lastUse,
                    out int release,
                    allowCompleteRelease: true
                ) || body[release].Definition.Opcode != TrimFrame
                || ReadTwoResultFrameTrim(
                    body,
                    entry + 2,
                    release,
                    end,
                    lastUse
                ) is not { } retirement)
            {
                continue;
            }

            // Slot zero's result survives the first consumer. Assign it to Y1
            // until the dead prefix is released, then it is again physically Y0.
            int operand = FirstStackOperandAtOrAfter(operands, body[retirement.FirstSave].Offset);
            int clear = body[retirement.Clear].Offset;
            while (operand < operands.Count && operands[operand].Start < clear)
            {
                var (from, to, slot) = operands[operand++];
                output.Write(code.Span[copied..from]);
                BeamCompactWriter.Write(output, YRegisterTag, 1 - slot);
                copied = to;
            }

            output.Write(code.Span[copied..clear]);
            WriteTrim(remaining: 1);
            output.Write(code.Span[body[retirement.Clear + 1].Offset..body[release].Offset]);
            WriteTrim(remaining: 0);
            copied = body[release + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteTrim(int remaining)
        {
            output.WriteByte(TrimFrame);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, remaining);
        }
    }

    private static (int FirstSave, int Clear)? ReadTwoResultFrameTrim(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int release,
        int end,
        IReadOnlyList<int> lastUse
    )
    {
        const string X = BeamOperandParameters.X;
        const string Y = BeamOperandParameters.Y;

        int[] core = Enumerable.Range(start, release - start)
            .Where(index => body[index].Definition.Opcode != Line).ToArray();
        if (core is not
            [
                var initial, var first, var firstSave, var second, var secondSave, var third,
                var firstCopy, var firstLoad, var clear, var firstConsumer, var secondCopy, var secondLoad
            ]
            || body[initial] is not
            {
                Definition.Opcode: InitYRegisters,
                Operands: [{ Value: var initialized }, { Kind: BeamOperandParameters.Y, Value.IsZero: true },
                { Kind: BeamOperandParameters.Y, Value.IsOne: true }]
            } || initialized != 2
            || !Ordinary(first) || !Ordinary(second) || !Ordinary(third)
            || lastUse[0] != secondLoad || lastUse[1] != clear
            || !RegisterMove(
                firstSave,
                X,
                0,
                Y,
                0
            )
            || !RegisterMove(
                secondSave,
                X,
                0,
                Y,
                1
            )
            || !RegisterMove(
                firstCopy,
                X,
                0,
                X,
                1
            )
            || !RegisterMove(
                firstLoad,
                Y,
                1,
                X,
                0
            )
            || !RegisterMove(
                secondCopy,
                X,
                0,
                X,
                1
            )
            || !RegisterMove(
                secondLoad,
                Y,
                0,
                X,
                0
            )
            || body[clear] is not
            {
                Definition.Opcode: InitYRegisters,
                Operands: [{ Value.IsOne: true }, { Kind: BeamOperandParameters.Y, Value.IsOne: true }]
            })
        {
            return null;
        }

        int secondConsumer = release + 1;
        while (secondConsumer < end && body[secondConsumer].Definition.Opcode == Line)
        {
            secondConsumer++;
        }

        if (secondConsumer >= end || !Ordinary(firstConsumer) || !Ordinary(secondConsumer)
            || body[firstConsumer].Operands[0].Value != 2 || body[secondConsumer].Operands[0].Value != 2)
        {
            return null;
        }

        return (firstSave, clear);

        bool Ordinary(int index) => body[index].Definition.Opcode is LocalCall or ExternalCall;

        bool RegisterMove(
            int index,
            string sourceKind,
            int source,
            string destinationKind,
            int destination
        ) => body[index] is
        {
            Definition.Opcode: Move,
            Operands: [{ TypeIndex: null } input, { TypeIndex: null } target]
        } && input.Kind == sourceKind && input.Value == source
            && target.Kind == destinationKind && target.Value == destination;
    }
}
