using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record OutgoingField(
        int Projection,
        int Load,
        BeamOperand Source,
        BeamOperand Field,
        BeamOperand Argument
    );

    private byte[]? SinkOwnedTupleArgumentFields(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteOwnedTupleArgumentFields(code, decoded.Body, decoded.StackOperands);
    }

    private static byte[]? RewriteOwnedTupleArgumentFields(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack
    )
    {
        using var output = new MemoryStream();
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
            if (!Enumerable.Range(start, end - start).Any(index =>
                    body[index] is { Definition.Opcode: GetTupleElement, Operands: [.., { Kind: BeamOperandParameters.Y }] })
                || ReadDisjointFrameOwnership(
                    body,
                    start,
                    end,
                    trackSiblingTrimEpochs: true
                ) is not { } ownership)
            {
                continue;
            }

            foreach (int allocation in ownership.Uses.Keys.Order())
            {
                int width = ownership.Uses[allocation].Length;
                int release = Array.LastIndexOf(ownership.Owners, allocation) + start;
                if (width < 2 || body[allocation].Definition.Opcode != Allocate || release < allocation + 4
                    || release + 1 >= end || body[release].Definition.Opcode != Deallocate
                    || body[release + 1].Definition.Opcode != Return
                    || Enumerable.Range(allocation + 1, release - allocation).Any(index =>
                        ownership.Owners[index - start] != allocation || body[index].Definition.Opcode == Label)
                    || PlanOwnedTupleArgumentFields(
                        body,
                        allocation,
                        release
                    ) is not { Count: > 0 } fields)
                {
                    continue;
                }

                var projections = fields.Values.Select(field => field.Projection).ToHashSet();
                var loads = fields.Values.ToDictionary(field => field.Load);
                int operand = FirstStackOperandAtOrAfter(stack, body[allocation].Offset);
                for (int index = allocation; index <= release; index++)
                {
                    int from = body[index].Offset;
                    int next = body[index + 1].Offset;
                    while (operand < stack.Count && stack[operand].Start < from)
                    {
                        operand++;
                    }

                    if (projections.Contains(index) || loads.TryGetValue(index, out _))
                    {
                        output.Write(code.Span[copied..from]);
                        if (loads.TryGetValue(index, out var field))
                        {
                            output.WriteByte(GetTupleElement);
                            if (field.Source.Kind == BeamOperandParameters.Y)
                            {
                                BeamCompactWriter.Write(output, YRegisterTag, Remap((int)field.Source.Value));
                            }
                            else
                            {
                                WriteRegister(output, field.Source);
                            }

                            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Field.Value);
                            WriteRegister(output, field.Argument);
                        }

                        copied = next;
                        continue;
                    }

                    if (body[index].Definition.Opcode == InitYRegisters)
                    {
                        output.Write(code.Span[copied..from]);
                        WriteFrameInitialization(
                            output,
                            body[index].Operands.Skip(1).Select(item => (int)item.Value)
                                .Where(slot => !fields.ContainsKey(slot)).Select(Remap).ToArray()
                        );
                        copied = next;
                        continue;
                    }

                    if (index == allocation || index == release)
                    {
                        copied = WritePostCallProjectionOperand(
                            output,
                            code,
                            from,
                            copied,
                            0,
                            width - fields.Count
                        );
                    }

                    while (operand < stack.Count && stack[operand].Start < next)
                    {
                        var (first, after, slot) = stack[operand++];
                        output.Write(code.Span[copied..first]);
                        BeamCompactWriter.Write(output, YRegisterTag, Remap(slot));
                        copied = after;
                    }
                }

                int Remap(int slot) => slot - fields.Keys.Count(removed => removed < slot);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static Dictionary<int, OutgoingField> PlanOwnedTupleArgumentFields(
        IReadOnlyList<BeamInstruction> body,
        int allocation,
        int release
    )
    {
        var fields = new Dictionary<int, OutgoingField>();
        for (int projection = allocation + 1; projection < release - 1; projection++)
        {
            if (body[projection] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } tuple,
                    { Kind: BeamOperandParameters.Unsigned } index,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                    ]
                } || tuple == saved)
            {
                continue;
            }

            // Before the argument copy, only pure setup operations may
            // intervene and none may change the original tuple carrier.
            int load = projection + 1;
            while (load < release && ReadPureTupleArgumentSetup(body[load], tuple, saved))
            {
                load++;
            }

            if (load >= release || body[load] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var through, { Kind: BeamOperandParameters.X, TypeIndex: null } argument]
                } || through != saved)
            {
                continue;
            }

            int call = load + 1;
            while (call < release && ReadPureTupleArgumentSetup(
                body[call],
                argument,
                saved,
                afterCopy: true
            ))
            {
                call++;
            }

            if (call >= release || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[call].Operands[0].Value <= argument.Value
                || Enumerable.Range(allocation + 1, release - allocation - 1).Any(position =>
                    position != projection && position != load && body[position].Definition.Opcode != InitYRegisters
                    && body[position].Operands.Any(item => SameProjectionRegister(item, saved))))
            {
                continue;
            }

            fields.Add((int)saved.Value, new OutgoingField(
                projection,
                load,
                tuple,
                index,
                argument
            ));
        }

        return fields;
    }

    private static bool ReadPureTupleArgumentSetup(
        BeamInstruction instruction,
        BeamOperand preserved,
        BeamOperand saved,
        bool afterCopy = false
    ) =>
        instruction.Definition.Opcode switch
        {
            Line => true,
            InitYRegisters => !instruction.Operands.Any(item =>
                SameProjectionRegister(item, preserved) || !afterCopy && SameProjectionRegister(item, saved)),
            Move or GetTupleElement => !SameProjectionRegister(instruction.Operands[^1], preserved)
                && !instruction.Operands.Any(item => SameProjectionRegister(item, saved)),
            _ => false
        };
}
