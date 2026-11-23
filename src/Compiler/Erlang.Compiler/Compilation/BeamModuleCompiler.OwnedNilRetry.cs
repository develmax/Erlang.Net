using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? KeepNonemptyStatusRetryInFallthrough(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsNil)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var nil = new BeamOperand(BeamOperandParameters.Nil, NilAtomIndex);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 1; index + 20 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: IsNil, Operands: [var retry, var tested] }
                || tested != x0 || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } original, var fallback]
                }
                || fallback != x0 || body[index + 2] is not { Definition.Opcode: Jump, Operands: [var joined] }
                || !HasLabel(index + 3, retry) || body[index + 4] is not
                {
                    Definition.Opcode: TestHeap,
                    Operands: [{ Kind: BeamOperandParameters.Unsigned } words, { Value.IsZero: true }]
                }
                || words.Value != 2 * ConsHeapWords || body[index + 5] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } first, var end, var list]
                }
                || end != nil || list != x0 || body[index + 6] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } second, var tail, var argument]
                }
                || tail != x0 || argument != x0 || first == second || first == original || second == original
                || body[index + 7] is not
                { Definition.Opcode: InitYRegisters, Operands: [{ Value.IsOne: true }, var cleared] }
                || cleared != second || !IsLocatedOrdinaryCall(index + 8, arity: 1)
                || body[index + 10] is not { Definition.Opcode: Move, Operands: [var constant, var nextArgument] }
                || !IsNonRegisterValue(constant) || nextArgument != x1 || !IsLocatedOrdinaryCall(index + 11, arity: 2)
                || body[index + 13] is not { Definition.Opcode: Jump, Operands: [var completed] } || completed != joined
                || body[index + 14].Definition.Opcode != Label || body[index + 15] is not
                {
                    Definition.Opcode: ExactNotEqualTest,
                    Operands: [var atomJoin, var otherStatus, { Kind: BeamOperandParameters.Atom }]
                }
                || atomJoin != joined || otherStatus != x0
                || body[index + 16] is not { Definition.Opcode: Label, Operands: [var restore] }
                || !IsMove(body[index + 17], original, x0) || !HasLabel(index + 18, joined)
                || body[index + 19] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } status]
                }
                || result != x0)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            int[] retired = [(int)original.Value, (int)first.Value, (int)second.Value];
            Array.Sort(retired);
            if (!flow.TryGetSingleForwardTarget(index, (int)retry.Value, out int entry) || entry != index + 3
                || !HasRetiredRetryRoots(
                    body,
                    flow,
                    index,
                    status,
                    retired
                ))
            {
                continue;
            }

            // A nil result takes the already existing original-status edge.
            // The retry's list retains both consumed fields through the call.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(ExactNotEqualTest);
            BeamCompactWriter.Write(output, LabelTag, restore.Value);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, AtomTag, NilAtomIndex);
            output.Write(code.Span[body[index + 4].Offset..body[index + 7].Offset]);
            WriteFrameInitialization(output, retired);
            copied = body[index + 8].Offset;
            index += 18;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        byte[] rewritten = output.ToArray();

        return OrderFinalPhysicalLabelsCore(
            rewritten,
            labelCount,
            formMultiKeySelections: false,
            relocateOnly: true
        );

        bool HasLabel(int at, BeamOperand target) =>
            body[at] is { Definition.Opcode: Label, Operands: [var definition] } && definition.Value == target.Value;

        bool IsLocatedOrdinaryCall(int at, int arity) =>
            body[at] is { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
            && body[at + 1] is { Definition.Opcode: LocalCall or ExternalCall, Operands: [var count, _] }
            && count.Value == arity;
    }

    private static bool HasRetiredRetryRoots(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int selected,
        BeamOperand status,
        int[] slots
    )
    {
        int start = selected;
        while (start > 0 && body[start - 1].Definition.Opcode != FunctionInfo)
        {
            start--;
        }

        int join = selected + 18;
        int end = join + 1;
        while (end + 1 < body.Count && body[end].Definition.Opcode is not (FunctionInfo or EndCode))
        {
            end++;
        }

        if (ReadDisjointFrameOwnership(
            body,
            start,
            end,
            trackSiblingTrimEpochs: true
        ) is not { } frame || frame.Uses.Count > 2 || !flow.HasForwardNestedStatusSuffix(join, end))
        {
            return false;
        }

        int owner = frame.Owners[selected - start];
        if (owner < start || frame.Owners[join - start] != owner || frame.Owners[selected + 7 - start] != owner
            || frame.Owners[selected + 16 - start] != owner || !frame.Uses.TryGetValue(owner, out var live)
            || !slots.SequenceEqual(Enumerable.Range(0, live.Length)))
        {
            return false;
        }

        // Both paths share the following assignment to the status slot.
        // Any slot kept by a later trim must be defined in this suffix.

        for (int index = join + 2; index < end; index++)
        {
            if (frame.Owners[index - start] != owner)
            {
                continue;
            }

            BeamInstruction operation = body[index];
            if (operation.Definition.Opcode == TrimFrame)
            {
                if (operation.Operands is not [var removed, { Value.IsOne: true }]
                    || removed.Value != status.Value && (body[index - 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [_, { Kind: BeamOperandParameters.Y, Value: var copyTarget }]
                    } || copyTarget != removed.Value))
                {
                    return false;
                }

                continue;
            }

            if (operation.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            bool definesTrimRoot = operation.Definition.Opcode == Move && index + 1 < end
                && body[index + 1] is { Definition.Opcode: TrimFrame, Operands: [var dropped, { Value.IsOne: true }] }
                && operation.Operands[1] is { Kind: BeamOperandParameters.Y } destination && destination.Value == dropped.Value;
            for (int position = 0; position < operation.Operands.Count; position++)
            {
                if (operation.Operands[position] is { Kind: BeamOperandParameters.Y } operand
                    && !SameProjectionRegister(operand, status) && (position != 1 || !definesTrimRoot))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
