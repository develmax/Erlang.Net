using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? CoalescePartialTrimListTails(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(TrimFrame) || !code.Span.Contains(GetList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        int slotIndex = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int allocation = FindLinearFrameSuffix(body, start, end);
            if (allocation < 0 || body[allocation] is not
                { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || width.Value < 2 || ReadPartialTrim(start, allocation, end) is not { } trim
                || body[trim].Operands is not [var drop, var kept]
                || drop.Value <= 0 || kept.Value <= 0 || drop.Value + kept.Value != width.Value
                || ReadPartialTrimListTail(allocation, trim) is not { } tail || tail.Source >= width.Value
                || !(flow ??= new BeamProjectionFlow(body)).OwnsPartialTrimListTail(
                    start,
                    allocation,
                    trim,
                    end,
                    tail.Guard
                ))
            {
                continue;
            }

            // The lowest slot is needed only between these two cons
            // projections. Both read their inputs before writing their outputs.
            // All surviving slots keep the same indices after the original cut.
            output.Write(code.Span[copied..body[allocation].Offset]);
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width.Value - 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live.Value);
            copied = body[allocation + 1].Offset;
            for (int index = allocation + 1; index < trim; index++)
            {
                int offset = body[index].Offset;
                int next = body[index + 1].Offset;
                while (slotIndex < decoded.StackOperands.Count
                    && decoded.StackOperands[slotIndex].Start < offset)
                {
                    slotIndex++;
                }

                if (body[index].Definition.Opcode == InitYRegisters)
                {
                    int[] slots = body[index].Operands.Skip(1).Where(slot => slot.Value > 0)
                        .Select(slot => (int)slot.Value - 1).ToArray();
                    output.Write(code.Span[copied..offset]);
                    if (slots.Length > 0)
                    {
                        output.WriteByte(InitYRegisters);
                        output.WriteByte(ExtendedListOperand);
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, slots.Length);
                        foreach (int slot in slots)
                        {
                            BeamCompactWriter.Write(output, YRegisterTag, slot);
                        }
                    }

                    copied = next;
                    continue;
                }

                while (slotIndex < decoded.StackOperands.Count && decoded.StackOperands[slotIndex].Start < next)
                {
                    var operand = decoded.StackOperands[slotIndex++];
                    output.Write(code.Span[copied..operand.Start]);
                    BeamCompactWriter.Write(output, YRegisterTag, operand.Slot == 0 ? tail.Source - 1 : operand.Slot - 1);
                    copied = operand.End;
                }
            }

            output.Write(code.Span[copied..body[trim].Offset]);
            if (drop.Value > 1)
            {
                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, drop.Value - 1);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, kept.Value);
            }

            copied = body[trim + 1].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int? ReadPartialTrim(int entry, int allocation, int end)
        {
            int? trim = null;
            for (int index = entry; index < end; index++)
            {
                if (index == allocation)
                {
                    continue;
                }

                byte opcode = body[index].Definition.Opcode;
                if (opcode is Allocate or AllocateHeap or CatchEnter or TryEnter or CatchEnd or TryEnd or TryCase
                    or TryCaseEnd or Raise or DebugLine or ExecutableLine or LoopReceive or LoopReceiveEnd
                    or Wait or WaitTimeout or RemoveMessage or ReceiveTimeout)
                {
                    return null;
                }

                if (opcode == TrimFrame)
                {
                    if (trim.HasValue)
                    {
                        return null;
                    }

                    trim = index;
                }
                else if (!trim.HasValue && LinearFrameSizeOperand(opcode) >= 0)
                {
                    return null;
                }
            }

            return trim;
        }

        (int Source, int Guard)? ReadPartialTrimListTail(int allocation, int trim)
        {
            var scratch = new BeamOperand(BeamOperandParameters.Y, 0);
            int[] uses = Enumerable.Range(allocation + 1, trim - allocation - 1)
                .Where(index => body[index].Definition.Opcode != InitYRegisters
                    && body[index].Operands.Any(operand => SameProjectionRegister(operand, scratch))).ToArray();
            if (uses is not [int split, int guard, int consume] || guard != split + 1 || consume != guard + 1
                || body[split] is not { Definition.Opcode: GetList, Operands: [var source, var head, var tail] }
                || source is not { Kind: BeamOperandParameters.Y, Value.Sign: > 0, TypeIndex: null }
                || head is not { Kind: BeamOperandParameters.Y, Value.Sign: > 0, TypeIndex: null }
                || source == head || tail != scratch
                || body[guard] is not
                { Definition.Opcode: IsNonemptyList, Operands: [{ Kind: BeamOperandParameters.Label }, var checkedTail] }
                || checkedTail != scratch)
            {
                return null;
            }

            bool overwritesSource = body[consume] switch
            {
                { Definition.Opcode: GetTail, Operands: [var rest, var result] } => rest == scratch && result == source,
                { Definition.Opcode: GetList, Operands: [var rest, var first, var second] } =>
                    rest == scratch && (first == source && DistinctRetained(second) || second == source && DistinctRetained(first)),
                _ => false
            };

            return overwritesSource ? ((int)source.Value, guard) : null;

            bool DistinctRetained(BeamOperand operand) => operand != source
                && operand is { Kind: BeamOperandParameters.Y, Value.Sign: > 0, TypeIndex: null };
        }
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool OwnsPartialTrimListTail(
            int entry,
            int allocation,
            int trim,
            int end,
            int guard
        )
        {
            if (successors[guard] is not [int rejected, int success] || rejected <= guard || rejected >= trim
                || success != guard + 1)
            {
                return false;
            }

            int failure = rejected + 1;
            if (body[failure] is
                { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.X }] })
            {
                failure++;
            }

            while (failure < trim && body[failure].Definition.Opcode == Line)
            {
                failure++;
            }

            if (failure >= trim || body[failure] is not
                { Definition.Opcode: BadMatch, Operands: [{ Kind: BeamOperandParameters.X }] })
            {
                return false;
            }

            return OwnsSinglePartialTrim(
                entry,
                allocation,
                trim,
                end
            );
        }

        internal bool OwnsSinglePartialTrim(
            int entry,
            int allocation,
            int trim,
            int end
        )
        {
            for (int from = entry; from < end; from++)
            {
                foreach (int target in successors[from])
                {
                    if (target == from + 1)
                    {
                        continue;
                    }

                    if (from < allocation && target > allocation || from >= allocation && target <= allocation
                        || from < trim && target > trim || from >= trim && target <= trim
                        || from >= allocation && target >= end)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
