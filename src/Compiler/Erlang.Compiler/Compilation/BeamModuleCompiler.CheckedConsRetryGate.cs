using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? BypassRejectedConsRetry(ReadOnlyMemory<byte> code, int labelCount) =>
        !ModuleOptimizationDisabled && SsaTypeInferenceEnabled && code.Span.Contains(IsNonemptyList)
            && DecodeLateAtomicProjectionSlots(code, labelCount) is { } decoded
            ? MoveRejectedConsRetryEdge(code, decoded.Body)
            : null;

    private static byte[]? MoveRejectedConsRetryEdge(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int first = 0; first + 2 < body.Count; first++)
        {
            if (body[first] is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Label } retry,
                    { Kind: BeamOperandParameters.X or BeamOperandParameters.Y, TypeIndex: null } state
                    ]
                }
                || body[first + 1] is not { Definition.Opcode: GetList, Operands: [var inspected, _, _] }
                || inspected != state
                || !(flow ??= new BeamProjectionFlow(body)).TryGetForwardLabel(first + 1, (int)retry.Value, out int target)
                || !flow.ProvesConsOnRemainingRetryEdges(first, target, state)
                || ReadRepeatedConsGate(
                    body,
                    flow,
                    target,
                    state
                ) is not { } gate
                || !HasUnprotectedCheckedFailureRegion(body, first, gate.Rejection)
                || body[first].Offset < copied)
            {
                continue;
            }

            int offset = body[first].Offset + 1;
            var cursor = new BeamCursor(code[offset..]);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[copied..offset]);
            BeamCompactWriter.Write(output, LabelTag, gate.Label.Value);
            output.Write(code.Span[(offset + cursor.Position)..body[gate.Predicate].Offset]);
            copied = body[gate.Predicate + 1].Offset;
            // Analyze any further changed function with its original predecessors.
            first = gate.Rejection;
            while (body[first + 1].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                first++;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Predicate, int Rejection, BeamOperand Label)? ReadRepeatedConsGate(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int target,
        BeamOperand state
    )
    {
        int predicate = target + 1;
        var overwritten = new List<BeamOperand>();
        while (state.Kind == BeamOperandParameters.X && predicate + 1 < body.Count && body[predicate] is
            {
                Definition.Opcode: GetTupleElement,
                Operands:
                [
                { Kind: BeamOperandParameters.X },
                    _,
                { Kind: BeamOperandParameters.X, TypeIndex: null } result
                ]
            })
        {
            if (SameProjectionRegister(result, state)
                || body[predicate + 1] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Label },
                        var compared,
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                                or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil
                    }
                    ]
                } || compared != result)
            {
                return null;
            }

            overwritten.Add(result);
            predicate += 2;
        }

        if (predicate + 1 >= body.Count || body[predicate] is not
            {
                Definition.Opcode: IsNonemptyList,
                Operands: [{ Kind: BeamOperandParameters.Label } rejected, var tested]
            }
            || tested != state
            || body[predicate + 1] is not { Definition.Opcode: GetList, Operands: [var subject, _, _] }
            || subject != state
            || !flow.TryGetForwardLabel(predicate, (int)rejected.Value, out int rejection))
        {
            return null;
        }

        for (int index = target + 2; index < predicate; index += 2)
        {
            if (body[index].Operands[0] != rejected)
            {
                return null;
            }
        }

        // Bypassing the pure comparisons cannot change any input to rejection.
        return overwritten.All(result => flow.IsEntryTemporaryDead(rejection, result))
            ? (predicate, rejection, rejected) : null;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool ProvesConsOnRemainingRetryEdges(int guard, int retry, BeamOperand state)
        {
            var incoming = predecessors[retry];
            if (incoming.Count < 2 || !incoming.Contains(guard)
                || incoming.Any(predecessor => predecessor < guard || predecessor >= retry))
            {
                return false;
            }

            int final = incoming.Max();
            for (int index = guard + 1; index <= final; index++)
            {
                if (predecessors[index] is not [var previous] || previous != index - 1)
                {
                    return false;
                }

                var instruction = body[index];
                bool stateUnchanged = instruction.Definition.Opcode switch
                {
                    GetList => !instruction.Operands.Skip(1).Any(operand => SameProjectionRegister(operand, state)),
                    GetHead or GetTail or GetTupleElement => !SameProjectionRegister(instruction.Operands[^1], state),
                    IsNonemptyList or IsTuple or TestArity or IsTaggedTuple or IsNil or ExactEqualTest => true,
                    _ => false
                };
                if (!stateUnchanged)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
