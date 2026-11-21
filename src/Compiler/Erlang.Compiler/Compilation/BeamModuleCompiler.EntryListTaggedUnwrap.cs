using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryListTaggedUnwrap(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Arguments: [Expr.Literal { Value: Atom selector }, Expr.List { Items: var items, Tail: null }]
                        } call,
                        Clauses:
                            [
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple wrapped],
                                Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom resultTag }, Expr.Variable payload] }
                            },
                            { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable returned }
                            ]
                    }
                } clause
                ]
                || function.Arity < 2 || !HasUnconditionalParameters(clause) || !DistinctBindings(fallback)
                || fallback.Name != returned.Name || !IsFresh(clause, fallback)
                || !IsFresh(clause, new Pattern.Variable(payload.Name)) || fallback.Name == payload.Name
                || items.Count != function.Arity
                || !items.OfType<Expr.Variable>().Select(item => item.Name)
                    .SequenceEqual(clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name), StringComparer.Ordinal)
                || !TryResolveOrdinaryCall(call, out string? module, preparedArgument: 1) || module is null
                || ReadTaggedProjection(wrapped, payload.Name) is not { Count: >= 2 } steps)
            {
                return false;
            }

            int failure = Label();
            EmitFrameAllocation(function.Arity, 0, heapWords: function.Arity * ConsHeapWords);
            for (int register = function.Arity - 1; register >= 0; register--)
            {
                owner.Op(PutList, XRegisterTag, register);
                if (register == function.Arity - 1)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, register + 1);
                }

                owner.Operand(XRegisterTag, Math.Max(register, 1));
            }
            owner.EmitConstant(selector, 0);
            owner.EmitCallLine(call, location);
            Call(module, call.Function, 2);

            int input = 0;
            for (int step = 0; step < steps.Count; step++)
            {
                var (tag, arity, index) = steps[step];
                owner.Op(IsTaggedTuple, LabelTag, failure);
                owner.Operand(XRegisterTag, input);
                owner.Operand(BeamWire.UnsignedTag, arity);
                owner.LiteralOperand(tag);
                if (step == steps.Count - 1)
                {
                    owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                    owner.Operand(BeamWire.UnsignedTag, input + 1);
                    owner.EmitTuplePart(input, index, 0);
                }
                else
                {
                    owner.EmitTuplePart(input, index, 1);
                    input = 1;
                }
            }

            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(resultTag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(0);
            Mark(failure);
            owner.EmitFrameReturn(0);

            return true;
        }

        private static List<(Atom Tag, int Arity, int Index)>? ReadTaggedProjection(Pattern.Tuple pattern, string result)
        {
            var steps = new List<(Atom, int, int)>();
            while (pattern.Items.Count >= 2 && pattern.Items[0] is Pattern.Literal { Value: Atom tag })
            {
                var indices = Enumerable.Range(1, pattern.Items.Count - 1).Where(index =>
                    pattern.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard }).ToArray();
                if (indices is not [int field])
                {
                    return null;
                }

                steps.Add((tag, pattern.Items.Count, field));
                if (pattern.Items[field] is Pattern.Variable retained)
                {
                    return retained.Name == result ? steps : null;
                }

                if (pattern.Items[field] is not Pattern.Tuple nested)
                {
                    return null;
                }

                pattern = nested;
            }

            return null;
        }
    }
}
