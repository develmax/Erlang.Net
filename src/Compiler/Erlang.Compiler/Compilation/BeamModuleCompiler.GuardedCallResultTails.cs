using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedCallResultTails(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Clauses is not
                [
                {
                    Guard: null, Body: Expr.Case
                    {
                        Value: Expr.Call producer,
                        Clauses: [
                        {
                            Guard: null, Patterns: [Pattern.Tuple
                            {
                                Items: [Pattern.Literal { Value: Atom other },
                                    Pattern.Variable result]
                            }], Body: Expr.Call otherCall
                        },
                        {
                            Patterns: [Pattern.Tuple
                            {
                                Items: [Pattern.Literal { Value: Atom selected },
                                    Pattern.Tuple payload]
                            }], Guard: Expr.GuardAlternatives
                            {
                                Items: [Expr.Binary
                                {
                                    Operator: ErlangOperators.AndAlso, Left: var size, Right: Expr.Binary
                                    {
                                        Operator: ErlangOperators.ExactEqual, Left: Expr.Call
                                        {
                                            Arguments: [Expr.Literal { Value: Integer index }, Expr.Variable guardInput]
                                        } element,
                                        Right: Expr.Literal marker
                                    }
                                }, Expr.Binary
                                {
                                    Operator: ErlangOperators.ExactEqual,
                                    Left: Expr.Variable fallbackInput, Right: Expr.Literal fallbackValue
                                }]
                            }, Body: Expr.Call selectedCall
                        },
                        {
                            Guard: null, Patterns: [Pattern.Tuple
                            {
                                Items: [Pattern.Literal { Value: Atom rejected },
                                    Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                            }], Body: Expr.Call rejectedCall
                        }
                            ]
                    } decision
                } clause
                ]
                || other.Equals(selected) || !selected.Equals(rejected) || !HasTupleTag(payload, out var payloadTag)
                || !IsElementGuardCall(element) || fallbackInput.Name != guardInput.Name
                || ReadTupleArityGuard(size) is not { Value: Expr.Variable sized, Arity: var arity }
                || sized.Name != guardInput.Name || index.Value < 1 || index.Value > arity
                || selectedCall.Arguments.Count >= FirstReservedXRegister)
            {
                return false;
            }

            var parameters = new string[function.Arity];
            for (int register = 0; register < parameters.Length; register++)
            {
                switch (clause.Patterns[register])
                {
                    case Pattern.Variable variable when variable.Name != VariableScopeNames.Wildcard
                        && !parameters.Contains(variable.Name):
                        parameters[register] = variable.Name;
                        break;
                    case Pattern.Literal { Value: Atom or Integer }:
                        break;
                    default:
                        return false;
                }
            }

            var bindings = payload.Items.Skip(1).OfType<Pattern.Variable>()
                .Where(variable => variable.Name != VariableScopeNames.Wildcard).ToArray();
            var fields = Enumerable.Range(1, payload.Items.Count - 1).ToArray();
            int[] checkedFields = fields.Where(field => payload.Items[field] is Pattern.Literal).ToArray();
            int guardField = fields.FirstOrDefault(field =>
                payload.Items[field] is Pattern.Variable variable && variable.Name == guardInput.Name);
            if (checkedFields is not [int checkedField] || guardField == 0 || bindings.Length != bindings.DistinctBy(v => v.Name).Count()
                || bindings.Any(variable => parameters.Contains(variable.Name))
                || parameters.Contains(result.Name) || result.Name == VariableScopeNames.Wildcard
                || fields.Any(field => payload.Items[field] is not (Erlang.Pattern.Variable or Erlang.Pattern.Literal)))
            {
                return false;
            }

            var resultRegisterNames = new string[3];
            resultRegisterNames[2] = result.Name;
            var guardedRegisterNames = new string[Math.Max(4, selectedCall.Arguments.Count)];
            guardedRegisterNames[0] = guardInput.Name;
            var projections = new List<(int Field, int Register)>();
            var arguments = selectedCall.Arguments.ToArray();
            foreach (int field in fields)
            {
                if (payload.Items[field] is not Pattern.Variable bound || bound.Name == guardInput.Name
                    || bound.Name == VariableScopeNames.Wildcard)
                {
                    continue;
                }

                int register = Array.FindIndex(arguments, argument =>
                    argument is Expr.Variable argumentName && argumentName.Name == bound.Name);
                if (register < 0)
                {
                    continue;
                }

                if (register is 0 or 2)
                {
                    register = Enumerable.Range(1, guardedRegisterNames.Length - 1)
                        .FirstOrDefault(candidate => candidate != 2 && guardedRegisterNames[candidate] is null, -1);
                }

                if (register < 0 || guardedRegisterNames[register] is not null)
                {
                    return false;
                }

                guardedRegisterNames[register] = bound.Name;
                projections.Add((field, register));
            }

            int consArgument = -1;
            EntryConstructedArgument? construction = null;
            for (int argument = 0; argument < producer.Arguments.Count; argument++)
            {
                if (producer.Arguments[argument] is Expr.List { Items: [var head], Tail: Expr.Variable tail }
                    && AttributeLiteral.TryNormalizeConstant(head, out var headValue) && Array.IndexOf(parameters, tail.Name) is int source
                    && source >= 0 && consArgument < 0)
                {
                    consArgument = argument;
                    construction = new(argument, false, [(null, headValue), (source, null)]);
                }
            }

            if (ReadEntryCall(producer, parameters, preparedOperand: consArgument) is not { } entry
                || entry.Moves.Any(move => move.Source == consArgument)
                || ReadEntryCall(selectedCall, guardedRegisterNames, allowControlTransfer: true) is not { } success
                || ReadEntryCall(rejectedCall, [], allowControlTransfer: true) is not { } onRejected
                || ReadEntryCall(otherCall, resultRegisterNames, allowControlTransfer: true) is not { } onOther
                || selectedCall.Arguments.Count >= FirstReservedXRegister)
            {
                return false;
            }

            int functionFailure = owner.nextLabel - 2;
            foreach (int register in Enumerable.Range(0, clause.Patterns.Count))
            {
                if (clause.Patterns[register] is Pattern.Literal literal)
                {
                    EmitGuardedResultEquality(register, literal.Value, functionFailure);
                }
            }

            int live = Math.Max(EntryCallLiveCount(entry, parameters), construction?.Operands[1].Source + 1 ?? 0);
            EmitFrameAllocation(live, 0, construction?.HeapWords ?? 0);
            EmitEntryCall(entry with { ConstructedArgument = construction });
            var previous = CallLocation(producer, location);

            int guarded = Label();
            int failed = Label();
            int alternative = Label();
            owner.EmitTupleCheck(0, 2, failed);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 2);
            if (TermOrder.Compare(other, selected, exact: true) < 0)
            {
                owner.EmitAtomSelection(
                    1,
                    failed,
                    other.Name,
                    alternative,
                    selected.Name,
                    guarded
                );
            }
            else
            {
                owner.EmitAtomSelection(
                    1,
                    failed,
                    selected.Name,
                    guarded,
                    other.Name,
                    alternative
                );
            }

            Mark(guarded);
            int reject = Label();
            owner.EmitRecordTest(
                reject,
                XRegisterTag,
                2,
                payloadTag,
                payload.Items.Count
            );
            owner.EmitTuplePart(2, checkedField, 0);
            EmitGuardedResultEquality(0, ((Pattern.Literal)payload.Items[checkedField]).Value, reject);
            owner.EmitTuplePart(2, guardField, 0);
            EmitGuardedTupleCheck(
                arity,
                (int)index.Value,
                marker.Value,
                fallbackValue.Value,
                reject
            );
            foreach (var (field, register) in projections)
            {
                owner.EmitTuplePart(2, field, register);
            }

            EmitEmptyFrameTail(success, previous);
            Mark(reject);
            EmitEmptyFrameTail(onRejected, previous);
            Mark(alternative);
            EmitEmptyFrameTail(onOther, previous);
            Mark(failed);
            owner.EmitLine(new(decision.PreprocessorFile ?? location.File, decision.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }

        private void EmitGuardedResultEquality(
            int register,
            Term value,
            int failure,
            bool boolean = false
        )
        {
            owner.Op(ExactEqualTest, LabelTag, failure);
            if (boolean)
            {
                owner.EmitTypedRegister(register, AtomTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, register);
            }

            owner.LiteralOperand(value);
        }

        private void EmitGuardedTupleCheck(
            int arity,
            int index,
            Term marker,
            Term fallbackValue,
            int rejected,
            int elementRegister = 3
        )
        {
            int failed = Label();
            int join = Label();
            int success = Label();
            EmitBif(
                BifOne,
                AutoImportNames.TupleSize,
                1,
                failed
            );
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            EmitBif(
                BifTwo,
                ErlangOperators.ExactEqual,
                2,
                0
            );
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                lower: 0,
                upper: MaxTupleArity
            );
            owner.Operand(BeamWire.IntegerTag, arity);
            owner.Operand(XRegisterTag, 1);
            EmitBif(
                BifTwo,
                AutoImportNames.Element,
                2,
                failed
            );
            owner.Operand(BeamWire.IntegerTag, index);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(XRegisterTag, elementRegister);
            EmitComparison(elementRegister, marker, elementRegister);
            EmitBif(
                BifTwo,
                ErlangOperators.And,
                2,
                0
            );
            owner.EmitTypedRegister(1, AtomTypeBits);
            owner.EmitTypedRegister(elementRegister, AtomTypeBits);
            owner.Operand(XRegisterTag, 1);
            JumpTo(join);
            Mark(failed);
            owner.EmitConstant(Term.A("false"), 1);
            Mark(join);
            EmitComparison(0, fallbackValue, elementRegister);
            EmitGuardedResultEquality(
                1,
                Term.A("false"),
                success,
                boolean: true
            );
            EmitGuardedResultEquality(
                elementRegister,
                Term.A("true"),
                rejected,
                boolean: true
            );
            Mark(success);

            void EmitBif(
                byte opcode,
                string name,
                int arguments,
                int failure
            )
            {
                owner.Op(opcode, LabelTag, failure);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, name, arguments));
            }

            void EmitComparison(int register, Term value, int output)
            {
                EmitBif(
                    BifTwo,
                    ErlangOperators.ExactEqual,
                    2,
                    0
                );
                owner.Operand(XRegisterTag, register);
                owner.LiteralOperand(value);
                owner.Operand(XRegisterTag, output);
            }
        }
    }
}
