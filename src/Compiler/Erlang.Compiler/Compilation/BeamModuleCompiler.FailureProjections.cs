using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveRepeatedFailureProjections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> labelOperands,
        bool shareCheckedFallbacks
    )
    {
        var removals = new SortedDictionary<int, int>();
        BeamProjectionFlow? flow = null;
        for (int index = 1; index < body.Count; index++)
        {
            BeamInstruction test = body[index];
            if (body[index - 1].Definition.Opcode != GetTupleElement
                || !IsEntryFramePredicate(test.Definition.Opcode)
                || test.Operands[0].Kind != BeamOperandParameters.Label)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (shareCheckedFallbacks && ShareExclusiveCheckedFailureProjection(
                code,
                body,
                labelOperands,
                flow,
                index
            ) is { } shared)
            {
                return shared;
            }

            if (!flow.TryGetSingleForwardTarget(index, (int)test.Operands[0].Value, out int target))
            {
                continue;
            }

            int first = index - 1;
            while (first > 0 && body[first - 1].Definition.Opcode == GetTupleElement)
            {
                first--;
            }

            int repeat = target + 1;
            int live = BeamOperandParameters.MaximumRegister;
            while (repeat < body.Count && body[repeat].Definition.Opcode is Line or TestHeap)
            {
                if (body[repeat].Definition.Opcode == TestHeap)
                {
                    live = Math.Min(live, (int)body[repeat].Operands[^1].Value);
                }

                repeat++;
            }

            int count = index - first;
            if (repeat + count >= body.Count || !SameStableProjections(
                body,
                first,
                repeat,
                count,
                live
            ))
            {
                continue;
            }

            removals.Add(body[repeat].Offset, body[repeat + count].Offset);
        }

        if (removals.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        foreach (var (from, to) in removals)
        {
            if (from >= copied)
            {
                output.Write(code.Span[copied..from]);
            }

            copied = Math.Max(copied, to);
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static byte[]? ShareExclusiveCheckedFailureProjection(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> labelOperands,
        BeamProjectionFlow flow,
        int predicate
    )
    {
        if (predicate < 2 || body[predicate] is not
            {
                Definition.Opcode: ExactEqualTest or ExactNotEqualTest,
                Operands: [var failure, var compared,
                {
                    Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                        or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                }]
            }
            || failure.Value.Sign <= 0
            || body[predicate - 1].Operands is not [var source, _, var destination]
            || !IsBareProjectionRegister(source) || !IsBareProjectionRegister(destination)
            || source == destination || compared != destination
            || body[predicate - 2] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [var shapeFailure, var carrier, var arity, var tag]
            }
            || shapeFailure != failure || carrier != source
            || !flow.TryGetExclusiveForwardTarget(
                predicate - 2,
                predicate,
                (int)failure.Value,
                out int target
            )
            || target + 3 >= body.Count
            || body[target + 1] is not
            {
                Definition.Opcode: IsTaggedTuple,
                Operands: [{ Kind: BeamOperandParameters.Label } finalFailure, var secondCarrier, var secondArity, var secondTag]
            }
            || finalFailure == failure || secondCarrier != carrier || secondArity != arity || secondTag != tag
            || body[target + 2].Definition.Opcode != GetTupleElement
            || !body[target + 2].Operands.SequenceEqual(body[predicate - 1].Operands)
            || body[target + 3].Definition.Opcode is Label or FunctionInfo or EndCode
            || !flow.TryGetForwardLabel(target + 2, (int)finalFailure.Value, out int finalTarget)
            || !HasUnprotectedCheckedFailureRegion(body, predicate - 2, finalTarget))
        {
            return null;
        }

        int failureOffset = body[predicate - 2].Offset + 1;
        var operand = labelOperands.FirstOrDefault(
            label => label.Start == failureOffset && label.Label == (int)failure.Value
        );
        if (operand.Start != failureOffset)
        {
            return null;
        }

        // A failed shape goes directly to the same rejection. The failed
        // comparison still enters with its checked field available.
        using var output = new MemoryStream();
        output.Write(code.Span[..operand.Start]);
        BeamCompactWriter.Write(output, LabelTag, finalFailure.Value);
        output.Write(code.Span[operand.End..body[target + 1].Offset]);
        output.Write(code.Span[body[target + 3].Offset..]);

        return output.ToArray();
    }

    private static bool HasUnprotectedCheckedFailureRegion(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int finalTarget
    )
    {
        int info = start;
        while (info >= 0 && body[info].Definition.Opcode != FunctionInfo)
        {
            info--;
        }

        if (info < 0)
        {
            return false;
        }

        int end = info + 1;
        while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
        {
            if (body[end].Definition.Opcode is CatchEnter or CatchEnd or TryEnter or TryEnd or TryCase or TryCaseEnd
                or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout or RemoveMessage
                or ReceiveMarkerBind or ReceiveMarkerClear or ReceiveMarkerReserve or ReceiveMarkerUse
                or ExecutableLine or DebugLine)
            {
                return false;
            }

            end++;
        }

        if (body[end].Definition.Opcode == FunctionInfo)
        {
            // The next function's info label precedes its FunctionInfo.
            while (body[end - 1].Definition.Opcode == Line)
            {
                end--;
            }

            if (body[end - 1].Definition.Opcode == Label)
            {
                end--;
            }
        }

        return finalTarget < end;
    }

    private static bool SameStableProjections(
        IReadOnlyList<BeamInstruction> body,
        int first,
        int repeat,
        int count,
        int live
    )
    {
        var sources = new HashSet<BeamOperand>();
        var destinations = new HashSet<BeamOperand>();
        for (int offset = 0; offset < count; offset++)
        {
            BeamInstruction original = body[first + offset];
            BeamInstruction duplicate = body[repeat + offset];
            if (duplicate.Definition.Opcode != GetTupleElement
                || !original.Operands.SequenceEqual(duplicate.Operands))
            {
                return false;
            }

            BeamOperand source = original.Operands[0];
            BeamOperand destination = original.Operands[^1];
            if (destination.Kind == BeamOperandParameters.X && destination.Value >= live)
            {
                return false;
            }

            sources.Add(source with { TypeIndex = null });
            destinations.Add(destination with { TypeIndex = null });
        }

        return !sources.Overlaps(destinations);
    }
}
