using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ForwardInteriorCallArgumentCopies(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            int allocation = FindEntryFrameAllocation(body, entry, end);
            if (allocation < 0 || body[allocation].Definition.Opcode != Allocate)
            {
                continue;
            }

            var references = new Dictionary<int, int>();
            var copies = new Dictionary<int, List<(int Save, int Load, BeamOperand Source)>>();
            if (!CollectInteriorCopies(
                allocation,
                end,
                references,
                copies
            ))
            {
                continue;
            }

            var selected = new SortedDictionary<int, BeamOperand>();
            foreach (var (register, group) in copies)
            {
                // A slot is only eligible if every semantic reference belongs to one of these copies.
                if (references[register] != group.Count * 2)
                {
                    continue;
                }

                BeamProjectionFlow analysis = flow ??= new BeamProjectionFlow(body);
                BeamOperand slot = body[group[0].Save].Operands[1];
                if (!group.All(copy => analysis.HasSingleStackCopyUse(
                    entry,
                    copy.Save,
                    end,
                    slot,
                    copy.Load
                )))
                {
                    continue;
                }

                foreach (var (_, load, source) in group)
                {
                    selected.Add(load, source);
                }
            }

            foreach (var (load, source) in selected)
            {
                BeamInstruction reload = body[load];
                output.Write(code.Span[copied..reload.Offset]);
                if (reload.Operands[1] != source)
                {
                    output.WriteByte(Move);
                    WriteRegister(output, source);
                    WriteRegister(output, reload.Operands[1]);
                }

                // Keep the stores and initialization until stack compaction proves the frame layout.
                copied = body[load + 1].Offset;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool CollectInteriorCopies(
            int allocation,
            int end,
            Dictionary<int, int> references,
            Dictionary<int, List<(int Save, int Load, BeamOperand Source)>> copies
        )
        {
            bool afterCall = false;
            for (int index = allocation + 1; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is Allocate or AllocateHeap or TrimFrame or TryEnter or CatchEnter
                    or LoopReceive or LoopReceiveEnd or RemoveMessage or ReceiveTimeout or Wait or WaitTimeout
                    or ReceiveMarkerBind or ReceiveMarkerClear or ReceiveMarkerReserve or ReceiveMarkerUse
                    || instruction.Definition.Name is "debug_line" or "executable_line")
                {
                    return false;
                }

                if (opcode != InitYRegisters)
                {
                    foreach (BeamOperand operand in instruction.Operands)
                    {
                        if (operand.Kind == BeamOperandParameters.Y)
                        {
                            int register = (int)operand.Value;
                            references[register] = references.GetValueOrDefault(register) + 1;
                        }
                    }
                }

                if (afterCall && instruction is
                    {
                        Definition.Opcode: Move,
                        Operands:
                        [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } slot
                        ]
                    } && FindInteriorReload(
                        index + 1,
                        end,
                        source,
                        slot
                    ) is int load)
                {
                    int register = (int)slot.Value;
                    if (!copies.TryGetValue(register, out var group))
                    {
                        group = [];
                        copies.Add(register, group);
                    }

                    group.Add((index, load, source));
                }

                afterCall |= opcode is LocalCall or ExternalCall or DynamicApply or CallFun or CallFunTyped or Send;
            }

            return true;
        }

        int? FindInteriorReload(
            int start,
            int end,
            BeamOperand source,
            BeamOperand slot
        )
        {
            for (int index = start; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == Line)
                {
                    continue;
                }

                if (instruction is not { Definition.Opcode: Move, Operands: [var read, var written] })
                {
                    return null;
                }

                if (SameProjectionRegister(read, slot))
                {
                    return read.TypeIndex is null && written is { Kind: BeamOperandParameters.X, TypeIndex: null }
                        && FeedsStaticBodyCall(index + 1, end, written) ? index : null;
                }

                if (SameProjectionRegister(written, source) || SameProjectionRegister(written, slot))
                {
                    return null;
                }
            }

            return null;
        }

        bool FeedsStaticBodyCall(int start, int end, BeamOperand target)
        {
            for (int index = start; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == Line)
                {
                    continue;
                }

                if (instruction.Definition.Opcode is LocalCall or ExternalCall)
                {
                    return target.Value < instruction.Operands[0].Value;
                }

                if (instruction.Definition.Opcode != Move
                    || instruction.Operands.Any(operand => SameProjectionRegister(operand, target)))
                {
                    return false;
                }
            }

            return false;
        }
    }
}
