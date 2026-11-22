using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitOrderedTupleConsLookup(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 2
                || function.Clauses.Count is not (2 or 4))
            {
                return false;
            }

            bool exhaustive = function.Clauses.Count == 4;
            Clause descend = function.Clauses[exhaustive ? 1 : 0];
            if (ReadLookupPair(descend) is not { } next
                || !HasLookupComparison(
                    descend.Guard,
                    ErlangOperators.Greater,
                    next.Key,
                    next.Index
                )
                || descend.Body is not Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable key, Expr.Variable tail]
                } recur
                || recur.Function != function.Name || key.Name != next.Key.Name || tail.Name != next.Tail.Name
                || !entries.TryGetValue((recur.Function, 2), out int entry))
            {
                return false;
            }

            Clause found = function.Clauses[exhaustive ? 2 : 1];
            if (ReadLookupPair(found) is not { } hit
                || (exhaustive ? found.Guard is not null
                    : !HasLookupComparison(
                        found.Guard,
                        ErlangOperators.NumericEqual,
                        hit.Key,
                        hit.Index
                    )))
            {
                return false;
            }

            Term? constantHit = null;
            Atom? wrapper = null;
            if (found.Body is not Expr.Variable selected || selected.Name != hit.Value.Name)
            {
                if (found.Body is Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom tag }, Expr.Variable payload]
                    } && payload.Name == hit.Value.Name)
                {
                    wrapper = tag;
                }
                else if (!AttributeLiteral.TryNormalizeConstant(found.Body, out constantHit))
                {
                    return false;
                }
            }

            Term? miss = null;
            if (exhaustive)
            {
                Clause less = function.Clauses[0];
                Clause empty = function.Clauses[3];
                if (ReadLookupPair(less) is not { } incomplete
                    || !HasLookupComparison(
                        less.Guard,
                        ErlangOperators.Less,
                        incomplete.Key,
                        incomplete.Index
                    )
                    || empty is not { Guard: null, Patterns: [global::Erlang.Pattern.Variable, var nil] }
                    || ListEntryTest(nil) != IsNil
                    || !AttributeLiteral.TryNormalizeConstant(less.Body, out miss)
                    || !AttributeLiteral.TryNormalizeConstant(empty.Body, out Term end)
                    || !miss.Equals(end) || constantHit is not null && constantHit.Equals(miss))
                {
                    return false;
                }
            }

            // Keep the original list in X1 until both rejecting shape checks
            // have finished. With a nil/miss clause no later test can reject the MFA.
            int nonCons = exhaustive ? Label() : functionInfo;
            int smallerOrEmpty = exhaustive ? Label() : functionInfo;
            int foundArm = Label();
            int indexRegister = exhaustive ? 1 : 4;
            owner.EmitNonemptyList(1, nonCons);
            owner.EmitListParts(
                1,
                XRegisterTag,
                2,
                XRegisterTag,
                3
            );
            owner.EmitTupleCheck(2, 2, functionInfo);
            owner.EmitTuplePart(2, 0, indexRegister);
            if (exhaustive)
            {
                owner.Op(IsGreaterOrEqual, LabelTag, smallerOrEmpty);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, indexRegister);
            }

            owner.Op(IsLess, LabelTag, foundArm);
            owner.Operand(XRegisterTag, indexRegister);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                3,
                XRegisterTag,
                1
            );
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);

            Mark(foundArm);
            if (!exhaustive)
            {
                owner.Op(NumericEqualTest, LabelTag, functionInfo);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, indexRegister);
            }

            if (constantHit is not null)
            {
                owner.EmitConstant(constantHit, 0);
            }
            else
            {
                if (wrapper is not null)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                    owner.Operand(BeamWire.UnsignedTag, 3);
                }

                owner.EmitTuplePart(2, 1, 0);
                if (wrapper is not null)
                {
                    owner.EmitTupleHeader(XRegisterTag, 2);
                    owner.LiteralOperand(wrapper);
                    owner.Operand(XRegisterTag, 0);
                }
            }

            owner.instructions.WriteByte(Return);
            if (exhaustive)
            {
                Mark(nonCons);
                owner.EmitNilTest(XRegisterTag, 1, functionInfo);
                Mark(smallerOrEmpty);
                owner.EmitConstant(miss!, 0);
                owner.instructions.WriteByte(Return);
            }

            return true;
        }

        private static (Pattern.Variable Key, Pattern.Variable Index, Pattern.Variable Value, Pattern.Variable Tail)?
            ReadLookupPair(Clause clause)
        {
            if (clause.Patterns is not
                [
                    Pattern.Variable key,
                    Pattern.List
                {
                    Items: [Pattern.Tuple { Items: [Pattern.Variable index, Pattern.Variable value] }],
                    Tail: Pattern.Variable tail
                }
                ]
                || !DistinctBindings(new[] { key, index, value, tail }
                    .Where(binding => binding.Name != VariableScopeNames.Wildcard).ToArray()))
            {
                return null;
            }

            return (key, index, value, tail);
        }

        private static bool HasLookupComparison(
            Expr? guard,
            string comparison,
            Pattern.Variable key,
            Pattern.Variable index
        ) =>
            key.Name != VariableScopeNames.Wildcard && index.Name != VariableScopeNames.Wildcard
            && guard is Expr.Binary { Left: Expr.Variable search, Right: Expr.Variable head } binary
            && binary.Operator == comparison && search.Name == key.Name && head.Name == index.Name;

        private bool TryEmitOrderedConsSearch(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses is not
                [
                {
                    Patterns:
                        [
                            Erlang.Pattern.Variable key,
                            Erlang.Pattern.List { Items: [Erlang.Pattern.Variable head], Tail: Erlang.Pattern.Variable tail }
                        ],
                    Guard: Expr.Binary { Operator: ErlangOperators.Greater, Left: Expr.Variable greaterKey, Right: Expr.Variable greaterHead },
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable nextKey, Expr.Variable nextTail] } recur
                },
                {
                    Patterns:
                        [
                            Erlang.Pattern.Variable secondKey,
                            Erlang.Pattern.List { Items: [Erlang.Pattern.Variable secondHead], Tail: Erlang.Pattern.Variable secondTail }
                        ],
                    Guard: Expr.Binary { Operator: ErlangOperators.Less, Left: Expr.Variable lessKey, Right: Expr.Variable lessHead },
                    Body: var miss
                },
                {
                    Patterns:
                        [
                            Erlang.Pattern.Variable hitKey,
                            Erlang.Pattern.List { Items: [Erlang.Pattern.Variable hitHead], Tail: Erlang.Pattern.Variable hitTail }
                        ],
                    Guard: null, Body: var hit
                },
                {
                    Patterns: [Erlang.Pattern.Variable, var empty],
                    Guard: null, Body: var exhausted
                }
                ]
                || !DistinctBindings(key, head, tail)
                || secondKey.Name == VariableScopeNames.Wildcard || secondHead.Name == VariableScopeNames.Wildcard
                || !HasDistinctVariablesForCons(secondKey, secondHead, secondTail)
                || !HasDistinctVariablesForCons(hitKey, hitHead, hitTail)
                || greaterKey.Name != key.Name || greaterHead.Name != head.Name
                || lessKey.Name != secondKey.Name || lessHead.Name != secondHead.Name
                || nextKey.Name != key.Name || nextTail.Name != tail.Name
                || recur.Function != function.Name || !entries.TryGetValue((recur.Function, 2), out int entry)
                || ListEntryTest(empty) != IsNil
                || !AttributeLiteral.TryNormalizeConstant(miss, out Term missedValue)
                || !AttributeLiteral.TryNormalizeConstant(exhausted, out Term exhaustedValue)
                || !missedValue.Equals(exhaustedValue)
                || !AttributeLiteral.TryNormalizeConstant(hit, out Term foundValue)
                || foundValue.Equals(missedValue))
            {
                return false;
            }

            const int keyRegister = 0;
            const int listRegister = 1;
            const int headRegister = 2;
            int compare = Label();
            bool checkNil = !HasProperListEntry(function, listRegister);
            int nonCons = Label();
            int notFound = checkNil ? Label() : nonCons;

            owner.EmitNonemptyList(listRegister, nonCons);
            owner.EmitListParts(
                listRegister,
                XRegisterTag,
                headRegister,
                XRegisterTag,
                listRegister
            );
            owner.Op(IsLess, LabelTag, compare);
            owner.Operand(XRegisterTag, headRegister);
            owner.Operand(XRegisterTag, keyRegister);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);

            Mark(compare);
            owner.Op(IsGreaterOrEqual, LabelTag, notFound);
            owner.Operand(XRegisterTag, keyRegister);
            owner.Operand(XRegisterTag, headRegister);
            owner.EmitConstant(foundValue, 0);
            owner.instructions.WriteByte(Return);

            Mark(nonCons);
            if (checkNil)
            {
                owner.EmitNilTest(XRegisterTag, listRegister, functionInfo);
                Mark(notFound);
            }

            owner.EmitConstant(missedValue, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private static bool HasDistinctVariablesForCons(
            Erlang.Pattern.Variable key,
            Erlang.Pattern.Variable head,
            Erlang.Pattern.Variable tail
        ) =>
            (key.Name == VariableScopeNames.Wildcard || key.Name != head.Name && key.Name != tail.Name)
            && (head.Name == VariableScopeNames.Wildcard || head.Name != tail.Name);
    }
}
