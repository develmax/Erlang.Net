using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveSelectFallbackFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Label)> labelOperands,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        for (int function = 0; function + 3 < body.Count; function++)
        {
            if (body[function] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || body[function + 1].Definition.Opcode != Label)
            {
                continue;
            }

            int dispatch = function + 2;
            while (body[dispatch].Definition.Opcode == Line)
            {
                dispatch++;
            }

            if (body[dispatch].Definition.Opcode != SelectValue
                || body[dispatch].Operands[1] is not { Kind: BeamOperandParameters.Label } fallback
                || labelOperands.Count(operand => operand.Label == (int)fallback.Value) != 1)
            {
                continue;
            }

            int first = dispatch + 1;
            while (body[first].Definition.Opcode is not (FunctionInfo or EndCode)
                && !(body[first].Definition.Opcode == Label && body[first].Operands[0].Value == fallback.Value))
            {
                first++;
            }

            if (body[first].Definition.Opcode != Label
                || !EndsBeamBlock(body[first - 1].Definition.Opcode)
                || body[first + 1].Definition.Opcode is not (Allocate or AllocateHeap))
            {
                continue;
            }

            int terminal = first + 2;
            while (body[terminal].Definition.Opcode is Line or InitYRegisters or Move or GetTupleElement or PutList or TestHeap)
            {
                terminal++;
            }

            if (body[terminal].Definition.Opcode != TailExternalCallWithFrame
                || body[terminal].Operands[0].Value >= BeamOperandParameters.MaximumRegister
                || body[terminal + 1].Definition.Opcode is not (Label or EndCode))
            {
                continue;
            }

            var planned = new List<BeamInstruction>(terminal - first + 1);
            var originalIndexes = new Dictionary<int, int>();
            for (int index = first; index <= terminal; index++)
            {
                BeamInstruction instruction = body[index];
                planned.Add(instruction);
                originalIndexes.Add(instruction.Offset, index);
            }

            PlaceSelectFallbackHeapTest(planned);
            ForwardSelectFallbackListInputs(planned);
            int?[]? registers = PlanLeafFrameRegisters(
                planned,
                0,
                planned.Count,
                (int)arity.Value,
                afterCanonicalization: false,
                isArithmetic: null,
                out Dictionary<int, int>? heapRoots,
                isolatedTailBranch: true
            );
            if (registers is null)
            {
                continue;
            }

            using var output = new MemoryStream();
            output.Write(code.Span[..body[first].Offset]);
            var slotsByInstruction = stackOperands.Where(operand =>
                operand.Start >= body[first].Offset && operand.Start < body[terminal + 1].Offset).ToArray();
            for (int index = 0; index < planned.Count; index++)
            {
                BeamInstruction instruction = planned[index];
                int from = instruction.Offset;
                int original = originalIndexes[from];
                int to = body[original + 1].Offset;
                switch (instruction.Definition.Opcode)
                {
                    case Allocate or InitYRegisters:
                        break;
                    case TailExternalCallWithFrame:
                        int frame = (int)instruction.Operands[^1].Value;
                        output.WriteByte(TailExternalCall);
                        output.Write(code.Span[(from + 1)..(to - CompactRegisterCountWidth(frame))]);
                        break;
                    case TestHeap or AllocateHeap:
                        if (instruction.Definition.Opcode == AllocateHeap)
                        {
                            output.WriteByte(TestHeap);
                            from += 1 + CompactRegisterCountWidth((int)instruction.Operands[0].Value);
                        }
                        output.Write(code.Span[from..(to - CompactRegisterCountWidth((int)instruction.Operands[^1].Value))]);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, heapRoots![index]);
                        break;
                    default:
                        WriteSelectFallbackInstruction(
                            output,
                            code,
                            instruction,
                            body[original],
                            to,
                            registers,
                            slotsByInstruction
                        );
                        break;
                }
            }

            output.Write(code.Span[body[terminal + 1].Offset..]);

            return output.ToArray();
        }

        return null;
    }

    private static void PlaceSelectFallbackHeapTest(List<BeamInstruction> planned)
    {
        if (planned[1].Definition.Opcode != Allocate)
        {
            return;
        }

        int first = 2;
        if (planned[first].Definition.Opcode == InitYRegisters)
        {
            first++;
        }

        int heap = first;
        while (planned[heap].Definition.Opcode is Move or GetTupleElement)
        {
            heap++;
        }

        if (planned[heap].Definition.Opcode == TestHeap && heap > first)
        {
            // The pinned SSA code generator accumulates heap need across neutral tuple projections.
            BeamInstruction test = planned[heap];
            planned.RemoveAt(heap);
            planned.Insert(first, test);
        }
    }

    private static void ForwardSelectFallbackListInputs(List<BeamInstruction> planned)
    {
        for (int index = 2; index + 1 < planned.Count; index++)
        {
            if (planned[index] is not
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                    ]
                }
                || planned[index + 1] is not { Definition.Opcode: PutList } list
                || !list.Operands.Take(2).Contains(saved)
                || planned.Skip(index + 2).Any(instruction =>
                    instruction.Operands.Any(operand => operand.Kind == saved.Kind && operand.Value == saved.Value)))
            {
                continue;
            }

            var operands = list.Operands.ToArray();
            for (int position = 0; position < 2; position++)
            {
                if (operands[position] == saved)
                {
                    operands[position] = source;
                }
            }

            planned[index + 1] = list with { Operands = operands };
            planned.RemoveAt(index);
        }
    }

    private static void WriteSelectFallbackInstruction(
        Stream output,
        ReadOnlyMemory<byte> code,
        BeamInstruction instruction,
        BeamInstruction original,
        int end,
        int?[] registers,
        (int Start, int End, int Slot)[] stackOperands
    )
    {
        int from = instruction.Offset;
        int position = 0;
        foreach (var (start, stop, slot) in stackOperands)
        {
            if (start < from || start >= end)
            {
                continue;
            }

            while (original.Operands[position].Kind != BeamOperandParameters.Y)
            {
                position++;
            }

            BeamOperand register = instruction.Operands[position++];
            output.Write(code.Span[from..start]);
            BeamCompactWriter.Write(output, XRegisterTag, register.Kind == BeamOperandParameters.X
                ? register.Value : registers[slot]!.Value);
            from = stop;
        }

        output.Write(code.Span[from..end]);
    }
}
