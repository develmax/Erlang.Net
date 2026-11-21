using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (byte[] Code, int LabelCount)? ShareMixedTupleCaseReturns(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var body = decoded.Body;
        var atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var labelUses = body.Where(instruction => instruction.Definition.Opcode != Label)
            .SelectMany(instruction => instruction.Operands)
            .Where(operand => operand.Kind == BeamOperandParameters.Label)
            .GroupBy(operand => operand.Value)
            .ToDictionary(group => group.Key, group => group.Count());
        using var output = new MemoryStream();
        int copied = 0;
        bool protectedFunction = false;
        int functionEnd = 0;
        for (int index = 1; index + 18 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                functionEnd = index + 1;
                protectedFunction = false;
                while (body[functionEnd].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    protectedFunction |= body[functionEnd].Definition.Opcode is TryEnter or CatchEnter or DebugLine or ExecutableLine;
                    functionEnd++;
                }
            }

            if (protectedFunction || index + 18 >= functionEnd
                || body[index - 1].Definition.Opcode is not (ExternalCall or LocalCall)
                || body[index] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [var tagDispatch, var compared, { Kind: BeamOperandParameters.Atom } scalar]
                } || compared != x0
                || body[index + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null }, var target]
                } || target != x0
                || body[index + 2] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame or TailExternalCallWithFrame,
                    Operands: [{ Value.IsOne: true }, _, _]
                }
                || !SoleEntry(index + 3, tagDispatch)
                || body[index + 4] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var alternate, var tuple, var wideArity, { Kind: BeamOperandParameters.Atom } sharedTag]
                } || tuple != x0
                || !RecordReturn(index + 5)
                || !SoleEntry(index + 10, alternate)
                || body[index + 11] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [var narrow, var otherTuple, var alternateArity, { Kind: BeamOperandParameters.Atom } otherTag]
                } || otherTuple != x0 || alternateArity != wideArity || otherTag == sharedTag)
            {
                continue;
            }

            int narrowEntry = index + 12;
            while (narrowEntry < functionEnd && body[narrowEntry].Definition.Opcode is not (Label or FunctionInfo))
            {
                narrowEntry++;
            }

            if (narrowEntry + 9 >= functionEnd || narrowEntry == index + 12
                || body[narrowEntry - 1].Definition.Opcode != Return
                || !SoleEntry(narrowEntry, narrow)
                || body[narrowEntry + 1] is not { Definition.Opcode: IsTuple, Operands: [var failure, var input] }
                || input != x0
                || body[narrowEntry + 2] is not { Definition.Opcode: TestArity, Operands: [var rejected, var arityInput, var narrowArity] }
                || failure != rejected || arityInput != x0 || narrowArity.Value >= wideArity.Value
                || !RecordReturn(narrowEntry + 3)
                || !code.Span[body[index + 5].Offset..body[index + 10].Offset]
                    .SequenceEqual(code.Span[body[narrowEntry + 3].Offset..body[narrowEntry + 8].Offset])
                || body[narrowEntry + 8] is not { Definition.Opcode: Label, Operands: [var closedFailure] }
                || closedFailure.Value != failure.Value)
            {
                continue;
            }

            int exit = narrowEntry + 9;
            if (body[exit].Definition.Opcode == Line)
            {
                exit++;
            }

            if (exit >= functionEnd || body[exit] is not { Definition.Opcode: CaseEnd, Operands: [var failed] }
                || failed != x0)
            {
                continue;
            }

            // The call defines X0 only. Both tuple cases enter the same record
            // return; only a non-tuple can still match the trailing atom arm.
            int nonTuple = labelCount++;
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(IsTuple);
            Branch(nonTuple);
            WriteRegister(output, x0);
            output.WriteByte(SelectTupleArity);
            output.WriteByte(ExtendedTypedRegisterOperand);
            WriteRegister(output, x0);
            Unsigned(GetOperandTypeIndex((TupleTypeBits, null, null, null)));
            Branch((int)failure.Value);
            PairTable();
            Unsigned((int)narrowArity.Value);
            Branch((int)narrow.Value);
            Unsigned((int)wideArity.Value);
            Branch((int)tagDispatch.Value);
            Copy(index + 3, index + 4);
            output.WriteByte(GetTupleElement);
            WriteRegister(output, x0);
            Unsigned(0);
            WriteRegister(output, x1);
            output.WriteByte(SelectValue);
            WriteRegister(output, x1);
            Branch((int)failure.Value);
            PairTable();
            int first = (int)sharedTag.Value;
            int second = (int)otherTag.Value;
            if (TermOrder.Compare(Term.A(atomNames[first - 1]), Term.A(atomNames[second - 1]), exact: true) < 0)
            {
                Tag(first, (int)narrow.Value);
                Tag(second, (int)alternate.Value);
            }
            else
            {
                Tag(second, (int)alternate.Value);
                Tag(first, (int)narrow.Value);
            }

            Copy(index + 10, index + 11);
            Copy(index + 12, narrowEntry + 1);
            Copy(narrowEntry + 3, narrowEntry + 8);
            output.WriteByte(Label);
            Unsigned(nonTuple);
            output.WriteByte(ExactEqualTest);
            Branch((int)failure.Value);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, AtomTag, scalar.Value);
            Copy(index + 1, index + 3);
            int failureEntry = narrowEntry + 8;
            int nextBlock = exit + 1;
            int nextExit = nextBlock + 1;
            if (nextExit < functionEnd && body[nextBlock].Definition.Opcode == Label)
            {
                while (nextExit < functionEnd && body[nextExit].Definition.Opcode != Label
                    && !EndsBeamBlock(body[nextExit].Definition.Opcode))
                {
                    nextExit++;
                }

                if (nextExit < functionEnd && body[nextExit].Definition.Opcode == Return)
                {
                    // beam_jump delays a short failure after the ordinary return.
                    // Both blocks have explicit entries and neither can fall through.
                    Copy(nextBlock, nextExit + 1);
                    Copy(failureEntry, nextBlock);
                    copied = body[nextExit + 1].Offset;
                    index = nextExit;
                    continue;
                }
            }

            copied = body[failureEntry].Offset;
            index = failureEntry - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, Math.Max(SelectTupleArity, SelectValue));
        output.Write(code.Span[copied..]);

        return (output.ToArray(), labelCount);

        bool SoleEntry(int index, BeamOperand reference) =>
            body[index] is { Definition.Opcode: Label, Operands: [var defined] }
            && defined.Value == reference.Value && labelUses.GetValueOrDefault(reference.Value) == 1;

        bool RecordReturn(int index) =>
            body[index] is { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
            && body[index + 1] is
            {
                Definition.Opcode: GetTupleElement,
                Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null }, _, var field]
            }
            && field == x0 && body[index + 2].Definition.Opcode == PutTuple
            && body[index + 3].Definition.Opcode == Deallocate && body[index + 4].Definition.Opcode == Return;

        void Copy(int first, int end) => output.Write(code.Span[body[first].Offset..body[end].Offset]);

        void Branch(int target) => BeamCompactWriter.Write(output, LabelTag, target);

        void Unsigned(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);

        void PairTable()
        {
            const int pairTableElements = 4;
            output.WriteByte(ExtendedListOperand);
            Unsigned(pairTableElements);
        }

        void Tag(int atom, int target)
        {
            BeamCompactWriter.Write(output, AtomTag, atom);
            Branch(target);
        }
    }
}
