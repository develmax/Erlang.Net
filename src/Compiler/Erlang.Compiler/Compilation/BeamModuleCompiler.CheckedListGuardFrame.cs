using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int CheckedListEntryFrameWidth = 5;
    private const int CheckedListCallFrameWidth = 3;

    private byte[]? LocalizeCheckedListGuardFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(GetList))
        {
            return null;
        }

        var labelUses = new Dictionary<int, int>();
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                (_, _, label) => labelUses[label] = labelUses.GetValueOrDefault(label) + 1
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        string[] atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 36 < body.Count; info++)
        {
            int allocation = info + 4;
            int guard = allocation + 4;
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
                || arity.Value != 2 || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } legacy, var state, _, _]
                }
                || !IsBareX0(state)
                || body[info + 3] is not { Definition.Opcode: GetTupleElement, Operands: [var input, _, var field] }
                || !IsBareX0(input) || !Reg(field, false, 2)
                || body[allocation] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                || width.Value != CheckedListEntryFrameWidth || live.Value != 3
                || body[allocation + 1].Definition.Opcode != InitYRegisters
                || !MoveTo(
                    allocation + 2,
                    false,
                    2,
                    true,
                    3
                )
                || !MoveTo(
                    allocation + 3,
                    false,
                    0,
                    true,
                    4
                )
                || body[guard] is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var list]
                }
                || !Reg(list, false, 1)
                || body[guard + 1] is not { Definition.Opcode: GetList, Operands: [var listSource, var head, var tail] }
                || !Reg(listSource, false, 1) || !Reg(head, true, 2) || !Reg(tail, true, 1)
                || !Test(
                    guard + 2,
                    IsTuple,
                    rejected,
                    2
                )
                || body[guard + 3] is not
                {
                    Definition.Opcode: TestArity,
                    Operands: [var arityFailure, var tuple, var rowArity]
                }
                || arityFailure != rejected || !Reg(tuple, true, 2)
                || body[guard + 4] is not { Definition.Opcode: GetTupleElement, Operands: [var row, var cell, var selected] }
                || !Reg(row, true, 2) || cell.Value < 0 || cell.Value >= rowArity.Value || !Reg(selected, true, 0)
                || !Test(
                    guard + 5,
                    IsNil,
                    rejected,
                    1
                ) || !Test(
                    guard + 6,
                    IsTuple,
                    rejected,
                    0
                )
                || body[guard + 7] is not
                {
                    Definition.Opcode: BifOne,
                    Operands: [var sizeFailure, var sizeImport, var measured, var size]
                }
                || sizeFailure != rejected || !Import(sizeImport, AutoImportNames.TupleSize, 1)
                || !Reg(measured, true, 0) || !IsBareX0(size)
                || !MoveTo(
                    guard + 8,
                    false,
                    0,
                    true,
                    2
                )
                || body[guard + 9] is not
                {
                    Definition.Opcode: IsGreaterOrEqual,
                    Operands: [var lengthFailure, var tupleSize, { Kind: BeamOperandParameters.Integer } minimum]
                }
                || lengthFailure != rejected || !Reg(tupleSize, true, 2) || tupleSize.TypeIndex is not int sizeType)
            {
                continue;
            }

            int end = guard + 10;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            int element = SkipLines(guard + 10, end);
            int firstCall = SkipLines(element + 2, end);
            int select = firstCall + 1;
            int firstArm = select + 1;
            if (firstArm + 5 >= end
                || body[element] is not
                {
                    Definition.Opcode: BifTwo,
                    Operands: [{ Kind: BeamOperandParameters.Failure }, var elementImport,
                    { Kind: BeamOperandParameters.Integer } fieldIndex, var tupleInput, var key]
                }
                || !Import(elementImport, AutoImportNames.Element, 2)
                || fieldIndex.Value <= 0 || fieldIndex.Value > minimum.Value
                || !Reg(tupleInput, true, 0) || !IsBareX0(key) || !MoveTo(
                    element + 1,
                    false,
                    0,
                    true,
                    0
                )
                || !Call(firstCall, LocalCall, 1)
                || body[select] is not
                {
                    Definition.Opcode: SelectValue,
                    Operands: [var result, { Kind: BeamOperandParameters.Label } badResult, _, _, var label1, _, var label2]
                }
                || !IsBareX0(result)
                || !Declares(firstArm, label1) && !Declares(firstArm, label2)
                || !MatchTail(firstArm, end, out int sequence))
            {
                continue;
            }

            int secondArm = sequence + 1;
            int secondCall = SkipLines(secondArm + 3, end);
            int guardFailure = secondCall + 5;
            if (guardFailure + 4 >= end
                || !Declares(secondArm, label1) && !Declares(secondArm, label2)
                || !MoveTo(
                    secondArm + 1,
                    true,
                    0,
                    false,
                    1
                ) || !MoveTo(
                    secondArm + 2,
                    true,
                    4,
                    false,
                    0
                )
                || !Call(secondCall, LocalCall, 2)
                || body[secondCall + 1] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                || body[secondCall + 2] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [var value, { Kind: BeamOperandParameters.Nil }, var returned]
                }
                || !IsBareX0(value) || !IsBareX0(returned)
                || body[secondCall + 3] is not { Definition.Opcode: Deallocate, Operands: [var release] }
                || release.Value != CheckedListEntryFrameWidth || body[secondCall + 4].Definition.Opcode != Return
                || !Declares(guardFailure, rejected) || !MatchTail(guardFailure, end, out int otherSequence)
                || body[sequence].Operands[1] != body[otherSequence].Operands[1]
                || IsTrappingTailBif(imported[(int)body[otherSequence].Operands[1].Value], atomNames)
                || !body[firstArm + 2].Operands.SequenceEqual(body[guardFailure + 2].Operands)
                || !Declares(otherSequence + 1, legacy)
                || label1.Kind != BeamOperandParameters.Label || label2.Kind != BeamOperandParameters.Label
                || label1 == label2 || badResult == rejected || badResult == legacy
                || labelUses.GetValueOrDefault((int)legacy.Value) != 1
                || labelUses.GetValueOrDefault((int)rejected.Value) != 7
                || labelUses.GetValueOrDefault((int)label1.Value) != 1
                || labelUses.GetValueOrDefault((int)label2.Value) != 1
                || labelUses.GetValueOrDefault((int)badResult.Value) != 1
                || ReadDisjointFrameOwnership(body, info + 1, end) is not { } ownership
                || Enumerable.Range(guard, otherSequence - guard + 1).Any(index =>
                    body[index].Definition.Opcode != Return && ownership.Owners[index - info - 1] != allocation))
            {
                continue;
            }

            int badLabel = otherSequence + 2;
            while (badLabel < end && !Declares(badLabel, badResult))
            {
                badLabel++;
            }

            int badExit = SkipLines(badLabel + 1, end);
            if (badLabel >= end || badExit >= end || ownership.Owners[badLabel - info - 1] != allocation
                || body[badExit] is not { Definition.Opcode: CaseEnd, Operands: [var offending] }
                || !IsBareX0(offending))
            {
                continue;
            }

            // Guard failures leave before any stack allocation. The two observed
            // projections are safe after their dominating tag and arity checks.
            int tupleType = GetOperandTypeIndex((TupleTypeBits, null, null, null));
            output.Write(code.Span[copied..body[allocation].Offset]);
            Copy(guard, guard + 1);
            output.WriteByte(GetList);
            X(1);
            X(3);
            X(1);
            Branch(IsTuple, rejected, 3);
            Branch(TestArity, rejected, 3);
            U((int)rowArity.Value);
            Branch(IsNil, rejected, 1);
            output.WriteByte(GetTupleElement);
            X(3);
            U((int)cell.Value);
            X(1);
            Branch(IsTuple, rejected, 1);
            output.WriteByte(BifOne);
            Failure();
            U((int)sizeImport.Value);
            X(1, tupleType);
            X(3);
            output.WriteByte(IsGreaterOrEqual);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            X(3, sizeType);
            BeamCompactWriter.Write(output, BeamWire.IntegerTag, minimum.Value);
            Copy(guard + 10, element);
            output.WriteByte(BifTwo);
            Failure();
            U((int)elementImport.Value);
            BeamCompactWriter.Write(output, BeamWire.IntegerTag, fieldIndex.Value);
            X(1, tupleType);
            X(1);
            output.WriteByte(Allocate);
            U(CheckedListCallFrameWidth);
            U(CheckedListCallFrameWidth);
            WriteMove(
                false,
                1,
                true,
                0
            );
            WriteMove(
                false,
                2,
                true,
                1
            );
            WriteMove(
                false,
                0,
                true,
                2
            );
            WriteMove(
                false,
                1,
                false,
                0
            );
            Copy(element + 2, firstArm + 1);
            WriteMove(
                true,
                1,
                false,
                1
            );
            Copy(firstArm + 2, sequence);
            Tail(sequence, framed: true);
            Copy(secondArm, secondArm + 2);
            WriteMove(
                true,
                2,
                false,
                0
            );
            output.WriteByte(TrimFrame);
            U(CheckedListCallFrameWidth);
            U(0);
            Copy(secondArm + 3, secondCall + 3);
            output.WriteByte(Deallocate);
            U(0);
            output.WriteByte(Return);
            Copy(guardFailure, guardFailure + 1);
            WriteMove(
                false,
                2,
                false,
                1
            );
            Copy(guardFailure + 2, otherSequence);
            Tail(otherSequence, framed: false);
            copied = body[otherSequence + 1].Offset;
            info = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool Test(
            int index,
            byte opcode,
            BeamOperand target,
            int slot
        ) =>
            body[index].Definition.Opcode == opcode && body[index].Operands is [var failure, var operand]
            && failure == target && Reg(operand, true, slot);

        bool Import(BeamOperand operand, string name, int arity)
        {
            var target = imported[(int)operand.Value];

            return target.Arity == arity && atomNames[target.Module - 1] == ErlangModuleNames.Module
                && atomNames[target.Function - 1] == name;
        }

        bool MatchTail(int label, int limit, out int tail)
        {
            tail = SkipLines(label + 3, limit);

            return tail < limit && MoveTo(
                label + 1,
                true,
                3,
                false,
                1
            )
                && body[label + 2] is { Definition.Opcode: Move, Operands: [{ Kind: BeamOperandParameters.Integer }, var first] }
                && IsBareX0(first) && body[tail] is
                { Definition.Opcode: TailExternalCallWithFrame, Operands: [var arity, _, var frame] }
                && arity.Value == 2 && frame.Value == CheckedListEntryFrameWidth;
        }

        bool MoveTo(
            int index,
            bool fromStack,
            int from,
            bool toStack,
            int to
        ) => body[index] is
        { Definition.Opcode: Move, Operands: [var source, var destination] }
            && source.TypeIndex is null && destination.TypeIndex is null
            && Reg(source, fromStack, from) && Reg(destination, toStack, to);

        bool Call(int index, byte opcode, int arity) =>
            body[index].Definition.Opcode == opcode && body[index].Operands[0].Value == arity;

        bool Declares(int index, BeamOperand target) => body[index] is
        { Definition.Opcode: Label, Operands: [var label] }
            && target.Kind == BeamOperandParameters.Label && label.Value == target.Value;

        int SkipLines(int index, int limit)
        {
            while (index < limit && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        void Tail(int index, bool framed)
        {
            output.WriteByte(framed ? TailExternalCallWithFrame : TailExternalCall);
            U((int)body[index].Operands[0].Value);
            U((int)body[index].Operands[1].Value);
            if (framed)
            {
                U(CheckedListCallFrameWidth);
            }
        }

        void Branch(byte opcode, BeamOperand target, int register)
        {
            output.WriteByte(opcode);
            BeamCompactWriter.Write(output, LabelTag, target.Value);
            X(register);
        }

        void WriteMove(
            bool fromStack,
            int source,
            bool toStack,
            int destination
        )
        {
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, fromStack ? YRegisterTag : XRegisterTag, source);
            BeamCompactWriter.Write(output, toStack ? YRegisterTag : XRegisterTag, destination);
        }

        void X(int index, int? type = null)
        {
            if (type is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            BeamCompactWriter.Write(output, XRegisterTag, index);
            if (type is int indexType)
            {
                U(indexType);
            }
        }

        void U(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);

        void Failure() => BeamCompactWriter.Write(output, LabelTag, 0);

        void Copy(int start, int end) => output.Write(code.Span[body[start].Offset..body[end].Offset]);

        static bool Reg(BeamOperand operand, bool stack, int register) =>
            operand.Kind == (stack ? BeamOperandParameters.Y : BeamOperandParameters.X) && operand.Value == register;
    }
}
