using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ForwardDuplicatedTrailingFrameReads(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        var edits = new List<CodeEdit>();
        int entry = 0;
        int stackIndex = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int allocation = FindLinearFrameSuffix(body, start, end);
            if (allocation < 0 || body[allocation].Operands[0].Value <= 0
                || body[allocation].Operands[0].Value > BeamOperandParameters.MaximumRegister
                || Enumerable.Range(start, end - start).Any(index =>
                    body[index].Definition.Opcode is DebugLine or ExecutableLine))
            {
                continue;
            }

            while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < body[allocation].Offset)
            {
                stackIndex++;
            }

            if (PlanDuplicatedFrameReads(
                body,
                allocation,
                end,
                (int)body[allocation].Operands[0].Value - 1,
                stackOperands,
                stackIndex
            ) is { } replacements)
            {
                edits.AddRange(replacements);
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }

    private static byte[]? ForwardDuplicatedOwnedFrameReads(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands
    )
    {
        var edits = new List<CodeEdit>();
        int entry = 0;
        int stackIndex = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (!Enumerable.Range(start, end - start).Any(index => body[index] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.Y }]
                })
                || Enumerable.Range(start, end - start).Any(index =>
                    body[index].Definition.Opcode is DebugLine or ExecutableLine)
                || ReadDisjointFrameOwnership(body, start, end) is not { } frames)
            {
                continue;
            }

            foreach (int allocation in frames.Uses.Keys.Order())
            {
                bool[] used = frames.Uses[allocation];
                int lastCandidate = frames.Uses.Count == 1 ? used.Length - 2 : used.Length - 1;
                if (lastCandidate < 0 || frames.Owners[allocation - start] != UnframedOwner)
                {
                    continue;
                }

                int regionEnd = allocation + 1;
                while (regionEnd < end && frames.Owners[regionEnd - start] == allocation)
                {
                    regionEnd++;
                }

                if (Array.IndexOf(frames.Owners, allocation, regionEnd - start) >= 0)
                {
                    continue;
                }

                // The release owns the frame, while its immediate return is unframed.
                if (body[regionEnd - 1].Definition.Opcode == Deallocate
                    && regionEnd < end && body[regionEnd].Definition.Opcode == Return)
                {
                    regionEnd++;
                }

                while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < body[allocation].Offset)
                {
                    stackIndex++;
                }

                for (int slot = lastCandidate; slot >= 0; slot--)
                {
                    if (used[slot] && PlanDuplicatedFrameReads(
                        body,
                        allocation,
                        regionEnd,
                        slot,
                        stackOperands,
                        stackIndex
                    ) is { Count: > 0 } replacements)
                    {
                        edits.AddRange(replacements);
                        break;
                    }
                }
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }

    private static byte[]? ApplyDuplicatedFrameReadEdits(ReadOnlyMemory<byte> code, List<CodeEdit> edits)
    {
        if (edits.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        foreach (CodeEdit edit in edits)
        {
            output.Write(code.Span[copied..edit.Start]);
            if (!edit.IsRemoval)
            {
                BeamCompactWriter.Write(output, edit.Tag, edit.Value);
            }

            copied = edit.End;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static List<CodeEdit>? PlanDuplicatedFrameReads(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int end,
        int slot,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        int stackIndex
    )
    {
        int width = (int)body[allocation].Operands[0].Value;
        int alias = -1;
        bool called = false;
        var edits = new List<CodeEdit>();
        for (int index = allocation + 1; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Deallocate or TailLocalCallWithFrame or TailExternalCallWithFrame)
            {
                int widthOperand = LinearFrameSizeOperand(opcode);
                int next = index + (opcode == Deallocate ? 2 : 1);
                bool returns = opcode != Deallocate || index + 1 < end && body[index + 1].Definition.Opcode == Return;

                return called && returns && instruction.Operands[widthOperand].Value == width
                    && Enumerable.Range(next, end - next).All(position => body[position].Definition.Opcode is Label or Line)
                    ? edits
                    : null;
            }

            switch (opcode)
            {
                case Line:
                    continue;
                case LocalCall:
                case ExternalCall:
                    called = true;
                    alias = -1;
                    continue;
                case InitYRegisters:
                    if (instruction.Operands.Skip(1).Any(operand => operand.Value == slot))
                    {
                        alias = -1;
                    }

                    continue;
                case TestHeap:
                    if (alias >= instruction.Operands[1].Value)
                    {
                        alias = -1;
                    }

                    continue;
                case GcBifOne:
                case GcBifTwo:
                case GcBifThree:
                    if (alias >= instruction.Operands[1].Value)
                    {
                        alias = -1;
                    }

                    break;
                case Move:
                case PutList:
                case PutTuple:
                case GetTupleElement:
                case BifZero:
                case BifOne:
                case BifTwo:
                case BifThree:
                    break;
                default:
                    return null;
            }

            if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label && !operand.Value.IsZero))
            {
                return null;
            }

            BeamOperand destination = instruction.Operands[StackWriteOperand(instruction)];
            if (destination.Kind == BeamOperandParameters.Y && destination.Value == slot)
            {
                if (opcode != Move || destination.TypeIndex is not null
                    || instruction.Operands[0] is not { Kind: BeamOperandParameters.X, TypeIndex: null } source)
                {
                    return null;
                }

                alias = (int)source.Value;
                continue;
            }

            if (instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y && operand.Value == slot))
            {
                if (alias < 0 || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    && operand.Value == slot && operand.TypeIndex is not null))
                {
                    return null;
                }

                int next = body[index + 1].Offset;
                if (opcode == Move && destination.Kind == BeamOperandParameters.X
                    && destination.Value == alias && destination.TypeIndex is null)
                {
                    edits.Add(new CodeEdit(
                        instruction.Offset,
                        next,
                        0,
                        0,
                        IsRemoval: true
                    ));
                }
                else
                {
                    while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < instruction.Offset)
                    {
                        stackIndex++;
                    }

                    while (stackIndex < stackOperands.Count && stackOperands[stackIndex].Start < next)
                    {
                        var (start, finish, register) = stackOperands[stackIndex++];
                        if (register == slot)
                        {
                            edits.Add(new CodeEdit(
                                start,
                                finish,
                                XRegisterTag,
                                alias
                            ));
                        }
                    }
                }
            }

            // Inputs can consume the existing X value before this instruction overwrites it.
            if (destination.Kind == BeamOperandParameters.X && destination.Value == alias)
            {
                alias = -1;
            }
        }

        return null;
    }
}
