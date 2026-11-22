using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleTrimmedClosureCallArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(MakeFun)
            || !code.Span.Contains(TrimFrame) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        var entries = new HashSet<int>();
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
            if (start + 8 >= end || body[start] is not { Definition.Opcode: Label, Operands: [var label] }
                || body[start + 1].Definition.Opcode is not (Allocate or AllocateHeap)
                || body[start + 2] is not { Definition.Opcode: InitYRegisters } initialized)
            {
                continue;
            }

            for (int literal = start + 3; literal + 6 < end; literal++)
            {
                if (body[literal].Definition.Opcode is Label or TrimFrame or Allocate or AllocateHeap
                    or TryEnter or CatchEnter or Deallocate)
                {
                    break;
                }

                if (!IsTrimmedClosureArgumentCorridor(
                    body,
                    initialized,
                    literal,
                    end,
                    out int reload
                ))
                {
                    continue;
                }

                int literalStart = body[literal].Offset;
                int heapStart = body[literal + 1].Offset;
                int makeStart = body[literal + 2].Offset;
                int makeEnd = body[literal + 3].Offset;
                int slotMoveStart = body[literal + 4].Offset;
                int reloadStart = body[reload].Offset;
                var term = new BeamCursor(code[(literalStart + 1)..heapStart]);
                var literalOperand = BeamCompactReader.Read(term);
                if (literalOperand.WireTag == ExtendedTag)
                {
                    _ = BeamCompactReader.Read(term);
                }

                var closure = new BeamCursor(code[(makeStart + 1)..makeEnd]);
                _ = BeamCompactReader.Read(closure);
                int destination = makeStart + 1 + closure.Position;
                _ = BeamCompactReader.Read(closure);

                // Argument copies follow heap reservation and trim in beam_ssa_codegen.
                // The closure can be constructed in its outgoing X because its
                // captures and the trim relocation are entirely in the frame.
                output.Write(code.Span[copied..literalStart]);
                output.Write(code.Span[heapStart..destination]);
                BeamCompactWriter.Write(output, XRegisterTag, 1);
                output.Write(code.Span[(makeStart + 1 + closure.Position)..makeEnd]);
                output.Write(code.Span[slotMoveStart..reloadStart]);
                output.Write(code.Span[literalStart..(literalStart + 1 + term.Position)]);
                BeamCompactWriter.Write(output, XRegisterTag, 0);
                copied = body[reload + 1].Offset;
                entries.Add((int)label.Value);
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();

        var compacted = DecodeLateAtomicProjectionSlots(rewritten, labelCount) is { } updated
            ? RewriteReusedStackSlots(
                rewritten,
                updated.Body,
                updated.StackOperands,
                useValueLiveness: false,
                closureEntries: null,
                atomicOnly: false,
                entriesToCompact: entries
            ) ?? rewritten
            : null;

        return compacted is null ? null : OrderRetainedClosureFrames(compacted, labelCount, entries) ?? compacted;
    }

    private byte[]? OrderSoleRetainedClosureFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(MakeFun)
            || !code.Span.Contains(TrimFrame) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, operands) = decoded;
        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        BeamOperand y0 = new(BeamOperandParameters.Y, 0);
        BeamOperand y1 = new(BeamOperandParameters.Y, 1);
        using var output = new MemoryStream(code.Length);
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
            int returnAt = end - 1;
            while (returnAt > start && body[returnAt].Definition.Opcode is Label or Line)
            {
                returnAt--;
            }

            if (start + 8 >= returnAt
                || body[start].Definition.Opcode != Label
                || body[start + 1] is not { Definition.Opcode: Allocate, Operands: [{ Value: var width }, { Value.IsOne: true }] }
                || width != 2
                || body[start + 2] is not { Definition.Opcode: InitYRegisters } initial
                || initial.Operands.Count(operand => operand.Kind == BeamOperandParameters.Y) != 1
                || !initial.Operands.Contains(y1) || !IsMove(body[start + 3], x0, y0)
                || body[returnAt].Definition.Opcode != Return
                || body[returnAt - 1] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] })
            {
                continue;
            }

            int trim = -1;
            int swapCount = 0;
            bool linear = true;
            for (int index = start + 4; index < returnAt - 1; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == TrimFrame && trim < 0)
                {
                    trim = index;
                }
                else if (instruction.Definition.Opcode is not
                    (Line or Move or Swap or TestHeap or MakeFun or LocalCall or ExternalCall))
                {
                    linear = false;
                    break;
                }
                else if (trim < 0 && instruction is { Definition.Opcode: Swap, Operands: [var left, var right] }
                    && ((left == x0 && right == y0) || (left == y0 && right == x0)))
                {
                    swapCount++;
                }
            }

            if (!linear || swapCount != 1 || trim < start + 7
                || body[trim] is not { Operands: [{ Value.IsOne: true }, { Value.IsOne: true }] }
                || body[trim - 1] is not { Definition.Opcode: MakeFun, Operands: [_, var destination, _, var first, var second] }
                || destination != x1 || first != y1 || second != y0)
            {
                continue;
            }

            // The input uses the high slot until the closure consumes the
            // swapped result. The one value still live across the call moves
            // back into that slot so beam_trim can release the low prefix.
            output.Write(code.Span[copied..body[start + 2].Offset]);
            WriteFrameInitialization(output, [0]);
            int from = body[start + 3].Offset;
            int until = body[trim].Offset;
            int operandIndex = FirstStackOperandAtOrAfter(operands, from);
            while (operandIndex < operands.Count && operands[operandIndex].Start < until)
            {
                var (at, after, slot) = operands[operandIndex++];
                output.Write(code.Span[from..at]);
                BeamCompactWriter.Write(output, YRegisterTag, 1 - slot);
                from = after;
            }

            output.Write(code.Span[from..until]);
            output.WriteByte(Move);
            WriteRegister(output, y0);
            WriteRegister(output, y1);
            copied = until;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private byte[]? OrderRetainedClosureFrames(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlySet<int> entries
    )
    {
        if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, operands) = decoded;
        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        BeamOperand y0 = new(BeamOperandParameters.Y, 0);
        BeamOperand y1 = new(BeamOperandParameters.Y, 1);
        BeamOperand y2 = new(BeamOperandParameters.Y, 2);
        using var output = new MemoryStream();
        int copied = 0;
        int next = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = next;
            next = end + 1;
            int returnAt = end - 1;
            while (returnAt > start && body[returnAt].Definition.Opcode is Label or Line)
            {
                returnAt--;
            }

            if (start + 10 >= returnAt
                || body[start] is not { Definition.Opcode: Label, Operands: [var label] }
                || !entries.Contains((int)label.Value)
                || body[start + 1] is not { Definition.Opcode: Allocate, Operands: [{ Value: var width }, { Value: var live }] }
                || width != 3 || live != 2
                || body[start + 2] is not { Definition.Opcode: InitYRegisters } initial || !initial.Operands.Contains(y2)
                || !IsMove(body[start + 3], x0, y1) || !IsMove(body[start + 4], x1, y0)
                || body[returnAt].Definition.Opcode != Return
                || body[returnAt - 1] is not { Definition.Opcode: Deallocate, Operands: [{ Value: var released }] }
                || released != 2 || !IsMove(body[returnAt - 2], y1, x0)
                || body[returnAt - 3].Definition.Opcode is not (LocalCall or ExternalCall))
            {
                continue;
            }

            int trim = -1;
            bool linear = true;
            for (int index = start + 5; index < returnAt - 2; index++)
            {
                byte opcode = body[index].Definition.Opcode;
                if (opcode == TrimFrame && trim < 0)
                {
                    trim = index;
                }
                else if (opcode is not (Line or Move or Swap or TestHeap or MakeFun or LocalCall or ExternalCall))
                {
                    linear = false;
                    break;
                }
            }

            if (!linear || trim < start + 7 || trim >= returnAt - 4
                || body[trim] is not { Operands: [{ Value.IsOne: true }, { Value: var kept }] } || kept != 2
                || !IsMove(body[trim - 1], y0, y1)
                || body[trim - 2] is not { Definition.Opcode: MakeFun, Operands: [_, var destination, _, var first, var second] }
                || destination != x1 || first != y2 || second != y1)
            {
                continue;
            }

            int finalSetup = returnAt - 3;
            while (finalSetup > trim + 1 && body[finalSetup - 1].Definition.Opcode == Line)
            {
                finalSetup--;
            }

            // The two incoming copies reserve their reversed Y-order before
            // the first call. The result is the remaining slot. Keep the same
            // two values in the existing physical order at the trimmed edge.
            output.Write(code.Span[copied..body[start + 2].Offset]);
            WriteFrameInitialization(output, [0]);
            WriteMove(x1, y1);
            WriteMove(x0, y2);
            int from = body[start + 5].Offset;
            int beforeTrimCopy = body[trim - 1].Offset;
            int operand = FirstStackOperandAtOrAfter(operands, from);
            while (operand < operands.Count && operands[operand].Start < beforeTrimCopy)
            {
                var (at, after, slot) = operands[operand++];
                output.Write(code.Span[from..at]);
                BeamCompactWriter.Write(output, YRegisterTag, slot == 2 ? 0 : slot + 1);
                from = after;
            }

            output.Write(code.Span[from..beforeTrimCopy]);
            WriteMove(y0, y2);
            output.Write(code.Span[body[trim].Offset..body[finalSetup].Offset]);

            // All arguments to the last call are in X. Only its saved return
            // value remains live in Y and occupies the highest slot.
            output.WriteByte(TrimFrame);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            output.Write(code.Span[body[finalSetup].Offset..body[returnAt - 2].Offset]);
            WriteMove(y0, x0);
            output.WriteByte(Deallocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            copied = body[returnAt].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteMove(BeamOperand source, BeamOperand target)
        {
            output.WriteByte(Move);
            WriteRegister(output, source);
            WriteRegister(output, target);
        }
    }

    private static bool IsTrimmedClosureArgumentCorridor(
        IReadOnlyList<BeamInstruction> body,
        BeamInstruction initialized,
        int start,
        int end,
        out int reload
    )
    {
        reload = -1;
        if (body[start] is not
            {
                Definition.Opcode: Move,
                Operands: [
                {
                    Kind: BeamOperandParameters.LiteralValue or BeamOperandParameters.Atom or BeamOperandParameters.Integer
                        or BeamOperandParameters.Nil
                },
                { Kind: BeamOperandParameters.Y, TypeIndex: null } value
                ]
            }
            || !initialized.Operands.Contains(value)
            || body[start + 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
            || body[start + 2] is not
            {
                Definition.Opcode: MakeFun,
                Operands: [_, { Kind: BeamOperandParameters.Y, TypeIndex: null } savedFun, _, ..]
            }
            || savedFun == value || body[start + 2].Operands.Skip(3).Any(operand =>
                operand.Kind != BeamOperandParameters.Y || SameProjectionRegister(operand, value))
            || body[start + 3] is not
            {
                Definition.Opcode: Move,
                Operands: [var loadedFun, { Kind: BeamOperandParameters.X, Value.IsOne: true, TypeIndex: null }]
            }
            || loadedFun != savedFun)
        {
            return false;
        }

        int trim = start + 4;
        bool relocatesClosureSlot = body[trim] is
        {
            Definition.Opcode: Move,
            Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } retained, var relocated]
        } && relocated == savedFun && retained != savedFun && retained != value;
        if (relocatesClosureSlot)
        {
            trim++;
        }
        else if (!initialized.Operands.Contains(savedFun))
        {
            return false;
        }

        if (body[trim] is not { Definition.Opcode: TrimFrame, Operands: [var removed, _] }
            || value.Value < removed.Value || (relocatesClosureSlot && savedFun.Value < removed.Value))
        {
            return false;
        }

        reload = trim + 1;
        if (body[reload] is not
            {
                Definition.Opcode: Move,
                Operands: [
                { Kind: BeamOperandParameters.Y, TypeIndex: null } loadedValue,
                { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }
                ]
            }
            || loadedValue.Value != value.Value - removed.Value)
        {
            return false;
        }

        int call = reload + 1;
        while (call < end && body[call].Definition.Opcode == Line)
        {
            call++;
        }

        if (call >= end || body[call] is not
            {
                Definition.Opcode: LocalCall or ExternalCall,
                Operands: [{ Value: var arity }, _]
            } || arity != 2)
        {
            return false;
        }

        var argumentSlot = loadedValue.Value;
        var closureSlot = relocatesClosureSlot ? -1 : savedFun.Value - removed.Value;
        for (int index = call + 1; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            switch (instruction.Definition.Opcode)
            {
                case Deallocate:
                    return index + 1 < end && body[index + 1].Definition.Opcode == Return
                        && Enumerable.Range(index + 2, end - index - 2).All(at =>
                            body[at].Definition.Opcode is Label or Line);
                case TrimFrame:
                    var dropped = instruction.Operands[0].Value;
                    argumentSlot -= dropped;
                    closureSlot -= dropped;
                    break;
                case Move:
                    if (ReadsOmittedSlot(instruction.Operands[0]))
                    {
                        return false;
                    }

                    BeamOperand destination = instruction.Operands[1];
                    if (destination.Kind == BeamOperandParameters.Y)
                    {
                        if (destination.Value == argumentSlot)
                        {
                            argumentSlot = -1;
                        }

                        if (destination.Value == closureSlot)
                        {
                            closureSlot = -1;
                        }
                    }

                    break;
                case Swap:
                    if (instruction.Operands.Any(ReadsOmittedSlot))
                    {
                        return false;
                    }

                    break;
                case Line or LocalCall or ExternalCall:
                    break;
                default:
                    return false;
            }
        }

        return false;

        bool ReadsOmittedSlot(BeamOperand operand) =>
            operand.Kind == BeamOperandParameters.Y && (operand.Value == argumentSlot || operand.Value == closureSlot);
    }
}
