using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? EmitUnusedMapFieldPresence(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetMapElements)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        for (int end = 1; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (EndCode or FunctionInfo))
            {
                continue;
            }

            if (!body.Skip(entry).Take(end - entry).Any(instruction => instruction.Definition.Opcode
                is TryEnter or CatchEnter or DebugLine or ExecutableLine))
            {
                for (int index = entry + 1; index + 1 < end; index++)
                {
                    Rewrite(index, entry, end);
                }
            }

            entry = end + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Rewrite(int index, int start, int end)
        {
            BeamInstruction operation = body[index];
            if (operation is not
                {
                    Definition.Opcode: GetMapElements,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Label } failure,
                    { Kind: BeamOperandParameters.Y } map,
                        _,
                    { Kind: BeamOperandParameters.Atom } firstKey,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } first,
                    { Kind: BeamOperandParameters.Atom } secondKey,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } second
                    ]
                } || firstKey.Value == secondKey.Value
                || SameProjectionRegister(first, second) || SameProjectionRegister(map, first)
                || SameProjectionRegister(map, second)
                || body[index - 1] is not
                {
                    Definition.Opcode: IsMap,
                    Operands: [var typeFailure, var checkedMap]
                } || typeFailure != failure || !SameProjectionRegister(map, checkedMap))
            {
                return;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetForwardLabel(index, (int)failure.Value, out int badMatch)
                || !PreservesBadMatch(
                    body,
                    badMatch,
                    end,
                    map
                ))
            {
                return;
            }

            int success = index + 1;
            if (body[success] is { Definition.Opcode: Jump, Operands: [var jump] })
            {
                if (!flow.TryGetForwardLabel(success, (int)jump.Value, out success))
                {
                    return;
                }

                success++;
            }

            if (!ReadMapTerminalUses(
                body,
                success,
                end,
                first,
                second,
                out bool firstUsed,
                out bool secondUsed
            ) || firstUsed == secondUsed)
            {
                return;
            }

            BeamOperand unused = firstUsed ? second : first;
            if (!HasInitializedMapOutput(
                body,
                index,
                start,
                unused
            ))
            {
                return;
            }

            BeamOperand presence = firstUsed ? secondKey : firstKey;
            BeamOperand retainedKey = firstUsed ? firstKey : secondKey;
            BeamOperand retained = firstUsed ? first : second;
            output.Write(code.Span[copied..operation.Offset]);
            output.WriteByte(HasMapFields);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            WriteRegister(output, map);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, AtomTag, presence.Value);
            output.WriteByte(GetMapElements);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            if (map.TypeIndex is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            WriteRegister(output, map);
            if (map.TypeIndex is int type)
            {
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }

            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 2);
            BeamCompactWriter.Write(output, AtomTag, retainedKey.Value);
            WriteRegister(output, retained);
            copied = body[index + 1].Offset;
        }
    }

    private static bool PreservesBadMatch(
        IReadOnlyList<BeamInstruction> body,
        int label,
        int end,
        BeamOperand map
    )
    {
        int move = SkipLines(label + 1);
        if (move >= end || body[move] is not
            {
                Definition.Opcode: Move,
                Operands: [var source, { Kind: BeamOperandParameters.X, Value.IsZero: true }]
            } || !SameProjectionRegister(source, map))
        {
            return false;
        }

        int bad = SkipLines(move + 1);

        return bad < end && body[bad] is
        {
            Definition.Opcode: BadMatch,
            Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true }]
        };

        int SkipLines(int index)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }
    }

    private static bool ReadMapTerminalUses(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        BeamOperand first,
        BeamOperand second,
        out bool firstUsed,
        out bool secondUsed
    )
    {
        firstUsed = false;
        secondUsed = false;
        for (int index = start; index + 1 < end; index++)
        {
            BeamInstruction item = body[index];
            if (item.Definition.Opcode == Deallocate)
            {
                return body[index + 1].Definition.Opcode == Return;
            }

            if (item.Definition.Opcode is not (Line or Move or TestHeap or PutTuple or PutList))
            {
                return false;
            }

            firstUsed |= item.Operands.Any(operand => SameProjectionRegister(first, operand));
            secondUsed |= item.Operands.Any(operand => SameProjectionRegister(second, operand));
        }

        return false;
    }

    private static bool HasInitializedMapOutput(
        IReadOnlyList<BeamInstruction> body,
        int projection,
        int start,
        BeamOperand slot
    )
    {
        bool initialized = false;
        for (int index = projection - 1; index >= start; index--)
        {
            BeamInstruction item = body[index];
            switch (item.Definition.Opcode)
            {
                case Allocate:
                    return initialized && slot.Value < item.Operands[0].Value;
                case Label or TrimFrame or Deallocate or AllocateHeap:
                    return false;
                case InitYRegisters:
                    initialized |= item.Operands.Any(operand => SameProjectionRegister(slot, operand));
                    break;
            }
        }

        return false;
    }
}
