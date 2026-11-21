using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareConsGuardNilReturns(ReadOnlyMemory<byte> code, int labelCount) =>
        !ModuleOptimizationDisabled && code.Span.Contains(IsLess)
        && DecodeLateAtomicProjectionSlots(code, labelCount) is { Body: var body }
            ? RewriteConsGuardNilReturns(code, body) : null;

    private static byte[]? RewriteConsGuardNilReturns(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        var labels = new Dictionary<int, int>();
        for (int index = 0; index < body.Count; index++)
        {
            if (body[index] is { Definition.Opcode: Label, Operands: [var label] })
            {
                labels.Add((int)label.Value, index);
            }
        }

        using var output = new MemoryStream();
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (ReadConsGuardNilReturn(
                body,
                labels,
                entry,
                end
            ) is not { } plan)
            {
                continue;
            }

            int test = body[plan.Guard].Offset;
            int continuation = body[plan.Continuation].Offset;
            int next = body[plan.Continuation + 1].Offset;
            int result = body[plan.NilTest + 1].Offset;
            // The relocated label is exclusively owned by the comparison.
            // Its operands keep their order and the original nil branch keeps its test.
            output.Write(code.Span[copied..test]);
            output.WriteByte(IsGreaterOrEqual);
            output.Write(code.Span[(test + 1)..body[plan.Guard + 1].Offset]);
            output.Write(code.Span[next..result]);
            output.Write(code.Span[continuation..next]);
            copied = result;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static (int Guard, int Continuation, int NilTest)? ReadConsGuardNilReturn(
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyDictionary<int, int> labels,
        int entry,
        int end
    )
    {
        if (entry == 0 || entry + 10 >= end || body[entry].Definition.Opcode != Label
            || body[entry - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var incoming] }
            || body[entry + 1] is not
            {
                Definition.Opcode: IsNonemptyList,
                Operands: [
                { Kind: BeamOperandParameters.Label, Value.Sign: > 0 } cold,
                { Kind: BeamOperandParameters.X, TypeIndex: null } list
                ]
            } || list.Value >= incoming.Value
            || body[entry + 2] is not
            {
                Definition.Opcode: GetList,
                Operands: [
                var source, { Kind: BeamOperandParameters.X, TypeIndex: null } head,
                { Kind: BeamOperandParameters.X, TypeIndex: null } tail
                ]
            } || source != list || head == tail || head.Value < incoming.Value || tail.Value < incoming.Value
            || body[entry + 3] is not { Definition.Opcode: IsTuple, Operands: [var nonTuple, var tuple] }
            || tuple != head
            || body[entry + 4] is not
            {
                Definition.Opcode: TestArity,
                Operands: [var wrongArity, var checkedTuple, { Value.Sign: > 0 } arity]
            } || wrongArity != nonTuple || checkedTuple != head)
        {
            return null;
        }

        var fields = new HashSet<BeamOperand>();
        int guard = entry + 5;
        while (guard < end && body[guard] is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var carrier, var field, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
            } && carrier == head && field.Value < arity.Value && destination != head && destination.Value >= incoming.Value)
        {
            fields.Add(destination);
            guard++;
        }

        int continuation = guard + 3;
        int failure = EntryFrameFailureLabel(body, entry);
        if (failure <= 0 || continuation >= end || body[guard] is not
            {
                Definition.Opcode: IsLess,
                Operands: [{ Kind: BeamOperandParameters.Label } next, var left, var right]
            } || next == cold || !(IsOtherArgument(left) && fields.Contains(right) || IsOtherArgument(right) && fields.Contains(left))
            || body[guard + 1] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Atom } result, var returned]
            } || !IsBareX0(returned) || body[guard + 2].Definition.Opcode != Return
            || body[continuation] is not { Definition.Opcode: Label, Operands: [var label] }
            || label.Value != next.Value
            || !labels.TryGetValue((int)cold.Value, out int nil) || nil <= continuation || nil + 3 >= end
            || body[nil + 1] is not { Definition.Opcode: IsNil, Operands: [var rejected, var empty] }
            || rejected.Value != failure || empty != list
            || body[nil + 2] is not { Definition.Opcode: Move, Operands: [var other, var coldDestination] }
            || other != result || coldDestination != returned || body[nil + 3].Definition.Opcode != Return
            || !HasUnprotectedCheckedFailureRegion(body, entry, nil)
            || body.SelectMany(instruction => instruction.Operands)
                .Count(operand => operand.Kind == BeamOperandParameters.Label && operand.Value == next.Value) != 1)
        {
            return null;
        }

        if (body.Skip(entry).Take(end - entry).Any(instruction =>
                LinearFrameSizeOperand(instruction.Definition.Opcode) >= 0
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
            && (ReadDisjointFrameOwnership(body, entry, end) is not { } ownership
                || Enumerable.Range(guard, 4).Concat(Enumerable.Range(nil, 4))
                    .Any(index => ownership.Owners[index - entry] != UnframedOwner)))
        {
            return null;
        }

        return (guard, continuation, nil + 1);

        bool IsOtherArgument(BeamOperand operand) =>
            operand.Kind == BeamOperandParameters.X && operand.TypeIndex is null
            && operand.Value < incoming.Value && operand != list;
    }
}
