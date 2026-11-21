using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAtomListTail(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses is [var leaf]
                && ReadAtomEntryReturn(leaf) is { } single)
            {
                owner.Op(ExactEqualTest, LabelTag, functionInfo);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(single.Selector);
                EmitAtomEntryReturn(single.Suffix);

                return true;
            }

            if (function.Clauses is [var callClause] && ReadAtomEntryCall(callClause) is { } singleCall)
            {
                owner.Op(ExactEqualTest, LabelTag, functionInfo);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(singleCall.Selector);
                singleCall.Emit();

                return true;
            }

            if (function.Clauses.Count is not (2 or 3)
                || function.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [
                        Pattern.Literal { Value: Atom selector },
                        Pattern.Variable value,
                        Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest }
                    ],
                    Body: Expr.Call
                    {
                        Module: null,
                        DynamicRemote: false,
                        Arguments: [Expr.Variable passedHead, Expr.Variable passedValue, Expr.Variable passedRest]
                    } call
                }
                || !DistinctBindings(value, head, rest)
                || passedHead.Name != head.Name || passedValue.Name != value.Name || passedRest.Name != rest.Name
                || !entries.TryGetValue((call.Function, call.Arguments.Count), out int target))
            {
                return false;
            }

            Clause regular = function.Clauses[0];
            Atom key;
            Atom? alias = null;
            Action emitRegular;
            if (ReadAtomEntryReturn(regular) is { } returned)
            {
                key = returned.Selector;
                emitRegular = () => EmitAtomEntryReturn(returned.Suffix);
                if (function.Clauses.Count == 3)
                {
                    if (ReadAtomEntryReturn(function.Clauses[1]) is not { } other
                        || other.Selector.Equals(key) || !Equals(returned.Suffix, other.Suffix))
                    {
                        return false;
                    }

                    alias = other.Selector;
                }
            }
            else if (function.Clauses.Count == 2 && ReadAtomEntryCall(regular) is { } called)
            {
                (key, emitRegular) = called;
            }
            else
            {
                return false;
            }

            if (selector.Equals(key) || selector.Equals(alias))
            {
                return false;
            }

            var atomOrder = Comparer<Atom>.Create((a, b) => TermOrder.Compare(a, b, exact: true));
            Atom[] keys = alias is null ? [key, selector] : [key, alias, selector];
            Array.Sort(keys, atomOrder);
            // The shared return and checked tail are visited by their first
            // ascending switch edge. Terminal blocks follow in reverse postorder.
            bool tailFirst = !keys[0].Equals(selector);
            int firstTarget = Label();
            int secondTarget = Label();
            int listTarget = tailFirst ? firstTarget : secondTarget;
            int returnTarget = tailFirst ? secondTarget : firstTarget;

            owner.Op(SelectValue, XRegisterTag, 0);
            owner.Operand(LabelTag, functionInfo);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, keys.Length * 2);
            foreach (Atom atom in keys)
            {
                owner.LiteralOperand(atom);
                owner.Operand(LabelTag, atom.Equals(selector) ? listTarget : returnTarget);
            }

            if (!tailFirst)
            {
                Mark(returnTarget);
                emitRegular();
            }

            Mark(listTarget);
            owner.EmitNonemptyList(2, functionInfo);
            owner.EmitListParts(
                2,
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(LabelTag, target);

            if (tailFirst)
            {
                Mark(returnTarget);
                emitRegular();
            }

            return true;
        }

        private (Atom Selector, Action Emit)? ReadAtomEntryCall(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Variable value, Pattern.Variable unused]
                }
                || !DistinctBindings(value)
                || value.Name == unused.Name)
            {
                return null;
            }

            string[] parameters = [VariableScopeNames.Wildcard, value.Name, unused.Name];
            if (clause.Body is Expr.Call { Module: null, DynamicRemote: false } call
                && ReadEntryCall(call, parameters) is { Module: null } target)
            {
                return (key, () => EmitRegisterTailCall(target));
            }

            if (clause.Body is not Expr.Sequence { Items.Count: 2 }
                || ReadStraightCallSequence(clause.Body, parameters) is not { First.Module: null } chain
                || chain.Continuation is not [{ Call.Module: null, ListDestination: null }])
            {
                return null;
            }

            return (key, () => EmitEntryCallChain(
                chain.First,
                parameters,
                chain.Continuation,
                chain.RelocateBeforeTrim
            ));
        }

        private static (Atom Selector, Integer? Suffix)? ReadAtomEntryReturn(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Atom selector }, Pattern.Variable value, Pattern.Variable unused]
                }
                || !DistinctBindings(value)
                || value.Name == unused.Name)
            {
                return null;
            }

            if (clause.Body is Expr.Variable returned && returned.Name == value.Name)
            {
                return (selector, null);
            }

            if (clause.Body is Expr.Tuple
                {
                    Items: [Expr.Variable field, Expr.Literal { Value: Integer suffix }]
                }
                && field.Name == value.Name)
            {
                return (selector, suffix);
            }

            return null;
        }

        private void EmitAtomEntryReturn(Integer? suffix)
        {
            if (suffix is null)
            {
                owner.Op(Move, XRegisterTag, 1);
                owner.Operand(XRegisterTag, 0);
            }
            else
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Op(PutTuple, XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(XRegisterTag, 1);
                owner.LiteralOperand(suffix);
            }

            owner.instructions.WriteByte(Return);
        }
    }
}
