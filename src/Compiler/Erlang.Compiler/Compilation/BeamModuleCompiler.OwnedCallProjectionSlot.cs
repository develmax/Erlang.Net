using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveOwnedCallProjectionSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || !code.Span.Contains(TrimFrame) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        byte[]? outgoing = RewriteOwnedCallProjectionSlot(
            code,
            decoded.Body,
            decoded.StackOperands,
            deferComparisons: false
        );
        if (outgoing is not null)
        {
            code = outgoing;
            if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { } updated)
            {
                return outgoing;
            }

            decoded = updated;
        }

        // The incoming argument and the checked comparison can own different
        // slots in the same frame. Use its new physical addresses for the latter.
        byte[]? comparison = RewriteOwnedCallProjectionSlot(
            code,
            decoded.Body,
            decoded.StackOperands,
            deferComparisons: true
        );
        if (comparison is not null)
        {
            code = comparison;
            if (DecodeLateAtomicProjectionSlots(code, labelCount) is not { } updated)
            {
                return comparison;
            }

            decoded = updated;
        }

        return RewriteOwnedLaterCallProjectionSlot(code, decoded.Body, decoded.StackOperands)
            ?? comparison ?? outgoing;
    }

    private static byte[]? RewriteOwnedCallProjectionSlot(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack,
        bool deferComparisons
    ) => RewriteOwnedCallProjectionSlotCore(
        code,
        body,
        stack,
        deferComparisons ? OwnedCallProjectionUse.Comparison : OwnedCallProjectionUse.Outgoing
    );

    private static byte[]? RewriteOwnedLaterCallProjectionSlot(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack
    ) => RewriteOwnedCallProjectionSlotCore(
        code,
        body,
        stack,
        OwnedCallProjectionUse.LaterCall
    );

    private enum OwnedCallProjectionUse
    {
        Outgoing,
        Comparison,
        LaterCall
    }

    private static byte[]? RewriteOwnedCallProjectionSlotCore(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stack,
        OwnedCallProjectionUse use
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int entry = 0;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        BeamProjectionFlow? flow = null;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (start >= end || !HasUnprotectedCallKillSchedule(body, start))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? ownership = null;
            for (int first = start; first + 2 < end; first++)
            {
                if (body[first] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var source, var field, { Kind: BeamOperandParameters.Y, TypeIndex: null } scratch]
                    } || source is not { Kind: BeamOperandParameters.X, TypeIndex: null })
                {
                    continue;
                }

                BeamOperand? retained = null;
                int argument = first + 1;
                int call;
                if (use == OwnedCallProjectionUse.Outgoing && source == x0
                    && body[argument] is { Definition.Opcode: Move, Operands: [var saved, var outgoing] }
                    && saved == scratch && outgoing == x0)
                {
                    call = argument + 1;
                    while (call < end && body[call].Definition.Opcode == Line)
                    {
                        call++;
                    }

                    if (call >= end || !IsOwnedProjectionUnaryCall(body[call]))
                    {
                        continue;
                    }
                }
                else if (use == OwnedCallProjectionUse.Comparison && first > start && ReadOwnedCallComparison(
                    body,
                    first,
                    end,
                    source,
                    scratch,
                    x0
                ) is { } comparison)
                {
                    (call, argument, retained) = comparison;
                    flow ??= new BeamProjectionFlow(body);
                    if (!flow.IsEntryTemporaryDead(argument + 1, x1)
                        || !flow.IsGuardTemporaryDeadOnFailure(argument, x1, preserveHeapRoots: true))
                    {
                        continue;
                    }
                }
                else if (use == OwnedCallProjectionUse.LaterCall && first > start
                    && ReadOwnedLaterCallProjection(
                        body,
                        first,
                        end,
                        source,
                        scratch,
                        x0,
                        x1
                    ) is { } later)
                {
                    (call, argument, retained) = later;
                }
                else
                {
                    continue;
                }

                int allocation = first - 1;
                while (allocation >= start && body[allocation].Definition.Opcode is
                    (Move or InitYRegisters or GetTupleElement or Line))
                {
                    allocation--;
                }

                if (allocation < start || body[allocation] is not
                    { Definition.Opcode: Allocate, Operands: [var width, _] } || width.Value <= 1
                    || (ownership ??= ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    )) is not { } frame
                    || frame.Owners[first - start] != allocation || frame.Owners[argument - start] != allocation
                    || frame.Owners[call - start] != allocation
                    || !CanRemoveCallProjectionSlot(
                        body,
                        frame.Owners,
                        start,
                        allocation,
                        first,
                        argument,
                        scratch
                    ))
                {
                    continue;
                }

                int removed = checked((int)scratch.Value);
                int newWidth = checked((int)width.Value) - 1;
                int at = FirstStackOperandAtOrAfter(stack, body[allocation].Offset);
                for (int index = allocation; index < end; index++)
                {
                    if (index != allocation && frame.Owners[index - start] != allocation)
                    {
                        continue;
                    }

                    BeamInstruction instruction = body[index];
                    int next = body[index + 1].Offset;
                    switch (instruction.Definition.Opcode)
                    {
                        case GetTupleElement when index == first:
                            output.Write(code.Span[copied..instruction.Offset]);
                            if (retained is null)
                            {
                                output.WriteByte(GetTupleElement);
                                WriteRegister(output, x0);
                                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                                WriteRegister(output, x0);
                                copied = body[index + 2].Offset;
                                index++;
                            }
                            else
                            {
                                copied = next;
                            }

                            continue;

                        case ExactEqualTest when index == argument && retained is { } carrier:
                            // Re-read the immutable carrier after the call, with
                            // the argument result still available in X0.
                            output.Write(code.Span[copied..instruction.Offset]);
                            output.WriteByte(GetTupleElement);
                            BeamCompactWriter.Write(
                                output,
                                YRegisterTag,
                                carrier.Value > removed ? carrier.Value - 1 : carrier.Value
                            );
                            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                            WriteRegister(output, x1);
                            int input = FirstStackOperandAtOrAfter(stack, instruction.Offset);
                            var (fromCompare, toCompare, _) = stack[input];
                            output.Write(code.Span[instruction.Offset..fromCompare]);
                            WriteRegister(output, x1);
                            copied = toCompare;
                            continue;

                        case Move when index == argument && use == OwnedCallProjectionUse.LaterCall
                                     && retained is { } laterCarrier:
                            output.Write(code.Span[copied..instruction.Offset]);
                            output.WriteByte(GetTupleElement);
                            BeamCompactWriter.Write(
                                output,
                                YRegisterTag,
                                laterCarrier.Value > removed ? laterCarrier.Value - 1 : laterCarrier.Value
                            );
                            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                            WriteRegister(output, x0);
                            copied = next;
                            continue;

                        case InitYRegisters:
                            output.Write(code.Span[copied..instruction.Offset]);
                            WriteFrameInitialization(
                                output,
                                instruction.Operands.Skip(1)
                                .Select(slot => checked((int)slot.Value))
                                .Where(slot => slot != removed)
                                .Select(slot => slot > removed ? slot - 1 : slot).ToArray()
                            );
                            copied = next;
                            continue;

                        case TrimFrame:
                            output.Write(code.Span[copied..instruction.Offset]);
                            if (instruction.Operands[0].Value > 1)
                            {
                                output.WriteByte(TrimFrame);
                                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, instruction.Operands[0].Value - 1);
                                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, instruction.Operands[1].Value);
                            }

                            copied = next;
                            continue;
                    }

                    int size = LinearFrameSizeOperand(instruction.Definition.Opcode);
                    if (size >= 0)
                    {
                        copied = WritePostCallProjectionOperand(
                            output,
                            code,
                            instruction.Offset,
                            copied,
                            size,
                            newWidth
                        );
                    }

                    while (at < stack.Count && stack[at].Start < instruction.Offset)
                    {
                        at++;
                    }

                    while (at < stack.Count && stack[at].Start < next)
                    {
                        var (from, to, slot) = stack[at++];
                        if (slot > removed)
                        {
                            output.Write(code.Span[copied..from]);
                            BeamCompactWriter.Write(output, YRegisterTag, slot - 1);
                            copied = to;
                        }
                    }
                }

                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Call, int Compare, BeamOperand Carrier)? ReadOwnedCallComparison(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int end,
        BeamOperand source,
        BeamOperand scratch,
        BeamOperand x0
    )
    {
        if (ReadOwnedCallProjectionCarrier(
            body,
            first,
            end,
            source,
            scratch
        ) is not { } owner || owner.Call + 2 >= end || body[owner.Call + 1] is not
        {
            Definition.Opcode: ExactEqualTest,
            Operands: [{ Kind: BeamOperandParameters.Label }, var result, var compared]
        } || !SameProjectionRegister(result, x0) || compared != scratch)
        {
            return null;
        }

        return (owner.Call, owner.Call + 1, owner.Carrier);
    }

    private static (int Call, int Argument, BeamOperand Carrier)? ReadOwnedLaterCallProjection(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int end,
        BeamOperand source,
        BeamOperand scratch,
        BeamOperand x0,
        BeamOperand x1
    )
    {
        if (ReadOwnedCallProjectionCarrier(
            body,
            first,
            end,
            source,
            scratch
        ) is not { } owner || owner.Call + 4 >= end
            || body[owner.Call + 1] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var comparedCarrier, _, var compared]
            } || comparedCarrier != owner.Carrier || compared != x1
            || body[owner.Call + 2] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 }, var left, var right]
            } || !SameProjectionRegister(left, x0) || right != x1
            || body[owner.Call + 3] is not
            {
                Definition.Opcode: Move,
                Operands: [var passed, var destination]
            } || passed != scratch || destination != x0)
        {
            return null;
        }

        int call = owner.Call + 4;
        while (call < end && body[call].Definition.Opcode == Line)
        {
            call++;
        }

        // The caller's Y carrier survives the first ordinary call. The exact test
        // neither changes the carrier nor passes the saved field to its retry.
        return call < end && IsOwnedProjectionUnaryCall(body[call])
            ? (call, owner.Call + 3, owner.Carrier)
            : null;
    }

    private static (int Call, BeamOperand Carrier)? ReadOwnedCallProjectionCarrier(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int end,
        BeamOperand source,
        BeamOperand scratch
    )
    {
        if (body[first - 1] is not
            {
                Definition.Opcode: Move,
                Operands: [var savedSource, { Kind: BeamOperandParameters.Y, TypeIndex: null } carrier]
            }
            || savedSource != source || carrier == scratch)
        {
            return null;
        }

        int call = first + 1;
        while (call < end && body[call].Definition.Opcode is (Move or InitYRegisters or GetTupleElement or Line))
        {
            BeamInstruction instruction = body[call];
            if (instruction.Definition.Opcode == InitYRegisters && instruction.Operands.Contains(carrier)
                || instruction.Definition.Opcode is (Move or GetTupleElement)
                    && SameProjectionRegister(instruction.Operands[^1], carrier))
            {
                return null;
            }

            call++;
        }

        return call < end && IsOwnedProjectionUnaryCall(body[call]) ? (call, carrier) : null;
    }

    private static bool IsOwnedProjectionUnaryCall(BeamInstruction instruction) =>
        instruction is { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] };

    private static bool CanRemoveCallProjectionSlot(
        IReadOnlyList<BeamInstruction> body,
        int[] owners,
        int start,
        int allocation,
        int first,
        int argument,
        BeamOperand scratch
    )
    {
        bool trimmed = false;
        for (int index = allocation + 1; index < start + owners.Length; index++)
        {
            if (owners[index - start] != allocation || index == first || index == argument)
            {
                continue;
            }

            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == TrimFrame)
            {
                if (instruction.Operands[0].Value <= scratch.Value)
                {
                    return false;
                }

                trimmed = true;
            }
            else if (instruction.Definition.Opcode != InitYRegisters
                     && instruction.Operands.Any(operand => SameProjectionRegister(operand, scratch)))
            {
                return false;
            }
        }

        // Every first trim discards the slot. All descendant epochs retain
        // exactly the same address range and therefore need no edits.
        return trimmed;
    }
}
