using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCallWithRetainedArgument(FunctionDefinition function)
        {
            if (function.Arity < 2 || function.Clauses is not
                [
                    { Guard: null, Body: Expr.Tuple { Items: [Expr.Call sourceCall, Expr.Variable retained] } } clause
                ]
                || !HasUnconditionalParameters(clause)
                || clause.Patterns[^1] is not Pattern.Variable parameter
                || parameter.Name == VariableScopeNames.Wildcard || parameter.Name != retained.Name)
            {
                return false;
            }

            Expr.Call call = owner.ResolveImportedCall(sourceCall, entries);
            int arity = function.Arity - 1;
            if (call.DynamicRemote || call.Arguments.Count != arity
                || !call.Arguments.SequenceEqual(clause.Patterns.Take(arity)
                    .Cast<Pattern.Variable>().Select(argument => new Expr.Variable(argument.Name)))
                || call.Arguments.Any(argument => argument is Expr.Variable { Name: VariableScopeNames.Wildcard }))
            {
                return false;
            }

            int entry = 0;
            if (call.Module is null && !entries.TryGetValue((call.Function, arity), out entry)
                || owner.ResolvesErlangBif(call))
            {
                return false;
            }

            EmitFrameAllocation(function.Arity, 1);
            owner.MoveRegister(XRegisterTag, arity, YRegisterTag, 0);
            owner.EmitCallLine(call, location);
            if (call.Module is null)
            {
                owner.EmitFieldCall(arity, entry);
            }
            else
            {
                owner.Op(ExternalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module, call.Function, arity));
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
            owner.EmitFrameReturn(1);

            return true;
        }

        private bool TryEmitCallResultConstruction(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null } clause])
            {
                return false;
            }

            var head = clause.Patterns is [Pattern.List { Items: [Pattern.Variable element], Tail: null }]
                && element.Name != VariableScopeNames.Wildcard ? element : null;
            Clause bindingsClause = head is null ? clause : clause with { Patterns = [head] };
            IReadOnlyList<Expr> expressions = clause.Body is Expr.Sequence sequence ? sequence.Items : [clause.Body];
            string? resultBinding = null;
            Expr.Call? call = null;
            var result = expressions[^1];
            if (expressions is [.., Expr.Match { Pattern: Pattern.Variable binding,
                Value: Expr.Call { DynamicRemote: false } invoke }, _] && AcceptsCall(invoke))
            {
                if (binding.Name == VariableScopeNames.Wildcard
                    || bindingsClause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == binding.Name))
                {
                    return false;
                }

                resultBinding = binding.Name;
                call = invoke;
                expressions = expressions.Take(expressions.Count - 1).ToArray();
            }

            bool testsNil = clause.Patterns is [Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil }];
            if (!testsNil && head is null && !HasUnconditionalParameters(clause))
            {
                return false;
            }

            if (ReadConstantBindings(bindingsClause, new Expr.Sequence(expressions)) is not { } constants
                || resultBinding is not null && constants.ContainsKey(resultBinding))
            {
                return false;
            }

            var containers = new List<(bool IsList, Term?[] Items)>();
            if (!ReadConstruction(result) || containers.Count == 0 || call is null)
            {
                return false;
            }

            int entry = 0;
            if (call.Module is null && !entries.TryGetValue((call.Function, call.Arguments.Count), out entry))
            {
                return false;
            }

            if (head is not null)
            {
                int failure = owner.nextLabel - 2;
                owner.EmitNonemptyList(0, failure);
                owner.EmitListParts(0, XRegisterTag, 1, XRegisterTag, 2);
                owner.EmitNilTest(XRegisterTag, 2, failure);
                EmitFrameAllocation(2, 0);
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            }
            else
            {
                if (testsNil)
                {
                    owner.Op(IsNil, LabelTag, owner.nextLabel - 2);
                    owner.Operand(XRegisterTag, 0);
                }
                EmitFrameAllocation(0, 0);
            }

            owner.EmitCallLine(call, location);
            int arity = call.Arguments.Count;
            if (call.Module is null)
            {
                owner.EmitFieldCall(arity, entry);
            }
            else
            {
                owner.Op(ExternalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module, call.Function, arity));
            }
            int heap = containers.Sum(container => container.IsList ? ConsHeapWords * container.Items.Length : container.Items.Length + 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, heap);
            owner.Operand(BeamWire.UnsignedTag, 1);
            foreach (var (isList, items) in containers)
            {
                if (isList)
                {
                    for (int index = items.Length - 1; index >= 0; index--)
                    {
                        owner.instructions.WriteByte(PutList);
                        Operand(items[index]);
                        Operand(index == items.Length - 1 ? Nil.Value : null);
                        owner.Operand(XRegisterTag, 0);
                    }
                }
                else
                {
                    owner.Op(PutTuple, XRegisterTag, 0);
                    owner.instructions.WriteByte(ExtendedListOperand);
                    owner.Operand(BeamWire.UnsignedTag, items.Length);
                    foreach (Term? item in items)
                    {
                        Operand(item);
                    }
                }
            }

            owner.EmitFrameReturn(0);
            return true;

            bool AcceptsCall(Expr.Call candidate) => head is null
                ? candidate.Arguments.Count == 0
                : candidate.Arguments is [Expr.Variable argument] && argument.Name == head.Name
                    && TryResolveOrdinaryCall(candidate, out string? module) && module == candidate.Module;

            void Operand(Term? value)
            {
                if (value is null)
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    owner.LiteralOperand(value);
                }
            }

            bool ReadConstruction(Expr expression)
            {
                if (expression is Expr.Variable variable)
                {
                    return variable.Name == resultBinding;
                }

                if (call is null && expression is Expr.Call { DynamicRemote: false } nestedCall && AcceptsCall(nestedCall))
                {
                    call = nestedCall;
                    return true;
                }

                IReadOnlyList<Expr>? children = expression switch
                {
                    Expr.Tuple tuple => tuple.Items,
                    Expr.List { Tail: null } list => list.Items,
                    _ => null
                };
                if (children is null)
                {
                    return false;
                }

                bool isList = expression is Expr.List;
                var items = new Term?[children.Count];
                int dynamicIndex = -1;
                for (int index = 0; index < items.Length; index++)
                {
                    Expr child = children[index];
                    if (AttributeLiteral.TryNormalizeConstant(child, out Term literal, constants))
                    {
                        items[index] = literal;
                    }
                    else if (dynamicIndex < 0 && (!isList || index == items.Length - 1) && ReadConstruction(child))
                    {
                        dynamicIndex = index;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (dynamicIndex < 0)
                {
                    return false;
                }

                containers.Add((isList, items));
                return true;
            }
        }
    }
}
