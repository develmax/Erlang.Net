using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ShareCheckedEntryRecordFields(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var carrier = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 7 < body.Count; info++)
        {
            int entry = info + 1;
            int selector = info + 4;
            int first = selector + 1;
            if (body[info].Definition.Opcode != FunctionInfo
                || body[entry].Definition.Opcode != Label
                || body[entry + 1] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var invalidShape, var tuple, { Kind: BeamOperandParameters.Unsigned } arity,
                    { Kind: BeamOperandParameters.Atom }]
                } || tuple != carrier
                || body[entry + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, var selectedField, var selected]
                } || source != carrier || selectedField.Value <= 0 || selectedField.Value >= arity.Value
                || selected is not { Kind: BeamOperandParameters.X, TypeIndex: null } || selected == carrier
                || body[selector] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var discriminator, { Kind: BeamOperandParameters.Atom }]
                } || discriminator != selected || rejected == invalidShape
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(selector, (int)rejected.Value, out int retry)
                || retry <= first || body[retry].Definition.Opcode != Label
                || body.Skip(first).Take(retry - first)
                    .Any(instruction => instruction.Definition.Opcode is FunctionInfo or EndCode)
                || !HasUnprotectedCallKillSchedule(body, entry))
            {
                continue;
            }

            int functionEnd = retry + 1;
            while (body[functionEnd].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                functionEnd++;
            }

            int last = first;
            var registers = new HashSet<BeamOperand> { carrier, selected };
            var field = selectedField.Value;
            while (last < retry && body[last] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var reader, var projectedField, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                } && reader == carrier && projectedField.Value > field && projectedField.Value < arity.Value
                && registers.Add(destination) && retry + 1 + last - first < functionEnd
                && body[retry + 1 + last - first].Definition.Opcode == GetTupleElement
                && body[retry + 1 + last - first].Operands.SequenceEqual(body[last].Operands))
            {
                field = projectedField.Value;
                last++;
            }

            int fallback = retry + 1 + last - first;
            if (last == first || fallback >= functionEnd || !flow.HasStraightCheckedCallPrefix(entry, last)
                || !flow.HasStraightCheckedCallPrefix(retry, fallback))
            {
                continue;
            }

            var result = body[last - 1].Operands[2];
            if (ReadCheckedRecordCallPrefix(
                body,
                flow,
                last,
                retry,
                carrier,
                result
            ) is not { } hot
                || ReadCheckedRecordCallPrefix(
                    body,
                    flow,
                    fallback,
                    functionEnd,
                    carrier,
                    result
                ) is not { } cold)
            {
                continue;
            }

            // Both exclusive successors project the same bounded fields. The
            // old tuple is dead at their first frames and the last payload can
            // occupy its X register until the outgoing arguments are ready.
            output.Write(code.Span[copied..body[selector].Offset]);
            output.Write(code.Span[body[first].Offset..body[last - 1].Offset]);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, carrier);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field);
            WriteRegister(output, carrier);
            output.Write(code.Span[body[selector].Offset..body[first].Offset]);
            WriteCheckedRecordCallPrefix(
                code,
                body,
                output,
                hot,
                carrier
            );
            output.Write(code.Span[body[hot.Call].Offset..body[retry + 1].Offset]);
            WriteCheckedRecordCallPrefix(
                code,
                body,
                output,
                cold,
                carrier
            );
            copied = body[cold.Call].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private sealed record CheckedRecordCallPrefix(
        int Allocation,
        int Call,
        int Live,
        List<int> Reloads
    );

    private static CheckedRecordCallPrefix? ReadCheckedRecordCallPrefix(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int allocation,
        int end,
        BeamOperand carrier,
        BeamOperand payload
    )
    {
        BeamInstruction frame = body[allocation];
        if (frame.Definition.Opcode is not (Allocate or AllocateHeap)
            || frame.Operands.Any(operand => operand.Kind != BeamOperandParameters.Unsigned))
        {
            return null;
        }

        var written = new HashSet<int>();
        var reloads = new List<int>();
        int live = 0;
        bool originalTupleDead = false;
        bool carrierHoldsPayload = true;
        for (int index = allocation + 1; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is LocalCall or ExternalCall)
            {
                int arity = (int)instruction.Operands[0].Value;
                if (!originalTupleDead || arity > payload.Value
                    || !flow.HasStraightCheckedCallPrefix(allocation, index))
                {
                    return null;
                }

                for (int argument = 0; argument < arity; argument++)
                {
                    RequireInput(argument);
                }

                return live <= frame.Operands[^1].Value
                    ? new CheckedRecordCallPrefix(
                        allocation,
                        index,
                        live,
                        reloads
                    )
                    : null;
            }

            if (instruction.Definition.Opcode is InitYRegisters or Line)
            {
                continue;
            }

            if (instruction.Definition.Opcode is not (Move or PutList))
            {
                return null;
            }

            BeamOperand target = instruction.Operands[^1];
            if (!IsBareProjectionRegister(target) || SameProjectionRegister(target, payload))
            {
                return null;
            }

            bool copiesPayload = false;
            foreach (var operand in instruction.Operands.Take(instruction.Operands.Count - 1))
            {
                if (operand.Kind != BeamOperandParameters.X)
                {
                    continue;
                }

                if (SameProjectionRegister(operand, carrier) && !originalTupleDead)
                {
                    return null;
                }

                if (SameProjectionRegister(operand, payload))
                {
                    if (instruction.Definition.Opcode != Move || operand.TypeIndex is not null || !carrierHoldsPayload)
                    {
                        return null;
                    }

                    reloads.Add(index);
                    RequireInput(0);
                    copiesPayload = true;
                }
                else
                {
                    RequireInput((int)operand.Value);
                }
            }

            if (target.Kind == BeamOperandParameters.X)
            {
                if (target == carrier)
                {
                    originalTupleDead = true;
                    carrierHoldsPayload = copiesPayload;
                }

                written.Add((int)target.Value);
            }
        }

        return null;

        void RequireInput(int register)
        {
            if (!written.Contains(register))
            {
                live = Math.Max(live, register + 1);
            }
        }
    }

    private static void WriteCheckedRecordCallPrefix(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Stream output,
        CheckedRecordCallPrefix prefix,
        BeamOperand carrier
    )
    {
        BeamInstruction frame = body[prefix.Allocation];
        output.WriteByte(frame.Definition.Opcode);
        foreach (var size in frame.Operands.Take(frame.Operands.Count - 1))
        {
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, size.Value);
        }

        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, prefix.Live);
        int copied = body[prefix.Allocation + 1].Offset;
        foreach (int reload in prefix.Reloads)
        {
            output.Write(code.Span[copied..body[reload].Offset]);
            BeamOperand target = body[reload].Operands[1];
            if (target != carrier)
            {
                output.WriteByte(Move);
                WriteRegister(output, carrier);
                WriteRegister(output, target);
            }

            copied = body[reload + 1].Offset;
        }

        output.Write(code.Span[copied..body[prefix.Call].Offset]);
    }
}
