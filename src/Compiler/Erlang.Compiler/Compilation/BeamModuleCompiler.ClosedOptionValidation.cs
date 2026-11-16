using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedOptionValidation(FunctionDefinition function)
        {
            if (function.Clauses is not [var first, {
                Patterns: [Pattern.Literal { Value: Atom boundedKey }, Pattern.Variable number],
                Guard: Expr.Binary { Operator: ErlangOperators.AndAlso,
                    Left: Expr.Binary { Operator: ErlangOperators.GreaterOrEqual, Left: Expr.Variable lowerInput,
                        Right: var lowerBound },
                    Right: Expr.Binary { Operator: ErlangOperators.LessOrEqual, Left: Expr.Variable upperInput,
                        Right: var upperBound }
                }, Body: Expr.Literal valid
            }, var boundedFailure, {
                Guard: null, Patterns: [
                    Pattern.Variable { Name: VariableScopeNames.Wildcard },
                    Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ], Body: Expr.Literal fallback
            }] || number.Name == VariableScopeNames.Wildcard || lowerInput.Name != number.Name || upperInput.Name != number.Name
                || !AttributeLiteral.TryNormalizeConstant(lowerBound, out Term minimum) || minimum is not Integer lower
                || !AttributeLiteral.TryNormalizeConstant(upperBound, out Term maximum) || maximum is not Integer upper
                || lower.Value < long.MinValue || upper.Value > long.MaxValue || lower.Value > upper.Value
                || !valid.Value.Equals(fallback.Value)
                || ReadOptionRejection(first, 2) is not { } initial || ReadOptionRejection(boundedFailure, 3) is not { } bounded
                || !bounded.Key.Equals(boundedKey) || initial.Key.Equals(bounded.Key)
                || !HasOnlyLiteralOptionCalls(function, initial.Key, bounded.Key))
            {
                return false;
            }

            int firstError = Label();
            int interval = Label();
            int success = Label();
            int intervalError = Label();
            EmitConfigurationAtomTest(initial.Key, interval);
            owner.EmitAtomSelection(1, firstError, initial.Allowed[0].Name, success, initial.Allowed[1].Name, success);
            Mark(firstError);
            EmitRejection(initial.Key, initial.Context, initial.Error, initial.Call);
            Mark(interval);
            owner.Op(IsGreaterOrEqual, LabelTag, intervalError);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(lower);
            owner.Op(IsLess, LabelTag, success);
            owner.LiteralOperand(upper);
            owner.EmitTypedRegister(1, owner.typeBits[0].Bits, lower: (long)lower.Value);
            owner.instructions.WriteByte(SelectValue);
            owner.EmitTypedRegister(1, owner.typeBits[0].Bits, lower: (long)upper.Value);
            owner.Operand(LabelTag, intervalError);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, bounded.Allowed.Length * 2);
            foreach (Atom atom in bounded.Allowed)
            {
                owner.LiteralOperand(atom);
                owner.Operand(LabelTag, success);
            }

            Mark(success);
            owner.EmitConstant(valid.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(intervalError);
            EmitRejection(bounded.Key, bounded.Context, bounded.Error, bounded.Call);

            return true;

            void EmitRejection(Atom key, Atom context, Atom error, Expr.Call call)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(key.Name)), (XRegisterTag, 1));
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(context.Name)), (XRegisterTag, 0));
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(error.Name)), (XRegisterTag, 0));
                owner.EmitCallLine(call, location);
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Throw, 1));
            }
        }

        private (Atom Key, Atom Context, Atom Error, Atom[] Allowed, Expr.Call Call)? ReadOptionRejection(Clause clause, int count)
        {
            if (clause is not {
                Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Variable input], Guard: { } guard,
                Body: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: AutoImportNames.Throw,
                    Arguments: [Expr.Tuple { Items: [Expr.Literal { Value: Atom error }, Expr.Tuple { Items: [
                        Expr.Literal { Value: Atom context }, Expr.Tuple { Items: [
                            Expr.Literal { Value: Atom rejectedKey }, Expr.Variable rejected
                        ] }
                    ] }] }]
                } call
            } || input.Name == VariableScopeNames.Wildcard || input.Name != rejected.Name || !key.Equals(rejectedKey)
                || !owner.ResolvesErlangBif(call))
            {
                return null;
            }

            var allowed = new HashSet<Atom>();
            if (!ReadGuard(guard) || allowed.Count != count)
            {
                return null;
            }

            return (key, context, error, allowed.OrderBy(atom => atom.Name, StringComparer.Ordinal).ToArray(), call);

            bool ReadGuard(Expr condition) => condition switch
            {
                Expr.Binary { Operator: ErlangOperators.AndAlso } pair => ReadGuard(pair.Left) && ReadGuard(pair.Right),
                Expr.Binary { Operator: ErlangOperators.ExactNotEqual, Left: Expr.Variable value,
                    Right: Expr.Literal { Value: Atom atom } } when value.Name == input.Name => allowed.Add(atom),
                _ => false
            };
        }

        private bool HasOnlyLiteralOptionCalls(FunctionDefinition function, Atom first, Atom second)
        {
            var target = (function.Name, function.Arity);
            if (owner.exportedFunctions.Contains(target))
            {
                return false;
            }

            bool found = false;
            foreach (var expression in localFunctions.Values.SelectMany(definition => LocalFunctionEntries.ClauseExpressions(definition.Clauses))
                .SelectMany(LocalFunctionEntries.Walk))
            {
                if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == target)
                {
                    return false;
                }

                if (expression is Expr.Call { Module: null, DynamicRemote: false } call && (call.Function, call.Arguments.Count) == target)
                {
                    if (call.Arguments[0] is not Expr.Literal { Value: Atom key } || (!key.Equals(first) && !key.Equals(second)))
                    {
                        return false;
                    }

                    found = true;
                }
            }

            return found;
        }
    }
}
