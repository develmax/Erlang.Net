using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleClosedCatchSuccessFields(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CatchEnd)
            || !code.Span.Contains(SelectTupleArity)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stack) = decoded;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
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
            if (end - start < 18 || body[start].Definition.Opcode != Label
                || body[start + 1] is not { Definition.Opcode: Allocate, Operands: [var frame, _] }
                || frame.Value < 2 || frame.Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int enter = -1;
            int closed = -1;
            var handler = new BeamOperand(BeamOperandParameters.Y, frame.Value - 1);
            for (int index = start + 2; index < end; index++)
            {
                var instruction = body[index];
                switch (instruction.Definition.Opcode)
                {
                    case CatchEnter when enter < 0 && instruction.Operands[0] == handler:
                        enter = index;
                        break;
                    case CatchEnd when enter >= 0 && closed < 0 && instruction.Operands[0] == handler:
                        closed = index;
                        break;
                    case CatchEnter or CatchEnd or TryEnter or TryEnd or TryCase or Allocate or AllocateHeap
                        or DebugLine or ExecutableLine or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout:
                        closed = -1;
                        index = end;
                        break;
                }
            }

            if (closed < enter + 3 || enter < 0 || closed + 2 >= end
                || body[closed - 2].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[closed + 1] is not
                {
                    Definition.Opcode: IsTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } invalid, var tuple]
                } || tuple != x0
                || body[closed + 2] is not
                {
                    Definition.Opcode: SelectTupleArity,
                    Operands: [var arityInput, var arityFallback, _, _, _, ..]
                } arities || !SameProjectionRegister(arityInput, tuple) || arityFallback != invalid)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetExclusiveForwardTarget(
                enter,
                closed - 2,
                (int)body[enter].Operands[1].Value,
                out int landing
            ) || landing != closed - 1)
            {
                continue;
            }

            var plans = new SortedDictionary<int, (int Save, int Through, BeamOperand Slot, BeamOperand Argument, BeamOperand Kept)>();
            for (int option = 3; option + 1 < arities.Operands.Count; option += 2)
            {
                if (arities.Operands[option].Value != 3
                    || arities.Operands[option + 1] is not { Kind: BeamOperandParameters.Label } group
                    || !flow.TryGetSingleForwardTarget(closed + 2, (int)group.Value, out int selected)
                    || selected + 2 >= end
                    || body[selected + 1] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var source, { Value.IsZero: true }, var tag]
                    } || source != tuple
                    || body[selected + 2] is not
                    {
                        Definition.Opcode: SelectValue,
                        Operands: [var tagged, var rejected, _, _, _, ..]
                    } tags || tagged != tag || rejected != invalid)
                {
                    continue;
                }

                for (int key = 3; key + 1 < tags.Operands.Count; key += 2)
                {
                    if (tags.Operands[key].Kind == BeamOperandParameters.Atom
                        && tags.Operands[key + 1] is { Kind: BeamOperandParameters.Label } arm
                        && flow.TryGetSingleForwardTarget(selected + 2, (int)arm.Value, out int branch)
                        && ReadArm(
                            branch + 1,
                            end,
                            frame,
                            handler
                        ) is { } plan)
                    {
                        plans.Add(branch + 1, plan);
                    }
                }
            }

            foreach (var (first, (save, through, slot, argument, kept)) in plans)
            {
                output.Write(code.Span[copied..body[first].Offset]);

                // The unary call consumes the first field directly. Preserve
                // the other checked field in the former handler slot and clear
                // the retired slot before any operation that can collect.
                WriteProjection(kept, handler);
                WriteProjection(argument, x0);
                WriteFrameInitialization(output, [(int)slot.Value]);
                copied = body[first + 3].Offset;
                int finish = body[through].Offset;
                for (int operand = FirstStackOperandAtOrAfter(stack, body[save].Offset);
                    operand < stack.Count && stack[operand].Start < finish;
                    operand++)
                {
                    var use = stack[operand];
                    if (use.Slot != slot.Value && use.Slot != handler.Value)
                    {
                        continue;
                    }

                    output.Write(code.Span[copied..use.Start]);
                    BeamCompactWriter.Write(output, YRegisterTag, use.Slot == slot.Value ? handler.Value : slot.Value);
                    copied = use.End;
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        (int Save, int Through, BeamOperand Slot, BeamOperand Argument, BeamOperand Kept)? ReadArm(
            int first,
            int end,
            BeamOperand frame,
            BeamOperand handler
        )
        {
            if (first + 8 >= end || body[first] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var carrier, { Kind: BeamOperandParameters.Unsigned } argument, var transient]
                } || carrier != x0 || transient != handler || argument.Value < 1 || argument.Value >= 3
                || body[first + 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var same, { Kind: BeamOperandParameters.Unsigned } kept,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                } || same != x0 || kept.Value < 1 || kept.Value >= 3 || kept == argument
                || slot.Value < 0 || slot.Value >= handler.Value
                || body[first + 2] is not { Definition.Opcode: Move, Operands: [var read, var passed] }
                || read != handler || passed != x0)
            {
                return null;
            }

            int invoke = ReadUnaryCall(first + 3, end);
            int save = invoke + 1;
            if (invoke < 0 || save + 2 >= end
                || body[save] is not { Definition.Opcode: Move, Operands: [var produced, var stored] }
                || produced != x0 || stored != handler)
            {
                return null;
            }

            int consume = ReadUnaryCall(save + 1, end);
            int test = consume + 1;
            if (consume < 0 || test + 1 >= end || body[test] is not
                {
                    Definition.Opcode: IsNil,
                    Operands: [{ Kind: BeamOperandParameters.Label } nonempty, var result]
                } || !SameProjectionRegister(result, x0)
                || !flow!.TryGetSingleForwardTarget(test, (int)nonempty.Value, out int keptBranch))
            {
                return null;
            }

            int release = test + 1;
            bool readsLocation = false;
            while (release < end)
            {
                if (body[release].Definition.Opcode == Line)
                {
                    release++;
                    continue;
                }

                if (body[release] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } input,
                        { Kind: BeamOperandParameters.X, TypeIndex: null }]
                    } || input == handler)
                {
                    break;
                }

                readsLocation |= input == slot;
                release++;
            }

            if (!readsLocation || release + 5 >= end || keptBranch != release + 1
                || body[release] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame,
                    Operands: [_, _, var callFrame]
                } || callFrame != frame
                || body[release + 2] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                || body[release + 3] is not { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] } constructed
                || destination != x0
                || !constructed.Operands.Skip(2).Contains(slot) || !constructed.Operands.Skip(2).Contains(handler)
                || constructed.Operands.Skip(2).Any(value => value.TypeIndex is not null
                    && (SameProjectionRegister(value, slot) || SameProjectionRegister(value, handler)))
                || body[release + 4] is not { Definition.Opcode: Deallocate, Operands: [var released] }
                || released != frame || body[release + 5].Definition.Opcode != Return)
            {
                return null;
            }

            return (save, release + 5, slot, argument, kept);
        }

        int ReadUnaryCall(int index, int end)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index < end && body[index] is
            { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] }
                ? index : -1;
        }

        void WriteProjection(BeamOperand field, BeamOperand destination)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, destination);
        }
    }
}
