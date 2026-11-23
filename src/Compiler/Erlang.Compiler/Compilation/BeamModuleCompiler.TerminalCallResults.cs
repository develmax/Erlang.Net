using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepTerminalCallResultsInX0(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int stackIndex = 0;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        bool ownsSelector = false;
        for (int index = 0; index + 6 < body.Count; index++)
        {
            byte opcode = body[index].Definition.Opcode;
            if (opcode == FunctionInfo)
            {
                ownsSelector = body[index + 1].Definition.Opcode == Label
                    && registerSelectorEntries.Contains((int)body[index + 1].Operands[0].Value);
            }

            bool producesResult = opcode is LocalCall or ExternalCall;
            if (!producesResult && !(opcode == Label && ownsSelector))
            {
                continue;
            }

            int spill = index + 1;
            // The predicate goes before the spill. Its failure edge still sees the old frame slot.
            if (producesResult && body[spill] is
                {
                    Definition.Opcode: IsInteger,
                    Operands: [{ Kind: BeamOperandParameters.Label }, var tested]
                } && SameProjectionRegister(tested, result))
            {
                spill++;
            }

            if (spill + 4 >= body.Count
                || body[spill] is not { Definition.Opcode: Move, Operands: [var source, var saved] }
                || source != result || saved.Kind != BeamOperandParameters.Y
                || body[spill + 1] is not { Definition.Opcode: TestHeap, Operands: [_, var live] } heap
                || !live.Value.IsZero || body[spill + 3].Definition.Opcode != Deallocate
                || body[spill + 4].Definition.Opcode != Return)
            {
                continue;
            }

            BeamInstruction construction = body[spill + 2];
            bool returnsSavedResult = construction.Definition.Opcode switch
            {
                PutTuple => construction.Operands[0] == result
                    && construction.Operands.Skip(2).Any(operand => SameProjectionRegister(operand, saved)),
                PutList => construction.Operands[2] == result
                    && (SameProjectionRegister(construction.Operands[0], saved)
                        || SameProjectionRegister(construction.Operands[1], saved)),
                MakeFun => construction.Operands[1] == result
                    && construction.Operands.Skip(3).Any(operand => SameProjectionRegister(operand, saved)),
                _ => false
            };
            if (!returnsSavedResult)
            {
                continue;
            }

            output.Write(code.Span[copied..body[spill].Offset]);
            // The final compact operand of test_heap is the zero live-register count.
            output.Write(code.Span[heap.Offset..(construction.Offset - 1)]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            copied = construction.Offset;
            while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < construction.Offset)
            {
                stackIndex++;
            }

            int end = body[spill + 3].Offset;
            while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < end)
            {
                var (start, finish, slot) = stackOperands[stackIndex++];
                if (slot == saved.Value)
                {
                    output.Write(code.Span[copied..start]);
                    WriteRegister(output, result);
                    copied = finish;
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static byte[]? ForwardCallResult(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        int inspectedThrough = -1;
        bool hasHandler = false;
        BeamProjectionFlow? flow = null;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        for (int index = 0; index + 3 < body.Count; index++)
        {
            if (body[index].Definition.Opcode is not (LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped)
                || body[index + 1] is not { Definition.Opcode: Move, Operands: [var source, var saved] }
                || source != result || saved.Kind != BeamOperandParameters.Y
                || FindResultReload(body, index + 2, saved) is not { } setup)
            {
                continue;
            }

            var (reload, call) = setup;
            if (body[call].Definition.Opcode is LocalCall or ExternalCall)
            {
                if (index >= inspectedThrough)
                {
                    int start = index;
                    while (start > 0 && body[start - 1].Definition.Opcode != FunctionInfo)
                    {
                        start--;
                    }

                    inspectedThrough = start;
                    hasHandler = false;
                    while (inspectedThrough < body.Count
                        && body[inspectedThrough].Definition.Opcode is not (FunctionInfo or EndCode))
                    {
                        hasHandler |= body[inspectedThrough].Definition.Opcode is TryEnter or CatchEnter;
                        inspectedThrough++;
                    }
                }

                // The graph models normal edges. A handler could also read the saved slot.
                if (hasHandler || !(flow ??= new BeamProjectionFlow(body)).IsDead(call, saved))
                {
                    continue;
                }
            }

            BeamOperand destination = body[reload].Operands[1];
            output.Write(code.Span[copied..body[index + 1].Offset]);
            if (destination != result)
            {
                output.WriteByte(Move);
                WriteRegister(output, result);
                WriteRegister(output, destination);
            }

            int last = call - 1;
            while (last > index + 1 && body[last].Definition.Opcode == Line)
            {
                last--;
            }

            int previous = last - 1 == reload ? last - 2 : last - 1;
            // OTP leaves the independent X0 load next to the apply.
            bool orderApply = body[call].Definition.Opcode == TailDynamicApply
                && previous >= index + 2 && last != reload
                && body[previous] is { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Y }, var argument] }
                && argument == result
                && body[last] is { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Atom }, var function] }
                && function.Kind == BeamOperandParameters.X && function.Value == body[call].Operands[0].Value + 1;
            for (int move = index + 2; move < call; move++)
            {
                if (move == reload)
                {
                    continue;
                }

                int emitted = orderApply && move == previous ? last
                    : orderApply && move == last ? previous : move;
                output.Write(code.Span[body[emitted].Offset..body[emitted + 1].Offset]);
            }

            copied = body[call].Offset;
            index = call - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Reload, int Call)? FindResultReload(IReadOnlyList<BeamInstruction> body, int start, BeamOperand saved)
    {
        var touched = new HashSet<int>();
        int? reload = null;
        for (int index = start; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == Line)
            {
                continue;
            }

            if (instruction is not { Definition.Opcode: Move, Operands: [var source, var destination] })
            {
                return reload is int found
                    && instruction.Definition.Opcode is LocalCall or ExternalCall
                        or TailExternalCallWithFrame or TailLocalCallWithFrame or TailDynamicApply
                            ? (found, index) : null;
            }

            if (SameProjectionRegister(source, saved))
            {
                if (reload.HasValue || destination is not { Kind: BeamOperandParameters.X, TypeIndex: null }
                    || touched.Contains((int)destination.Value))
                {
                    return null;
                }

                reload = index;
            }
            else if (SameProjectionRegister(destination, saved))
            {
                return null;
            }
            else if (reload is null)
            {
                // An earlier move must neither read nor overwrite the replacement register.
                foreach (BeamOperand operand in instruction.Operands)
                {
                    if (operand.Kind == BeamOperandParameters.X)
                    {
                        touched.Add((int)operand.Value);
                    }
                }
            }
        }

        return null;
    }
}
