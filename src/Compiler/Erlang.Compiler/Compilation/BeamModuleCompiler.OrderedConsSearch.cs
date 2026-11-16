using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
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
            owner.EmitListParts(listRegister, XRegisterTag, headRegister, XRegisterTag, listRegister);
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
