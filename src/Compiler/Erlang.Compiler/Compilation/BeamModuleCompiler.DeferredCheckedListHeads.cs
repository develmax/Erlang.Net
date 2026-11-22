using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? DeferCheckedListHeadConstruction(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(SelectTupleArity)
            || !code.Span.Contains(TrimFrame) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var category = new BeamOperand(BeamOperandParameters.Y, 0);
        var checkedTag = new BeamOperand(BeamOperandParameters.Y, 1);
        var carrier = new BeamOperand(BeamOperandParameters.Y, 2);
        var savedTail = new BeamOperand(BeamOperandParameters.Y, 3);
        BeamProjectionFlow? flow = null;
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
            if (entry == 0 || end - entry < 20 || !body[entry - 1].Operands[^1].Value.IsOne
                || body[entry] is not { Definition.Opcode: Label, Operands: [var recursive] }
                || body[entry + 1] is not { Definition.Opcode: IsNonemptyList, Operands: [_, var list] }
                || list != x0
                || body[entry + 2] is not { Definition.Opcode: GetList, Operands: [var from, var head, var tail] }
                || from != x0 || head != x1 || tail != x0
                || body[entry + 3] is not { Definition.Opcode: IsTuple, Operands: [var failed, var tested] }
                || tested != x1
                || body[entry + 4] is not
                {
                    Definition.Opcode: SelectTupleArity,
                    Operands: [var selected, var rejected, _, _, _, ..]
                } arities || !SameProjectionRegister(selected, x1) || rejected != failed
                || body.Skip(entry + 1).Take(end - entry - 1).Any(instruction => instruction.Definition.Opcode is
                    CatchEnter or TryEnter or DebugLine or ExecutableLine))
            {
                continue;
            }

            var ownership = flow ??= new BeamProjectionFlow(body);
            for (int option = 3; option + 1 < arities.Operands.Count; option += 2)
            {
                if (arities.Operands[option].Value != 2 || arities.Operands[option + 1].Kind != BeamOperandParameters.Label
                    || !ownership.TryGetSingleForwardTarget(entry + 4, (int)arities.Operands[option + 1].Value, out int label)
                    || label + 6 >= end
                    || body[label + 1] is not { Definition.Opcode: Allocate, Operands: [var width, var live] }
                    || width.Value != 4 || live.Value != 2
                    || body[label + 2] is not
                    {
                        Definition.Opcode: InitYRegisters, Operands: [_, var first, var second]
                    } || first != category || second != checkedTag
                    || !ReadsMove(label + 3, x1, carrier) || !ReadsMove(label + 4, x0, savedTail)
                    || !ReadsProjection(
                        label + 5,
                        carrier,
                        x0,
                        out var testedField
                    ))
                {
                    continue;
                }

                // On a failed exact tag test, its failure label has not passed
                // through that tag's success arm. The frame saves are unchanged.
                int cold = label + 6;
                int tests = 0;
                while (cold < end && body[cold] is
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [{ Kind: BeamOperandParameters.Label } retry, var operand,
                        { Kind: BeamOperandParameters.Atom }]
                    } && operand == x0 && ownership.TryGetSingleForwardTarget(cold, (int)retry.Value, out int next))
                {
                    tests++;
                    cold = next + 1;
                }

                if (tests == 0 || cold + 7 >= end
                    || !ReadsProjection(
                        cold,
                        carrier,
                        checkedTag,
                        out var firstField
                    ) || firstField != testedField
                    || !ReadsProjection(
                        cold + 1,
                        carrier,
                        category,
                        out var remainingField
                    ) || remainingField == firstField
                    || !ReadsMove(cold + 2, checkedTag, x0))
                {
                    continue;
                }

                int classify = SkipLines(cold + 3, end);
                int choose = classify + 1;
                if (choose >= end || body[classify] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value.IsOne: true }, _] }
                    || body[choose] is not
                    {
                        Definition.Opcode: SelectValue,
                        Operands: [var discriminant, _, _, { Kind: BeamOperandParameters.Atom }, var a,
                        { Kind: BeamOperandParameters.Atom }, var b]
                    } || discriminant != x0
                    || !ownership.TryGetSingleForwardTarget(choose, (int)a.Value, out int aLabel)
                    || !ownership.TryGetSingleForwardTarget(choose, (int)b.Value, out int bLabel))
                {
                    continue;
                }

                int earlier = Math.Min(aLabel, bLabel) + 1;
                int later = Math.Max(aLabel, bLabel) + 1;
                int join = later + 2;
                if (join + 9 >= end || earlier + 3 != later
                    || !ReadCategoryMove(earlier, out var firstCategory) || !ReadCategoryMove(later, out var secondCategory)
                    || body[earlier + 1] is not { Definition.Opcode: Jump, Operands: [var joined] }
                    || !ownership.TryGetExclusiveForwardTarget(
                        earlier + 1,
                        later,
                        (int)joined.Value,
                        out int joinLabel
                    )
                    || joinLabel != later + 1 || !ReadsMove(join, x0, carrier)
                    || body[join + 1] is not
                    {
                        Definition.Opcode: TestHeap,
                        Operands: [{ Value: var tupleWords }, { Value.IsZero: true }]
                    } || tupleWords != 4
                    || body[join + 2] is not
                    {
                        Definition.Opcode: PutTuple,
                        Operands: [var tuple, _, var categoryField, var materialized, var tagField]
                    } || tuple != x0 || categoryField != carrier || materialized != category || tagField != checkedTag
                    || !ReadsMove(join + 3, x0, carrier) || !ReadsMove(join + 4, savedTail, x0)
                    || !ReadsMove(join + 5, carrier, savedTail)
                    || body[join + 6] is not { Definition.Opcode: TrimFrame, Operands: [var dropped, var kept] }
                    || dropped.Value != 3 || !kept.Value.IsOne)
                {
                    continue;
                }

                int call = SkipLines(join + 7, end);
                if (call + 4 >= end || body[call] is not
                    {
                        Definition.Opcode: LocalCall, Operands: [{ Value.IsOne: true }, var target]
                    } || target.Value != recursive.Value
                    || body[call + 1] is not
                    {
                        Definition.Opcode: TestHeap, Operands: [{ Value: var consWords }, { Value.IsOne: true }]
                    } || consWords != ConsHeapWords
                    || body[call + 2] is not { Definition.Opcode: PutList, Operands: [var item, var rest, var result] }
                    || item != category || rest != x0 || result != x0
                    || body[call + 3] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                    || body[call + 4].Definition.Opcode != Return)
                {
                    continue;
                }

                output.Write(code.Span[copied..body[cold + 1].Offset]);
                output.Write(code.Span[body[cold + 2].Offset..body[earlier].Offset]);
                WriteMove(firstCategory, category);
                output.Write(code.Span[body[earlier + 1].Offset..body[later].Offset]);
                WriteMove(secondCategory, category);
                output.Write(code.Span[body[later + 1].Offset..body[join].Offset]);
                WriteMove(savedTail, x0);
                WriteMove(category, savedTail);
                output.WriteByte(TrimFrame);
                WriteUnsigned(1);
                WriteUnsigned(3);
                output.Write(code.Span[body[join + 7].Offset..body[call + 1].Offset]);
                output.WriteByte(TestHeap);
                WriteUnsigned((int)tupleWords + ConsHeapWords);
                WriteUnsigned(1);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, checkedTag); // Former carrier shifted down by trim.
                WriteUnsigned((int)remainingField.Value);
                WriteRegister(output, x1);
                output.WriteByte(PutTuple);
                WriteRegister(output, x1);
                output.WriteByte(ExtendedListOperand);
                WriteUnsigned(3);
                WriteRegister(output, carrier); // Former tail now holds category.
                WriteRegister(output, x1);
                WriteRegister(output, category); // Former checked tag shifted down.
                output.WriteByte(PutList);
                WriteRegister(output, x1);
                WriteRegister(output, x0);
                WriteRegister(output, x0);
                output.WriteByte(Deallocate);
                WriteUnsigned(3);
                copied = body[call + 4].Offset;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool ReadsMove(int index, BeamOperand source, BeamOperand destination) =>
            body[index] is { Definition.Opcode: Move, Operands: [var actual, var output] }
            && actual == source && output == destination;

        bool ReadsProjection(
            int index,
            BeamOperand source,
            BeamOperand destination,
            out BeamOperand field
        )
        {
            field = null!;
            if (body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [var actual, { Kind: BeamOperandParameters.Unsigned } accessed, var output]
                } || actual != source || output != destination || accessed.Value < 0 || accessed.Value >= 2)
            {
                return false;
            }

            field = accessed;

            return true;
        }

        bool ReadCategoryMove(int index, out BeamOperand value)
        {
            value = null!;
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom } atom, var destination]
                } || destination != x0)
            {
                return false;
            }

            value = atom;

            return true;
        }

        int SkipLines(int index, int end)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        void WriteMove(BeamOperand source, BeamOperand destination)
        {
            output.WriteByte(Move);
            if (source.Kind == BeamOperandParameters.Atom)
            {
                BeamCompactWriter.Write(output, AtomTag, source.Value);
            }
            else
            {
                WriteRegister(output, source);
            }

            WriteRegister(output, destination);
        }

        void WriteUnsigned(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);
    }
}
