using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseTaggedTailProjectionCarrier(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var result = new BeamOperand(BeamOperandParameters.X, 0);
        var temporary = new BeamOperand(BeamOperandParameters.X, 1);
        var edits = new List<CodeEdit>();
        for (int start = 1; start + 4 < body.Count; start++)
        {
            if (body[start - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[start] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label, Value.Sign: > 0 }, var carrier, var width, _]
                }
                || carrier != result || body[start + 1] is not
                {
                    Definition.Opcode: TestHeap,
                    Operands: [{ Kind: BeamOperandParameters.Unsigned, Value.Sign: > 0 }, { Value.IsOne: true }]
                }
                || body[start + 2] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var source, { Value.Sign: > 0 } field, var projected]
                }
                || source != result || projected != temporary || field.Value >= width.Value
                || !IsTupleConstruction(body[start + 3], temporary))
            {
                continue;
            }

            int terminal = start + 4;
            while (terminal < body.Count && IsTupleConstruction(body[terminal], result))
            {
                terminal++;
            }

            if (terminal < body.Count && body[terminal].Definition.Opcode == Line)
            {
                terminal++;
            }

            if (terminal >= body.Count || body[terminal] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame or TailExternalCallWithFrame,
                    Operands: [{ Value.IsOne: true }, _, _]
                })
            {
                continue;
            }

            // The tuple lives through the check and the GC root boundary.
            // On success its final read can use the result register because
            // the remaining instructions only build the one tail argument.
            int offset = body[start + 2].Offset + 1;
            var projection = new BeamCursor(code[offset..body[start + 3].Offset]);
            _ = BeamCompactReader.Read(projection);
            _ = BeamCompactReader.Read(projection);
            ReplaceTemporary(projection, offset);

            offset = body[start + 3].Offset + 1;
            var tuple = new BeamCursor(code[offset..body[start + 4].Offset]);
            _ = BeamCompactReader.Read(tuple);
            _ = tuple.ReadByte();
            _ = BeamCompactReader.Read(tuple);
            foreach (var operand in body[start + 3].Operands.Skip(2))
            {
                if (operand == temporary)
                {
                    ReplaceTemporary(tuple, offset);
                    break;
                }

                _ = BeamCompactReader.Read(tuple);
            }
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);

        bool IsTupleConstruction(BeamInstruction instruction, BeamOperand value)
        {
            return instruction is { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] }
                && destination == result && instruction.Operands.Skip(2).Count(operand => operand == value) == 1
                && instruction.Operands.Skip(2).All(operand => operand == value || operand.TypeIndex is null
                    && operand.Kind is (BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil));
        }

        void ReplaceTemporary(BeamCursor cursor, int offset)
        {
            int from = offset + cursor.Position;
            _ = BeamCompactReader.Read(cursor);
            edits.Add(new CodeEdit(
                from,
                offset + cursor.Position,
                XRegisterTag,
                0
            ));
        }
    }
}
