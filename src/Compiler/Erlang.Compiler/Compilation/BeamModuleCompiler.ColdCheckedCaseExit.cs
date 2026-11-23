using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? PlaceColdCheckedCaseExit(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || !code.Span.Contains(CaseEnd) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body }
            || RewriteColdCheckedCaseExit(code, body) is not { } rewritten
            || OrderFinalPhysicalLabelsCore(
                rewritten,
                labelCount,
                formMultiKeySelections: false,
                relocateOnly: true
            ) is not { } ordered)
        {
            return null;
        }

        return (RebuildLineTable(ordered.Code.ToArray(), ordered.LabelCount), ordered.LabelCount);
    }

    private (ReadOnlyMemory<byte> Code, int LabelCount)? PlaceNestedIdentityCaseExits(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CaseEnd)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body }
            || RewriteNestedIdentityCaseExits(code, body) is not { } rewritten
            || OrderFinalPhysicalLabelsCore(
                rewritten,
                labelCount,
                formMultiKeySelections: false,
                relocateOnly: true
            ) is not { } ordered)
        {
            return null;
        }

        return (RebuildLineTable(ordered.Code.ToArray(), ordered.LabelCount), ordered.LabelCount);
    }

    private static byte[]? RewriteNestedIdentityCaseExits(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int entry = 0;
        for (int boundary = 0; boundary < body.Count; boundary++)
        {
            byte opcode = body[boundary].Definition.Opcode;
            if (opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = boundary + 1;
            int end = boundary;
            if (opcode == FunctionInfo)
            {
                while (end > start && body[end - 1].Definition.Opcode == Line)
                {
                    end--;
                }

                if (end <= start || body[--end].Definition.Opcode != Label)
                {
                    continue;
                }
            }

            if (end < start + 17 || body[start] is not { Definition.Opcode: Label, Operands: [var function] })
            {
                continue;
            }

            for (int outer = start + 1; outer + 12 < end; outer++)
            {
                if (body[outer] is not
                    {
                        Definition.Opcode: SelectValue,
                        Operands: [var outerValue, var outerFailure, { Value: var outsideKeys }, ..]
                    } dispatch
                    || outerValue != x0 || outsideKeys < 4
                    || body[outer + 1] is not { Definition.Opcode: Label, Operands: [var nested] }
                    || !dispatch.Operands.Skip(3).Any(operand => operand.Kind == BeamOperandParameters.Label
                        && operand.Value == nested.Value))
                {
                    continue;
                }

                int inner = outer + 2;
                while (inner < end && body[inner].Definition.Opcode is not (Label or SelectValue))
                {
                    inner++;
                }

                if (inner < outer + 4 || inner + 6 >= end
                    || body[inner - 2] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
                    || body[inner - 1].Definition.Opcode != ExternalCall
                    || body[inner] is not
                    {
                        Definition.Opcode: SelectValue,
                        Operands:
                        [
                            var innerValue, var innerFailure, { Value: var keyCount },
                        { Kind: BeamOperandParameters.Atom }, var first,
                        { Kind: BeamOperandParameters.Atom }, var second
                        ]
                    }
                    || innerValue != x0 || keyCount != 4 || first == second
                    || body[inner + 1] is not { Definition.Opcode: Label, Operands: [var retry] }
                    || (retry.Value != first.Value && retry.Value != second.Value)
                    || body[inner + 2] is not
                    {
                        Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Y }, var retryArgument]
                    }
                    || retryArgument != x0
                    || body[inner + 3] is not
                    {
                        Definition.Opcode: TailLocalCallWithFrame,
                        Operands: [{ Value.IsOne: true }, var retryFunction, var frame]
                    }
                    || retryFunction.Value != function.Value || frame.Value <= 0
                    || body[inner + 4] is not { Definition.Opcode: Label, Operands: [var rejected] }
                    || rejected.Value != innerFailure.Value
                    || body[inner + 5] is not { Definition.Opcode: CaseEnd, Operands: [var failed] } || failed != x0
                    || body[inner + 6].Definition.Opcode != Label)
                {
                    continue;
                }

                int outerCold = end - 9;
                int identity = end - 6;
                int fallback = end - 3;
                BeamOperand kept = first.Value == retry.Value ? second : first;
                flow ??= new BeamProjectionFlow(body);
                if (outerCold <= inner + 6
                    || !flow.TryGetSingleForwardTarget(outer, (int)nested.Value, out int nestedEntry)
                    || nestedEntry != outer + 1
                    || !flow.TryGetSingleForwardTarget(inner, (int)retry.Value, out int retryEntry)
                    || retryEntry != inner + 1
                    || !flow.TryGetSingleForwardTarget(inner, (int)innerFailure.Value, out int innerExit)
                    || innerExit != inner + 4
                    || !flow.TryGetSingleForwardTarget(outer, (int)outerFailure.Value, out int outerExit)
                    || outerExit != outerCold
                    || !flow.TryGetSingleForwardTarget(inner, (int)kept.Value, out int identityEntry)
                    || identityEntry != identity
                    || body[outerCold + 1] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
                    || body[outerCold + 2] is not { Definition.Opcode: CaseEnd, Operands: [var outerFailed] }
                    || outerFailed != x0
                    || body[identity + 1] is not { Definition.Opcode: Deallocate, Operands: [var released] }
                    || released != frame || body[identity + 2].Definition.Opcode != Return
                    || body[fallback].Definition.Opcode != Label
                    || body[fallback + 1] is not
                    {
                        Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Atom }, var fallbackResult]
                    }
                    || fallbackResult != x0 || body[fallback + 2].Definition.Opcode != Return
                    || ReadDisjointFrameOwnership(
                        body,
                        start,
                        end,
                        trackSiblingTrimEpochs: true
                    ) is not
                    { Owners: var owners }
                    || owners[outer - start] < 0 || owners[inner - start] != owners[outer - start]
                    || owners[fallback - start] != UnframedOwner)
                {
                    continue;
                }

                // Copy the selecting call's line for the rejected value before
                // moving that exit. The return and retry already end their blocks.
                output.Write(code.Span[copied..body[inner + 1].Offset]);
                Copy(identity, fallback);
                Copy(inner + 1, innerExit);
                Copy(inner + 6, outerCold);
                Copy(fallback, end);
                Copy(innerExit, innerExit + 1);
                Copy(inner - 2, inner - 1);
                Copy(innerExit + 1, innerExit + 2);
                Copy(outerCold, identity);
                copied = body[end].Offset;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void Copy(int start, int end) => output.Write(code.Span[body[start].Offset..body[end].Offset]);
    }

    private static byte[]? RewriteColdCheckedCaseExit(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        if (body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine))
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int start = 0;
        for (int boundary = 0; boundary < body.Count; boundary++)
        {
            byte opcode = body[boundary].Definition.Opcode;
            if (opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = boundary + 1;
            int end = boundary;
            if (opcode == FunctionInfo)
            {
                // The next function's label and declaration line belong to its func_info.
                while (end > entry && body[end - 1].Definition.Opcode == Line)
                {
                    end--;
                }

                if (end <= entry || body[--end].Definition.Opcode != Label)
                {
                    continue;
                }
            }

            if (end < entry + 8 || body[entry].Definition.Opcode != Label
                || !EndsBeamBlock(body[end - 1].Definition.Opcode))
            {
                continue;
            }

            int selected = -1;
            bool retainContinuation = false;
            for (int jump = entry + 2; jump + 5 < end; jump++)
            {
                if (body[jump] is not { Definition.Opcode: Jump, Operands: [var continuation] }
                    || body[jump + 1] is not { Definition.Opcode: Label, Operands: [var cold] }
                    || body[jump + 2] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
                    || body[jump + 3] is not { Definition.Opcode: CaseEnd, Operands: [var failed] }
                    || body[jump + 4] is not { Definition.Opcode: Label, Operands: [var resumed] }
                    || continuation.Value != resumed.Value || !PreservesContinuationLine(jump + 5, end)
                    || FindCheckedColdOwner(
                        entry,
                        jump,
                        cold,
                        failed
                    ) is not int check)
                {
                    continue;
                }

                flow ??= new BeamProjectionFlow(body);
                if (!flow.TryGetSingleForwardTarget(check, (int)cold.Value, out int coldEntry) || coldEntry != jump + 1
                    || !flow.HasUnprotectedIncomingPath(check, entry)
                    || !flow.HasUnprotectedIncomingPath(jump, entry))
                {
                    continue;
                }

                if (selected >= 0)
                {
                    selected = -1;
                    break;
                }

                selected = jump;
                retainContinuation = !flow.TryGetSingleForwardTarget(jump, (int)resumed.Value, out _);
            }

            if (selected < 0)
            {
                continue;
            }

            // Only the early checked failure moves. The later catch retains
            // every entry and handler edge, and the exited value stays in X0.
            output.Write(code.Span[copied..body[selected].Offset]);
            output.Write(code.Span[body[selected + (retainContinuation ? 4 : 5)].Offset..body[end].Offset]);
            output.Write(code.Span[body[selected + 1].Offset..body[selected + 4].Offset]);
            copied = body[end].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool PreservesContinuationLine(int index, int end)
        {
            while (index < end && body[index].Definition.Opcode is Move or InitYRegisters)
            {
                index++;
            }

            return index < end && body[index] is { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] };
        }

        int? FindCheckedColdOwner(
            int entry,
            int jump,
            BeamOperand cold,
            BeamOperand failed
        )
        {
            for (int index = entry + 2; index < jump; index++)
            {
                if (body[index] is
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [var failure, { Kind: BeamOperandParameters.X, Value.IsZero: true } carrier, _, _]
                    } && failure.Value == cold.Value && SameProjectionRegister(carrier, failed))
                {
                    return index;
                }
            }

            return null;
        }
    }
}
