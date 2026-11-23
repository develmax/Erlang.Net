using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseStableRetryTupleProjections(ReadOnlyMemory<byte> code, int labelCount) =>
        RewriteStableRetryTupleProjections(code, labelCount, interleaved: false);

    private byte[]? ReuseInterleavedRetryTupleProjections(ReadOnlyMemory<byte> code, int labelCount) =>
        RewriteStableRetryTupleProjections(code, labelCount, interleaved: true);

    private byte[]? RewriteStableRetryTupleProjections(ReadOnlyMemory<byte> code, int labelCount, bool interleaved)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamProjectionFlow? flow = null;
        var edits = new List<CodeEdit>();
        int entry = -1;
        bool unprotected = false;
        for (int retry = 0; retry + 3 < body.Count; retry++)
        {
            if (body[retry].Definition.Opcode == FunctionInfo)
            {
                entry = retry + 1;
                unprotected = HasUnprotectedCheckedFailureRegion(body, entry, entry);

                continue;
            }

            if (!unprotected || retry <= entry || body[retry].Definition.Opcode != Label
                || body[retry + 1].Definition.Opcode != GetTupleElement)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            var carrier = body[retry + 1].Operands[0];
            if (carrier is not { Kind: BeamOperandParameters.X, TypeIndex: null })
            {
                continue;
            }

            if (interleaved)
            {
                if (CanReuseInterleavedRetryFields(
                    body,
                    flow,
                    entry,
                    retry,
                    carrier
                ))
                {
                    RemoveProjection(retry + 1, retry + 2);
                    RemoveProjection(retry + 3, retry + 4);
                }

                continue;
            }

            if (body[retry + 2].Definition.Opcode != GetTupleElement)
            {
                continue;
            }

            int end = retry + 1;
            var destinations = new HashSet<BeamOperand>();
            while (end < body.Count && body[end] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands:
                    [
                        var tuple, _,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } result
                    ]
                } && tuple == carrier && result != carrier && destinations.Add(result)
                && flow.HasStableProjectionOnEveryRetryEdge(
                    entry,
                    retry,
                    body[end]
                ))
            {
                end++;
            }

            // The original projection instructions remain on the earlier
            // predecessors. A later retry may reuse them too, because every
            // admitted edge is forward and preserves each physical value.
            if (end < body.Count && end >= retry + 3 && body[end].Definition.Opcode != GetTupleElement)
            {
                RemoveProjection(retry + 1, end);
                retry = end - 1;
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        void RemoveProjection(int start, int end)
        {
            edits.Add(new(
                body[start].Offset,
                body[end].Offset,
                0,
                0,
                IsRemoval: true
            ));
        }
    }

    private static bool CanReuseInterleavedRetryFields(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int entry,
        int retry,
        BeamOperand carrier
    )
    {
        if (retry + 4 >= body.Count || body[retry + 1].Operands[^1] is not
            { Kind: BeamOperandParameters.X, TypeIndex: null } first || first == carrier
            || body[retry + 3] is not
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var tuple, _, { Kind: BeamOperandParameters.X, TypeIndex: null } second]
            } || tuple != carrier || second == first || second == carrier
            || body[retry + 2] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [var rejected, var left, var firstLiteral]
            } || left != first || rejected.Value.IsZero || !IsNonRegisterValue(firstLiteral)
            || body[retry + 4] is not
            {
                Definition.Opcode: ExactEqualTest,
                Operands: [var otherRejected, var right, var secondLiteral]
            } || right != second || rejected != otherRejected || !IsNonRegisterValue(secondLiteral))
        {
            return false;
        }

        // A retry reached only by a failing exact literal test can already
        // carry both fields. The interleaved tests keep their original order.
        return flow.HasStableProjectionOnEveryRetryEdge(
            entry,
            retry,
            body[retry + 1],
            allowSingleExactFailure: true
        )
            && flow.HasStableProjectionOnEveryRetryEdge(
                entry,
                retry,
                body[retry + 3],
                allowSingleExactFailure: true
            );
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool HasStableProjectionOnEveryRetryEdge(
            int entry,
            int retry,
            BeamInstruction projection,
            bool allowSingleExactFailure = false,
            bool allowStableSelector = false
        )
        {
            if (predecessors[retry].Count == 0 || predecessors[retry].Count == 1
                && (!allowSingleExactFailure || body[predecessors[retry][0]] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var failure, _, var literal]
                } || failure.Value != body[retry].Operands[0].Value || !IsNonRegisterValue(literal)))
            {
                return false;
            }

            var carrier = projection.Operands[0];
            var result = projection.Operands[^1];
            var visited = new HashSet<int>();
            var work = new Stack<int>(predecessors[retry]);
            while (work.TryPop(out int index))
            {
                if (index <= entry || index >= retry)
                {
                    return false;
                }

                if (!visited.Add(index))
                {
                    continue;
                }

                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode == GetTupleElement && instruction.Operands.SequenceEqual(projection.Operands))
                {
                    continue;
                }

                bool stable = opcode switch
                {
                    Move or GetHead or GetTail or GetTupleElement => !WritesProtected(instruction.Operands[^1]),
                    GetList => !WritesProtected(instruction.Operands[1]) && !WritesProtected(instruction.Operands[2]),
                    Label or Line or Jump => true,
                    SelectValue when allowStableSelector => true,
                    _ => IsEntryFramePredicate(opcode)
                };
                if (!stable || predecessors[index].Count == 0
                    || predecessors[index].Any(predecessor => predecessor >= index))
                {
                    return false;
                }

                foreach (int predecessor in predecessors[index])
                {
                    work.Push(predecessor);
                }
            }

            return true;

            bool WritesProtected(BeamOperand operand) =>
                SameProjectionRegister(operand, carrier) || SameProjectionRegister(operand, result);
        }
    }
}
