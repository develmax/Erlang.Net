using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepCallResultThroughCheckedProjection(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(BadMatch)
            || !code.Span.Contains(IsTuple) && !code.Span.Contains(IsNonemptyList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        var edits = new List<CodeEdit>();
        BeamProjectionFlow? flow = null;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            for (int spill = start + 2; spill + 12 < end; spill++)
            {
                int first = spill + 1;
                int last = first + 2;
                int jump = last + 1;
                int failure = jump + 1;
                int restore = failure + 1;
                if (body[spill - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[spill] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } carrier]
                    } || result != x0
                    || body[first] is not
                    {
                        Definition.Opcode: IsTuple or IsNonemptyList,
                        Operands: [{ Kind: BeamOperandParameters.Label } rejected, var checkedValue]
                    } || checkedValue != carrier
                    || body[jump] is not { Definition.Opcode: Jump, Operands: [var selected] }
                    || body[failure] is not { Definition.Opcode: Label, Operands: [var failedLabel] }
                    || failedLabel.Value != rejected.Value || !IsMove(body[restore], carrier, x0))
                {
                    continue;
                }

                int badMatch = SkipCheckedProjectionLine(body, restore + 1, end);
                int success = badMatch + 1;
                if (success + 3 >= end || body[badMatch] is not { Definition.Opcode: BadMatch, Operands: [var failed] }
                    || failed != x0
                    || body[success] is not { Definition.Opcode: Label, Operands: [var successLabel] }
                    || successLabel.Value != selected.Value)
                {
                    continue;
                }

                bool tuple = body[first].Definition.Opcode == IsTuple;
                int resultMove = success + 1;
                BeamOperand scratch;
                if (tuple)
                {
                    if (body[first + 1] is not
                        { Definition.Opcode: TestArity, Operands: [var badTuple, var tested, var arity] }
                        || badTuple != rejected || tested != carrier || body[last] is not
                        {
                            Definition.Opcode: GetTupleElement,
                            Operands: [var source, var field, { Kind: BeamOperandParameters.Y, TypeIndex: null } projected]
                        } || source != carrier || field.Value >= arity.Value
                        || !IsMove(body[resultMove], projected, x1)
                        || body[resultMove + 1] is not
                        {
                            Definition.Opcode: Move,
                            Operands: [
                            {
                                Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                                or BeamOperandParameters.Nil
                            }, var argument]
                        } || argument != x0)
                    {
                        continue;
                    }

                    scratch = projected;
                }
                else
                {
                    if (body[first + 1] is not
                        {
                            Definition.Opcode: GetList,
                            Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } head,
                            { Kind: BeamOperandParameters.Y, TypeIndex: null } tail]
                        } || source != carrier || tail == head || head == carrier
                        || body[last] is not { Definition.Opcode: IsNil, Operands: [var badTail, var testedTail] }
                        || badTail != rejected || testedTail != tail || !IsMove(body[resultMove], head, x0))
                    {
                        continue;
                    }

                    scratch = tail;
                }

                int call = SkipCheckedProjectionLine(body, resultMove + (tuple ? 2 : 1), end);
                if (call >= end || scratch == carrier || body[call] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arityAtCall, _] }
                    || arityAtCall.Value != (tuple ? 2 : 1)
                    || CountUses(carrier) != (tuple ? 5 : 4) || CountUses(scratch) != 2
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetExclusiveForwardTarget(
                        first,
                        tuple ? first + 1 : last,
                        (int)rejected.Value,
                        out int failureTarget
                    ) || failureTarget != failure || !flow.TryGetSingleForwardTarget(jump, (int)selected.Value, out int successTarget)
                    || successTarget != success
                    || !flow.IsInitialized(
                        spill,
                        carrier,
                        allowFramePredicates: true,
                        requireClearedSlot: true,
                        allowUnchangedTryEntry: true
                    )
                    || !flow.IsInitialized(
                        spill,
                        scratch,
                        allowFramePredicates: true,
                        requireClearedSlot: true,
                        allowUnchangedTryEntry: true
                    ))
                {
                    continue;
                }

                // Both rejected tests still reach the original badmatch with
                // the producer in X0. X1 holds the selected field or tested tail.
                Remove(spill);

                int upper = body[jump].Offset;
                int operand = FirstStackOperandAtOrAfter(stackOperands, body[first].Offset);
                for (; operand < stackOperands.Count && stackOperands[operand].Start < upper; operand++)
                {
                    var (from, to, slot) = stackOperands[operand];
                    if (slot == carrier.Value || slot == scratch.Value)
                    {
                        edits.Add(new(
                            from,
                            to,
                            XRegisterTag,
                            slot == carrier.Value ? 0 : 1
                        ));
                    }
                }

                Remove(restore);
                if (tuple)
                {
                    Remove(resultMove);
                }

                spill = call - 1;

                int CountUses(BeamOperand register) => Enumerable.Range(start, end - start)
                    .Where(at => body[at].Definition.Opcode != InitYRegisters)
                    .Sum(at => body[at].Operands.Count(operand => SameProjectionRegister(operand, register)));
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        void Remove(int at) => edits.Add(new(
            body[at].Offset,
            body[at + 1].Offset,
            0,
            0,
            IsRemoval: true
        ));
    }

    private static int SkipCheckedProjectionLine(IReadOnlyList<BeamInstruction> body, int start, int end)
    {
        while (start < end && body[start].Definition.Opcode == Line)
        {
            start++;
        }

        return start;
    }
}
