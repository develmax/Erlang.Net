using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? KeepWrappedArithmeticPayloadInX0(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GcBifTwo)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var names = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var edits = new List<CodeEdit>();
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        bool unprotected = false;
        for (int save = 0; save + 6 < body.Count; save++)
        {
            if (body[save].Definition.Opcode == FunctionInfo)
            {
                unprotected = HasUnprotectedCallKillSchedule(body, save + 1);
                continue;
            }

            if (!unprotected || body[save] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var original, { Kind: BeamOperandParameters.X, Value.Sign: > 0, TypeIndex: null } saved]
                } || original != x0 || body[save + 1].Definition.Opcode != Line
                || body[save + 2] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [
                    { Kind: BeamOperandParameters.Failure, Value.IsZero: true },
                        var live,
                        var target,
                        var first,
                        var second,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } result
                    ]
                } arithmetic || live.Value != result.Value || result.Value != saved.Value + 1
                || target.Value < 0 || target.Value >= imported.Length || !IsLeafArithmeticGcBif(arithmetic, names, imported)
                || !CanReadInput(first, saved) || !CanReadInput(second, saved)
                || body[save + 3] is not { Definition.Opcode: TestHeap, Operands: [_, var rooted] }
                || rooted.Value != result.Value + 1
                || body[save + 4] is not { Definition.Opcode: PutList, Operands: [var head, var tail, var list] }
                || list != result || (head != result && tail != result)
                || !CanReadConsInput(head, saved, result) || !CanReadConsInput(tail, saved, result)
                || body[save + 5] is not { Definition.Opcode: PutTuple, Operands: [var destination, _, ..] } tuple
                || destination != x0 || tuple.Operands.Skip(2).Count(field => SameProjectionRegister(field, saved)) != 1
                || !HasClosedWrappedArithmeticTail(
                    body,
                    save + 5,
                    saved,
                    result
                ))
            {
                continue;
            }

            if (ReadRootOperand(save + 2, LabelTag) is not { } arithmeticRoot
                || ReadRootOperand(save + 3, BeamWire.UnsignedTag) is not { } heapRoot)
            {
                continue;
            }

            int slot = (int)saved.Value;
            edits.Add(new CodeEdit(
                body[save].Offset,
                body[save + 1].Offset,
                0,
                0,
                IsRemoval: true
            ));
            edits.Add(new(
                arithmeticRoot.Start,
                arithmeticRoot.End,
                BeamWire.UnsignedTag,
                slot
            ));
            edits.Add(new(
                heapRoot.Start,
                heapRoot.End,
                BeamWire.UnsignedTag,
                slot + 1
            ));

            // Keep the original payload in X0. Use the removed slot for
            // the arithmetic result and every subsequent cons carrier use.
            for (int index = save + 2; body[index].Definition.Opcode != Return; index++)
            {
                if (index == save + 3)
                {
                    continue;
                }

                int start = body[index].Offset + 1;
                var span = new BeamCursor(code[start..body[index + 1].Offset]);
                while (span.Remaining != 0)
                {
                    int from = start + span.Position;
                    BeamCompactValue operand = BeamCompactReader.Read(span);
                    if (operand.WireTag != XRegisterTag)
                    {
                        continue;
                    }

                    if (operand.Value == result.Value)
                    {
                        edits.Add(new(
                            from,
                            start + span.Position,
                            XRegisterTag,
                            slot
                        ));
                    }
                    else if (index == save + 5 && operand.Value == saved.Value)
                    {
                        edits.Add(new(
                            from,
                            start + span.Position,
                            XRegisterTag,
                            0
                        ));
                    }
                }
            }
        }

        edits.Sort((left, right) => left.Start.CompareTo(right.Start));

        return ApplyDuplicatedFrameReadEdits(code, edits);

        (int Start, int End)? ReadRootOperand(int index, int firstTag)
        {
            int start = body[index].Offset + 1;
            var cursor = new BeamCursor(code[start..body[index + 1].Offset]);
            if (BeamCompactReader.Read(cursor).WireTag != firstTag)
            {
                return null;
            }

            int from = start + cursor.Position;

            return BeamCompactReader.Read(cursor).WireTag == BeamWire.UnsignedTag
                ? (from, start + cursor.Position) : null;
        }

        static bool CanReadConsInput(BeamOperand value, BeamOperand saved, BeamOperand result) => value == result
            || (CanReadInput(value, saved) && !(value.Kind == BeamOperandParameters.X && value.Value.IsZero));

        static bool CanReadInput(BeamOperand operand, BeamOperand saved) =>
            operand.Kind is BeamOperandParameters.Atom or BeamOperandParameters.Nil
                or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue
            || operand.Kind == BeamOperandParameters.X && operand.Value < saved.Value;
    }

    private static bool HasClosedWrappedArithmeticTail(
        IReadOnlyList<BeamInstruction> body,
        int tuple,
        BeamOperand saved,
        BeamOperand result
    )
    {
        for (int index = tuple; index < body.Count; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode == Return)
            {
                return true;
            }

            if (instruction.Definition.Opcode is not (Move or PutList or PutTuple or GetTupleElement or UpdateRecord)
                || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y
                    || operand.Kind == BeamOperandParameters.X
                        && (operand.Value > result.Value || (index != tuple && operand.Value == saved.Value))))
            {
                return false;
            }
        }

        return false;
    }
}
