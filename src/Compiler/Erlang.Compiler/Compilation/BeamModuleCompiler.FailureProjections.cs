using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? RemoveRepeatedFailureProjections(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> labelOperands,
        bool shareCheckedFallbacks
    ) => RewriteFailureProjectionJoins(
        code,
        body,
        labelOperands,
        shareCheckedFallbacks,
        literalAndNilOnly: false
    );

    private byte[]? RemoveFinalLiteralAndNilRetryProjections(ReadOnlyMemory<byte> code, int labelCount) =>
        !ModuleOptimizationDisabled && SsaTypeInferenceEnabled && code.Span.Contains(IsNil)
            && DecodeLateAtomicProjectionSlots(code, labelCount) is { } decoded
            ? RewriteFailureProjectionJoins(
                code,
                decoded.Body,
                [],
                shareCheckedFallbacks: false,
                literalAndNilOnly: true
            ) : null;

    private static byte[]? RewriteFailureProjectionJoins(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> labelOperands,
        bool shareCheckedFallbacks,
        bool literalAndNilOnly
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
            if (shareCheckedFallbacks && (ShareExclusiveCheckedFailureProjection(
                code,
                body,
                labelOperands,
                flow,
                index
            ) ?? ShareExclusiveArityFailureProjection(
                code,
                body,
                labelOperands,
                flow,
                index
            ) ?? ShareRepeatedTupleTagFailure(
                code,
                body,
                labelOperands,
                flow,
                index
            )) is { } shared)
            {
                return shared;
            }

            int label = (int)test.Operands[0].Value;
            int target;
            bool owned = literalAndNilOnly
                ? TryGetLiteralAndNilProjectionRetry(
                    body,
                    flow,
                    index,
                    label,
                    out target
                )
                : flow.TryGetSingleForwardTarget(index, label, out target);
            if (!owned)
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

    private static bool TryGetLiteralAndNilProjectionRetry(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int predicate,
        int label,
        out int target
    )
    {
        target = -1;
        if (predicate + 1 >= body.Count || body[predicate] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [var failure, var compared,
                {
                    Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                        or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                }]
            }
            || !IsBareProjectionRegister(compared) || compared != body[predicate - 1].Operands[^1]
            || body[predicate + 1] is not
            {
                Definition.Opcode: IsNil,
                Operands: [var nilFailure, var checkedValue]
            }
            || nilFailure != failure || !IsBareProjectionRegister(checkedValue))
        {
            return false;
        }

        // Both retry edges follow the same projection and neither test changes it.
        return flow.TryGetExclusiveForwardTarget(
            predicate,
            predicate + 1,
            label,
            out target
        )
            && HasUnprotectedCheckedFailureRegion(body, predicate - 1, target);
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

    private static byte[]? ShareExclusiveArityFailureProjection(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> labelOperands,
        BeamProjectionFlow flow,
        int predicate
    )
    {
        if (predicate < 3 || body[predicate] is not
            {
                Definition.Opcode: IsLess or IsGreaterOrEqual or NumericEqualTest or NumericNotEqualTest
                    or ExactEqualTest or ExactNotEqualTest,
                Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 } failure, var left, var right]
            })
        {
            return null;
        }

        int first = predicate - 3;
        if (body[first] is not
            {
                Definition.Opcode: IsTuple,
                Operands: [var nonTuple, { Kind: BeamOperandParameters.X, TypeIndex: null } tuple]
            } || nonTuple != failure
            || body[first + 1] is not
            {
                Definition.Opcode: TestArity,
                Operands: [var wrongArity, var tested, { Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 } arity]
            } || wrongArity != failure || tested != tuple
            || body[first + 2].Operands is not
            [
                var source, { Kind: BeamOperandParameters.Unsigned } field,
            { Kind: BeamOperandParameters.X, TypeIndex: null } destination
            ]
            || source != tuple || field.Value >= arity.Value
            || !SameProjectionRegister(left, destination) && !SameProjectionRegister(right, destination)
            || !flow.TryGetExclusiveForwardTarget(
                first,
                first + 1,
                predicate,
                (int)failure.Value,
                out int target
            ) || target + 4 >= body.Count
            || body[target + 1] is not
            {
                Definition.Opcode: IsTuple,
                Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 } rejected, var repeated]
            } || repeated != tuple || rejected == failure
            || body[target + 2] is not
            {
                Definition.Opcode: TestArity,
                Operands: [var secondRejected, var secondTested, var secondArity]
            } || secondRejected != rejected || secondTested != tuple || secondArity != arity
            || !SameStableProjections(
                body,
                first + 2,
                target + 3,
                1,
                BeamOperandParameters.MaximumRegister
            )
            || body[target + 4].Definition.Opcode is Label or FunctionInfo or EndCode
            || !flow.TryGetForwardLabel(target + 3, (int)rejected.Value, out int finalTarget)
            || !HasUnprotectedCheckedFailureRegion(body, first, finalTarget))
        {
            return null;
        }

        var changes = new List<CodeEdit>();
        for (int index = first; index <= first + 1; index++)
        {
            int offset = body[index].Offset + 1;
            var operand = labelOperands.FirstOrDefault(label => label.Start == offset && label.Label == (int)failure.Value);
            if (operand.Start != offset)
            {
                return null;
            }

            changes.Add(new(
                offset,
                operand.End,
                LabelTag,
                (int)rejected.Value
            ));
        }

        // Constructor rejection bypasses the repeated pure check. The sole
        // remaining incoming guard edge still owns the original field in X.
        changes.Add(new(
            body[target + 1].Offset,
            body[target + 4].Offset,
            0,
            0,
            IsRemoval: true
        ));

        return ApplyDuplicatedFrameReadEdits(code, changes);
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
