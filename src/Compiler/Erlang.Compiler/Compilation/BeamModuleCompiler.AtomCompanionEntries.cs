using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record DisjointEntryArm(
        Term Key,
        BeamOperand Literal,
        int Test,
        int Body,
        int End,
        int Failure
    );

    private (byte[] Code, int LabelCount)? GroupDisjointEntryTests(
        ReadOnlyMemory<byte> code,
        int labelCount,
        IReadOnlyList<FunctionDefinition> functions,
        IReadOnlyDictionary<(string Name, int Arity), int> localEntries
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(ExactEqualTest))
        {
            return null;
        }

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
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        var names = atoms.OrderBy(item => item.Value).Select(item => item.Key).ToArray();
        var definitions = functions.ToDictionary(item => (item.Name, item.Arity));
        Dictionary<int, int>? labels = null;
        ILookup<int, int>? transfers = null;
        int copied = 0;
        using var output = new MemoryStream();
        for (int index = 0; index + 4 < body.Count; index++)
        {
            if (body[index].Definition.Opcode != FunctionInfo || body[index + 1].Definition.Opcode != Label
                || body[index].Operands is not [_, var functionName, var arity]
                || !definitions.TryGetValue((names[(int)functionName.Value - 1], (int)arity.Value), out var function))
            {
                continue;
            }

            int start = index + 2;
            if (WriteTupleAtomPartition(start, function))
            {
                continue;
            }

            if (body[start] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [_, { Kind: BeamOperandParameters.X, TypeIndex: null } selector, var firstKey]
                }
                || firstKey.Kind is not (BeamOperandParameters.Atom or BeamOperandParameters.Integer)
                || selector.Value >= function.Arity)
            {
                continue;
            }

            int argument = (int)selector.Value;
            // A preceding or later tuple/list partition can take priority over an
            // atom group. Limit this rewrite to the scalar selector family.
            bool companionPartition = firstKey.Kind == BeamOperandParameters.Atom
                && function.Clauses.All(clause => clause.Patterns[argument] is Pattern.Variable or Pattern.Literal { Value: Atom });
            bool terminalPartition = function.Clauses.All(clause => IsScalarEntrySelection(clause.Patterns[argument]));
            if (!companionPartition && !terminalPartition)
            {
                continue;
            }

            labels = ReadLabels();
            transfers ??= body.SelectMany((instruction, position) => instruction.Operands
                    .Where(operand => operand.Kind == BeamOperandParameters.Label)
                    .Select(operand => (label: (int)operand.Value, position)))
                .ToLookup(item => item.label, item => item.position);

            int cursor = start;
            var arms = new List<DisjointEntryArm>();
            while (ReadArm(cursor, selector, firstKey.Kind) is { } arm
                && (arms.Count == 0 || SameTests(arms[0], arm)))
            {
                arms.Add(arm);
                cursor = arm.End + 1;
            }

            if (arms.Count < 2 || arms.DistinctBy(arm => arm.Key).Count() != arms.Count)
            {
                continue;
            }

            bool terminalArms = arms[0].Body == arms[0].Test + 1;
            if (terminalArms
                ? !terminalPartition || HasLineInAnyArm(arms)
                : !companionPartition)
            {
                continue;
            }

            int fallback = arms[^1].Failure;
            var emitter = new GeneralEmitter(
                this,
                localEntries,
                definitions,
                default,
                frameSize: 0
            );
            var finite = firstKey.Kind == BeamOperandParameters.Atom
                ? emitter.ReadFiniteAtomArguments(function, argument, admitGuardFacts: true)
                : null;
            if (terminalArms && finite is not null)
            {
                continue;
            }

            var ordered = arms.OrderBy(arm => arm.Key, Comparer<Term>.Create(
                (left, right) => TermOrder.Compare(left, right, exact: true)
            )).ToArray();
            bool exhaustive = finite is not null && finite.SetEquals(ordered.Select(arm => (Atom)arm.Key));
            var representatives = terminalArms ? GroupIdenticalTerminalArms(ordered, selector) : null;
            if (terminalArms && ordered is [var first, var second] && UsesOneBooleanArm(first, second, representatives!))
            {
                continue;
            }

            var entries = new Dictionary<Term, int> { [arms[0].Key] = labelCount++ };
            for (int arm = 1; arm < arms.Count; arm++)
            {
                entries.Add(arms[arm].Key, arms[arm - 1].Failure);
            }

            output.Write(code.Span[copied..body[start].Offset]);
            output.WriteByte(SelectValue);
            highestOpcode = Math.Max(highestOpcode, SelectValue);
            int selectorType = terminalArms ? 0 : exhaustive ? AtomTypeBits : emitter.ReadAtomEntryType(function, argument);
            WriteInput(selector, selectorType);
            Write(LabelTag, exhaustive ? entries[ordered[0].Key] : fallback);
            output.WriteByte(ExtendedListOperand);
            int firstTableKey = exhaustive ? 1 : 0;
            Write(BeamWire.UnsignedTag, (ordered.Length - firstTableKey) * 2);
            foreach (DisjointEntryArm branch in ordered.Skip(firstTableKey))
            {
                if (branch.Literal.Kind == BeamOperandParameters.Atom)
                {
                    Write(AtomTag, branch.Literal.Value);
                }
                else
                {
                    WriteIntegerOperand(output, branch.Literal.Value);
                }

                Term target = representatives?.GetValueOrDefault(branch.Key) ?? branch.Key;
                Write(LabelTag, entries[target]);
            }

            foreach (DisjointEntryArm branch in ordered.Reverse())
            {
                if (representatives?.ContainsKey(branch.Key) == true)
                {
                    continue;
                }

                output.WriteByte(Label);
                Write(BeamWire.UnsignedTag, entries[branch.Key]);
                for (int test = branch.Test + 1; test < branch.Body; test++)
                {
                    BeamInstruction instruction = body[test];
                    output.WriteByte(instruction.Definition.Opcode);
                    Write(LabelTag, fallback);
                    BeamOperand input = instruction.Operands[1];
                    int type = input.Value < function.Arity
                        ? emitter.ReadSavedComparisonEntryType(function, (int)input.Value) : 0;
                    if (type == 0 && instruction.Definition.Opcode == ExactEqualTest
                        && instruction.Operands[2].Kind == BeamOperandParameters.Integer
                        && input.Value < function.Arity && ReadClosedIntegerRecurrence(function, (int)input.Value, definitions))
                    {
                        type = IntegerTypeBits;
                    }
                    WriteInput(input, type);
                    if (instruction.Definition.Opcode == ExactEqualTest)
                    {
                        BeamOperand literal = instruction.Operands[2];
                        if (literal.Kind == BeamOperandParameters.Integer)
                        {
                            WriteIntegerOperand(output, literal.Value);
                        }
                        else
                        {
                            Write(AtomTag, literal.Value);
                        }
                    }
                }
                int branchBody = terminalArms ? ReadTerminalEntryBody(branch, selector) : branch.Body;
                output.Write(code.Span[body[branchBody].Offset..body[branch.End].Offset]);
            }

            copied = body[arms[^1].End].Offset;
            index = arms[^1].End - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return (output.ToArray(), labelCount);

        Dictionary<int, int> ReadLabels() => labels ??= body.Select((instruction, position) => (instruction, position))
            .Where(item => item.instruction.Definition.Opcode == Label)
            .ToDictionary(item => (int)item.instruction.Operands[0].Value, item => item.position);

        bool WriteTupleAtomPartition(int start, FunctionDefinition function)
        {
            if (function.Clauses.Count != 3 || body[start] is not
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [
                    { Kind: BeamOperandParameters.Label } failedTuple,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } input,
                        var size,
                    { Kind: BeamOperandParameters.Atom } tag
                    ]
                } || input.Value >= function.Arity)
            {
                return false;
            }

            var indexedLabels = ReadLabels();
            if (!indexedLabels.TryGetValue((int)failedTuple.Value, out int atomStart) || atomStart <= start
                || atomStart + 1 >= body.Count || body[atomStart + 1] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [
                    { Kind: BeamOperandParameters.Label } fallback,
                        var atomInput,
                    { Kind: BeamOperandParameters.Atom } key
                    ]
                } || atomInput != input || !indexedLabels.TryGetValue((int)fallback.Value, out int fallbackStart)
                || fallbackStart <= atomStart + 1
                || !HasTupleAtomEntryPartition(
                    function,
                    (int)input.Value,
                    (int)size.Value,
                    names[(int)tag.Value - 1],
                    names[(int)key.Value - 1]
                ))
            {
                return false;
            }

            // The failed tuple type test enters the atom decision. A tuple
            // which fails the shape or tag cannot match that atom.
            output.Write(code.Span[copied..body[start].Offset]);
            output.WriteByte(IsTuple);
            Write(LabelTag, failedTuple.Value);
            WriteInput(input, 0);
            output.WriteByte(IsTaggedTuple);
            Write(LabelTag, fallback.Value);
            WriteInput(input, TupleTypeBits);
            Write(BeamWire.UnsignedTag, size.Value);
            Write(AtomTag, tag.Value);
            copied = body[start + 1].Offset;

            return true;
        }

        bool HasLineInAnyArm(List<DisjointEntryArm> arms) =>
            arms.Any(arm => body.Skip(arm.Body).Take(arm.End - arm.Body)
                .Any(instruction => instruction.Definition.Opcode == Line));

        int ReadTerminalEntryBody(DisjointEntryArm arm, BeamOperand selector)
        {
            // A select edge already proves that X0 equals its integer key.
            if (arm.Literal.Kind == BeamOperandParameters.Integer && IsBareX0(selector)
                && arm.Body + 2 == arm.End
                && body[arm.Body] is
                {
                    Definition.Opcode: Move,
                    Operands: [var literal, var destination]
                }
                && literal == arm.Literal && destination == selector
                && body[arm.Body + 1].Definition.Opcode == Return)
            {
                return arm.Body + 1;
            }

            return arm.Body;
        }

        Dictionary<Term, Term> GroupIdenticalTerminalArms(DisjointEntryArm[] ordered, BeamOperand selector)
        {
            var representatives = new Dictionary<Term, Term>();
            var distinct = new List<DisjointEntryArm>();
            foreach (DisjointEntryArm arm in ordered)
            {
                int start = ReadTerminalEntryBody(arm, selector);
                ReadOnlySpan<byte> instructions = code.Span[body[start].Offset..body[arm.End].Offset];
                DisjointEntryArm? same = null;
                foreach (DisjointEntryArm previous in distinct)
                {
                    int previousStart = ReadTerminalEntryBody(previous, selector);
                    if (instructions.SequenceEqual(code.Span[body[previousStart].Offset..body[previous.End].Offset]))
                    {
                        same = previous;
                        break;
                    }
                }

                if (same is null)
                {
                    distinct.Add(arm);
                }
                else
                {
                    representatives.Add(arm.Key, same.Key);
                }
            }

            return representatives;
        }

        DisjointEntryArm? ReadArm(int start, BeamOperand selector, string kind)
        {
            if (start + 2 >= body.Count || body[start] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } failure, var input, var key]
                } || input != selector || key.Kind != kind || !labels!.TryGetValue((int)failure.Value, out int end)
                || end <= start + 2 || body[end - 1].Definition.Opcode is not
                (Return or TailLocalCall or TailLocalCallWithFrame or TailExternalCall or TailExternalCallWithFrame))
            {
                return null;
            }

            int cursor = start + 1;
            while (cursor < end && IsCompanionTest(body[cursor], failure))
            {
                cursor++;
            }

            if (cursor == end
                || !transfers![(int)failure.Value].Order().SequenceEqual(Enumerable.Range(start, cursor - start))
                || body.Skip(cursor).Take(end - cursor).Any(instruction =>
                    instruction.Definition.Opcode is FunctionInfo or EndCode))
            {
                return null;
            }

            for (int index = cursor; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == Label
                    && transfers[(int)instruction.Operands[0].Value].Any(reference => reference < cursor || reference >= end))
                {
                    return null;
                }

                if (instruction.Definition.Opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame)
                    && instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                        && (!labels.TryGetValue((int)operand.Value, out int target) || target < cursor || target >= end)))
                {
                    return null;
                }
            }

            Term keyValue = kind == BeamOperandParameters.Atom
                ? new Atom(names[(int)key.Value - 1])
                : new Integer(key.Value);

            return new(
                keyValue,
                key,
                start,
                cursor,
                end,
                (int)failure.Value
            );
        }

        bool SameTests(DisjointEntryArm first, DisjointEntryArm second)
        {
            if (first.Body - first.Test != second.Body - second.Test)
            {
                return false;
            }

            for (int offset = 1; offset < first.Body - first.Test; offset++)
            {
                BeamInstruction left = body[first.Test + offset];
                BeamInstruction right = body[second.Test + offset];
                if (left.Definition.Opcode != right.Definition.Opcode
                    || !left.Operands.Skip(1).SequenceEqual(right.Operands.Skip(1)))
                {
                    return false;
                }
            }

            return true;
        }

        void WriteInput(BeamOperand value, int type)
        {
            int? index = value.TypeIndex ?? (type == 0 ? null : GetOperandTypeIndex((type, null, null, null)));
            if (index.HasValue)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
            }
            Write(XRegisterTag, value.Value);
            if (index.HasValue)
            {
                Write(BeamWire.UnsignedTag, index.Value);
            }
        }

        void Write(int tag, System.Numerics.BigInteger value) => BeamCompactWriter.Write(output, tag, value);
    }

    private static bool IsScalarEntrySelection(Pattern pattern) => pattern is
        Pattern.Variable or Pattern.Literal { Value: Atom or Integer }
        or AliasPattern { Left: Pattern.Variable, Right: Pattern.Literal { Value: Atom or Integer } }
        or AliasPattern { Left: Pattern.Literal { Value: Atom or Integer }, Right: Pattern.Variable };

    private static bool UsesOneBooleanArm(
        DisjointEntryArm first,
        DisjointEntryArm second,
        IReadOnlyDictionary<Term, Term> representatives
    ) => first.Key is Atom { Name: ErlangBooleanAtoms.False } && second.Key is Atom { Name: ErlangBooleanAtoms.True }
        && representatives.ContainsKey(second.Key);

    private static bool IsCompanionTest(BeamInstruction instruction, BeamOperand failure) =>
        instruction.Operands is [var target, { Kind: BeamOperandParameters.X }, ..] && target == failure
        && (instruction is
        {
            Definition.Opcode: ExactEqualTest,
            Operands: [_, _, { Kind: BeamOperandParameters.Integer or BeamOperandParameters.Atom }]
        }
            || instruction.Definition.Opcode == IsNil);
}
