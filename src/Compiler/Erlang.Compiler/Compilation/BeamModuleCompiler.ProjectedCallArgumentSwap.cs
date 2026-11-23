using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FoldProjectedCallArgumentSwap(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetTupleElement)
            || !code.Span.Contains(InitYRegisters) && !code.Span.Contains(DynamicApply)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        byte[]? promoted = RewriteGuardedCallPayloadCarrier(code, decoded.Body);
        if (promoted is not null)
        {
            if (DecodeLateAtomicProjectionSlots(promoted, labelCount) is not { } updated)
            {
                return null;
            }

            code = promoted;
            decoded = updated;
        }

        if (RewriteProjectedCallArgumentSwap(code, decoded.Body) is not { } rewritten)
        {
            return promoted;
        }

        highestOpcode = Math.Max(highestOpcode, Swap);

        return rewritten;
    }

    private static byte[]? RewriteProjectedCallArgumentSwap(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 6 < body.Count; index++)
        {
            if (ReadGuardedProjectedCallArguments(body, index, ref flow) is { } guarded)
            {
                output.Write(code.Span[copied..body[index].Offset]);
                EmitReassignedProjection(index, x0);
                int firstArgument = guarded.FirstArgument;
                output.Write(code.Span[body[index + 2].Offset..body[firstArgument].Offset]);
                EmitReassignedProjection(firstArgument, x1);
                output.Write(code.Span[body[firstArgument + 1].Offset..body[firstArgument + 2].Offset]);
                int[] literals = Enumerable.Range(firstArgument + 3, guarded.AfterArguments - firstArgument - 3)
                    .OrderBy(instruction => body[instruction].Operands[1].Value).ToArray();
                foreach (int position in IndependentCallMoveOrder(literals.Length))
                {
                    int literal = literals[position];
                    output.Write(code.Span[body[literal].Offset..body[literal + 1].Offset]);
                }

                WriteSwap();

                copied = body[guarded.AfterArguments].Offset;
                index = guarded.AfterArguments - 1;
                continue;
            }

            if (ReadCheckedTupleApply(index) is { } apply)
            {
                output.Write(code.Span[copied..body[index].Offset]);
                output.WriteByte(IsTuple);
                BeamCompactWriter.Write(output, LabelTag, apply.Failure.Value);
                WriteRegister(output, apply.Carrier);
                output.WriteByte(TestArity);
                BeamCompactWriter.Write(output, LabelTag, apply.Failure.Value);
                WriteRegister(output, apply.Carrier);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, apply.Arity.Value);
                WriteProjection(apply.Carrier, 0, apply.Saved);
                WriteProjection(apply.Carrier, 1, x0);
                WriteProjection(apply.Carrier, 2, x1);
                output.Write(code.Span[body[index + 7].Offset..body[index + 8].Offset]);
                WriteSwap();

                copied = body[index + 8].Offset;
                index += 7;
                continue;
            }

            if (body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null, Value: var sourceRegister } tuple,
                        var field,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } fieldSlot
                    ]
                } || sourceRegister < 2
                || body[index + 1] is not
                {
                    Definition.Opcode: Move, Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } retained]
                } || source != tuple || retained == fieldSlot
                || body[index + 2] is not
                {
                    Definition.Opcode: Move, Operands: [var original, { Kind: BeamOperandParameters.Y, TypeIndex: null } argumentSlot]
                } || original != x1 || argumentSlot == retained || argumentSlot == fieldSlot
                || !IsMove(index + 3, fieldSlot, x1) || !IsMove(index + 4, argumentSlot, x0)
                || body[index + 5] is not { Definition.Opcode: InitYRegisters, Operands: var cleared }
                || !cleared.Skip(1).Contains(fieldSlot) || !cleared.Skip(1).Contains(argumentSlot))
            {
                continue;
            }

            int call = index + 6;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call == body.Count || body[call] is not
                { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arity }, _] } || arity != 2)
            {
                continue;
            }

            // There is no branch or collection between the projection and the
            // existing initialization. Both removed Y values die at that
            // initialization, including when their slots receive later writes.
            output.Write(code.Span[copied..body[index].Offset]);
            output.Write(code.Span[body[index + 1].Offset..body[index + 2].Offset]);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, tuple);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, x0);
            WriteSwap();
            copied = body[index + 5].Offset;
            index += 4;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        (BeamOperand Carrier, BeamOperand Failure, BeamOperand Arity, BeamOperand Saved)? ReadCheckedTupleApply(int index)
        {
            if (index + 8 >= body.Count || body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null } carrier, var temporary]
                }
                || carrier.Value < 2 || temporary != x1 || body[index + 1] is not
                {
                    Definition.Opcode: IsTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } failure, var tuple]
                }
                || tuple != x1 || body[index + 2] is not
                {
                    Definition.Opcode: TestArity,
                    Operands: [var arityFailure, var checkedTuple, { Kind: BeamOperandParameters.Unsigned } arity]
                }
                || arityFailure != failure || checkedTuple != x1 || arity.Value != 3
                || body[index + 3] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var savedTuple, { Value.IsZero: true }, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                }
                || savedTuple != x1 || !IsProjection(index + 4, 1, x2) || !IsProjection(index + 5, 2, x0)
                || !IsMove(index + 6, x2, x1) || body[index + 7] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom }, var memberRegister]
                }
                || memberRegister != x2)
            {
                return null;
            }

            int call = index + 8;
            while (call < body.Count && body[call].Definition.Opcode == Line)
            {
                call++;
            }

            if (call >= body.Count || body[call] is not { Definition.Opcode: DynamicApply, Operands: [{ Value.IsOne: true }] }
                || !(flow ??= new BeamProjectionFlow(body)).TryGetForwardLabel(index + 2, (int)failure.Value, out int rejected)
                || rejected <= call)
            {
                return null;
            }

            // Both checks run with the original X0. Their cold edge can only
            // raise using that term, so dropping its unused X1 copy is safe.
            do
            {
                rejected++;
            } while (rejected < body.Count && body[rejected].Definition.Opcode == Line);

            return rejected < body.Count && body[rejected] is { Definition.Opcode: CaseEnd, Operands: [var reason] }
                && reason == x0 ? (carrier, failure, arity, saved) : null;
        }

        bool IsProjection(int index, int field, BeamOperand destination) => body[index] is
        { Definition.Opcode: GetTupleElement, Operands: [var tuple, var offset, var result] }
            && tuple == x1 && offset.Value == field && result == destination;

        void WriteProjection(
            BeamOperand tuple,
            int field,
            BeamOperand result
        )
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, tuple);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field);
            WriteRegister(output, result);
        }

        void EmitReassignedProjection(int index, BeamOperand destination)
        {
            var operands = body[index].Operands;
            output.WriteByte(GetTupleElement);
            WriteRegister(output, operands[0]);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, operands[1].Value);
            WriteRegister(output, destination);
        }

        void WriteSwap()
        {
            output.WriteByte(Swap);
            WriteRegister(output, x0);
            WriteRegister(output, x1);
        }

        bool IsMove(int index, BeamOperand from, BeamOperand to) => body[index] is
        { Definition.Opcode: Move, Operands: [var source, var destination] } && source == from && destination == to;
    }
}
