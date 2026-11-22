using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PlaceCheckedCatchArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || !code.Span.Contains(CatchEnter) && !code.Span.Contains(TryEnter)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteCheckedCatchArguments(code, body);
    }

    private static byte[]? RewriteCheckedCatchArguments(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 2; info + 5 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || body[info + 1].Definition.Opcode != Label || arity.Value.Sign <= 0
                || arity.Value >= BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int entry = info + 1;
            int first = entry + 1;
            int failure = EntryFrameFailureLabel(body, entry);
            if (failure < 0 || !ReadCheckedCatchTuple(
                body,
                ref first,
                failure,
                out BeamOperand? tuple,
                out int fields
            ) || tuple is null || tuple.Value >= arity.Value
                || body[first] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || live != arity || width.Value < 2 || width.Value > BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int end = first + 1;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            if (body[end].Definition.Opcode == FunctionInfo)
            {
                end--;
                while (end > first && body[end].Definition.Opcode == Line)
                {
                    end--;
                }

                if (body[end].Definition.Opcode != Label)
                {
                    continue;
                }
            }

            int frame = (int)width.Value;
            int inputCount = (int)live.Value;
            var saves = new Dictionary<BeamOperand, BeamOperand>();
            int projection = first + 1;
            for (; projection < end; projection++)
            {
                BeamInstruction instruction = body[projection];
                if (instruction is { Definition.Opcode: Move, Operands: [var input, var slot] }
                    && IsCatchArgumentX(input, inputCount) && BareStack(slot, frame))
                {
                    saves[slot] = input;
                }
                else if (instruction.Definition.Opcode == InitYRegisters)
                {
                    foreach (BeamOperand initialized in instruction.Operands.Skip(1))
                    {
                        saves.Remove(initialized);
                    }
                }
                else
                {
                    break;
                }
            }

            var loads = new List<(BeamOperand Slot, BeamOperand Field)>();
            int enter = projection;
            while (enter < end && body[enter] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var carrier, var field, var slot]
                } && (carrier == tuple || saves.GetValueOrDefault(carrier) == tuple)
                    && field.Value.Sign >= 0 && field.Value < fields && BareStack(slot, frame))
            {
                if (slot == carrier || loads.Any(load => load.Slot == slot))
                {
                    break;
                }

                saves.Remove(slot);
                loads.Add((slot, field));
                enter++;
            }

            if (loads.Count == 0 || enter + 5 >= end || body[enter] is not
                {
                    Definition.Opcode: CatchEnter or TryEnter,
                    Operands: [var token, { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } handler]
                } || !BareStack(token, frame) || loads.Any(load => load.Slot == token)
                || loads.Count + inputCount - 1 >= BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            // Allocate the new X roots contiguously. The last field can
            // replace the input tuple after all remaining reads of it.
            int tupleRegister = (int)tuple.Value;
            var homes = new Dictionary<BeamOperand, int>();
            for (int position = 0; position < loads.Count; position++)
            {
                homes.Add(loads[position].Slot, position == loads.Count - 1 ? tupleRegister : inputCount + position);
            }

            var protectedHomes = homes.Values.ToHashSet();
            saves.Remove(token);
            var routes = new Dictionary<int, int>();
            var writes = new HashSet<int>();
            var literals = new List<int>();
            int call = enter + 1;
            bool compatible = true;
            for (; call < end; call++)
            {
                BeamInstruction instruction = body[call];
                if (instruction is { Definition.Opcode: Move, Operands: [var value, var destination] }
                    && IsCatchArgumentX(destination, BeamOperandParameters.MaximumRegister))
                {
                    int target = (int)destination.Value;
                    if (!writes.Add(target))
                    {
                        compatible = false;
                        break;
                    }

                    if (homes.TryGetValue(value, out int home))
                    {
                        compatible = routes.TryAdd(home, target);
                    }
                    else if (saves.TryGetValue(value, out BeamOperand? saved))
                    {
                        compatible = saved == destination && !protectedHomes.Contains(target);
                    }
                    else if (value.Kind is BeamOperandParameters.Atom or BeamOperandParameters.Integer
                        or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue)
                    {
                        compatible = !protectedHomes.Contains(target);
                        literals.Add(call);
                    }
                    else
                    {
                        compatible = false;
                    }

                    if (!compatible)
                    {
                        break;
                    }
                }
                else if (instruction.Definition.Opcode == InitYRegisters)
                {
                    if (instruction.Operands.Contains(token))
                    {
                        compatible = false;
                        break;
                    }

                    foreach (BeamOperand slot in instruction.Operands.Skip(1))
                    {
                        homes.Remove(slot);
                        saves.Remove(slot);
                    }
                }
                else
                {
                    break;
                }
            }

            int line = call;
            while (call < end && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (!compatible || routes.Count != loads.Count || call + 3 >= end
                || body[call] is not { Definition.Opcode: LocalCall or ExternalCall or DynamicApply, Operands: [var args, ..] }
                || args.Value < 0 || args.Value >= BeamOperandParameters.MaximumRegister)
            {
                continue;
            }

            int arguments = (int)args.Value + (body[call].Definition.Opcode == DynamicApply ? 2 : 0);
            var exchanges = routes.Where(route => route.Key != route.Value).ToArray();
            if (writes.Any(register => register >= arguments)
                || Enumerable.Range(0, arguments).Any(register => !writes.Contains(register)
                    && (register >= inputCount || protectedHomes.Contains(register)))
                || exchanges.Length != 0 && (exchanges.Length != 2 || exchanges[0].Key != exchanges[1].Value
                    || exchanges[1].Key != exchanges[0].Value)
                || !TryReadCheckedArgumentCloses(
                    body,
                    flow ??= new BeamProjectionFlow(body),
                    enter,
                    call,
                    end,
                    frame,
                    out int normalClose,
                    out int exceptionalClose
                ))
            {
                continue;
            }

            output.Write(code.Span[copied..body[first].Offset]);
            for (int position = 0; position < loads.Count; position++)
            {
                output.WriteByte(GetTupleElement);
                BeamCompactWriter.Write(output, XRegisterTag, tupleRegister);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, loads[position].Field.Value);
                BeamCompactWriter.Write(output, XRegisterTag, position == loads.Count - 1 ? tupleRegister : inputCount + position);
            }

            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, inputCount + loads.Count - 1);
            output.WriteByte(body[enter].Definition.Opcode);
            BeamCompactWriter.Write(output, YRegisterTag, 0);
            BeamCompactWriter.Write(output, LabelTag, handler.Value);
            foreach (int literal in literals)
            {
                output.Write(code.Span[body[literal].Offset..body[literal + 1].Offset]);
            }

            if (exchanges.Length != 0)
            {
                output.WriteByte(Swap);
                BeamCompactWriter.Write(output, XRegisterTag, Math.Min(exchanges[0].Key, exchanges[1].Key));
                BeamCompactWriter.Write(output, XRegisterTag, Math.Max(exchanges[0].Key, exchanges[1].Key));
            }

            output.Write(code.Span[body[line].Offset..body[normalClose].Offset]);
            output.WriteByte(body[normalClose].Definition.Opcode);
            BeamCompactWriter.Write(output, YRegisterTag, 0);
            copied = body[normalClose + 1].Offset;
            for (int index = normalClose + 1; index < end; index++)
            {
                if (index == exceptionalClose)
                {
                    output.Write(code.Span[copied..body[index].Offset]);
                    output.WriteByte(TryCase);
                    BeamCompactWriter.Write(output, YRegisterTag, 0);
                    copied = body[index + 1].Offset;
                    continue;
                }

                if (body[index].Definition.Opcode != Deallocate)
                {
                    continue;
                }

                output.Write(code.Span[copied..body[index].Offset]);
                output.WriteByte(Deallocate);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                copied = body[index + 1].Offset;
            }

            info = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ReadCheckedCatchTuple(
        IReadOnlyList<BeamInstruction> body,
        ref int first,
        int failure,
        out BeamOperand? tuple,
        out int fields
    )
    {
        tuple = null;
        fields = 0;
        if (body[first] is
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [var rejected, var input, var arity, _]
            } && rejected.Value == failure && IsCatchArgumentX(input, BeamOperandParameters.MaximumRegister))
        {
            first++;
            tuple = input;
            fields = (int)arity.Value;

            return true;
        }

        if (body[first] is { Definition.Opcode: IsTuple, Operands: [var nonTuple, var source] }
            && body[first + 1] is { Definition.Opcode: TestArity, Operands: [var wrong, var same, var count] }
            && nonTuple == wrong && wrong.Value == failure && same == source
            && IsCatchArgumentX(source, BeamOperandParameters.MaximumRegister))
        {
            first += 2;
            tuple = source;
            fields = (int)count.Value;

            return true;
        }

        return false;
    }

    private static bool IsCatchArgumentX(BeamOperand operand, int limit) =>
        operand is { Kind: BeamOperandParameters.X, TypeIndex: null } && operand.Value >= 0 && operand.Value < limit;

    private static bool TryReadCheckedArgumentCloses(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int enter,
        int call,
        int end,
        int frame,
        out int normal,
        out int exceptional
    )
    {
        BeamInstruction protection = body[enter];
        BeamOperand token = protection.Operands[0];
        int handler = (int)protection.Operands[1].Value;
        exceptional = -1;
        if (protection.Definition.Opcode == CatchEnter)
        {
            normal = call + 2;

            return body[normal] is { Definition.Opcode: CatchEnd, Operands: [var release] }
                && release == token
                && flow.TryGetExclusiveForwardTarget(
                    enter,
                    call,
                    handler,
                    out int target
                ) && target == call + 1
                && CanKeepCheckedCatchResult(
                    body,
                    normal + 1,
                    end,
                    frame,
                    exceptional
                );
        }

        // Try has distinct normal and exceptional closes. The latter may
        // only be entered via the active handler attached to this region.
        normal = call + 1;
        if (body[normal] is not { Definition.Opcode: TryEnd, Operands: [var finished] } || finished != token
            || !flow.TryGetSingleForwardTarget(enter, handler, out int landing)
            || landing <= normal || landing + 1 >= end
            || body[landing + 1] is not { Definition.Opcode: TryCase, Operands: [var caught] } || caught != token)
        {
            return false;
        }

        exceptional = landing + 1;

        return CanKeepCheckedCatchResult(
            body,
            normal + 1,
            end,
            frame,
            exceptional
        );
    }

    private static bool CanKeepCheckedCatchResult(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int frame,
        int exceptional
    )
    {
        var labels = body.Skip(start).Take(end - start).Where(instruction => instruction.Definition.Opcode == Label)
            .Select(instruction => instruction.Operands[0].Value).ToHashSet();
        bool hasExit = false;
        for (int index = start; index < end; index++)
        {
            if (index == exceptional)
            {
                continue;
            }

            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Allocate or AllocateHeap or TrimFrame or CatchEnter or CatchEnd or TryEnter or TryEnd
                or TryCase or TryCaseEnd or DebugLine or ExecutableLine or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y)
                || opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame)
                    && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                        && operand.Value.Sign != 0 && !labels.Contains(operand.Value)))
            {
                return false;
            }

            int width = LinearFrameSizeOperand(opcode);
            if (width >= 0)
            {
                if (opcode != Deallocate || instruction.Operands[width].Value != frame)
                {
                    return false;
                }

                hasExit = true;
            }
        }

        return hasExit;
    }
}
