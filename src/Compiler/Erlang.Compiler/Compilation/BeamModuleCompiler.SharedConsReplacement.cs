using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedConsReplacement(FunctionDefinition function, int functionInfo)
        {
            var clauses = function.Clauses;
            if (function.Arity != 2 || clauses.Count < 4
                || clauses[^2] is not
                {
                    Guard: null,
                    Patterns:
                    [
                        Erlang.Pattern.List { Items: [Erlang.Pattern.Variable head], Tail: Erlang.Pattern.Variable tail },
                        Erlang.Pattern.Variable accumulator
                    ]
                } forward
                || !DistinctBindings(head, tail, accumulator)
                || !IsConsReplacement(forward, tail, accumulator, out Expr item)
                || item is not Expr.Variable variable || variable.Name != head.Name
                || clauses[^1] is not
                {
                    Guard: null, Patterns: [var nil, Erlang.Pattern.Variable emptyAccumulator], Body: Expr.Call empty
                }
                || ListEntryTest(nil) != IsNil
                || ReadEntryCall(empty, [VariableScopeNames.Wildcard, emptyAccumulator.Name]) is not { } finish)
            {
                return false;
            }

            var keys = new HashSet<Term>();
            Term? replacement = null;
            foreach (Clause clause in clauses.Take(clauses.Count - 2))
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns:
                        [
                            Erlang.Pattern.List { Items: [Erlang.Pattern.Literal { Value: Atom or Integer } key],
                                Tail: Erlang.Pattern.Variable rest },
                            Erlang.Pattern.Variable acc
                        ]
                    }
                    || !DistinctBindings(rest, acc) || !keys.Add(key.Value)
                    || !IsConsReplacement(clause, rest, acc, out Expr candidate)
                    || !AttributeLiteral.TryNormalizeConstant(candidate, out Term value)
                    || replacement is not null && !replacement.Equals(value))
                {
                    return false;
                }

                replacement = value;
            }

            int replaced = Label();
            int unchanged = Label();
            int emptyList = Label();
            const int headRegister = 2;
            owner.EmitNonemptyList(0, emptyList);
            owner.EmitListParts(0, XRegisterTag, headRegister, XRegisterTag, 0);
            owner.Op(SelectValue, XRegisterTag, headRegister);
            owner.Operand(LabelTag, unchanged);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, keys.Count * 2);
            foreach (Term key in keys.Order(Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true))))
            {
                owner.LiteralOperand(key);
                owner.Operand(LabelTag, replaced);
            }

            Mark(replaced);
            EmitPrepend(replacement, 2);
            Mark(unchanged);
            EmitPrepend(null, 3);
            Mark(emptyList);
            if (!HasProperListEntry(function, 0))
            {
                owner.EmitNilTest(XRegisterTag, 0, functionInfo);
            }

            EmitEntryCallArguments(finish);
            EmitRegisterTailTarget(empty with { Module = finish.Module }, empty.Arguments.Count);

            return true;

            bool IsConsReplacement(Clause clause, Erlang.Pattern.Variable rest, Erlang.Pattern.Variable acc, out Expr value)
            {
                if (clause.Body is Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                        [
                            Expr.Variable input,
                            Expr.List { Items: [var first], Tail: Expr.Variable suffix }
                        ]
                    } call
                    && call.Function == function.Name && input.Name == rest.Name && suffix.Name == acc.Name)
                {
                    value = first;

                    return true;
                }

                value = null!;

                return false;
            }

            void EmitPrepend(Term? value, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.instructions.WriteByte(PutList);
                if (value is null)
                {
                    owner.Operand(XRegisterTag, headRegister);
                }
                else
                {
                    owner.LiteralOperand(value);
                }

                owner.Operand(XRegisterTag, 1);
                owner.Operand(XRegisterTag, 1);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, entries[(function.Name, 2)]);
            }
        }
    }
}
