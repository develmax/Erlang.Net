using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const string RecordCopyHint = "copy";
    private const int OwnedFrameRegisterCount = sizeof(ulong) * 8 - 1;

    private sealed record OwnedRecordStep(
        BeamInstruction Instruction,
        int[] Values,
        int Destination,
        int Result
    );

    private byte[]? LocalizeCheckedRecordCallFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Func<BeamInstruction, bool> isArithmetic,
        string[] atomNames,
        (int Module, int Function, int Arity)[] importsByIndex
    )
    {
        using var output = new MemoryStream(code.Length);
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int info = 0; info + 4 < body.Count; info++)
        {
            if (body[info].Definition.Opcode != FunctionInfo
                || !body[info].Operands[^1].Value.IsOne
                || body[info + 1].Definition.Opcode != Label
                || body[info + 2] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [{ Kind: BeamOperandParameters.Label } tagFailure, var record, var width, _]
                }
                || !IsBareX0(record)
                || body[info + 3] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value.Sign: > 0 } oldFrame, { Value.IsOne: true }]
                })
            {
                continue;
            }

            int end = info + 4;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                end++;
            }

            flow ??= new BeamProjectionFlow(body);
            int tag = info + 2;
            if (!flow.TryGetSingleForwardTarget(tag, (int)tagFailure.Value, out int fallback)
                || fallback >= end
                || ReadDisjointFrameOwnership(body, info + 1, end) is not { } ownership)
            {
                continue;
            }

            int next = info + 4;
            while (next < fallback && body[next].Definition.Opcode is
                Line or InitYRegisters or Move or GetTupleElement or GcBifTwo)
            {
                next++;
            }

            int test = next;
            if (test >= fallback
                || body[test].Definition.Opcode is not (IsLess or IsGreaterOrEqual)
                || body[test].Operands[0] is not { Kind: BeamOperandParameters.Label } failed
                || !flow.TryGetSingleForwardTarget(test, (int)failed.Value, out int ordinary)
                || ordinary >= fallback)
            {
                continue;
            }

            int call = test + 1;
            while (call < ordinary && body[call].Definition.Opcode is Line or Move or GetTupleElement or GcBifTwo)
            {
                call++;
            }

            if (call >= ordinary || body[call] is not
                { Definition.Opcode: LocalCall, Operands: [{ Value.IsOne: true }, _] }
                || !OwnedTerminal(call + 1, ordinary)
                || !OwnedTerminal(ordinary + 1, fallback)
                || !HasCopyOnlyUpdate(call + 1, ordinary)
                || !HasCopyOnlyUpdate(ordinary + 1, fallback)
                || Enumerable.Range(info + 4, fallback - info - 4)
                    .Any(index => body[index].Definition.Opcode != Return
                        && ownership.Owners[index - (info + 1)] != info + 3))
            {
                continue;
            }

            int tagLabel = (int)tagFailure.Value;
            int ordinaryLabel = (int)failed.Value;
            bool physicalLabels = ordinaryLabel == tagLabel + 1
                && !body.Any(instruction => instruction.Definition.Opcode is
                    (LocalCall or TailLocalCall or TailLocalCallWithFrame)
                    && instruction.Operands[1].Value >= tagLabel
                    && instruction.Operands[1].Value <= ordinaryLabel);
            if (PlanOwnedRecordFrame(
                code,
                body,
                isArithmetic,
                IsOrdinaryNumberProducer,
                (int)width.Value,
                info + 4,
                test,
                call,
                ordinary,
                fallback,
                physicalLabels ? tagLabel : null
            ) is not { } replacement)
            {
                continue;
            }

            // beam_clean renumbers labels in physical order. Both interior
            // branch labels have exactly one reference and no local call entry.
            if (physicalLabels)
            {
                int operandEnd = body[tag].Offset + 1;
                var cursor = new BeamCursor(code[operandEnd..]);
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[copied..operandEnd]);
                BeamCompactWriter.Write(output, LabelTag, ordinaryLabel);
                output.Write(code.Span[(operandEnd + cursor.Position)..body[info + 3].Offset]);
            }
            else
            {
                output.Write(code.Span[copied..body[info + 3].Offset]);
            }

            output.Write(replacement);
            copied = body[fallback].Offset;
            if (physicalLabels)
            {
                output.WriteByte(Label);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, ordinaryLabel);
                copied = body[fallback + 1].Offset;
            }

            info = end - 1;

            bool HasCopyOnlyUpdate(int start, int finish)
            {
                var recordUpdates = body.Skip(start).Take(finish - start)
                    .Where(instruction => instruction.Definition.Opcode == UpdateRecord);

                return recordUpdates.Any()
                    && recordUpdates.All(instruction => instruction.Operands[0] is
                    { Kind: BeamOperandParameters.Atom, Value.Sign: > 0 } hint
                        && atomNames[(int)hint.Value - 1] == RecordCopyHint);
            }

            bool OwnedTerminal(int start, int finish) => finish - start >= 2
                && body[finish - 2] is { Definition.Opcode: Deallocate, Operands: [var size] }
                && size == oldFrame && body[finish - 1].Definition.Opcode == Return;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsOrdinaryNumberProducer(BeamInstruction instruction)
        {
            if (instruction.Definition.Opcode != GcBifTwo)
            {
                return false;
            }

            var (module, operation, arity) = importsByIndex[(int)instruction.Operands[2].Value];

            return arity == 2 && atomNames[module - 1] == ErlangModuleNames.Module
                && atomNames[operation - 1] is ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply;
        }
    }

    private byte[]? PlanOwnedRecordFrame(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        Func<BeamInstruction, bool> isArithmetic,
        Func<BeamInstruction, bool> isNumberProducer,
        int tupleArity,
        int first,
        int test,
        int call,
        int ordinary,
        int fallback,
        int? ordinaryTarget
    )
    {
        const int input = 0;
        const int callResult = 1;
        const int firstValue = 2;
        int nextValue = firstValue;
        const int yBase = BeamOperandParameters.MaximumRegister;

        var state = new Dictionary<int, int> { [0] = input };
        if (Read(first, test + 1, state) is not { } prefix)
        {
            return null;
        }

        int callLine = call;
        while (callLine > test + 1 && body[callLine - 1].Definition.Opcode == Line)
        {
            callLine--;
        }

        var withCall = new Dictionary<int, int>(state);
        if (Read(test + 1, callLine, withCall) is not { } hotPrefix
            || !withCall.TryGetValue(0, out int argument))
        {
            return null;
        }

        var afterCall = withCall.Where(pair => pair.Key >= yBase).ToDictionary();
        afterCall.Add(0, callResult);
        if (Read(call + 1, ordinary - 2, afterCall) is not { } hot
            || Read(ordinary + 1, fallback - 2, state) is not { } cold
            || !afterCall.TryGetValue(0, out int hotReturn)
            || !state.TryGetValue(0, out int coldReturn))
        {
            return null;
        }

        int[] spills = LiveEntry(hot, [hotReturn]).Where(value => value != callResult).Order().ToArray();
        if (spills.Length == 0 || spills.Any(value => !withCall.ContainsValue(value)))
        {
            return null;
        }

        // Only checked tuple reads are moved. Arithmetic stays after its original
        // source line and never moves to a route that previously skipped it.
        var projections = prefix.Where(step => step.Instruction.Definition.Opcode == GetTupleElement)
            .ToDictionary(step => step.Result);
        int[] checkedFields = projections.Keys.ToArray();
        HashSet<int> coldEntry = LiveEntry(cold, [coldReturn]);
        HashSet<int> hotEntry = LiveEntry(hotPrefix, spills.Append(argument));
        prefix = Schedule(prefix, coldEntry.Intersect(hotEntry));
        hotPrefix = Schedule(hotPrefix, spills.Append(argument));
        cold = Schedule(cold, [coldReturn]);
        if (projections.Count != 0)
        {
            return null;
        }

        HashSet<int> numbers = SsaTypeInferenceEnabled
            ? prefix.Concat(hotPrefix).Concat(hot).Concat(cold)
                .Where(step => isNumberProducer(step.Instruction)
                    && (checkedFields.Contains(step.Values[3])
                        && step.Instruction.Operands[4].Kind == BeamOperandParameters.Integer
                        || checkedFields.Contains(step.Values[4])
                        && step.Instruction.Operands[3].Kind == BeamOperandParameters.Integer))
                .Select(step => step.Result).ToHashSet()
            : [];
        int numberTest = prefix[^1].Values[1];
        long? ordinaryMinimum = body[test] is
        {
            Definition.Opcode: IsLess,
            Operands: [_, _, { Kind: BeamOperandParameters.Integer } limit]
        }
            && numbers.Contains(numberTest) && limit.Value >= long.MinValue && limit.Value <= long.MaxValue
                ? (long)limit.Value : null;

        using var output = new MemoryStream();
        var registers = new Dictionary<int, (bool Stack, int Index)> { [input] = (false, 0) };
        ulong initialized = 1;
        var branchEntry = LiveEntry(cold, [coldReturn]);
        branchEntry.UnionWith(LiveEntry(hotPrefix, spills.Append(argument)));
        if (!Emit(
            prefix,
            branchEntry,
            registers,
            ref initialized
        ))
        {
            return null;
        }

        var onFailure = new Dictionary<int, (bool Stack, int Index)>(registers);
        ulong failureLive = initialized;
        if (!Emit(
            hotPrefix,
            spills.Append(argument),
            registers,
            ref initialized
        )
            || !Roots(
                registers,
                spills.Append(argument),
                initialized,
                out int frameLive
            ))
        {
            return null;
        }

        output.WriteByte(Allocate);
        Unsigned(spills.Length);
        Unsigned(frameLive);
        var afterCallRegisters = new Dictionary<int, (bool Stack, int Index)> { [callResult] = (false, 0) };
        for (int slot = 0; slot < spills.Length; slot++)
        {
            int value = spills[spills.Length - slot - 1];
            var physical = registers[value];
            WriteMove(physical, (true, slot));
            afterCallRegisters.Add(value, (true, slot));
        }

        WriteMove(registers[argument], (false, 0));
        output.Write(code.Span[body[callLine].Offset..body[call + 1].Offset]);
        initialized = 1;
        if (!Emit(
            hot,
            [hotReturn],
            afterCallRegisters,
            ref initialized
        )
            || !afterCallRegisters.TryGetValue(hotReturn, out var hotResult))
        {
            return null;
        }

        WriteMove(hotResult, (false, 0));
        output.WriteByte(Deallocate);
        Unsigned(spills.Length);
        output.WriteByte(Return);
        if (ordinaryTarget is int target)
        {
            output.WriteByte(Label);
            Unsigned(target);
        }
        else
        {
            Copy(ordinary);
        }

        if (!Emit(
            cold,
            [coldReturn],
            onFailure,
            ref failureLive,
            ordinaryArm: true
        )
            || !onFailure.TryGetValue(coldReturn, out var coldResult))
        {
            return null;
        }

        WriteMove(coldResult, (false, 0));
        output.WriteByte(Return);

        return output.ToArray();

        void Copy(int index) => output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);

        List<OwnedRecordStep>? Read(int from, int to, Dictionary<int, int> logical)
        {
            var steps = new List<OwnedRecordStep>();
            for (int index = from; index < to; index++)
            {
                BeamInstruction instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                var operands = instruction.Operands;
                if (opcode == InitYRegisters && index == first)
                {
                    continue;
                }

                int destination = opcode switch
                {
                    PutTuple => 0,
                    UpdateRecord => StackWriteOperand(instruction),
                    Move or GcBifTwo or GetTupleElement or PutList => operands.Count - 1,
                    Line or TestHeap or IsLess or IsGreaterOrEqual => -1,
                    _ => -2
                };
                if (destination == -2 || index != test && opcode is (IsLess or IsGreaterOrEqual)
                    || opcode == TestHeap && operands.Count != 2 || opcode == GcBifTwo
                    && (operands[0].Kind != BeamOperandParameters.Failure || !isArithmetic(instruction)))
                {
                    return null;
                }

                int[] values = new int[operands.Count];
                Array.Fill(values, -1);
                for (int position = 0; position < operands.Count; position++)
                {
                    int register = LeafRegisterIndex(operands[position]);
                    if (position != destination && register >= 0 && !logical.TryGetValue(register, out values[position]))
                    {
                        return null;
                    }
                }

                if (opcode == GetTupleElement
                    && (values[0] != input || operands[1].Value < 0 || operands[1].Value >= tupleArity))
                {
                    return null;
                }

                if (opcode is GcBifTwo or TestHeap)
                {
                    int live = (int)operands[opcode == GcBifTwo ? 1 : operands.Count - 1].Value;
                    foreach (int register in logical.Keys.Where(register => register >= live && register < yBase).ToArray())
                    {
                        logical.Remove(register);
                    }
                }

                int result = -1;
                if (destination >= 0)
                {
                    int register = LeafRegisterIndex(operands[destination]);
                    if (register < 0)
                    {
                        return null;
                    }

                    result = opcode == Move ? values[0] : nextValue++;
                    if (result < 0)
                    {
                        return null;
                    }

                    logical[register] = result;
                }

                if (opcode != Move)
                {
                    steps.Add(new(
                        instruction,
                        values,
                        destination,
                        result
                    ));
                }
            }

            return steps;
        }

        List<OwnedRecordStep> Schedule(List<OwnedRecordStep> steps, IEnumerable<int> beforeEnd)
        {
            var ordered = new List<OwnedRecordStep>();
            var lines = new List<OwnedRecordStep>();
            foreach (OwnedRecordStep step in steps)
            {
                byte opcode = step.Instruction.Definition.Opcode;
                if (opcode == GetTupleElement && projections.ContainsKey(step.Result))
                {
                    continue;
                }

                if (opcode == Line)
                {
                    lines.Add(step);
                    continue;
                }

                AddReads(opcode is IsLess or IsGreaterOrEqual ? step.Values.Concat(beforeEnd) : step.Values);
                ordered.AddRange(lines);
                lines.Clear();
                ordered.Add(step);
            }

            AddReads(beforeEnd);
            ordered.AddRange(lines);

            return ordered;

            void AddReads(IEnumerable<int> reads)
            {
                foreach (int value in reads.Distinct().Order())
                {
                    if (projections.Remove(value, out OwnedRecordStep? projection))
                    {
                        ordered.Add(projection);
                    }
                }
            }
        }

        bool Emit(
            List<OwnedRecordStep> steps,
            IEnumerable<int> end,
            Dictionary<int, (bool Stack, int Index)> physical,
            ref ulong present,
            bool ordinaryArm = false
        )
        {
            var after = new HashSet<int>[steps.Count];
            var needed = new HashSet<int>(end);
            for (int index = steps.Count - 1; index >= 0; index--)
            {
                after[index] = new(needed);
                needed.Remove(steps[index].Result);
                needed.UnionWith(steps[index].Values.Where(value => value >= 0));
            }

            if (needed.Any(value => !physical.ContainsKey(value)))
            {
                return false;
            }

            for (int index = 0; index < steps.Count; index++)
            {
                OwnedRecordStep step = steps[index];
                var instruction = step.Instruction;
                byte opcode = instruction.Definition.Opcode;
                int collector = opcode == GcBifTwo ? 1 : opcode == TestHeap ? instruction.Operands.Count - 1 : -1;
                int live = -1;
                if (collector >= 0)
                {
                    var reads = after[index].Where(value => value != step.Result)
                        .Concat(step.Values.Where(value => value >= 0));
                    if (!Roots(
                        physical,
                        reads,
                        present,
                        out live
                    ))
                    {
                        return false;
                    }

                    present &= (1UL << live) - 1;
                }

                int target = -1;
                if (step.Destination >= 0)
                {
                    var used = after[index].Where(value => value != step.Result)
                        .Select(value => physical[value]).Where(register => !register.Stack)
                        .Select(register => register.Index).ToHashSet();
                    target = Enumerable.Range(0, OwnedFrameRegisterCount).FirstOrDefault(register => !used.Contains(register), -1);
                    if (target < 0)
                    {
                        return false;
                    }
                }

                output.WriteByte(opcode);
                for (int position = 0; position < instruction.Operands.Count; position++)
                {
                    BeamOperand operand = instruction.Operands[position];
                    if (opcode == PutTuple && position == 1 || opcode == UpdateRecord && position == 4)
                    {
                        output.WriteByte(ExtendedListOperand);
                    }

                    if (position == step.Destination)
                    {
                        WritePhysical((false, target), operand.TypeIndex);
                    }
                    else if (position == collector)
                    {
                        Unsigned(live);
                    }
                    else if (step.Values[position] >= 0)
                    {
                        int value = step.Values[position];
                        int? type = operand.TypeIndex;
                        if (type is null && numbers.Contains(value)
                            && (opcode is IsLess or IsGreaterOrEqual
                                || opcode == UpdateRecord && position >= FirstRecordUpdateValueOperand))
                        {
                            long? minimum = ordinaryArm && value == numberTest ? ordinaryMinimum : null;
                            type = GetOperandTypeIndex((NumberTypeBits, minimum, null, null));
                        }

                        WritePhysical(physical[value], type);
                    }
                    else if (position == 0 && ordinaryTarget is int branchTarget
                        && opcode is IsLess or IsGreaterOrEqual)
                    {
                        BeamCompactWriter.Write(output, LabelTag, branchTarget);
                    }
                    else if (!WriteScalar(operand))
                    {
                        return false;
                    }
                }

                if (target >= 0)
                {
                    physical[step.Result] = (false, target);
                    present |= 1UL << target;
                }

                foreach (int value in physical.Keys.Where(value => !after[index].Contains(value)).ToArray())
                {
                    physical.Remove(value);
                }
            }

            return true;
        }

        bool WriteScalar(BeamOperand operand)
        {
            int tag = operand.Kind switch
            {
                BeamOperandParameters.Failure or BeamOperandParameters.Label => LabelTag,
                BeamOperandParameters.Atom or BeamOperandParameters.Nil => AtomTag,
                BeamOperandParameters.Integer => BeamWire.IntegerTag,
                BeamOperandParameters.Unsigned or BeamOperandParameters.Count or BeamOperandParameters.LiteralValue
                    => BeamWire.UnsignedTag,
                _ => -1
            };
            if (tag < 0)
            {
                return false;
            }

            if (operand.Kind == BeamOperandParameters.LiteralValue)
            {
                output.WriteByte(ExtendedLiteralOperand);
            }

            BeamCompactWriter.Write(output, tag, operand.Value);

            return true;
        }

        void Unsigned(int value) => BeamCompactWriter.Write(output, BeamWire.UnsignedTag, value);

        void WritePhysical((bool Stack, int Index) register, int? type = null)
        {
            if (type is not null)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }

            BeamCompactWriter.Write(output, register.Stack ? YRegisterTag : XRegisterTag, register.Index);
            if (type is int typeIndex)
            {
                Unsigned(typeIndex);
            }
        }

        void WriteMove((bool Stack, int Index) source, (bool Stack, int Index) target)
        {
            if (source != target)
            {
                output.WriteByte(Move);
                WritePhysical(source);
                WritePhysical(target);
            }
        }

        static HashSet<int> LiveEntry(List<OwnedRecordStep> steps, IEnumerable<int> end)
        {
            var needed = new HashSet<int>(end);
            for (int index = steps.Count - 1; index >= 0; index--)
            {
                needed.Remove(steps[index].Result);
                needed.UnionWith(steps[index].Values.Where(value => value >= 0));
            }

            return needed;
        }

        static bool Roots(
            Dictionary<int, (bool Stack, int Index)> physical,
            IEnumerable<int> reads,
            ulong present,
            out int live
        )
        {
            var roots = reads.Select(value => physical[value]).Where(register => !register.Stack);
            live = roots.Select(register => register.Index + 1).DefaultIfEmpty(0).Max();
            ulong prefix = (1UL << live) - 1;

            return (present & prefix) == prefix;
        }
    }
}
