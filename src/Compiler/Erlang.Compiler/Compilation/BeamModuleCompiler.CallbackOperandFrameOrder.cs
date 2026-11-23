using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? OrderEntryCallbackOperandFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(MakeFun)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stack) = decoded;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var y0 = new BeamOperand(BeamOperandParameters.Y, 0);
        var y1 = new BeamOperand(BeamOperandParameters.Y, 1);
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            for (int allocation = start + 1; allocation + 10 < end; allocation++)
            {
                if (body[allocation] is not
                    {
                        Definition.Opcode: AllocateHeap,
                        Operands: [{ Value: var width }, _, { Value: var live }]
                    } || width != 3 || live != 2
                    || body[allocation + 1] is not
                    {
                        Definition.Opcode: InitYRegisters,
                        Operands: [_, { Kind: BeamOperandParameters.Y, Value: var scratch }]
                    } || scratch != 2 || !IsMove(body[allocation + 2], x0, y0)
                    || !IsMove(body[allocation + 3], x1, y1)
                    || !IsCapturelessCallback(body[allocation + 4], x0)
                    || !IsMove(body[allocation + 5], y1, x1))
                {
                    continue;
                }

                int first = allocation + 6;
                while (first < end && body[first].Definition.Opcode == Line)
                {
                    first++;
                }

                if (first + 7 >= end || !IsBinaryCall(body[first])
                    || !IsMove(body[first + 1], x0, x1) || !IsMove(body[first + 2], y0, x0)
                    || !IsBinaryCall(body[first + 3])
                    || body[first + 4] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, Value: var destination }]
                    } || result != x0 || destination != scratch
                    || body[first + 5].Definition.Opcode != TestHeap
                    || !IsCapturelessCallback(body[first + 6], x0)
                    || !IsMove(body[first + 7], y1, x1)
                    || Enumerable.Range(first + 8, end - first - 8).Any(index =>
                        body[index].Definition.Opcode != InitYRegisters
                        && body[index].Operands.Any(value => SameProjectionRegister(value, y1)))
                    || ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    ) is not { Owners: var owners, Uses: var frames } || frames.Count != 1
                    || owners[first - start] != allocation || owners[first + 7 - start] != allocation)
                {
                    continue;
                }

                // The incoming X1 still carries the second argument for
                // the first callback. Put its shorter-lived save in low Y.
                output.Write(code.Span[copied..body[allocation + 2].Offset]);
                output.WriteByte(Move);
                WriteRegister(output, x1);
                WriteRegister(output, y0);
                output.WriteByte(Move);
                WriteRegister(output, x0);
                WriteRegister(output, y1);
                output.Write(code.Span[body[allocation + 4].Offset..body[allocation + 5].Offset]);
                copied = body[allocation + 6].Offset;
                int operand = FirstStackOperandAtOrAfter(stack, copied);
                for (int index = allocation + 6; index < end; index++)
                {
                    BeamInstruction instruction = body[index];
                    int next = body[index + 1].Offset;
                    if (instruction.Definition.Opcode == InitYRegisters)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        WriteFrameInitialization(output, instruction.Operands.Skip(1)
                            .Select(value => SwapCallbackSlot((int)value.Value)).Order().ToArray());
                        copied = next;
                    }

                    while (operand < stack.Count && stack[operand].Start < next)
                    {
                        var (from, to, slot) = stack[operand++];
                        if (from >= copied && slot < 2)
                        {
                            output.Write(code.Span[copied..from]);
                            BeamCompactWriter.Write(output, YRegisterTag, SwapCallbackSlot(slot));
                            copied = to;
                        }
                    }
                }

                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static int SwapCallbackSlot(int slot) => slot < 2 ? 1 - slot : slot;

        static bool IsCapturelessCallback(BeamInstruction instruction, BeamOperand destination) =>
            instruction is { Definition.Opcode: MakeFun, Operands: [_, var result, { Value.IsZero: true }] }
            && result == destination;

        static bool IsBinaryCall(BeamInstruction instruction) => instruction is
        {
            Definition.Opcode: LocalCall or ExternalCall,
            Operands: [{ Value: var arity }, _]
        } && arity == 2;
    }
}
