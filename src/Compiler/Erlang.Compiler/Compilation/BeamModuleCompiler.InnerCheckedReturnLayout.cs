using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? PlaceInnerCheckedReturnBeforeOuterExits(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(CaseEnd)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(AutoImportNames.Exit, out int exit)
            || !imports.TryGetValue((erlang, exit, 1), out int exitImport)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        byte[]? relocated = RewriteInnerCheckedReturnBeforeOuterExits(
            code,
            body,
            emittedImports?[exitImport] ?? exitImport
        );

        return relocated is null
            ? null
            : OrderFinalPhysicalLabelsCore(
                relocated,
                labelCount,
                formMultiKeySelections: false,
                relocateOnly: true
            );
    }

    private static byte[]? RewriteInnerCheckedReturnBeforeOuterExits(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int exitImport
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = -1;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            int tail = end;
            if (body[end].Definition.Opcode == FunctionInfo)
            {
                while (tail > start && body[tail - 1].Definition.Opcode == Line)
                {
                    tail--;
                }

                if (tail - 1 <= start || body[--tail].Definition.Opcode != Label)
                {
                    continue;
                }
            }

            int returned = tail - 3;
            if (start < 0 || returned < start + 9 || body[returned] is not
                {
                    Definition.Opcode: Label,
                    Operands: [var returnLabel]
                }
                || body[returned + 1] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                || body[returned + 2].Definition.Opcode != Return
                || body[returned - 1] is not { Definition.Opcode: CaseEnd, Operands: [var caseValue] }
                || caseValue != x0 || body[returned - 2].Definition.Opcode != Line
                || body[returned - 3] is not { Definition.Opcode: Label, Operands: [var caseLabel] })
            {
                continue;
            }

            int first = returned - 3;
            var outer = new HashSet<int> { (int)caseLabel.Value };
            while (first - 4 > start && body[first - 4] is
                {
                    Definition.Opcode: Label,
                    Operands: [var exitLabel]
                } && body[first - 3] is { Definition.Opcode: Move, Operands: [_, var argument] }
                && argument == x0 && body[first - 2].Definition.Opcode == Line
                && body[first - 1] is
                {
                    Definition.Opcode: TailExternalCallWithFrame,
                    Operands: [{ Value.IsOne: true }, var import, var deallocation]
                } && import.Value == exitImport && deallocation == width)
            {
                outer.Add((int)exitLabel.Value);
                first -= 4;
            }

            if (outer.Count < 2 || body[first - 1].Definition.Opcode != Return
                || !HasUnprotectedCallKillSchedule(body, start))
            {
                continue;
            }

            int check = -1;
            for (int index = start + 2; index < first; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [var target, var checkedResult, _, _]
                    } || target.Value != returnLabel.Value || checkedResult != x0
                    || body[index - 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var captured, { Kind: BeamOperandParameters.Y, TypeIndex: null }]
                    } || captured != x0 || body[index - 2].Definition.Opcode is not (LocalCall or ExternalCall))
                {
                    continue;
                }

                check = index;
                break;
            }

            if (check < 0 || !HasEarlierOuterFailureEdges(
                start,
                check,
                returned,
                outer
            )
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                    check,
                    (int)returnLabel.Value,
                    out int owned
                ) || owned != returned
                || ReadDisjointFrameOwnership(
                    body,
                    start,
                    tail,
                    trackSiblingTrimEpochs: true
                ) is not { } frame)
            {
                continue;
            }

            int owner = frame.Owners[returned - start];
            if (owner < 0 || frame.Owners[check - start] != owner || !outer.All(label =>
                Enumerable.Range(first, returned - first).Any(index =>
                    body[index] is { Definition.Opcode: Label, Operands: [var operand] }
                    && operand.Value == label && frame.Owners[index - start] == owner)))
            {
                continue;
            }

            // SSA linearization visits the enclosing failures before their
            // normal successors and emits the reverse postorder.
            // The inner identity return therefore precedes outer exits.
            output.Write(code.Span[copied..body[first].Offset]);
            output.Write(code.Span[body[returned].Offset..body[tail].Offset]);
            output.Write(code.Span[body[first].Offset..body[returned].Offset]);
            copied = body[tail].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool HasEarlierOuterFailureEdges(
            int start,
            int check,
            int returned,
            HashSet<int> outer
        )
        {
            var earlier = new HashSet<int>();
            for (int index = start; index < returned; index++)
            {
                foreach (BeamOperand operand in body[index].Operands)
                {
                    if (operand.Kind != BeamOperandParameters.Label || !outer.Contains((int)operand.Value))
                    {
                        continue;
                    }

                    if (index >= check)
                    {
                        return false;
                    }

                    earlier.Add((int)operand.Value);
                }
            }

            return earlier.Count == outer.Count;
        }
    }
}
