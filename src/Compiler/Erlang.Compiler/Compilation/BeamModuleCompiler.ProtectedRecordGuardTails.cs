using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProtectedRecordGuardTails(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Case
                    {
                        Value: Expr.Call producer,
                        Clauses:
                            [
                            {
                                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom other }, Pattern.Variable payload] }],
                                Guard: null,
                                Body: Expr.Call otherCall
                            },
                            {
                                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom selected }, Pattern.Tuple record] }],
                                Guard: Expr.GuardAlternatives alternatives,
                                Body: Expr.Call selectedCall
                            },
                            {
                                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom rejected }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }],
                                Guard: null,
                                Body: Expr.Call rejectedCall
                            }
                            ]
                    } choice
                } clause
                ]
                || !HasUnconditionalParameters(clause) || other.Equals(selected) || !selected.Equals(rejected)
                || !HasTupleTag(record, out Atom tag)
                || record.Items is not [_, Pattern.Variable address, Pattern.Variable scalar, Pattern.Variable first, Pattern.Variable last]
                || !DistinctBindings(
                    address,
                    scalar,
                    first,
                    last
                )
                || ReadProtectedRecordGuard(alternatives, address.Name, scalar.Name) is not { } guard
                || producer.Arguments is not
                    [
                        Expr.List
                    {
                        Items: [Expr.Tuple { Items: [Expr.Literal key, Expr.Variable value] }],
                        Tail: Expr.Variable list
                    },
                        ..
                    ]
                || selectedCall.Arguments.Count >= FirstReservedXRegister)
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int valueRegister = Array.IndexOf(parameters, value.Name);
            int listRegister = Array.IndexOf(parameters, list.Name);
            if (valueRegister < 0 || listRegister <= 0
                || ReadEntryCall(producer, parameters, preparedOperand: 0) is not { } entry
                || entry.Moves.Any(move => move.Source == 0)
                || ReadEntryCall(
                    selectedCall,
                    [address.Name, scalar.Name, last.Name, first.Name],
                    allowControlTransfer: true
                ) is not { } success
                || selectedCall.Arguments.OfType<Expr.Variable>().Count() != 4
                || selectedCall.Arguments.OfType<Expr.Variable>().Select(variable => variable.Name)
                    .Distinct(StringComparer.Ordinal).Count() != 4
                || ReadEntryCall(rejectedCall, [], allowControlTransfer: true) is not { } onRejected
                || ReadEntryCall(otherCall, ["", "", payload.Name], allowControlTransfer: true) is not { } onOther)
            {
                return false;
            }

            int live = Math.Max(Math.Max(valueRegister, listRegister) + 1, EntryCallLiveCount(entry, parameters));
            const int tupleWords = 3;
            const int consWords = 2;
            EmitFrameAllocation(live, 0, tupleWords + consWords);
            owner.EmitTupleHeader(XRegisterTag, 2, 0);
            owner.LiteralOperand(key.Value);
            owner.Operand(XRegisterTag, valueRegister);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, listRegister);
            owner.Operand(XRegisterTag, 0);
            EmitEntryCall(entry);
            var previous = CallLocation(producer, location);

            int accepted = Label();
            int rejectedRecord = Label();
            int alternative = Label();
            int malformed = Label();
            int matched = Label();
            owner.EmitTupleCheck(0, 2, malformed);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 2);
            bool otherFirst = TermOrder.Compare(other, selected, exact: true) < 0;
            owner.EmitAtomSelection(
                1,
                malformed,
                otherFirst ? other.Name : selected.Name,
                otherFirst ? alternative : matched,
                otherFirst ? selected.Name : other.Name,
                otherFirst ? matched : alternative
            );

            Mark(matched);
            owner.EmitRecordTest(
                rejectedRecord,
                XRegisterTag,
                2,
                tag,
                record.Items.Count
            );
            owner.EmitTuplePart(2, 1, 0);
            owner.EmitTuplePart(2, 2, 1);
            EmitProtectedRecordGuard(
                guard,
                accepted,
                rejectedRecord,
                previous
            );

            Mark(accepted);
            owner.EmitTuplePart(2, 3, 3);
            owner.EmitTuplePart(2, 4, 2);
            EmitEmptyFrameTail(success, previous);
            Mark(rejectedRecord);
            EmitEmptyFrameTail(onRejected, previous);
            Mark(alternative);
            EmitEmptyFrameTail(onOther, previous);
            Mark(malformed);
            owner.EmitLine(new(choice.PreprocessorFile ?? location.File, choice.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);

            return true;
        }

        private (
            Integer ScalarSentinel,
            Integer ScalarMask,
            Term AddressSentinel,
            int Arity,
            Integer TupleMask,
            Expr FirstFold,
            Expr SecondFold
        )? ReadProtectedRecordGuard(Expr.GuardAlternatives alternatives, string address, string scalar)
        {
            if (alternatives.Items is not
                [
                    Expr.Call { Arguments: [Expr.Variable mapped] } map,
                    Expr.Binary { Operator: ErlangOperators.AndAlso, SourceLine: 0 } first,
                    Expr.Binary { Operator: ErlangOperators.AndAlso, SourceLine: 0 } second,
                    Expr.Binary { Operator: ErlangOperators.AndAlso, SourceLine: 0 } third,
                    Expr.Binary { Operator: ErlangOperators.AndAlso, SourceLine: 0 } fourth
                ]
                || map.Function != AutoImportNames.IsMap || mapped.Name != address || !owner.ResolvesErlangBif(map)
                || ReadEquality(first.Left, scalar) is not Integer scalarSentinel
                || ReadEquality(second.Left, scalar) is not Integer repeatedSentinel || !scalarSentinel.Equals(repeatedSentinel)
                || ReadEquality(second.Right, address) is not { } addressSentinel
                || ReadEquality(fourth.Right, address) is not { } repeatedAddress || !addressSentinel.Equals(repeatedAddress)
                || ReadZeroMask(third.Left) is not { Input: Expr.Variable masked } mask || masked.Name != scalar
                || ReadZeroMask(fourth.Left) is not { Input: Expr.Variable otherMask } repeatedMask
                || otherMask.Name != scalar || !mask.Mask.Equals(repeatedMask.Mask)
                || ReadProtectedAddressFold(first.Right, address) is not { } firstFold
                || ReadProtectedAddressFold(third.Right, address) is not { } secondFold
                || firstFold.Arity != secondFold.Arity || !firstFold.Mask.Equals(secondFold.Mask))
            {
                return null;
            }

            return (scalarSentinel, mask.Mask, addressSentinel, firstFold.Arity, firstFold.Mask, firstFold.At, secondFold.At);

            static Term? ReadEquality(Expr expression, string name) => expression is Expr.Binary
            {
                Operator: ErlangOperators.ExactEqual, Left: Expr.Variable variable, Right: var right
            } && variable.Name == name && AttributeLiteral.TryNormalizeConstant(right, out Term literal)
                && literal is Atom or Integer ? literal : null;
        }

        private (int Arity, Integer Mask, Expr At)? ReadProtectedAddressFold(Expr expression, string address)
        {
            if (expression is not Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction
                || ReadTupleArityGuard(conjunction.Left) is not { Value: Expr.Variable value, Arity: >= 2 } checkedTuple
                || value.Name != address || checkedTuple.Arity > CoreAliasDepthLimit
                || conjunction.Left is not Expr.Binary check
                || (check.Left as Expr.Call ?? check.Right as Expr.Call) is not { } size
                || !owner.ResolvesErlangBif(size)
                || ReadZeroMask(conjunction.Right) is not { } test)
            {
                return null;
            }

            Expr input = test.Input;
            for (int field = checkedTuple.Arity; field > 0; field--)
            {
                if (field > 1)
                {
                    if (input is not Expr.Binary { Operator: ErlangOperators.BitwiseOr } fold || !ReadsField(fold.Right, field))
                    {
                        return null;
                    }

                    input = fold.Left;
                }
                else if (!ReadsField(input, field))
                {
                    return null;
                }
            }

            return (checkedTuple.Arity, test.Mask, conjunction.Right);

            bool ReadsField(Expr read, int field) => read is Expr.Call
            {
                Arguments: [Expr.Literal { Value: Integer position }, Expr.Variable source]
            } call && source.Name == address && position.Value == field
                && IsElementGuardCall(call) && owner.ResolvesErlangBif(call);
        }

        private void EmitProtectedRecordGuard(
            (Integer ScalarSentinel, Integer ScalarMask, Term AddressSentinel, int Arity, Integer TupleMask, Expr FirstFold, Expr SecondFold) guard,
            int accepted,
            int rejected,
            BeamSourceLocation previous
        )
        {
            const int address = 0;
            const int scalar = 1;
            const int retainedInputs = 3;
            int first = retainedInputs;
            int second = first + 1;
            int third = second + 1;
            int fourth = third + 1;
            int check = Label();
            EmitMapTest(address, check);
            JumpTo(accepted);
            Mark(check);

            EmitGuardValueEquality(scalar, guard.ScalarSentinel, first);
            EmitTupleConjunction(first, guard.FirstFold);
            EmitGuardValueEquality(scalar, guard.ScalarSentinel, second);
            EmitGuardValueEquality(address, guard.AddressSentinel, third);
            EmitGuardBinary(ErlangOperators.And, second, third);

            int thirdFailure = Label();
            EmitScalarMask(third, thirdFailure);
            EmitTupleConjunction(third, guard.SecondFold, thirdFailure);

            int fourthFailure = Label();
            int joined = Label();
            EmitScalarMask(fourth, fourthFailure);
            EmitGuardValueEquality(address, guard.AddressSentinel, fourth + 1);
            EmitGuardBinary(ErlangOperators.And, fourth, fourth + 1);
            JumpTo(joined);
            Mark(fourthFailure);
            owner.EmitConstant(Term.A(ErlangBooleanAtoms.False), fourth);
            Mark(joined);
            EmitGuardBinary(ErlangOperators.Or, third, fourth);
            EmitGuardBinary(ErlangOperators.Or, second, third);
            EmitGuardedResultEquality(
                first,
                Term.A(ErlangBooleanAtoms.False),
                accepted,
                boolean: true
            );
            EmitGuardedResultEquality(
                second,
                Term.A(ErlangBooleanAtoms.True),
                rejected,
                boolean: true
            );

            void EmitScalarMask(int result, int failure)
            {
                EmitGuardMask(
                    failure,
                    result,
                    scalar,
                    guard.ScalarMask,
                    result,
                    typed: false
                );
                EmitGuardValueEquality(
                    result,
                    Term.I(0),
                    result,
                    typedInteger: true
                );
            }

            void EmitTupleConjunction(int result, Expr at, int? rejected = null)
            {
                int wrongArity = Label();
                int badOperand = rejected ?? Label();
                int checkedValue = Label();
                int done = Label();
                int fold = result + 1;
                int field = fold + 1;
                owner.Op(IsTuple, LabelTag, badOperand);
                owner.Operand(XRegisterTag, address);
                owner.Op(TestArity, LabelTag, wrongArity);
                owner.Operand(XRegisterTag, address);
                owner.Operand(BeamWire.UnsignedTag, guard.Arity);
                owner.EmitTuplePart(address, 0, fold);
                int union = owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseOr, 2);
                for (int index = 1; index < guard.Arity; index++)
                {
                    owner.EmitTuplePart(address, index, field);
                    owner.Op(GcBifTwo, LabelTag, badOperand);
                    owner.Operand(BeamWire.UnsignedTag, field + 1);
                    owner.Operand(BeamWire.UnsignedTag, union);
                    if (index == 1)
                    {
                        owner.Operand(XRegisterTag, fold);
                    }
                    else
                    {
                        owner.EmitTypedRegister(fold, IntegerTypeBits);
                    }

                    owner.Operand(XRegisterTag, field);
                    owner.Operand(XRegisterTag, fold);
                }

                if (at is Expr.Binary { SourceLine: not 0 } source)
                {
                    var current = new BeamSourceLocation(previous.File, source.SourceLine);
                    if (current != previous)
                    {
                        owner.EmitLine(current);
                        previous = current;
                    }
                }

                EmitGuardMask(
                    0,
                    fold + 1,
                    fold,
                    guard.TupleMask,
                    fold,
                    typed: true
                );
                EmitGuardValueEquality(
                    fold,
                    Term.I(0),
                    fold,
                    typedInteger: true
                );
                JumpTo(checkedValue);
                Mark(wrongArity);
                owner.EmitConstant(Term.A(ErlangBooleanAtoms.False), fold);
                Mark(checkedValue);
                EmitGuardBinary(ErlangOperators.And, result, fold);
                JumpTo(done);
                Mark(badOperand);
                owner.EmitConstant(Term.A(ErlangBooleanAtoms.False), result);
                Mark(done);
            }

            void EmitGuardBinary(string name, int left, int right)
            {
                owner.Op(BifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, name, 2));
                owner.EmitTypedRegister(left, AtomTypeBits);
                owner.EmitTypedRegister(right, AtomTypeBits);
                owner.Operand(XRegisterTag, left);
            }

            void EmitGuardMask(
                int failure,
                int live,
                int input,
                Integer mask,
                int output,
                bool typed
            )
            {
                owner.Op(GcBifTwo, LabelTag, failure);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.BitwiseAnd, 2));
                if (typed)
                {
                    owner.EmitTypedRegister(input, IntegerTypeBits);
                }
                else
                {
                    owner.Operand(XRegisterTag, input);
                }

                owner.LiteralOperand(mask);
                owner.Operand(XRegisterTag, output);
            }
        }
    }
}
