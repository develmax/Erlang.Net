using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RedirectPreCallClosureDestinations(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(MakeFun)
            || !code.Span.Contains(Move)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, operands) = decoded;
        var edits = new List<CodeEdit>();
        var swaps = new List<int>();
        var entries = new HashSet<int>();
        BeamProjectionFlow? flow = null;
        int start = 0;
        bool hasClosure = false;
        for (int end = 0; end < body.Count; end++)
        {
            byte opcode = body[end].Definition.Opcode;
            if (opcode is not (FunctionInfo or EndCode))
            {
                hasClosure |= opcode == MakeFun;

                continue;
            }

            if (hasClosure && start < end && body[start] is { Definition.Opcode: Label, Operands: [var label] }
                && ReadPreCallClosureDestination(body, start, end) is { } plan
                && ReadDisjointFrameOwnership(body, start, end) is { } frame && frame.Uses.Count == 1)
            {
                int allocation = frame.Uses.Keys.Single();
                if (frame.Owners[plan.Make - start] == allocation
                    && frame.Owners[plan.Reload - start] == allocation
                    && frame.Owners[plan.Call - start] == allocation
                    && HasRetainedPreCallRoot(
                        body,
                        flow ??= new BeamProjectionFlow(body),
                        plan.Call,
                        end
                    ))
                {
                    int operand = FirstStackOperandAtOrAfter(operands, body[plan.Make].Offset);
                    int limit = body[plan.Make + 1].Offset;
                    while (operand < operands.Count && operands[operand].Start < limit)
                    {
                        var (from, to, slot) = operands[operand++];
                        if (slot != plan.Slot)
                        {
                            continue;
                        }

                        edits.Add(new(
                            from,
                            to,
                            XRegisterTag,
                            plan.Outgoing
                        ));
                        edits.Add(new(
                            body[plan.Reload].Offset,
                            body[plan.Reload + 1].Offset,
                            0,
                            0,
                            IsRemoval: true
                        ));
                        entries.Add((int)label.Value);
                        break;
                    }
                }
            }

            if (hasClosure && start < end && body[start] is { Definition.Opcode: Label, Operands: [var entry] }
                && !entries.Contains((int)entry.Value)
                && ReadPreCallClosureInput(body, start, end) is { } input
                && ReadDisjointFrameOwnership(body, start, end) is { } inputFrame && inputFrame.Uses.Count == 1)
            {
                int allocation = inputFrame.Uses.Keys.Single();
                if (inputFrame.Owners[input.Spill - start] == allocation
                    && inputFrame.Owners[input.Make - start] == allocation
                    && inputFrame.Owners[input.Call - start] == allocation)
                {
                    AddPreCallClosureInputEdits(
                        code,
                        body,
                        operands,
                        input,
                        edits
                    );
                    swaps.Add(body[input.Reload].Offset);
                    entries.Add((int)entry.Value);
                }
            }

            start = end + 1;
            hasClosure = false;
        }

        if (swaps.Count != 0)
        {
            byte[] swapped = code.ToArray();
            foreach (int reload in swaps)
            {
                swapped[reload] = Swap;
            }

            code = swapped;
        }

        if (ApplyDuplicatedFrameReadEdits(code, edits) is not { } rewritten
            || DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } updated)
        {
            return null;
        }

        return RewriteDisjointFrames(
            rewritten,
            updated.Body,
            updated.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );
    }

    private static (int Make, int Reload, int Call, int Slot, int Outgoing)? ReadPreCallClosureDestination(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        if (ReadFirstPreCallFrameReferences(body, start, end) is not { } plan)
        {
            return null;
        }

        (int Make, int Reload, int Call, int Slot, int Outgoing)? result = null;
        foreach (var (slot, (make, reload, count)) in plan.References)
        {
            if (count != 2 || make >= reload || reload >= plan.Call || body[make] is not
                {
                    Definition.Opcode: MakeFun,
                    Operands: [
                    _,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved,
                    { Kind: BeamOperandParameters.Count },
                    ..
                    ]
                }
                || saved.Value != slot || body[reload] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var load, { Kind: BeamOperandParameters.X, TypeIndex: null } outgoing]
                }
                || load != saved || outgoing.Value >= body[plan.Call].Operands[0].Value
                || !IsPreCallClosureCorridor(
                    body,
                    make,
                    reload,
                    plan.Call,
                    saved,
                    outgoing
                ))
            {
                continue;
            }

            if (result is not null)
            {
                return null;
            }

            result = (make, reload, plan.Call, slot, (int)outgoing.Value);
        }

        return result;
    }

    private static bool IsPreCallClosureCorridor(
        IReadOnlyList<BeamInstruction> body,
        int make,
        int reload,
        int call,
        BeamOperand saved,
        BeamOperand outgoing
    )
    {
        // make_fun3 uses an existing heap reservation. Moving its destination
        // earlier than the reload must leave every intervening X read intact.
        if (body[make].Operands.Skip(3).Any(operand => SameProjectionRegister(operand, outgoing)))
        {
            return false;
        }

        for (int index = make + 1; index < call; index++)
        {
            if (index == reload)
            {
                continue;
            }

            var instruction = body[index];
            if (instruction.Definition.Opcode is InitYRegisters)
            {
                if (index < reload && instruction.Operands.Skip(1).Any(operand => SameProjectionRegister(operand, saved)))
                {
                    return false;
                }

                continue;
            }

            if (instruction.Definition.Opcode is not
                (Line or Move or Swap or GetTupleElement or GetList or GetHead or GetTail or PutTuple or PutList))
            {
                return false;
            }

            int writes = ReadPreCallAliasWriteMask(instruction);
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                if (SameProjectionRegister(instruction.Operands[position], outgoing)
                    && (index < reload || position < sizeof(int) * 8 && (writes & (1 << position)) != 0))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private sealed record PreCallClosureInput(
        int Spill,
        int Make,
        int Reload,
        int Call,
        BeamOperand Saved,
        BeamOperand Input,
        BeamOperand Outgoing
    );

    private static PreCallClosureInput? ReadPreCallClosureInput(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end
    )
    {
        if (ReadFirstPreCallFrameReferences(
            body,
            start,
            end,
            allowLaterCalls: true
        ) is not { } plan)
        {
            return null;
        }

        int arity = (int)body[plan.Call].Operands[0].Value;
        foreach (var (slot, (spill, reload, _)) in plan.References)
        {
            if (spill >= reload || reload >= plan.Call || body[spill] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } input,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null }
                    ]
                }
                || body[reload] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } outgoing
                    ]
                } || saved.Value != slot || input == outgoing || input.Value >= arity || outgoing.Value >= arity)
            {
                continue;
            }

            int make = AfterLines(spill);
            if (make >= reload || body[make] is not
                {
                    Definition.Opcode: MakeFun,
                    Operands: [_, var destination, { Kind: BeamOperandParameters.Count }, ..]
                }
                || destination != input || AfterLines(make) != reload || AfterLines(reload) != plan.Call)
            {
                continue;
            }

            // make_fun3 uses the preceding heap reservation and reads its
            // environment before writing its destination.
            return new(
                spill,
                make,
                reload,
                plan.Call,
                saved,
                input,
                outgoing
            );
        }

        return null;

        int AfterLines(int instruction)
        {
            int next = instruction + 1;
            while (next < plan.Call && body[next].Definition.Opcode == Line)
            {
                next++;
            }

            return next;
        }
    }

    private static void AddPreCallClosureInputEdits(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        PreCallClosureInput plan,
        List<CodeEdit> edits
    )
    {
        edits.Add(new(
            body[plan.Spill].Offset,
            body[plan.Spill + 1].Offset,
            0,
            0,
            IsRemoval: true
        ));

        int make = body[plan.Make].Offset + 1;
        int makeEnd = body[plan.Make + 1].Offset;
        var cursor = new BeamCursor(code[make..makeEnd]);
        _ = BeamCompactReader.Read(cursor);
        int destination = make + cursor.Position;
        _ = BeamCompactReader.Read(cursor);
        edits.Add(new(
            destination,
            make + cursor.Position,
            XRegisterTag,
            (int)plan.Outgoing.Value
        ));
        int operand = FirstStackOperandAtOrAfter(stackOperands, make + cursor.Position);
        while (operand < stackOperands.Count && stackOperands[operand].Start < makeEnd)
        {
            var (from, to, slot) = stackOperands[operand++];
            if (slot == plan.Saved.Value)
            {
                edits.Add(new(
                    from,
                    to,
                    XRegisterTag,
                    (int)plan.Input.Value
                ));
            }
        }

        int register = body[plan.Reload].Offset + 1;
        var reload = new BeamCursor(code[register..body[plan.Reload + 1].Offset]);
        _ = BeamCompactReader.Read(reload);
        edits.Add(new(
            register,
            register + reload.Position,
            XRegisterTag,
            (int)plan.Input.Value
        ));
    }
}
