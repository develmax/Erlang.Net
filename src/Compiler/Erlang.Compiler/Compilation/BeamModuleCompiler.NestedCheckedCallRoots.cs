using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RetainNestedCheckedCallResult(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int functionEnd = 0;
        bool protectedFunction = false;
        for (int index = 1; index + 7 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                functionEnd = index + 1;
                protectedFunction = false;
                while (body[functionEnd].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    protectedFunction |= body[functionEnd].Definition.Opcode is
                        TryEnter or CatchEnter or LoopReceive or Wait or WaitTimeout or DebugLine or ExecutableLine;
                    functionEnd++;
                }
            }

            if (protectedFunction || index + 7 >= functionEnd
                || body[index - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[index] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } failure, var outer, var outerArity, _]
                } || outer != x0
                || body[index + 1] is not
                { Definition.Opcode: GetTupleElement, Operands: [var parent, var outerField, var inner] }
                || parent != outer || outerField.Value < 0 || outerField.Value >= outerArity.Value
                || inner is not { Kind: BeamOperandParameters.X, Value.Sign: > 0, TypeIndex: null }
                || body[index + 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var innerFailure, var checkedInner, var innerArity, { Kind: BeamOperandParameters.Atom } innerTag]
                } || innerFailure != failure || checkedInner != inner
                || body[index + 3] is not
                { Definition.Opcode: GetTupleElement, Operands: [var child, var innerField, var stackSlot] }
                || child != inner || innerField.Value < 0 || innerField.Value >= innerArity.Value
                || stackSlot is not { Kind: BeamOperandParameters.Y, TypeIndex: null })
            {
                continue;
            }

            int firstGuard = index + 4;
            int lastGuard = firstGuard;
            while (lastGuard + 2 < functionEnd && IsExactLiteralGuard(body[lastGuard], stackSlot)
                && body[lastGuard].Operands[0] == failure)
            {
                lastGuard++;
            }

            int cold = lastGuard + 1;
            if (cold + 2 >= functionEnd || !IsExactLiteralGuard(body[lastGuard], stackSlot)
                || body[lastGuard].Operands[0] == failure || body[cold].Definition.Opcode != Label
                || body[cold].Operands[0].Value != failure.Value
                || body[cold + 1] is not { Definition.Opcode: Move, Operands: [var original, var saved] }
                || original != x0 || saved != stackSlot)
            {
                continue;
            }

            flow ??= new BeamProjectionFlow(body);
            if (!flow.ProvesNestedCheckedCallResult(
                index,
                lastGuard,
                cold,
                functionEnd,
                x0,
                inner,
                stackSlot
            ))
            {
                continue;
            }

            var guardSlots = decoded.StackOperands.Where(operand => operand.Start > body[firstGuard].Offset
                && operand.End <= body[cold].Offset && operand.Slot == stackSlot.Value).ToArray();
            if (guardSlots.Length != lastGuard - firstGuard + 1)
            {
                continue;
            }

            // The same Y slot holds the new payload result on success or the
            // original call result on rejection. Its successful value is dead
            // after the final guard. Preserve the raw result before reusing X0.
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(Move);
            WriteRegister(output, x0);
            WriteRegister(output, stackSlot);
            CopyInstruction(index);
            WriteProjection(outerField);
            output.WriteByte(IsTaggedTuple);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, innerArity.Value);
            BeamCompactWriter.Write(output, AtomTag, innerTag.Value);
            WriteProjection(innerField);
            for (int guard = firstGuard; guard <= lastGuard; guard++)
            {
                BeamInstruction operation = body[guard];
                var register = guardSlots[guard - firstGuard];
                output.Write(code.Span[operation.Offset..register.Start]);
                WriteRegister(output, x0);
                output.Write(code.Span[register.End..body[guard + 1].Offset]);
            }

            CopyInstruction(cold);
            copied = body[cold + 2].Offset;
            index = cold + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void CopyInstruction(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        void WriteProjection(BeamOperand field)
        {
            output.WriteByte(GetTupleElement);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
            WriteRegister(output, x0);
        }

        static bool IsExactLiteralGuard(BeamInstruction instruction, BeamOperand slot) =>
            instruction.Definition.Opcode is ExactEqualTest or ExactNotEqualTest
            && instruction.Operands is
            [
            { Kind: BeamOperandParameters.Label },
                var compared,
            {
                Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                    or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue, TypeIndex: null
            }
            ] && compared == slot;
    }

    private sealed partial class BeamProjectionFlow
    {
        internal bool ProvesNestedCheckedCallResult(
            int outer,
            int guard,
            int cold,
            int functionEnd,
            BeamOperand result,
            BeamOperand scratch,
            BeamOperand saved
        )
        {
            if (successors[guard] is not [int success, int fallthrough] || fallthrough != cold || success <= cold
                || success + 1 >= functionEnd || predecessors[cold].Count != guard - outer - 1
                || predecessors[cold].Any(source => source != outer && source != outer + 2 && source < outer + 4
                    || source > guard))
            {
                return false;
            }

            return IsEntryTemporaryDead(cold + 2, result) && IsEntryTemporaryDead(cold + 2, scratch)
                && IsEntryTemporaryDead(success + 1, result) && IsEntryTemporaryDead(success + 1, scratch)
                && IsDead(
                    success + 1,
                    saved,
                    afterCall: true
                );
        }
    }
}
