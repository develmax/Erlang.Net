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
            owner.MoveRegister(
                XRegisterTag,
                arity,
                YRegisterTag,
                0
            );
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

        private bool TryEmitCallResultConstruction(
            FunctionDefinition function,
            bool preparedNil = false,
            bool validateOnly = false
        )
        {
            if (preparedNil && (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil }],
                        Body: Expr.List { Items: [Expr.Tuple], Tail: null }
                    }
                ]))
            {
                return false;
            }

            EntryCall? emptyTail = null;
            Clause clause;
            if (function.Clauses is [{ Guard: null } single])
            {
                clause = single;
            }
            else if (!owner.ModuleOptimizationDisabled && function.Clauses is
                [
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Nil } or Pattern.List { Items.Count: 0, Tail: null }],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr fallbackValue] } fallback
                },
                { Guard: null, Patterns: [Pattern.List { Items: [Erlang.Pattern.Variable { }], Tail: null }] } nonempty
                ]
                && function.Arity == 1 && function.Name == fallback.Function
                && AttributeLiteral.TryNormalizeConstant(fallbackValue, out Term defaultList)
                && defaultList is Cons { Tail: Nil })
            {
                emptyTail = ReadEntryCall(fallback, []);
                if (emptyTail is null)
                {
                    return false;
                }

                clause = nonempty;
            }
            else
            {
                return false;
            }

            var head = clause.Patterns is [Pattern.List { Items: [Pattern.Variable element], Tail: null }]
                && element.Name != VariableScopeNames.Wildcard ? element : null;
            Clause bindingsClause = head is null ? clause : clause with { Patterns = [head] };
            IReadOnlyList<Expr> expressions = clause.Body is Expr.Sequence sequence ? sequence.Items : [clause.Body];
            string? resultBinding = null;
            Expr.Call? call = null;
            EntryCall? closureCall = null;
            EntryCall? headCall = null;
            EntryCall? preparedCall = null;
            var result = expressions[^1];
            if (expressions is [.., Expr.Match
                {
                    Pattern: Pattern.Variable binding,
                    Value: Expr.Call { DynamicRemote: false } invoke
                }, _] && AcceptsCall(invoke))
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
            if (head is null && (emptyTail is not null || !testsNil && !HasUnconditionalParameters(clause)))
            {
                return false;
            }

            List<EntryCall>? discarded = null;
            if (head is null && expressions.Take(expressions.Count - 1).Any(expression => expression is Expr.Call))
            {
                var bindingsAndResult = new List<Expr>(expressions.Count);
                discarded = [];
                foreach (Expr expression in expressions.Take(expressions.Count - 1))
                {
                    if (expression is Expr.Call prefix)
                    {
                        if (ReadEntryCall(prefix, []) is not { } plan)
                        {
                            return false;
                        }

                        discarded.Add(plan);
                    }
                    else
                    {
                        bindingsAndResult.Add(expression);
                    }
                }

                bindingsAndResult.Add(result);
                expressions = bindingsAndResult;
            }

            if (ReadConstantBindings(bindingsClause, new Expr.Sequence(expressions)) is not { } constants
                || resultBinding is not null && constants.ContainsKey(resultBinding))
            {
                return false;
            }

            Expr.Binary? adjustment = null;
            Integer? increment = null;
            var containers = new List<(bool IsList, Term?[] Items)>();
            if (!ReadConstruction(result) || containers.Count == 0 || call is null
                || result is Expr.RecordConstruction && adjustment is null
                || discarded is not null && closureCall is null)
            {
                return false;
            }

            int entry = 0;
            if (preparedCall is { } prepared)
            {
                call = call with { Module = prepared.Module };
            }

            if ((closureCall?.Module ?? call.Module) is null && !entries.TryGetValue((call.Function, call.Arguments.Count), out entry))
            {
                return false;
            }

            if (validateOnly)
            {
                return true;
            }

            BeamSourceLocation previous = location;
            int functionInfo = owner.nextLabel - 2;
            int emptyLabel = emptyTail is null ? 0 : Label();
            if (headCall is not null)
            {
                // OTP emits the checked cons branch before the nil fallback.
                owner.EmitNonemptyList(0, emptyTail is null ? functionInfo : emptyLabel);
                owner.EmitListParts(
                    0,
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    2
                );
                owner.EmitNilTest(XRegisterTag, 2, functionInfo);
                EmitFrameAllocation(2, 0);
                EmitEntryCallArguments(headCall);
            }
            else
            {
                if (testsNil && !preparedNil)
                {
                    owner.Op(IsNil, LabelTag, owner.nextLabel - 2);
                    owner.Operand(XRegisterTag, 0);
                }

                if (closureCall?.FunctionArgument is { } functionArgument)
                {
                    if (discarded is null)
                    {
                        owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
                        owner.EmitClosureHeap(0, 0, 1);
                    }
                    else
                    {
                        // The calls clobber X registers. Reserve closure heap only after the last one.
                        EmitFrameAllocation(0, 0);
                        foreach (EntryCall prefix in discarded)
                        {
                            EmitEntryCall(prefix, previous);
                            previous = CallLocation(prefix.Call, previous);
                        }

                        owner.EmitClosureAllocation(0, 0, 1);
                    }

                    owner.EmitDeclaredFunctionReference(
                        functionArgument.Reference,
                        functionArgument.Entry,
                        functionArgument.Destination
                    );
                    EmitEntryCallArguments(closureCall);
                    call = call with { Module = closureCall.Module };
                }
                else
                {
                    EmitFrameAllocation(0, 0);
                    if (preparedCall is { } arguments)
                    {
                        EmitEntryCallArguments(arguments);
                    }
                }
            }

            owner.EmitCallLine(call, previous);
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
            if (adjustment is not null)
            {
                var adjustmentLocation = CallLocation(call, previous);
                owner.EmitArithmeticLine(adjustment.SourceLine, ref adjustmentLocation);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, adjustment.Operator, 2));
                if (call.Module is null && localFunctions.TryGetValue((call.Function, arity), out var callee)
                    && !owner.IsDeclaredNative((call.Function, arity))
                    && callee.Clauses.All(clause => ReturnsNumber(clause.Body)))
                {
                    owner.EmitTypedRegister(0, NumberTypeBits);
                }
                else
                {
                    owner.Operand(XRegisterTag, 0);
                }

                owner.LiteralOperand(increment!);
                owner.Operand(XRegisterTag, 0);
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
            if (emptyTail is not null)
            {
                Mark(emptyLabel);
                owner.EmitNilTest(XRegisterTag, 0, functionInfo);
                EmitEntryCallArguments(emptyTail);
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, emptyTail.Call.Arguments.Count);
                owner.Operand(LabelTag, entries[(emptyTail.Call.Function, emptyTail.Call.Arguments.Count)]);
            }

            return true;

            bool AcceptsCall(Expr.Call candidate)
            {
                if (head is not null)
                {
                    if (owner.ModuleOptimizationDisabled && candidate.Arguments.Count != 1
                        || !candidate.Arguments.OfType<Expr.Variable>().Any(argument => argument.Name == head.Name)
                        || ReadEntryCall(candidate, [VariableScopeNames.Wildcard, head.Name]) is not { } plannedHead
                        || plannedHead.Module != candidate.Module)
                    {
                        return false;
                    }

                    headCall = plannedHead;

                    return true;
                }

                if (candidate.Arguments.Count == 0)
                {
                    return true;
                }

                if (preparedNil && ReadEntryCall(candidate, []) is { } closed
                    && closed.Constants.Count == candidate.Arguments.Count)
                {
                    preparedCall = closed;

                    return true;
                }

                if (ReadEntryCall(candidate, [], allowFunctionArgument: true) is { FunctionArgument: not null } planned)
                {
                    closureCall = planned;

                    return true;
                }

                return false;
            }

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

            static bool ReturnsNumber(Expr expression) => expression switch
            {
                Expr.Binary { Operator: ErlangOperators.Plus or ErlangOperators.Minus or ErlangOperators.Multiply } => true,
                Expr.Sequence { Items.Count: > 0 } sequence => ReturnsNumber(sequence.Items[^1]),
                Expr.Block block => ReturnsNumber(block.Body),
                _ => false
            };

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

                if (call is null && owner.SsaTypeInferenceEnabled && !owner.ModuleOptimizationDisabled
                    && expression is Expr.Binary arithmetic
                    && ReadCallArithmetic(arithmetic) is { } operands
                    && TryResolveOrdinaryCallTarget(operands.Call, out _))
                {
                    // A single owner passes from the call through arithmetic to the enclosing constructors.
                    call = operands.Call;
                    adjustment = arithmetic;
                    increment = operands.Increment;

                    return true;
                }

                IReadOnlyList<Expr>? children = expression switch
                {
                    Expr.Tuple tuple => tuple.Items,
                    Expr.RecordConstruction record when ReferenceEquals(record, result) => RecordExpressions.Create(record).Items,
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

            static (Expr.Call Call, Integer Increment)? ReadCallArithmetic(Expr.Binary expression) => expression switch
            {
                {
                    Operator: ErlangOperators.Plus or ErlangOperators.Minus,
                    Left: Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 0 } call,
                    Right: Expr.Literal { Value: Integer value }
                } => (call, value),
                {
                    Operator: ErlangOperators.Plus,
                    Left: Expr.Literal { Value: Integer value },
                    Right: Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 0 } call
                } => (call, value),
                _ => null
            };
        }
    }
}
