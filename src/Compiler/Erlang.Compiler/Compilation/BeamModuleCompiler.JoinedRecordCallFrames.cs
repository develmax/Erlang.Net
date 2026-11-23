using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int JoinedRecordFrameWidth = 5;

    private byte[]? LocalizeJoinedRecordCallFrame(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlyList<FunctionDefinition> functions
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(UpdateRecord))
        {
            return null;
        }

        var references = new List<(int Start, int End, int Label)>();
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
                (start, end, target) => references.Add((start, end, target))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var atomNames = atoms.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        var imported = imports.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray();
        using var output = new MemoryStream();
        int copied = 0;
        for (int info = 0; info + 38 < body.Count; info++)
        {
            if (body[info] is not { Definition.Opcode: FunctionInfo, Operands: [_, var name, { Value.IsOne: true }] }
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not { Definition.Opcode: IsTaggedTuple, Operands: [var failure, var tagged, var tupleArity, _] }
                || !IsBareX0(tagged)
                || body[info + 3] is not { Definition.Opcode: Allocate, Operands: [var frame, { Value.IsOne: true }] }
                || frame.Value != JoinedRecordFrameWidth
                || body[info + 4].Definition.Opcode != InitYRegisters
                || !MoveBetween(
                    info + 5,
                    false,
                    0,
                    true,
                    4
                )
                || !Projection(info + 6, 3, (int)tupleArity.Value) || !Projection(info + 7, 2, (int)tupleArity.Value)
                || !Projection(info + 8, 1, (int)tupleArity.Value))
            {
                continue;
            }

            int end = info + 9;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            int first = SkipLines(info + 9, end);
            int second = SkipLines(first + 2, end);
            int call = SkipLines(second + 3, end);
            int compare = call + 2;
            int increased = SkipLines(compare + 2, end);
            int hotHeap = increased + 2;
            int cold = hotHeap + 3;
            int coldHeap = cold + 1;
            int join = coldHeap + 2;
            int saved = join + 1;
            int released = saved + 7;
            int legacy = released + 2;
            if (legacy >= end
                || first == info + 9 || second == first + 2
                || !Adjusts(first, 2) || !MoveBetween(
                    first + 1,
                    false,
                    0,
                    true,
                    0
                )
                || !Adjusts(second, 3) || !MoveBetween(
                    second + 1,
                    false,
                    0,
                    true,
                    3
                )
                || !MoveBetween(
                    second + 2,
                    true,
                    1,
                    false,
                    0
                )
                || body[call] is not { Definition.Opcode: LocalCall, Operands: [{ Value.IsOne: true }, _] }
                || body[call + 1] is not
                {
                    Definition.Opcode: GcBifTwo,
                    Operands: [{ Kind: BeamOperandParameters.Failure }, _, _, var callInput,
                    { Kind: BeamOperandParameters.Integer }, var compareResult]
                }
                || !Reg(callInput, false, 0)
                || !IsBareX0(compareResult)
                || body[compare] is not { Definition.Opcode: NumericEqualTest, Operands: [var alternate, var left, var threshold] }
                || !Reg(left, false, 0) || !Reg(threshold, true, 0)
                || threshold.TypeIndex is not int number
                || typeBits[number] != (NumberTypeBits, null, null, null)
                || !MoveBetween(
                    compare + 1,
                    false,
                    0,
                    true,
                    0
                )
                || !Adjusts(increased, 1) || !MoveBetween(
                    increased + 1,
                    false,
                    0,
                    true,
                    1
                )
                || !TerminalHeap(hotHeap) || !HotUpdate(hotHeap + 1)
                || body[hotHeap + 2] is not { Definition.Opcode: Jump, Operands: [var shared] }
                || !Declares(cold, alternate) || !TerminalHeap(coldHeap) || !ColdUpdate(coldHeap + 1)
                || !Declares(join, shared) || !Declares(legacy, failure)
                || !MoveBetween(
                    saved,
                    false,
                    0,
                    true,
                    1
                ) || !TerminalHeap(saved + 1)
                || !List(saved + 2, 2) || !MoveBetween(
                    saved + 3,
                    false,
                    0,
                    true,
                    4
                )
                || !List(saved + 4, 3) || !MoveBetween(
                    saved + 5,
                    false,
                    0,
                    true,
                    3
                )
                || body[saved + 6] is not { Definition.Opcode: PutTuple, Operands: [var tuple, _, var recordValue, var list1, var list2] }
                || !IsBareX0(tuple) || !Reg(recordValue, true, 1) || !Reg(list1, true, 4) || !Reg(list2, true, 3)
                || body[released] is not { Definition.Opcode: Deallocate, Operands: [var width] }
                || width.Value != JoinedRecordFrameWidth || body[released + 1].Definition.Opcode != Return)
            {
                continue;
            }

            int[] physical = [(int)alternate.Value, (int)shared.Value, (int)failure.Value];
            if (physical.Distinct().Count() != physical.Length
                || physical.Any(target => references.Count(reference => reference.Label == target) != 1)
                || ReadDisjointFrameOwnership(body, info + 1, end) is not { } owner
                || Enumerable.Range(info + 4, released - info - 3).Any(index => owner.Owners[index - info - 1] != info + 3)
                || !TryReadBindingOrder(
                    atomNames[(int)name.Value - 1],
                    first,
                    second,
                    out int firstSlot
                ))
            {
                continue;
            }

            int secondSlot = 3 - firstSlot;
            var labels = physical.Zip(physical.Order()).ToDictionary();
            output.Write(code.Span[copied..body[info + 2].Offset]);
            CopyWithLabels(info + 2, info + 3, labels);
            Project(info + 7, false, 1);
            Copy(info + 9, first);
            Arithmetic(
                first,
                2,
                false,
                1,
                false,
                2
            );
            Project(info + 6, false, 3);
            Copy(first + 2, second);
            Arithmetic(
                second,
                4,
                false,
                3,
                false,
                3
            );
            output.WriteByte(Allocate);
            U(JoinedRecordFrameWidth);
            U(4);
            WriteMove(
                false,
                firstSlot == 1 ? 2 : 3,
                true,
                1
            );
            WriteMove(
                false,
                firstSlot == 2 ? 2 : 3,
                true,
                2
            );
            WriteMove(
                false,
                1,
                true,
                3
            );
            WriteMove(
                false,
                0,
                true,
                4
            );
            Project(info + 8, true, 0);
            WriteMove(
                true,
                0,
                false,
                0
            );
            Copy(second + 3, compare);
            output.WriteByte(NumericEqualTest);
            BeamCompactWriter.Write(output, LabelTag, labels[(int)alternate.Value]);
            Register(false, 0, left.TypeIndex);
            Register(true, firstSlot, number);
            Copy(compare + 2, increased);
            Arithmetic(
                increased,
                0,
                true,
                0,
                false,
                0
            );
            Heap(hotHeap, 1);
            Update(hotHeap + 1, hot: true);
            CopyWithLabels(hotHeap + 2, cold, labels);
            Define(labels[(int)alternate.Value]);
            Heap(coldHeap, 0);
            Update(coldHeap + 1, hot: false);
            Define(labels[(int)shared.Value]);
            Heap(saved + 1, 1);
            PutSavedList(3, 1);
            PutSavedList(secondSlot, 2);
            output.WriteByte(PutTuple);
            Register(false, 0);
            output.WriteByte(ExtendedListOperand);
            U(3);
            Register(false, 0);
            Register(false, 1);
            Register(false, 2);
            Copy(released, legacy);
            Define(labels[(int)failure.Value]);
            copied = body[legacy + 1].Offset;
            info = end - 1;

            void Update(int index, bool hot)
            {
                var operands = body[index].Operands;
                output.WriteByte(UpdateRecord);
                Scalar(operands[0]);
                Scalar(operands[1]);
                Register(true, 4, operands[2].TypeIndex);
                Register(false, 0);
                output.WriteByte(ExtendedListOperand);
                Scalar(operands[4]);
                for (int field = 5; field < operands.Count; field += 2)
                {
                    Scalar(operands[field]);
                    BeamOperand prior = operands[field + 1];
                    if (prior.Kind != BeamOperandParameters.Y)
                    {
                        Scalar(prior);
                    }
                    else if (prior.Value == 3)
                    {
                        Register(true, secondSlot, number);
                    }
                    else if (hot)
                    {
                        Register(false, 0, number);
                    }
                    else
                    {
                        Register(true, firstSlot, number);
                    }
                }
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool TryReadBindingOrder(
            string name,
            int first,
            int second,
            out int firstSlot
        )
        {
            firstSlot = 0;
            var function = functions.FirstOrDefault(candidate => candidate.Name == name && candidate.Arity == 1);
            if (function?.Clauses.FirstOrDefault()?.Body is not
                Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable firstName, Value: Expr.Binary firstExpression },
                        Expr.Match { Pattern: Pattern.Variable secondName, Value: Expr.Binary secondExpression },
                        ..]
                }
                || firstName.Name == secondName.Name || !SameLocation(firstExpression, first)
                || !SameLocation(secondExpression, second))
            {
                return false;
            }

            // reserve_yregs sorts source variable identities by Erlang term order,
            // independently of which arithmetic instruction ran first.
            firstSlot = TermOrder.Compare(Term.A(firstName.Name), Term.A(secondName.Name), exact: true) < 0 ? 2 : 1;

            return true;
        }

        bool SameLocation(Expr.Binary expression, int index)
        {
            var location = body[index - 1].Operands[0].Value;
            var (module, function, _) = imported[(int)body[index].Operands[2].Value];

            return atomNames[module - 1] == ErlangModuleNames.Module && atomNames[function - 1] == expression.Operator
                && lines.Any(line => line.Value == location && line.Key.Line == expression.SourceLine);
        }

        int SkipLines(int index, int end)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }

        bool Adjusts(int index, int source) =>
            IsLeafArithmeticGcBif(body[index], atomNames, imported)
            && body[index].Operands[0].Kind == BeamOperandParameters.Failure
            && IsNumberOperation((int)body[index].Operands[2].Value)
            && Reg(body[index].Operands[3], true, source)
            && body[index].Operands[4].Kind == BeamOperandParameters.Integer
            && IsBareX0(body[index].Operands[5]);

        bool IsNumberOperation(int index)
        {
            var (module, operation, arity) = imported[index];

            return arity == 2 && atomNames[module - 1] == ErlangModuleNames.Module
                && atomNames[operation - 1] is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply;
        }

        bool MoveBetween(
            int index,
            bool stackSource,
            int source,
            bool stackTarget,
            int target
        ) => body[index] is
        { Definition.Opcode: Move, Operands: [var read, var write] }
            && read.TypeIndex is null && write.TypeIndex is null
            && Reg(read, stackSource, source) && Reg(write, stackTarget, target);

        bool Projection(int index, int slot, int arity) => body[index] is
        { Definition.Opcode: GetTupleElement, Operands: [var record, var element, var field] }
            && element.Value >= 0 && element.Value < arity && Reg(record, true, 4) && Reg(field, true, slot);

        bool TerminalHeap(int index) => body[index] is
        { Definition.Opcode: TestHeap, Operands: [{ Kind: BeamOperandParameters.Unsigned }, { Value.IsZero: true }] };

        bool CopyUpdate(BeamInstruction instruction) => instruction.Definition.Opcode == UpdateRecord
            && instruction.Operands[0] is { Kind: BeamOperandParameters.Atom, Value.Sign: > 0 } hint
            && atomNames[(int)hint.Value - 1] == RecordCopyHint
            && Reg(instruction.Operands[2], true, 4) && IsBareX0(instruction.Operands[3]);

        bool HotUpdate(int index) => CopyUpdate(body[index]) && body[index].Operands is
            [_, _, _, _, _, _, var next, _, { Kind: BeamOperandParameters.Integer }, _, var level]
            && Reg(next, true, 3) && Reg(level, true, 1);

        bool ColdUpdate(int index) => CopyUpdate(body[index]) && body[index].Operands is
            [_, _, _, _, _, _, var next, _, var position]
            && Reg(next, true, 3) && Reg(position, true, 0);

        bool Declares(int index, BeamOperand label) => body[index] is
        { Definition.Opcode: Label, Operands: [var defined] }
            && label.Kind == BeamOperandParameters.Label && label.Value == defined.Value;

        bool List(int index, int source) => body[index] is
        { Definition.Opcode: PutList, Operands: [var head, { Kind: BeamOperandParameters.Nil }, var target] }
            && Reg(head, true, source) && IsBareX0(target);

        void Copy(int first, int last) => output.Write(code.Span[body[first].Offset..body[last].Offset]);

        void CopyWithLabels(int first, int last, Dictionary<int, int> mapped)
        {
            int offset = body[first].Offset;
            int end = body[last].Offset;
            foreach (var reference in references.Where(reference => reference.Start >= offset
                && reference.Start < end && mapped.ContainsKey(reference.Label)).ToArray())
            {
                output.Write(code.Span[offset..reference.Start]);
                BeamCompactWriter.Write(output, LabelTag, mapped[reference.Label]);
                offset = reference.End;
            }

            output.Write(code.Span[offset..end]);
        }

        void Project(int original, bool stack, int target)
        {
            output.WriteByte(GetTupleElement);
            Register(false, 0);
            Scalar(body[original].Operands[1]);
            Register(stack, target);
        }

        void Arithmetic(
            int original,
            int live,
            bool fromStack,
            int source,
            bool toStack,
            int destination
        )
        {
            var operands = body[original].Operands;
            output.WriteByte(GcBifTwo);
            Scalar(operands[0]);
            U(live);
            Scalar(operands[2]);
            Register(fromStack, source, operands[3].TypeIndex);
            Scalar(operands[4]);
            Register(toStack, destination);
        }

        void Heap(int original, int live)
        {
            output.WriteByte(TestHeap);
            Scalar(body[original].Operands[0]);
            U(live);
        }

        void PutSavedList(int slot, int destination)
        {
            output.WriteByte(PutList);
            Register(true, slot);
            BeamCompactWriter.Write(output, AtomTag, 0);
            Register(false, destination);
        }

        void WriteMove(
            bool stackSource,
            int source,
            bool stackDestination,
            int destination
        )
        {
            output.WriteByte(Move);
            Register(stackSource, source);
            Register(stackDestination, destination);
        }

        void Define(int label)
        {
            output.WriteByte(Label);
            U(label);
        }

        void U(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);

        void Register(bool stack, int index, int? type = null)
        {
            if (type is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            BeamCompactWriter.Write(output, stack ? YRegisterTag : XRegisterTag, index);
            if (type is int ordinal)
            {
                U(ordinal);
            }
        }

        void Scalar(BeamOperand operand) => BeamCompactWriter.Write(
            output,
            operand.Kind switch
            {
                BeamOperandParameters.Integer => BeamWire.IntegerTag,
                BeamOperandParameters.Atom => AtomTag,
                BeamOperandParameters.Failure => LabelTag,
                _ => BeamWire.UnsignedTag
            },
            operand.Value
        );

        static bool Reg(BeamOperand operand, bool stack, int index) =>
            operand.Kind == (stack ? BeamOperandParameters.Y : BeamOperandParameters.X) && operand.Value == index;
    }
}
