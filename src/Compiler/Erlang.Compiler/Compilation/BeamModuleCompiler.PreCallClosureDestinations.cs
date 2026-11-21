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

            start = end + 1;
            hasClosure = false;
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
        if (ReadSinglePreCallFrameReferences(body, start, end) is not { } plan)
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
}
