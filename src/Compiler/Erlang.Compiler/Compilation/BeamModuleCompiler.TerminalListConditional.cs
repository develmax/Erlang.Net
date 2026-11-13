using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TerminalListBranch(
            Expr.Call[] Calls,
            string? SavedHead,
            int? TupleField,
            Expr.Binary? TailOperand);

        private sealed record MeasuredListTypes(string Measurement, string Limit, long Lower);

        private static MeasuredListTypes? ReadMeasuredListTypes(Expr.If conditional, string? measurement, Expr? entryGuard)
        {
            if (measurement is null || entryGuard is not Expr.Binary
                {
                    Operator: ErlangOperators.Greater, Left: Expr.Variable limit,
                    Right: Expr.Literal { Value: Integer lower }
                }
                || lower.Value < 0 || lower.Value > MaximumBitSize
                || conditional.Clauses is not [var first, var greater, var less, var equal]
                || first.Guard is not Expr.Binary
                {
                    Operator: ErlangOperators.AndAlso,
                    Left: Expr.Binary
                    {
                        Operator: ErlangOperators.Greater,
                        Left: Expr.Binary { Operator: ErlangOperators.Minus } difference,
                        Right: Expr.Literal { Value: Integer threshold }
                    },
                    Right: Expr.Binary
                    {
                        Operator: ErlangOperators.LessOrEqual, Left: Expr.Variable bounded,
                        Right: Expr.Literal { Value: Integer upper }
                    }
                }
                || bounded.Name != limit.Name || threshold.Value != upper.Value
                || !Matches(difference, ErlangOperators.Minus, measurement, limit.Name)
                || !Matches(greater.Guard, ErlangOperators.Greater, measurement, limit.Name)
                || !Matches(less.Guard, ErlangOperators.Less, measurement, limit.Name)
                || !Matches(equal.Guard, ErlangOperators.ExactEqual, measurement, limit.Name))
            {
                return null;
            }

            return new(measurement, limit.Name, (long)lower.Value);

            static bool Matches(Expr? expr, string op, string left, string right) =>
                expr is Expr.Binary { Left: Expr.Variable lhs, Right: Expr.Variable rhs } comparison
                && comparison.Operator == op && lhs.Name == left && rhs.Name == right;
        }

        private void EmitMeasuredListGuard(MeasuredListTypes types, Expr guard, int branch, int failure)
        {
            var test = (Expr.Binary)guard;
            if (branch == 0)
            {
                var differenceTest = (Expr.Binary)test.Left;
                var limitTest = (Expr.Binary)test.Right;
                EmitMeasuredSubtraction(types.Measurement, types.Limit, types, failure, 0);
                owner.Op(IsLess, LabelTag, failure);
                owner.LiteralOperand(((Expr.Literal)differenceTest.Right).Value);
                owner.EmitTypedRegister(0, NumberTypeBits, upper: MaximumBitSize - types.Lower);
                owner.Op(IsGreaterOrEqual, LabelTag, failure);
                owner.LiteralOperand(((Expr.Literal)limitTest.Right).Value);
                owner.EmitTypedRegister(variables[types.Limit], NumberTypeBits, lower: types.Lower,
                    registerTag: YRegisterTag);
                return;
            }

            var (left, right) = EntryComparisonOperands(test);
            owner.Op(GuardComparisonOpcode(test), LabelTag, failure);
            EmitMeasuredOperand(((Expr.Variable)left).Name, types, branch == 3);
            EmitMeasuredOperand(((Expr.Variable)right).Name, types, branch == 3);
        }

        private void EmitMeasuredSubtraction(string left, string right, MeasuredListTypes types, int failure, int destination)
        {
            owner.Op(GcBifTwo, LabelTag, failure);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Minus, 2));
            EmitMeasuredOperand(left, types);
            EmitMeasuredOperand(right, types);
            owner.Operand(XRegisterTag, destination);
        }

        private void EmitMeasuredOperand(string name, MeasuredListTypes types, bool boundedLimit = false)
        {
            int slot = variables[name];
            if (name == types.Measurement)
            {
                owner.EmitTypedRegister(slot, IntegerTypeBits, 0, MaximumBitSize, registerTag: YRegisterTag);
            }
            else if (boundedLimit)
            {
                owner.EmitTypedRegister(slot, NumberTypeBits, types.Lower, MaximumBitSize, registerTag: YRegisterTag);
            }
            else
            {
                owner.EmitTypedRegister(slot, AnyTypeBits, registerTag: YRegisterTag);
            }
        }

        private bool TryEmitTerminalListConditional(Expr.If conditional, string? measurement, Expr? entryGuard)
        {
            if (owner.receiveScratchRegisters is not null || decoderSlotCount != 0 || deferredPairMembers.Count != 0)
            {
                return false;
            }

            var plans = conditional.Clauses.Select(clause => ReadTerminalListBranch(clause.Body)).ToArray();
            if (plans.All(plan => plan is null))
            {
                return false;
            }

            var types = ReadMeasuredListTypes(conditional, measurement, entryGuard);
            var original = variables;
            int branchBase = nextSlot;
            for (int index = 0; index < plans.Length; index++)
            {
                Clause clause = conditional.Clauses[index];
                variables = new(original, StringComparer.Ordinal);
                nextSlot = branchBase;
                if (clause.Guard is Expr.Literal { Value: Atom { Name: "true" } })
                {
                    EmitBranch(clause.Body, plans[index]);
                    variables = original;
                    nextSlot = branchBase;
                    return true;
                }

                int next = Label();
                if (types is { } measured)
                {
                    EmitMeasuredListGuard(measured, clause.Guard!, index, next);
                }
                else
                {
                    Guard(clause.Guard, next);
                }
                EmitBranch(clause.Body, plans[index]);
                Mark(next);
            }
            owner.EmitLine(conditional.SourceLine == 0 ? location
                : new BeamSourceLocation(conditional.PreprocessorFile ?? location.File, conditional.SourceLine));
            owner.instructions.WriteByte(IfEnd);
            variables = original;
            nextSlot = branchBase;
            return true;

            void EmitBranch(Expr body, TerminalListBranch? plan)
            {
                if (plan is null)
                {
                    EmitExportingBranch(body, [], branchContinuation: []);
                    owner.EmitFrameReturn(frameSize);
                }
                else
                {
                    EmitTrimmedListBranch(plan, types);
                }
            }
        }

        private TerminalListBranch? ReadTerminalListBranch(Expr body)
        {
            IReadOnlyList<Expr> items = body is Expr.Sequence sequence ? sequence.Items : [body];
            if (items[^1] is not Expr.List { Items: [var head] } list)
            {
                return null;
            }

            var calls = new List<Expr.Call>();
            int? field = null;
            string? saved = null;
            foreach (Expr expression in items.SkipLast(1))
            {
                if (expression is Expr.Call prefix)
                {
                    calls.Add(prefix);
                }
                else if (ReferenceEquals(expression, items[^2])
                    && ReadSplitBinaryBinding(expression) is { } split
                    && head is Expr.Variable projected
                    && Array.FindIndex(split.Fields, item => item.Name == projected.Name) is int index && index >= 0)
                {
                    calls.Add(split.Call);
                    field = index;
                }
                else
                {
                    return null;
                }
            }

            if (head is Expr.Call call)
            {
                if (list.Tail is not null)
                {
                    return null;
                }
                calls.Add(call);
            }
            else if (field is null)
            {
                if (head is not Expr.Variable variable || !variables.ContainsKey(variable.Name))
                {
                    return null;
                }
                saved = variable.Name;
            }

            string[] savedParameters = SavedParameters();
            Expr.Binary? tailOperand = null;
            if (list.Tail is not null)
            {
                if (saved is null || list.Tail is not Expr.Call
                    {
                        Arguments: [Expr.Variable, Expr.Binary
                        {
                            Operator: ErlangOperators.Minus, Left: Expr.Variable left, Right: Expr.Variable right
                        } difference]
                    } tail
                    || !variables.ContainsKey(left.Name) || !variables.ContainsKey(right.Name)
                    || ReadEntryCall(tail, [], savedParameters, preparedOperand: 1) is null)
                {
                    return null;
                }
                tailOperand = difference;
                calls.Add(tail);
            }
            if (calls.Count == 0 || calls.Any(call => ReadEntryCall(call, [], savedParameters) is null
                    && !((field is not null || tailOperand is not null) && ReferenceEquals(call, calls[^1]))))
            {
                return null;
            }
            return new(calls.ToArray(), saved, field, tailOperand);
        }

        private string[] SavedParameters()
        {
            var parameters = Enumerable.Repeat(VariableScopeNames.Wildcard, nextSlot).ToArray();
            foreach (var (name, slot) in variables)
            {
                parameters[slot] = name;
            }
            return parameters;
        }

        private void EmitTrimmedListBranch(TerminalListBranch branch, MeasuredListTypes? types)
        {
            int remaining = frameSize;
            var previous = location;
            for (int index = 0; index < branch.Calls.Length; index++)
            {
                Expr.Call call = branch.Calls[index];
                if (index == branch.Calls.Length - 1 && branch.TailOperand is { } operand)
                {
                    if (types is { } measured && operand.Left is Expr.Variable left && operand.Right is Expr.Variable right
                        && left.Name == measured.Limit && right.Name == measured.Measurement)
                    {
                        owner.EmitArithmeticLine(operand.SourceLine, ref previous);
                        EmitMeasuredSubtraction(left.Name, right.Name, measured, 0, 1);
                    }
                    else
                    {
                        EmitArithmetic(operand, null, destination: 1, previousLine: previous);
                    }
                    if (operand.SourceLine != 0)
                    {
                        previous = new(previous.File, operand.SourceLine);
                    }
                    Load(variables[((Expr.Variable)call.Arguments[0]).Name]);
                }
                else
                {
                    LoadCallArguments(call.Arguments, null, orderSimpleOperands: true);
                }
                var live = branch.Calls.Skip(index + 1).SelectMany(later => later.Arguments)
                    .SelectMany(LocalFunctionEntries.Walk).OfType<Expr.Variable>()
                    .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
                if (branch.SavedHead is { } head)
                {
                    live.Add(head);
                }
                remaining = TrimTerminalListFrame(remaining, live);
                owner.EmitCallLine(call, previous);
                Call(call.Module, call.Function, call.Arguments.Count);
                previous = CallLocation(call, previous);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, branch.SavedHead is null || branch.TailOperand is not null ? 1 : 0);
            if (branch.TupleField is int field)
            {
                owner.EmitTuplePart(0, field, 0);
            }
            if (branch.SavedHead is { } name)
            {
                owner.Op(PutList, YRegisterTag, variables[name]);
            }
            else
            {
                owner.Op(PutList, XRegisterTag, 0);
            }
            if (branch.TailOperand is null)
            {
                owner.LiteralOperand(Nil.Value);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(remaining);
        }

        private int TrimTerminalListFrame(int frame, HashSet<string> live)
        {
            // The planning pass has no frame size yet.
            if (frame < nextSlot)
            {
                return frame;
            }

            int trim = frame - live.Count;
            var occupied = live.Select(name => variables[name]).ToHashSet();
            var holes = Enumerable.Range(trim, live.Count).Where(slot => !occupied.Contains(slot)).Reverse().GetEnumerator();
            foreach (string name in live.OrderBy(name => variables[name]))
            {
                int source = variables[name];
                int destination = source;
                if (source < trim)
                {
                    holes.MoveNext();
                    destination = holes.Current;
                    owner.MoveRegister(YRegisterTag, source, YRegisterTag, destination);
                }
                variables[name] = destination - trim;
            }
            if (trim > 0)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, trim);
                owner.Operand(BeamWire.UnsignedTag, live.Count);
                nextSlot = live.Count;
            }
            return live.Count;
        }
    }
}
