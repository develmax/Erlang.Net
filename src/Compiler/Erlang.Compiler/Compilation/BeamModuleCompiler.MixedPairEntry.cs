using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record MixedPairTail(
            Term? Constant,
            EntryCall? Direct,
            (Expr.Call Call, CallArgumentConstructions Plan)? Constructed
        );

        private sealed record MixedPairAtom(
            Atom Key,
            MixedPairTail? Tail,
            Term? Format,
            Expr.Call? Bif
        );

        private bool TryEmitMixedPairEntry(FunctionDefinition function, int functionInfo)
        {
            int arity = function.Arity;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || arity <= 0
                || arity + 1 >= FirstReservedXRegister || function.Clauses.Count < 4
                || nextSlot != 0 || variables.Count != 0 || owner.receiveScratchRegisters is not null)
            {
                return false;
            }

            var clauses = function.Clauses;
            Atom? remainingAtom = null;
            MixedPairTail? remaining = null;
            MixedPairTail? guardedList = null;
            IEnumerable<Clause> pairs;
            Clause first = clauses[0];
            if (first is { Guard: null, Patterns: [Pattern.Literal { Value: Atom scalar }, ..] }
                && HasUnconditionalParameters(first with { Patterns = first.Patterns.Skip(1).ToArray() })
                && AttributeLiteral.TryNormalizeConstant(first.Body, out var prefix))
            {
                remainingAtom = scalar;
                remaining = new(prefix, null, null);
                pairs = clauses.Skip(1);
            }
            else
            {
                Clause empty = clauses[^2];
                Clause list = clauses[^1];
                if (arity != 1 || empty.Guard is not null || ListEntryTest(empty.Patterns[0]) != IsNil
                    || list.Patterns is not [Pattern.Variable listBinding]
                    || list.Guard is not Expr.Call
                    {
                        Module: null or ErlangModuleNames.Module,
                        Function: AutoImportNames.IsList,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable tested]
                    } guard
                    || !owner.ResolvesErlangBif(guard) || tested.Name != listBinding.Name
                    || ReadMixedPairTail(empty.Body, [VariableScopeNames.Wildcard]) is not { } nilTail
                    || ReadMixedPairTail(list.Body, [listBinding.Name]) is not { } listTail)
                {
                    return false;
                }

                remaining = nilTail;
                guardedList = listTail;
                pairs = clauses.Take(clauses.Count - 2);
            }

            var atoms = new List<(Atom Key, Clause Clause, Pattern.Variable Payload)>();
            Clause? nested = null;
            Pattern.Variable? nestedPayload = null;
            Pattern.Variable? left = null;
            Pattern.Variable? right = null;
            foreach (Clause clause in pairs)
            {
                if (clause.Guard is not null
                    || clause.Patterns[0] is not Pattern.Tuple { Items: [Pattern.Variable payload, var mode] })
                {
                    return false;
                }

                var fields = new List<Pattern> { payload };
                fields.AddRange(clause.Patterns.Skip(1));
                if (mode is Pattern.Literal { Value: Atom key })
                {
                    if (!HasUnconditionalParameters(clause with { Patterns = fields }))
                    {
                        return false;
                    }

                    atoms.Add((key, clause, payload));
                    continue;
                }

                if (nested is not null || mode is not Pattern.Tuple { Items: [Pattern.Variable head, Pattern.Variable tail] })
                {
                    return false;
                }

                fields.Add(head);
                fields.Add(tail);
                if (!HasUnconditionalParameters(clause with { Patterns = fields })
                    || LocalFunctionEntries.Walk(clause.Body).Any(expression =>
                        expression is Expr.Try or Expr.Catch or Expr.Receive or Expr.Maybe or Expr.Fun
                            or Expr.RecordAccess or Expr.RecordUpdate))
                {
                    return false;
                }

                nested = clause;
                nestedPayload = payload;
                left = head;
                right = tail;
            }

            if (nested is null || nestedPayload is null || left is null || right is null
                || atoms.Count < 2 || atoms.Select(arm => arm.Key).Distinct().Count() != atoms.Count)
            {
                return false;
            }

            var candidates = new List<Pattern> { nestedPayload, left, right };
            candidates.AddRange(nested.Patterns.Skip(1));
            if (ReadUsedEntryParameters(nested with { Patterns = candidates }) is not { } used)
            {
                return false;
            }

            bool nestedReadsPayload = used.Contains(nestedPayload.Name);
            int payloadRegister = nestedReadsPayload ? arity : arity + 1;
            int modeRegister = nestedReadsPayload ? arity + 1 : arity;
            var atomPlans = new List<MixedPairAtom>();
            foreach (var (key, clause, payload) in atoms)
            {
                if (payload.Name == VariableScopeNames.Wildcard)
                {
                    return false;
                }

                var inputs = MixedPairSiblingBindings(clause);
                inputs.Add(payload.Name, payloadRegister);
                if (ReadMixedPairTail(clause.Body, NestedTupleInputNames(inputs)) is { } tail)
                {
                    atomPlans.Add(new(
                        key,
                        tail,
                        null,
                        null
                    ));
                    continue;
                }

                if (ReadMixedPairAtomReport(clause.Body, payload.Name) is not { } report)
                {
                    return false;
                }

                atomPlans.Add(new(
                    key,
                    null,
                    report.Format,
                    report.Bif
                ));
            }

            var nestedInputs = MixedPairSiblingBindings(nested);
            if (nestedReadsPayload)
            {
                nestedInputs.Add(nestedPayload.Name, payloadRegister);
            }

            if (left.Name != VariableScopeNames.Wildcard)
            {
                nestedInputs.Add(left.Name, 0);
            }

            if (right.Name != VariableScopeNames.Wildcard)
            {
                nestedInputs.Add(right.Name, modeRegister);
            }

            MixedPairTail? nestedTail = ReadMixedPairTail(nested.Body, NestedTupleInputNames(nestedInputs));
            if (nestedTail is null)
            {
                return false;
            }

            int notTuple = Label();
            int atomEntry = Label();
            int fallback = guardedList is null ? functionInfo : Label();
            owner.Op(IsTuple, LabelTag, notTuple);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TestArity, LabelTag, fallback);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            if (nestedReadsPayload)
            {
                owner.EmitTuplePart(0, 0, payloadRegister);
            }

            owner.EmitTuplePart(0, 1, modeRegister);
            owner.Op(IsTuple, LabelTag, atomEntry);
            owner.Operand(XRegisterTag, modeRegister);
            owner.Op(TestArity, LabelTag, fallback);
            owner.Operand(XRegisterTag, modeRegister);
            owner.Operand(BeamWire.UnsignedTag, 2);

            // The inputs occupying X0 and the selector register are already
            // valid roots before either nested projection overwrites them.
            if (nestedTail.Constructed is { } prepared)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, prepared.Plan.Constructions.Sum(item => item.HeapWords));
                owner.Operand(BeamWire.UnsignedTag, prepared.Plan.Live);
            }

            if (used.Contains(left.Name))
            {
                owner.EmitTuplePart(modeRegister, 0, 0);
            }

            if (used.Contains(right.Name))
            {
                owner.EmitTuplePart(modeRegister, 1, modeRegister);
            }

            EmitMixedPairTail(nestedTail, heapChecked: nestedTail.Constructed is not null);

            Mark(atomEntry);
            if (!nestedReadsPayload)
            {
                owner.EmitTuplePart(0, 0, payloadRegister);
            }

            var order = Comparer<Atom>.Create((x, y) => TermOrder.Compare(x, y, exact: true));
            var targets = atomPlans.OrderByDescending(arm => arm.Key, order)
                .Select(arm => (Arm: arm, Label: Label())).ToArray();
            owner.EmitMembershipSelect(
                modeRegister,
                fallback,
                targets.Reverse().Select(target => ((Term)target.Arm.Key, target.Label))
            );
            foreach (var (arm, target) in targets)
            {
                Mark(target);
                if (arm.Tail is { } tail)
                {
                    EmitMixedPairTail(tail);
                }
                else
                {
                    EmitMixedPairAtomReport(arm, payloadRegister);
                }
            }

            Mark(notTuple);
            if (remainingAtom is not null)
            {
                owner.EmitAtomTest(0, remainingAtom.Name, functionInfo);
                EmitMixedPairTail(remaining!);
            }
            else
            {
                owner.EmitNilTest(XRegisterTag, 0, fallback);
                EmitMixedPairTail(remaining!);
                Mark(fallback);
                owner.EmitNonemptyList(0, functionInfo);
                EmitMixedPairTail(guardedList!);
            }

            owner.nestedEntryLabelOrder.Add((function.Name, function.Arity));

            return true;
        }

        private static Dictionary<string, int> MixedPairSiblingBindings(Clause clause) =>
            clause.Patterns.Skip(1).Cast<Pattern.Variable>()
                .Select((variable, index) => (variable.Name, Register: index + 1))
                .Where(input => input.Name != VariableScopeNames.Wildcard)
                .ToDictionary(input => input.Name, input => input.Register, StringComparer.Ordinal);

        private MixedPairTail? ReadMixedPairTail(Expr expression, string[] inputs)
        {
            if (AttributeLiteral.TryNormalizeConstant(expression, out Term literal))
            {
                return new(literal, null, null);
            }

            Expr body = expression is Expr.Binary { Operator: ErlangOperators.Append } append
                ? new Expr.Call(ErlangModuleNames.Module, ErlangOperators.Append, [append.Left, append.Right])
                {
                    SourceLine = append.SourceLine
                }
                : expression;
            if (body is not Expr.Call call)
            {
                return null;
            }

            if (ReadEntryCall(call, inputs) is { } direct)
            {
                return new(null, direct, null);
            }

            return ReadEntryTailConstruction(call, inputs, allowOrdinary: true) is { } constructed
                ? new(null, null, constructed)
                : null;
        }

        private void EmitMixedPairTail(MixedPairTail tail, bool heapChecked = false)
        {
            if (tail.Constant is { } literal)
            {
                owner.EmitConstant(literal, 0);
                owner.instructions.WriteByte(Return);
            }
            else if (tail.Direct is { } direct)
            {
                EmitEntryCallArguments(direct);
                EmitRegisterTailTarget(direct.Call with { Module = direct.Module }, direct.Call.Arguments.Count);
            }
            else if (tail.Constructed is { } constructed)
            {
                EmitCallArgumentConstructions(constructed.Plan, -1, heapChecked: heapChecked);
                EmitRegisterTailTarget(constructed.Call, constructed.Call.Arguments.Count);
            }
        }

        private (Term Format, Expr.Call? Bif)? ReadMixedPairAtomReport(Expr body, string payload)
        {
            if (body is not Expr.Tuple
                {
                    Items: [var text, Expr.List { Tail: null, Items: [Expr.Variable first, ..] } args]
                }
                || first.Name != payload || !AttributeLiteral.TryNormalizeConstant(text, out Term format))
            {
                return null;
            }

            if (args.Items.Count == 1)
            {
                return (format, null);
            }

            return args.Items is [_, Expr.Call { Arguments: [Expr.Variable read] } bif]
                && read.Name == payload && owner.ResolvesErlangBif(bif) && ReadUnaryBifOpcode(bif.Function) == BifOne
                    ? (format, bif) : null;
        }

        private void EmitMixedPairAtomReport(MixedPairAtom arm, int payload)
        {
            Expr.Call? bif = arm.Bif;
            if (bif is not null)
            {
                owner.EmitCallLine(bif, location);
                owner.Op(BifOne, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, bif.Function, 1));
                owner.Operand(XRegisterTag, payload);
                owner.Operand(XRegisterTag, 0);
            }

            int listWords = bif is null ? ConsHeapWords : 2 * ConsHeapWords;
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 + listWords);
            owner.Operand(BeamWire.UnsignedTag, payload + 1);
            if (bif is not null)
            {
                owner.Op(PutList, XRegisterTag, 0);
                owner.LiteralOperand(Nil.Value);
                owner.Operand(XRegisterTag, 0);
            }

            owner.Op(PutList, XRegisterTag, payload);
            if (bif is null)
            {
                owner.LiteralOperand(Nil.Value);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }

            owner.Operand(XRegisterTag, 0);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(arm.Format!);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
        }
    }
}
