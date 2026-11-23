using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DeferConstructionFieldPastCalls(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var stack = decoded.StackOperands;
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
            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int first = start; first + 5 < end; first++)
            {
                if (body[first] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } tuple,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } carrier]
                    }
                    || body[first + 1] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var source, { Kind: BeamOperandParameters.Unsigned } field,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                    } || source != tuple || carrier == saved
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } owners)
                {
                    continue;
                }

                int allocation = owners.Owners[first - start];
                if (allocation < start || body[allocation].Offset < copied
                    || body[allocation].Definition.Opcode != Allocate
                    || owners.Uses[allocation].Length < 2
                    || PlanDeferredConstructionField(
                        body,
                        owners.Owners,
                        start,
                        end,
                        allocation,
                        first,
                        carrier,
                        saved
                    ) is not { } plan)
                {
                    continue;
                }

                var (consumer, release, register) = plan;
                int slot = (int)saved.Value;
                int holder = (int)carrier.Value;
                int width = owners.Uses[allocation].Length;
                int operand = FirstStackOperandAtOrAfter(stack, body[allocation].Offset);
                for (int index = allocation; index <= release; index++)
                {
                    int offset = body[index].Offset;
                    int next = body[index + 1].Offset;
                    while (operand < stack.Count && stack[operand].Start < offset)
                    {
                        operand++;
                    }

                    if (index == first + 1)
                    {
                        output.Write(code.Span[copied..offset]);
                        copied = next;
                        continue;
                    }

                    if (body[index].Definition.Opcode == InitYRegisters)
                    {
                        output.Write(code.Span[copied..offset]);
                        WriteFrameInitialization(
                            output,
                            body[index].Operands.Skip(1).Select(item => (int)item.Value)
                                .Where(item => item != slot).Select(Remap).ToArray()
                        );
                        copied = next;
                        continue;
                    }

                    if (index == allocation || index == release)
                    {
                        copied = WritePostCallProjectionOperand(
                            output,
                            code,
                            offset,
                            copied,
                            LinearFrameSizeOperand(body[index].Definition.Opcode),
                            width - 1
                        );
                    }

                    if (index == consumer)
                    {
                        // The constructor overwrites this X register without reading
                        // its earlier value. No collector lies between the read and use.
                        output.Write(code.Span[copied..offset]);
                        output.WriteByte(GetTupleElement);
                        BeamCompactWriter.Write(output, YRegisterTag, Remap(holder));
                        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                        BeamCompactWriter.Write(output, XRegisterTag, register);
                        copied = offset;
                    }

                    while (operand < stack.Count && stack[operand].Start < next)
                    {
                        var (from, to, old) = stack[operand++];
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(
                            output,
                            old == slot ? XRegisterTag : YRegisterTag,
                            old == slot ? register : Remap(old)
                        );
                        copied = to;
                    }
                }

                first = release;

                int Remap(int old) => old > slot ? old - 1 : old;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Consumer, int Release, int Register)? PlanDeferredConstructionField(
        IReadOnlyList<BeamInstruction> body,
        int[] owners,
        int start,
        int end,
        int allocation,
        int save,
        BeamOperand carrier,
        BeamOperand saved
    )
    {
        int consumer = -1;
        int register = -1;
        bool passedCall = false;
        for (int index = allocation + 1; index < end && owners[index - start] == allocation; index++)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode == Deallocate)
            {
                return consumer >= 0
                    && index + 1 < end && body[index + 1].Definition.Opcode == Return
                    && Array.LastIndexOf(owners, allocation) == index - start
                    ? (consumer, index, register)
                    : null;
            }

            if (opcode == Label || opcode == TrimFrame || LinearFrameSizeOperand(opcode) >= 0
                || opcode is not (LocalCall or ExternalCall)
                    && instruction.Operands.Any(item => item.Kind == BeamOperandParameters.Label))
            {
                return null;
            }

            if (opcode == InitYRegisters)
            {
                if (index > save && consumer < 0
                    && instruction.Operands.Any(item => SameProjectionRegister(item, carrier)))
                {
                    return null;
                }

                continue;
            }

            if (index > save + 1 && consumer < 0)
            {
                if (opcode is not (Move or GetTupleElement or Line or LocalCall or ExternalCall
                    or TestHeap or PutTuple or PutList)
                    || instruction.Operands.Any(item => SameProjectionRegister(item, carrier)))
                {
                    return null;
                }

                passedCall |= opcode is LocalCall or ExternalCall;
            }

            if (index == save + 1 || !instruction.Operands.Any(item => SameProjectionRegister(item, saved)))
            {
                continue;
            }

            if (!passedCall || consumer >= 0 || opcode is not (PutTuple or PutList)
                || instruction.Operands.Count(item => SameProjectionRegister(item, saved)) != 1)
            {
                return null;
            }

            int destination = StackWriteOperand(instruction);
            BeamOperand result = instruction.Operands[destination];
            if (result.Kind != BeamOperandParameters.X || result.TypeIndex is not null
                || instruction.Operands.Where((_, position) => position != destination)
                    .Any(item => SameProjectionRegister(item, result)))
            {
                return null;
            }

            consumer = index;
            register = (int)result.Value;
        }

        return null;
    }
}
