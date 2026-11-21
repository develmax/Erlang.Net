using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FrameOnlyContinuingRecordBranch(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 8 < body.Count; info++)
        {
            if (body[info] is not
                {
                    Definition.Opcode: FunctionInfo,
                    Operands: [_, _, { Value.Sign: > 0 } arity]
                }
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [
                    { Kind: BeamOperandParameters.Label } invalid,
                    { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null } input,
                    { Kind: BeamOperandParameters.Unsigned } size,
                    { Kind: BeamOperandParameters.Atom }
                    ]
                }
                || body[info + 3] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value.Sign: > 0 } width, var live]
                }
                || live.Value != arity.Value || live.Value >= FirstReservedXRegister)
            {
                continue;
            }

            int allocation = info + 3;
            int save = body[allocation + 1].Definition.Opcode == InitYRegisters ? allocation + 2 : allocation + 1;
            int projection = save + 1;
            int guard = projection + 1;
            if (body[save] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var copy, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                }
                || copy != input || slot.Value >= width.Value
                || body[projection] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                    var source,
                    { Kind: BeamOperandParameters.Unsigned } field,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } tested
                    ]
                }
                || source != slot && source != input
                || field.Value.Sign < 0 || field.Value >= size.Value
                || body[guard] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var value, var literal]
                }
                || value != tested || !IsNonRegisterValue(literal)
                || rejected == invalid || rejected.Value.Sign <= 0)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.TryGetSingleForwardTarget(guard, (int)rejected.Value, out int failure)
                || !flow.IsBranchTemporaryDead(guard, tested)
                || failure + 6 >= body.Count
                || !IsSimpleRecordReturn(
                    failure,
                    size,
                    width,
                    slot,
                    input
                )
                || body.Skip(guard + 1).Take(failure - guard - 1)
                    .Any(instruction => instruction.Definition.Opcode is FunctionInfo or DebugLine or ExecutableLine))
            {
                continue;
            }

            // The prefix has no call and its field is used only by the predicate.
            // Keep the original record in X and allocate only on the call path.
            var scratch = new BeamOperand(BeamOperandParameters.X, live.Value);
            int start = body[allocation].Offset;
            output.Write(code.Span[copied..start]);
            WriteCheckedField(input, field, scratch);
            output.WriteByte(ExactEqualTest);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            WriteRegister(output, scratch);
            int guardOperands = body[guard].Offset + 1;
            var cursor = new BeamCursor(code[guardOperands..]);
            _ = BeamCompactReader.Read(cursor);
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[(guardOperands + cursor.Position)..body[guard + 1].Offset]);

            output.Write(code.Span[start..body[projection].Offset]);
            int continuation = guard + 1;
            if (body[continuation] is { Definition.Opcode: Move, Operands: [var saved, var restored] }
                && saved == slot && restored == input)
            {
                continuation++;
            }

            int branch = failure + 1;
            output.Write(code.Span[body[continuation].Offset..body[branch].Offset]);
            // The tag/arity proof permits reserving the small result heap before
            // reading the field. The original record remains live in X0 there.
            output.Write(code.Span[body[branch + 1].Offset..body[branch + 2].Offset]);
            WriteCheckedField(input, body[branch].Operands[1], input);
            output.Write(code.Span[body[branch + 2].Offset..body[branch + 3].Offset]);
            copied = body[branch + 4].Offset;
            info = failure;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteCheckedField(BeamOperand source, BeamOperand field, BeamOperand destination)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, source);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, destination);
        }

        bool IsSimpleRecordReturn(
            int label,
            BeamOperand size,
            BeamOperand width,
            BeamOperand slot,
            BeamOperand input
        )
        {
            int first = label + 1;

            return body[first] is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [var source, { Kind: BeamOperandParameters.Unsigned } field, var destination]
            }
                && source == slot && destination == input && field.Value.Sign >= 0 && field.Value < size.Value
                && body[first + 1] is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                && body[first + 2] is { Definition.Opcode: PutTuple, Operands: [var result, _, ..] } construction
                && result == input && construction.Operands.All(operand =>
                    operand.Kind is not (BeamOperandParameters.Y or BeamOperandParameters.X)
                    || operand == input)
                && body[first + 3] is { Definition.Opcode: Deallocate, Operands: [var discarded] }
                && discarded == width && body[first + 4].Definition.Opcode == Return;
        }
    }
}
