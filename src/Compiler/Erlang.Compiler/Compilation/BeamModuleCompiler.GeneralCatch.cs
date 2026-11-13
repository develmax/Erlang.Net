using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool ModuleOptimizationDisabled => sourceModule?.DisableModuleOptimization == true;

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryCatchResultDispatch(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Body: Expr.Sequence
                    { Items: [Expr.Call before, Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Case decision }, Expr.Variable returned] }
                } clause]
                || !HasUnconditionalParameters(clause) || binding.Name != returned.Name
                || binding.Name == VariableScopeNames.Wildcard
                || decision.Value is not Expr.Catch { Operand: Expr.Call protectedCall }
                || !TryResolveOrdinaryCall(protectedCall, out string? protectedModule)
                || decision.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "ok" } ok }],
                    Body: Expr.Literal { Value: Atom { Name: "ok" } }
                },
                {
                    Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom { Name: "error" } error }, Pattern.Variable errorValue] }],
                    Body: Expr.Tuple errorTuple
                },
                {
                    Guard: null, Patterns: [Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom { Name: "EXIT" } exit },
                        Pattern.Tuple { Items: [Pattern.Literal { Value: Atom nestedTag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }] }]
                    }],
                    Body: var specialResult
                },
                {
                    Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom { Name: "EXIT" } }, Pattern.Variable exitValue] }],
                    Body: Expr.Tuple exitTuple
                },
                { Guard: null, Patterns: [Pattern.Variable otherValue], Body: Expr.Tuple otherTuple }
                ]
                || !Wraps(errorTuple, errorValue) || !Wraps(exitTuple, exitValue) || !Wraps(otherTuple, otherValue)
                || !AttributeLiteral.TryNormalizeConstant(specialResult, out Term special))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(binding.Name, StringComparer.Ordinal)
                || parameters.Contains(errorValue.Name, StringComparer.Ordinal)
                || parameters.Contains(exitValue.Name, StringComparer.Ordinal)
                || parameters.Contains(otherValue.Name, StringComparer.Ordinal)
                || ReadEntryCall(before, parameters) is not { } entry)
            {
                return false;
            }

            var sources = new List<int>();
            foreach (var argument in protectedCall.Arguments)
            {
                if (argument is not Expr.Variable variable || variable.Name == VariableScopeNames.Wildcard)
                {
                    return false;
                }

                int register = Array.IndexOf(parameters, variable.Name);
                if (register < 0)
                {
                    return false;
                }

                sources.Add(register);
            }

            int[] saved = sources.AsEnumerable().Reverse().Distinct().ToArray();
            int token = saved.Length;
            int size = token + 1;
            int handler = Label();
            int unwrap = Label();
            int scalar = Label();
            int identity = Label();
            int wrap = Label();
            EmitFrameAllocation(function.Arity, size);
            EmitFrameInitialization(token, size);
            for (int slot = 0; slot < saved.Length; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    saved[slot],
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(entry);
            EnterCatch(token, handler);
            for (int register = sources.Count - 1; register >= 0; register--)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    Array.IndexOf(saved, sources[register]),
                    XRegisterTag,
                    register
                );
            }

            EmitFrameInitialization(0, token);
            owner.EmitCallLine(protectedCall, CallLocation(before, location));
            Call(protectedModule, protectedCall.Function, protectedCall.Arguments.Count);
            EndCatch(token, handler);
            owner.Op(IsTuple, LabelTag, scalar);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TestArity, LabelTag, wrap);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(0, 0, 1);
            owner.Op(SelectValue, XRegisterTag, 1);
            owner.Operand(LabelTag, wrap);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(exit);
            owner.Operand(LabelTag, unwrap);
            owner.LiteralOperand(error);
            owner.Operand(LabelTag, identity);

            Mark(unwrap);
            owner.EmitTuplePart(0, 1, 0);
            EmitDetailRecordTest(
                0,
                2,
                nestedTag,
                wrap
            );
            owner.EmitConstant(special, 0);
            owner.EmitFrameReturn(size);
            Mark(scalar);
            owner.Op(ExactEqualTest, LabelTag, wrap);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(ok);
            Mark(identity);
            owner.EmitFrameReturn(size);
            Mark(wrap);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(error);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(size);

            return true;

            static bool Wraps(Expr.Tuple tuple, Pattern.Variable variable) =>
                variable.Name != VariableScopeNames.Wildcard && tuple.Items is
                    [Expr.Literal { Value: Atom { Name: "error" } }, Expr.Variable value] && value.Name == variable.Name;
        }

        private bool TryEmitDiscardedBranchCatch(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Body: Expr.Case
                    {
                        Value: Expr.Call selector,
                        Clauses: [
                        {
                            Guard: null, Patterns: [Pattern.Tuple
                            { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable ignored] }],
                            Body: Expr.Sequence { Items: [Expr.Catch { Operand: Expr.Call cleanup }, Expr.Literal success] }
                        },
                        { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Literal failure }
                    ]
                    }
                } clause] || !HasUnconditionalParameters(clause)
                || !TryResolveOrdinaryCall(cleanup, out string? cleanupModule))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (parameters.Contains(ignored.Name, StringComparer.Ordinal) && ignored.Name != VariableScopeNames.Wildcard
                || parameters.Contains(fallback.Name, StringComparer.Ordinal) && fallback.Name != VariableScopeNames.Wildcard
                || ReadEntryCall(selector, parameters) is not { } selection)
            {
                return false;
            }

            var sources = new List<int>();
            foreach (var argument in cleanup.Arguments)
            {
                if (argument is not Expr.Variable variable || variable.Name == VariableScopeNames.Wildcard)
                {
                    return false;
                }

                int register = Array.IndexOf(parameters, variable.Name);
                if (register < 0)
                {
                    return false;
                }

                sources.Add(register);
            }

            int[] saved = sources.AsEnumerable().Reverse().Distinct().ToArray();
            int token = saved.Length;
            int size = token + 1;
            int handler = Label();
            int result = Label();
            int other = Label();
            EmitFrameAllocation(function.Arity, size);
            EmitFrameInitialization(token, size);
            for (int slot = 0; slot < saved.Length; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    saved[slot],
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(selection);
            EmitDetailRecordTest(
                0,
                2,
                tag,
                other
            );
            owner.Op(TryEnter, YRegisterTag, token);
            owner.Operand(LabelTag, handler);
            for (int register = sources.Count - 1; register >= 0; register--)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    Array.IndexOf(saved, sources[register]),
                    XRegisterTag,
                    register
                );
            }

            EmitFrameInitialization(0, token);
            owner.EmitCallLine(cleanup, CallLocation(selector, location));
            Call(cleanupModule, cleanup.Function, cleanup.Arguments.Count);
            owner.Op(TryEnd, YRegisterTag, token);
            JumpTo(result);
            Mark(handler);
            owner.Op(TryCase, YRegisterTag, token);
            Mark(result);
            owner.EmitConstant(success.Value, 0);
            owner.EmitFrameReturn(size);
            Mark(other);
            owner.EmitConstant(failure.Value, 0);
            owner.EmitFrameReturn(size);

            return true;
        }

        private bool TryEmitNestedListCallCatch(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable first, Pattern.Variable second, Pattern.Variable saved],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match
                        {
                            Pattern: Pattern.Variable binding,
                            Value: Expr.Call
                            {
                                Arguments:
                                    [
                                        var prefix,
                                        Expr.Call
                                    {
                                        Arguments:
                                            [Expr.List { Tail: null, Items: [Expr.Variable head, Expr.Variable next, var suffix] }]
                                    } inner,
                                        Expr.Variable forwarded
                                    ]
                            } outer
                        },
                            Expr.Catch
                        {
                            Operand: Expr.Call
                            {
                                Arguments:
                                [var format, Expr.List { Tail: null, Items: [Expr.Variable result] }]
                            } finish
                        }
                        ]
                    }
                }
                ]
                || !DistinctBindings(
                    first,
                    second,
                    saved,
                    binding
                )
                || first.Name != head.Name || second.Name != next.Name || saved.Name != forwarded.Name
                || binding.Name != result.Name
                || !AttributeLiteral.TryNormalizeConstant(prefix, out Term prefixValue)
                || !AttributeLiteral.TryNormalizeConstant(suffix, out Term suffixValue)
                || !AttributeLiteral.TryNormalizeConstant(format, out Term formatValue)
                || !TryResolveOrdinaryCall(inner, out string? innerModule, preparedArgument: 0)
                || !TryResolveOrdinaryCall(outer, out string? outerModule, preparedArgument: 1)
                || !TryResolveOrdinaryCall(finish, out string? finishModule, preparedArgument: 1))
            {
                return false;
            }

            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            owner.Op(PutList, XRegisterTag, 1);
            owner.LiteralOperand(new Cons(suffixValue, Nil.Value));
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(inner, location);
            Call(innerModule, inner.Function, 1);

            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.EmitSlotKills([0]);
            owner.EmitConstant(prefixValue, 0);
            var previous = CallLocation(inner, location);
            owner.EmitCallLine(outer, previous);
            Call(outerModule, outer.Function, 3);

            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            int handler = Label();
            EnterCatch(0, handler);
            owner.EmitConstant(formatValue, 0);
            owner.EmitCallLine(finish, CallLocation(outer, previous));
            Call(finishModule, finish.Function, 2);
            EndCatch(0, handler);
            owner.EmitFrameReturn(1);

            return true;
        }

        private bool TryEmitCaughtTaggedSingleton(FunctionDefinition function)
        {
            if (ReadCaughtTaggedSingleton(function) is not { } plan
                || !TryEmitDirectCatch(function with { Clauses = [plan.Clause with { Body = plan.Caught }] }, emitReturn: false))
            {
                return false;
            }

            int failure = Label();
            owner.Op(IsNonemptyList, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            EmitDetailRecordTest(
                1,
                2,
                plan.Tag,
                failure
            );
            owner.Op(IsNil, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(1, 1, 0);
            owner.Op(PutTuple, XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(plan.ResultTag);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
            Mark(failure);
            owner.EmitConstant(plan.Fallback, 0);
            owner.EmitFrameReturn(1);

            return true;
        }

        private bool TryEmitCaughtTaggedApply(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Body: Expr.Case
                    {
                        Value: Expr.Call { Arguments: [] } selector,
                        Clauses: [
                        {
                            Guard: null, Patterns: [Pattern.Tuple
                            { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable target] }],
                            Body: Expr.Catch
                            {
                                Operand: Expr.Call
                                {
                                    DynamicRemote: true, Arguments: [
                                Expr.Variable module, Expr.Variable method, Expr.List
                                    {
                                        Tail: null, Items: [
                                    Expr.Variable first, Expr.Variable second
                                ]
                                    }
                            ]
                                } invocation
                            }
                        },
                        {
                            Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Literal other
                        }
                    ]
                    }
                } clause]
                || !HasUnconditionalParameters(clause) || target.Name != module.Name
                || clause.Patterns.Count != 3
                || ReadEntryCall(selector, []) is not { Module: null } selection
                || !localFunctions.TryGetValue((selector.Function, 0), out var callee)
                || ReadCaughtTaggedSingleton(callee) is not { ResultTag: Atom resultAtom, Fallback: Atom }
                || resultAtom.Name != tag.Name)
            {
                return false;
            }

            var inputs = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (target.Name == VariableScopeNames.Wildcard || inputs.Contains(target.Name, StringComparer.Ordinal)
                || !inputs.SequenceEqual(new[] { method.Name, first.Name, second.Name }, StringComparer.Ordinal)
                || !DistinctBindings(clause.Patterns.Cast<Pattern.Variable>().ToArray()))
            {
                return false;
            }

            const int token = 3;
            const int size = token + 1;
            int handler = Label();
            int failure = Label();
            EmitFrameAllocation(function.Arity, size);
            EmitFrameInitialization(token, size);
            for (int slot = 0; slot < token; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    token - 1 - slot,
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(selection);
            owner.Op(IsTuple, LabelTag, failure);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            owner.EmitTuplePart(0, 1, 0);
            EnterCatch(token, handler);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                2,
                XRegisterTag,
                3
            );
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                0
            );
            EmitFrameInitialization(0, token);
            owner.EmitCallLine(invocation, CallLocation(selector, location));
            owner.Op(DynamicApply, BeamWire.UnsignedTag, 2);
            EndCatch(token, handler);
            owner.EmitFrameReturn(size);
            Mark(failure);
            owner.EmitConstant(other.Value, 0);
            owner.EmitFrameReturn(size);

            return true;
        }

        private sealed record CaughtTaggedSingleton(
            Clause Clause,
            Expr.Catch Caught,
            Atom Tag,
            Term ResultTag,
            Term Fallback
        );

        private static CaughtTaggedSingleton? ReadCaughtTaggedSingleton(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Body: Expr.Case
                    {
                        Value: Expr.Catch { Operand: Expr.Call { DynamicRemote: false } } protectedCall,
                        Clauses: [
                        {
                            Guard: null, Patterns: [Pattern.List
                            {
                                Tail: null, Items: [
                            Pattern.Tuple { Items: [Pattern.Literal { Value: Atom inputTag }, Pattern.Variable binding] }
                        ]
                            }], Body: Expr.Tuple { Items: [Expr.Literal outputTag, Expr.Variable returned] }
                        },
                        { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Literal other }
                    ]
                    }
                } source]
                || binding.Name == VariableScopeNames.Wildcard || binding.Name != returned.Name
                || source.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == binding.Name))
            {
                return null;
            }

            return new(
                source,
                protectedCall,
                inputTag,
                outputTag.Value,
                other.Value
            );
        }

        private bool TryEmitDirectCatch(FunctionDefinition function, bool emitReturn = true)
        {
            if (function.Clauses is not [{ Guard: null } clause] || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            Expr? carried = null;
            Expr.Catch? caught = clause.Body as Expr.Catch;
            if (clause.Body is Expr.Tuple { Items: [Expr.Catch nested, Expr.Variable retained] })
            {
                caught = nested;
                carried = retained;
            }
            if (carried is null && caught is not null && AttributeLiteral.TryNormalizeConstant(caught.Operand, out Term constant))
            {
                owner.EmitConstant(constant, 0);
                owner.instructions.WriteByte(Return);

                return true;
            }
            if (caught?.Operand is not Expr.Call { DynamicRemote: false } call
                || !call.Arguments.All(IsCallOperand))
            {
                return false;
            }

            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            int carriedRegister = carried is Expr.Variable variable ? ArgumentRegister(clause, variable.Name) : -1;
            int live = carriedRegister + 1;
            for (int register = 0; register < call.Arguments.Count; register++)
            {
                if (call.Arguments[register] is Expr.Variable argument)
                {
                    int source = ArgumentRegister(clause, argument.Name);
                    live = Math.Max(live, source + 1);
                    if (register != source)
                    {
                        moves.Add((register, source));
                    }
                }
                else
                {
                    AttributeLiteral.TryNormalizeConstant(call.Arguments[register], out Term value);
                    constants.Add((register, value));
                }
            }

            int token = carriedRegister < 0 ? 0 : 1;
            int handler = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, token + 1);
            owner.Operand(BeamWire.UnsignedTag, live);
            if (carriedRegister >= 0)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    carriedRegister,
                    YRegisterTag,
                    0
                );
            }
            EnterCatch(token, handler);
            owner.EmitArgumentMoves(moves, constants);
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, call.Arguments.Count);
            EndCatch(token, handler);
            if (carriedRegister >= 0)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
            }
            if (emitReturn)
            {
                owner.EmitFrameReturn(token + 1);
            }

            return true;
        }

        private sealed record EntryCaughtApply(
            Expr.Case Decision,
            Expr.Call Call,
            int Arity,
            List<(int Destination, int Source)> Moves,
            List<(int Destination, Term Value)> Constants,
            HashSet<string> Survivors
        );

        private static EntryCaughtApply? ReadEntryCaughtApply(Clause clause)
        {
            if (clause.Body is not Expr.Case
                {
                    Value: Expr.Catch
                    {
                        Operand: Expr.Call { DynamicRemote: true, Arguments: [var module, var function, Expr.List { Tail: null } args] } call
                    }
                } decision
                || ReadUsedEntryParameters(clause with { Body = decision with { Value = new Expr.Literal(Nil.Value) } }) is not { } survivors)
            {
                return null;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            var moves = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            Expr[] operands = [.. args.Items, module, function];
            for (int register = 0; register < operands.Length; register++)
            {
                if (operands[register] is Expr.Variable variable && variable.Name != VariableScopeNames.Wildcard
                    && Array.IndexOf(parameters, variable.Name) is int source && source >= 0)
                {
                    if (source != register)
                    {
                        moves.Add((register, source));
                    }
                }
                else if (AttributeLiteral.TryNormalizeConstant(operands[register], out Term literal))
                {
                    constants.Add((register, literal));
                }
                else
                {
                    return null;
                }
            }

            return new EntryCaughtApply(
                decision,
                call,
                args.Items.Count,
                moves,
                constants,
                survivors
            );
        }

        private void EmitEntryCaughtApply(EntryCaughtApply plan)
        {
            int token = nextSlot;
            int handler = Label();
            EnterCatch(Slot(), handler);
            owner.EmitOrderedTailMoves(plan.Moves, plan.Constants);
            owner.EmitCallLine(plan.Call, location);
            owner.Op(DynamicApply, BeamWire.UnsignedTag, plan.Arity);
            EndCatch(token, handler);
            nextSlot = token;
            if (!TryEmitCaughtLiteralBranches(plan.Decision))
            {
                EmitCase(
                    plan.Decision,
                    null,
                    inputPrepared: true,
                    reuseBranchSlots: true
                );
                owner.EmitFrameReturn(frameSize);
            }
        }

        private sealed record RegisterCaseCons(int Destination, CaseTupleOperand Head, CaseTupleOperand Tail);

        private sealed record RegisterCaseResult(
            int? Slot,
            Term? Constant,
            EntryCall? Tail = null,
            CaseTuplePlan? Tuple = null,
            RegisterCaseCons? Cons = null,
            BeamSourceLocation? FailureLocation = null,
            EntryCall? TupleCall = null,
            (int Slot, int Field)? DeferredField = null,
            RegisterCaseCallSequence? Sequence = null,
            bool KeepFrame = false,
            int? Register = null,
            NestedRegisterCase? NestedCase = null
        );

        private sealed record NestedRegisterCase(
            EntryCall Selector,
            RegisterCase Plan,
            int[] ExpiredSlots,
            (int Slot, bool Reuse)? Spill,
            int? ProjectedSlot = null
        );

        private sealed record RegisterCase(
            byte Opcode,
            Term? Key,
            RegisterCaseResult? Match,
            RegisterCaseResult Fallback,
            IReadOnlyList<(Term Key, RegisterCaseResult Result)>? Dispatch = null,
            int TupleArity = 0,
            int Field = -1,
            int FieldRegister = 0,
            IReadOnlyList<RegisterCase>? Tests = null,
            (int Field, int Arity)? NestedField = null,
            int? FieldSlot = null,
            bool KnownTupleTag = false
        );

        private bool TryEmitCaughtLiteralBranches(Expr.Case decision)
        {
            if (ReadRegisterCase(decision, variables) is not { } plan)
            {
                return false;
            }

            EmitRegisterCase(plan, frameSize, CallLocation((Expr.Call)((Expr.Catch)decision.Value).Operand, location));

            return true;
        }

        private bool TryEmitNilTailTupleFallback(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil }],
                    Body: Expr.Call { Arguments.Count: 0 } call
                },
                { Guard: null, Patterns: [Pattern.Variable input], Body: Expr.Tuple tuple }
                ]
                || input.Name == VariableScopeNames.Wildcard
                || ReadEntryCall(call, []) is not { } tail
                || ReadCaseTuple(tuple, input.Name, new Dictionary<string, int>(StringComparer.Ordinal)) is not { UsesSelector: true } result)
            {
                return false;
            }

            int fallback = Label();
            owner.EmitNilTest(XRegisterTag, 0, fallback);
            EmitRegisterTailTarget(tail.Call with { Module = tail.Module }, 0);
            Mark(fallback);
            EmitCaseTuple(result);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitLiteralEntryCallCase(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }, Pattern.Variable input], Body: var body },
                { Guard: null } fallback
                ]
                || input.Name == VariableScopeNames.Wildcard || !HasUnconditionalParameters(fallback)
                || !AttributeLiteral.TryNormalizeConstant(fallback.Body, out Term rejected))
            {
                return false;
            }

            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            if (body is Expr.Sequence
                {
                    Items: [Expr.Match { Pattern: Pattern.Variable binding, Value: var value }, Expr.Case continuation]
                } && binding.Name != input.Name && binding.Name != VariableScopeNames.Wildcard
                && AttributeLiteral.TryNormalizeConstant(value, out Term constant))
            {
                constants.Add(binding.Name, constant);
                body = continuation;
            }

            if (body is not Expr.Case
                {
                    Value: Expr.Call
                    { Arguments: [var first, var second, Expr.List { Items: [Expr.Variable item], Tail: null }] } call
                } decision
                || item.Name != input.Name
                || !AttributeLiteral.TryNormalizeConstant(first, out Term argument0, constants)
                || !AttributeLiteral.TryNormalizeConstant(second, out Term argument1, constants))
            {
                return false;
            }

            var prepared = call with { Arguments = [new Expr.Literal(argument0), new Expr.Literal(argument1), new Expr.Literal(Nil.Value)] };
            if (!TryResolveOrdinaryCall(prepared, out string? module))
            {
                return false;
            }

            var saved = new Dictionary<string, int>(StringComparer.Ordinal) { [input.Name] = 0 };
            if (ReadRegisterCase(decision, saved) is not
                {
                    Opcode: IsTaggedTuple, Dispatch: null, Tests: null, Field: >= 0,
                    Match.Tuple: not null, Fallback: { Slot: null, Constant: null, Tail: null, Tuple: null }
                } plan)
            {
                return false;
            }

            int caseFailure = Label();
            int entryFailure = Label();
            owner.Op(ExactEqualTest, LabelTag, entryFailure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(key);
            owner.Op(AllocateHeap, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.Op(PutList, XRegisterTag, 1);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 2);
            owner.EmitConstant(argument1, 1);
            owner.EmitConstant(argument0, 0);
            owner.EmitCallLine(call, location);
            Call(module, call.Function, 3);
            EmitRegisterCase(
                plan,
                1,
                CallLocation(call, location),
                preparedFailure: caseFailure
            );
            Mark(entryFailure);
            owner.EmitConstant(rejected, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private bool TryEmitEntryCallCaseWithFallback(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case
                {
                    Value: Expr.Call call,
                    Clauses: [
                    { Guard: null, Patterns: [Pattern.Literal key], Body: var matched },
                    { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: var fallback }
                    ]
                } || !AttributeLiteral.TryNormalizeConstant(matched, out Term constant)
                || ReadUsedEntryParameters(clause with { Body = fallback }) is not { } used)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadEntryCall(call, parameters) is not { } plan)
            {
                return false;
            }

            EmitFrameAllocation(parameters.Length, frameSize);
            EmitFrameInitialization(used.Count, frameSize);
            for (int index = parameters.Length - 1; index >= 0; index--)
            {
                if (used.Contains(parameters[index]))
                {
                    int slot = Slot();
                    owner.MoveRegister(
                        XRegisterTag,
                        index,
                        YRegisterTag,
                        slot
                    );
                    variables.Add(parameters[index], slot);
                }
            }

            EmitEntryCall(plan);
            int failure = Label();
            owner.Op(ExactEqualTest, LabelTag, failure);
            if (ReadCallResultUnion(call) is int resultType and > 0)
            {
                owner.EmitTypedRegister(0, resultType);
            }
            else
            {
                owner.Operand(XRegisterTag, 0);
            }

            owner.LiteralOperand(key.Value);
            owner.EmitConstant(constant, 0);
            owner.EmitFrameReturn(frameSize);

            Mark(failure);
            if (fallback is Expr.Sequence
                {
                    Items: [
                        Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call read },
                        Expr.Call tail
                    ]
                }
                && result.Name != VariableScopeNames.Wildcard
                && !parameters.Contains(result.Name, StringComparer.Ordinal)
                && ReadForwardedCall(new Expr.Sequence([tail]), result.Name, parameters) is { } forwarded)
            {
                Expression(read);

                return EmitForwardedCall(forwarded, CallLocation(read, location));
            }

            if (!TailLocal(fallback, reuseCallSlots: true))
            {
                EmitReturnExpression(fallback, reuseCallSlots: true);
                owner.EmitFrameReturn(frameSize);
            }

            return true;
        }

        private bool TryEmitEntryRegisterCase(Clause clause)
        {
            if (ReadEntryRegisterCase(clause, specializeBranchInputs: true) is not { } entry)
            {
                return false;
            }

            EmitEntryRegisterCase(entry);

            return true;
        }

        private sealed record EntryRegisterCase(
            EntrySelector Selector,
            RegisterCase Case,
            Expr.Call Read,
            string[] Parameters,
            (int Tuple, int Field, int Register)? Deferred = null,
            bool PreserveSelector = false,
            int ProjectionDestination = 0,
            CallListExtraction? Extraction = null,
            bool ReserveNestedSlot = false,
            (int Field, int Register)[]? SavedTupleFields = null
        );

        private static Expr.Case? ReadCallSelectorCase(
            Expr body,
            out (Expr.Call Call, Pattern.Variable Result, bool Preserve)? preceding
        )
        {
            preceding = null;
            if (body is Expr.Case { Value: Expr.Call } direct)
            {
                return direct;
            }

            if (body is not Expr.Sequence
                {
                    Items: [
                        Expr.Match
                    {
                        Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } result,
                        Value: Expr.Call prefix
                    },
                        Expr.Case decision
                    ]
                }
                || ReadUsedEntryParameters(new Clause([result], null,
                    decision with { Value = new Expr.Literal(Nil.Value) })) is not { } used)
            {
                return null;
            }

            if (decision.Value is Expr.Variable variable && variable.Name == result.Name)
            {
                return used.Count == 0 ? decision with { Value = prefix } : null;
            }

            if (decision.Value is not Expr.Call)
            {
                return null;
            }

            preceding = (prefix, result, used.Count != 0);

            return decision;
        }

        private EntryRegisterCase? ReadEntryRegisterCase(
            Clause clause,
            (int Tuple, int Field, int Register)? field = null,
            (int Head, int Tail)? listParts = null,
            bool projectAnyArgument = false,
            bool specializeBranchInputs = false,
            CallListExtraction? extraction = null,
            IReadOnlySet<int>? entryTupleFields = null
        )
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause))
            {
                return null;
            }

            var candidate = ReadCallSelectorCase(clause.Body, out var preceding);
            (TaggedCallMatch Check, int? Field)? taggedPrefix = null;
            if (candidate is null && clause.Body is Expr.Sequence { Items: [var prefixMatch, Expr.Case { Value: Expr.Call } taggedCase] }
                && ReadSimpleTaggedCallMatch(prefixMatch) is { } tagged)
            {
                var payload = new Pattern.Variable(tagged.Payload?.Name ?? "\0discarded-tagged-case-prefix");
                if (clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == payload.Name)
                    || ReadUsedEntryParameters(new Clause([payload], null,
                        taggedCase with { Value = new Expr.Literal(Nil.Value) })) is not { } payloadUses)
                {
                    return null;
                }

                candidate = taggedCase;
                preceding = (tagged.Check.Call, payload, payloadUses.Count != 0);
                bool usedBySelector = ((Expr.Call)taggedCase.Value).Arguments.OfType<Expr.Variable>()
                    .Any(argument => argument.Name == payload.Name);
                taggedPrefix = (tagged.Check, usedBySelector || payloadUses.Count != 0 ? tagged.Payload?.Field : null);
            }

            if (candidate is not { Value: Expr.Call read } decision
                || ReadUsedEntryParameters(clause with { Body = decision with { Value = new Expr.Literal(Nil.Value) } }) is not { } used)
            {
                return null;
            }

            if (clause.Body is Expr.Sequence { Items: [Expr.Match { Pattern: Pattern.Variable result }, _] }
                && clause.Patterns.OfType<Pattern.Variable>().Any(parameter => parameter.Name == result.Name))
            {
                return null;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            bool specializedInputs = false;
            if (specializeBranchInputs && used.Count != 0
                && parentFunction is { Clauses: [var original] } function && ReferenceEquals(clause, original)
                && ReadPrivateBranchConstants(function, parameters, used) is { Count: > 0 } constants)
            {
                var specialized = decision with
                {
                    Clauses = decision.Clauses.Select(branch => branch with
                    {
                        Body = SubstituteCaseBranchConstants(branch.Body, constants)
                    }).ToArray()
                };
                if (ReadUsedEntryParameters(clause with { Body = specialized with { Value = new Expr.Literal(Nil.Value) } })
                    is { } remaining)
                {
                    // The original arguments still feed the selector call.
                    decision = specialized;
                    used = remaining;
                    specializedInputs = true;
                }
            }

            int[] inputs = Enumerable.Range(0, parameters.Length).Where(index => used.Contains(parameters[index]))
                .OrderByDescending(index => extraction is { } list && index == list.Tail ? list.Head + 1 : index)
                .ToArray();
            if (listParts is { } pair)
            {
                int head = Array.IndexOf(inputs, pair.Head);
                int tail = Array.IndexOf(inputs, pair.Tail);
                if (head == tail + 1 && tail >= 0)
                {
                    (inputs[head], inputs[tail]) = (inputs[tail], inputs[head]);
                }
            }

            if (ReadEntrySelector(
                read,
                parameters,
                inputs,
                trimFrame: false,
                preceding: preceding is { } prefix ? (prefix.Call, prefix.Result.Name, prefix.Preserve) : null,
                taggedPrefix: taggedPrefix,
                extraction: extraction
            ) is not { } selector)
            {
                return RetryWithoutSpecialization();
            }

            var saved = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (int input in inputs)
            {
                saved.Add(parameters[input], Array.IndexOf(selector.Saved, input) + selector.SavedSlotOffset - selector.Trimmed);
            }

            if (preceding is { } binding && selector.PrefixResultSlot is int slot)
            {
                saved.Add(binding.Result.Name, slot - selector.Trimmed);
            }

            var plan = ReadRegisterCase(
                decision,
                saved,
                IsBooleanRegisterCaseSelector(selector.Call),
                allowUnmatchedLiterals: true,
                selectorCall: selector.Call,
                deferField: selector.Prefix is null,
                reuseMatchedLiteral: taggedPrefix is not null
            );
            bool reserveNestedSlot = false;
            if (selector.Prefix is null && extraction is null
                && (field is null && listParts is null || entryTupleFields is not null)
                && (entryTupleFields is null || inputs.Count(entryTupleFields.Contains) < 2)
                && decision.Clauses.Any(branch => branch.Body is Expr.Case { Value: Expr.Call }))
            {
                var shifted = saved.ToDictionary(binding => binding.Key, binding => binding.Value + 1, StringComparer.Ordinal);
                var reserved = ReadRegisterCase(
                    decision,
                    shifted,
                    IsBooleanRegisterCaseSelector(selector.Call),
                    allowUnmatchedLiterals: true,
                    selectorCall: selector.Call,
                    reservedNestedSlot: 0
                );
                if (reserved is not null && UsesReservedNestedSlot(reserved))
                {
                    plan = reserved;
                    reserveNestedSlot = true;
                }
            }

            if (plan is null)
            {
                return RetryWithoutSpecialization();
            }

            bool preserveSelector = (plan.Tests ?? [plan]).Any(test => test.Match?.DeferredField is not null);
            if (extraction is { } extracted)
            {
                if (preserveSelector || !selector.Saved.Contains(extracted.Head) || !selector.Saved.Contains(extracted.Tail)
                    || selector.Prefix is null && !HasSequence(plan))
                {
                    return null;
                }

                if (selector.Prefix is not null)
                {
                    return new(
                        selector,
                        plan,
                        read,
                        parameters,
                        Extraction: extraction
                    );
                }

                string[] direct = (string[])parameters.Clone();
                direct[extracted.Head] = VariableScopeNames.Wildcard;
                direct[extracted.Tail] = VariableScopeNames.Wildcard;
                if (ReadEntryCall(read, direct, selector.StackInputs) is not { } fromList)
                {
                    return null;
                }

                return new(
                    selector with { Call = fromList },
                    plan,
                    read,
                    parameters,
                    Extraction: extraction
                );
            }

            if (preserveSelector)
            {
                var shifted = saved.ToDictionary(binding => binding.Key, binding => binding.Value + 1, StringComparer.Ordinal);
                if (ReadRegisterCase(
                    decision,
                    shifted,
                    IsBooleanRegisterCaseSelector(selector.Call),
                    allowUnmatchedLiterals: true,
                    selectorCall: selector.Call,
                    deferField: true
                ) is not { } shiftedPlan)
                {
                    return RetryWithoutSpecialization();
                }

                plan = shiftedPlan;
            }

            if ((projectAnyArgument || plan.Match?.TupleCall is not null)
                && plan is { Opcode: IsTaggedTuple, TupleArity: 2, Key: Atom { Name: "ok" }, Tests: null }
                && selector.Call is { Module: "maps", Call: { Function: "find", Arguments.Count: 2 } })
            {
                plan = plan with { Opcode = IsTuple, TupleArity = 0, Key = null };
            }

            if (field is { } projection && selector.Prefix is null && !inputs.Contains(projection.Register))
            {
                string name = parameters[projection.Register];
                int destination = -1;
                for (int argument = 0; argument < read.Arguments.Count; argument++)
                {
                    if (read.Arguments[argument] is Expr.Variable variable && variable.Name == name)
                    {
                        if (destination >= 0)
                        {
                            destination = -1;
                            break;
                        }

                        destination = argument;
                    }
                }

                if (destination >= 0 && (projectAnyArgument || destination == 0)
                    && selector.Call.Moves.All(move => move.Source != destination))
                {
                    var call = selector.Call with { Moves = selector.Call.Moves.Where(move => move.Destination != destination).ToList() };

                    return new(
                        selector with { Call = call },
                        plan,
                        read,
                        parameters,
                        projection,
                        preserveSelector,
                        destination,
                        ReserveNestedSlot: reserveNestedSlot
                    );
                }
            }

            return new(
                selector,
                plan,
                read,
                parameters,
                PreserveSelector: preserveSelector,
                ReserveNestedSlot: reserveNestedSlot
            );

            EntryRegisterCase? RetryWithoutSpecialization() =>
                specializedInputs ? ReadEntryRegisterCase(
                    clause,
                    field,
                    listParts,
                    projectAnyArgument,
                    entryTupleFields: entryTupleFields
                ) : null;

            static bool HasSequence(RegisterCase plan) =>
                plan.Fallback.Sequence is not null
                || (plan.Tests ?? [plan]).Any(test => test.Match?.Sequence is not null)
                || plan.Dispatch?.Any(arm => arm.Result.Sequence is not null) == true;

            static bool UsesReservedNestedSlot(RegisterCase plan) =>
                UsesSlot(plan.Fallback) || (plan.Tests ?? [plan]).Any(test => UsesSlot(test.Match))
                || plan.Dispatch?.Any(arm => UsesSlot(arm.Result)) == true;

            static bool UsesSlot(RegisterCaseResult? result) =>
                result?.NestedCase is { ProjectedSlot: 0 } or { Spill: { Slot: 0 } };
        }

        private Dictionary<string, Term>? ReadPrivateBranchConstants(
            FunctionDefinition function, string[] parameters, HashSet<string> used
        )
        {
            if (owner.ModuleOptimizationDisabled
                || owner.IsDeclaredNative((function.Name, function.Arity)))
            {
                return null;
            }

            int[] candidates = Enumerable.Range(0, parameters.Length).Where(index => used.Contains(parameters[index])).ToArray();
            var agreed = new Dictionary<int, Term>();
            bool first = true;
            var active = new HashSet<(string Name, int Arity, int Index)>();
            var proven = new Dictionary<(string Name, int Arity, int Index), Term>();
            bool closed = HasOnlyLocalCalls(
                function,
                parameters.Length,
                (caller, call) =>
            {
                foreach (int index in candidates)
                {
                    if (!first && !agreed.ContainsKey(index))
                    {
                        continue;
                    }

                    if (ReadArgument(caller, call, index) is not { } value)
                    {
                        agreed.Remove(index);
                    }
                    else if (first)
                    {
                        agreed.Add(index, value);
                    }
                    else if (!agreed[index].Equals(value))
                    {
                        agreed.Remove(index);
                    }
                }

                first = false;

                return agreed.Count != 0;
            }
            );

            return closed ? agreed.ToDictionary(binding => parameters[binding.Key], binding => binding.Value, StringComparer.Ordinal)
                : null;

            Term? ReadArgument(FunctionDefinition caller, Expr.Call call, int index)
            {
                if (call.Arguments[index] is Expr.Literal { Value: Atom or Integer or Nil } literal)
                {
                    return literal.Value;
                }

                if (call.Arguments[index] is not Expr.Variable { Name: not VariableScopeNames.Wildcard } variable)
                {
                    return null;
                }

                foreach (Clause clause in caller.Clauses)
                {
                    Expr tail = clause.Body is Expr.Sequence sequence ? sequence.Items[^1] : clause.Body;
                    if (ReferenceEquals(tail, call))
                    {
                        Term? matched = clause.Patterns.Select(pattern => ReadMatchedLiteral(pattern, variable.Name))
                            .FirstOrDefault(value => value is not null);
                        if (matched is not null)
                        {
                            return matched;
                        }

                        int source = Enumerable.Range(0, clause.Patterns.Count).FirstOrDefault(position =>
                            clause.Patterns[position] is Pattern.Variable parameter && parameter.Name == variable.Name, -1);

                        return source < 0 ? null : ReadParameter(caller, source);
                    }
                }

                return null;
            }

            static Term? ReadMatchedLiteral(Pattern pattern, string name) => pattern switch
            {
                AliasPattern { Left: Pattern.Variable variable, Right: Pattern.Literal { Value: Atom or Integer or Nil } literal }
                    when variable.Name == name => literal.Value,
                AliasPattern { Right: Pattern.Variable variable, Left: Pattern.Literal { Value: Atom or Integer or Nil } literal }
                    when variable.Name == name => literal.Value,
                AliasPattern alias => ReadMatchedLiteral(alias.Left, name) ?? ReadMatchedLiteral(alias.Right, name),
                Pattern.Tuple tuple => tuple.Items.Select(item => ReadMatchedLiteral(item, name))
                    .FirstOrDefault(value => value is not null),
                Pattern.List list => list.Items.Select(item => ReadMatchedLiteral(item, name))
                    .FirstOrDefault(value => value is not null) ?? (list.Tail is { } tail ? ReadMatchedLiteral(tail, name) : null),
                _ => null
            };

            Term? ReadParameter(FunctionDefinition target, int index)
            {
                var key = (target.Name, target.Arity, index);
                if (proven.TryGetValue(key, out Term? cached))
                {
                    return cached;
                }

                if (owner.IsDeclaredNative((target.Name, target.Arity)) || !active.Add(key))
                {
                    return null;
                }

                Term? value = null;
                bool complete = HasOnlyLocalCalls(
                    target,
                    target.Arity,
                    (caller, call) =>
                {
                    Term? actual = ReadArgument(caller, call, index);
                    if (actual is null || value is not null && !value.Equals(actual))
                    {
                        return false;
                    }

                    value = actual;

                    return true;
                }
                );
                active.Remove(key);
                if (complete && value is not null)
                {
                    proven.Add(key, value);

                    return value;
                }

                return null;
            }
        }

        private static Expr SubstituteCaseBranchConstants(Expr body, Dictionary<string, Term> constants) =>
            body is Expr.Sequence { Items: [Expr.Match match, Expr.Tuple tuple] } sequence
                ? sequence with { Items = [match with { Value = SubstituteConstants(match.Value, constants) }, SubstituteConstants(tuple, constants)] }
                : SubstituteConstants(body, constants);

        private void EmitEntryRegisterCase(EntryRegisterCase entry, int? preparedCaseFailure = null)
        {
            int extra = entry.PreserveSelector || entry.ReserveNestedSlot ? 1 : 0;
            int? matchFailureOperand = null;
            int trimmed = EmitEntrySelector(
                entry.Selector,
                entry.Read,
                entry.Parameters,
                entry.Deferred,
                extra,
                entry.ProjectionDestination,
                position => matchFailureOperand = position,
                extraction: entry.Extraction,
                savedTupleFields: entry.SavedTupleFields
            );
            if (entry.PreserveSelector && (entry.Case.Tests ?? [entry.Case]).All(test => test.NestedField is null))
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    0
                );
            }

            var beforeRead = entry.Selector.Prefix is { } prefix ? CallLocation(prefix.Call, location) : location;
            var selectorLocation = CallLocation(entry.Read, beforeRead);
            int selectorLineCount = owner.lineInstructions;
            EmitRegisterCase(
                entry.Case,
                entry.Selector.FrameSize + extra - trimmed,
                selectorLocation,
                RegisterCaseSelectorType(entry.Selector.Call, entry.Case.Opcode == IsTaggedTuple ? IsTuple : entry.Case.Opcode),
                preparedCaseFailure
            );
            if (entry.Selector.TaggedPrefix is { } tagged && matchFailureOperand is int operand)
            {
                int failure = Label();
                owner.deferredLabelOperands.Add((operand, failure));
                EmitTaggedCallMatchFailure(
                    tagged.Check.Match,
                    failure,
                    beforeRead,
                    selectorLocation,
                    forceLine: selectorLineCount != owner.lineInstructions
                );
            }
        }

        private RegisterCase? ReadRegisterCase(
            Expr.Case decision,
            Dictionary<string, int> saved,
            bool boolean = false,
            bool allowUnmatchedLiterals = false,
            EntryCall? selectorCall = null,
            bool deferField = false,
            bool reuseMatchedLiteral = false,
            int? reservedNestedSlot = null
        )
        {
            if (selectorCall is { Module: null } generated
                && owner.capturedListFilters.ContainsKey((generated.Call.Function, generated.Call.Arguments.Count))
                && decision.Clauses is [var firstArm, var secondArm])
            {
                var empty = firstArm;
                var nonempty = secondArm;
                if (!IsEmptyArm(empty))
                {
                    (empty, nonempty) = (nonempty, empty);
                }

                if (IsEmptyArm(empty) && nonempty is { Guard: null, Patterns: [AliasPattern alias] }
                    && (alias.Left as Pattern.Variable ?? alias.Right as Pattern.Variable) is { } whole
                    && !saved.ContainsKey(whole.Name)
                    && (alias.Left is Pattern.Variable ? alias.Right : alias.Left) is Pattern.List
                    {
                        Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                        Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    }
                    && ReadResult(nonempty.Body, whole.Name) is { } present
                    && ReadResult(empty.Body, null) is { } absent)
                {
                    // The selector call has already cleared the frame when no branch carries a slot.
                    return new RegisterCase(
                        IsNonemptyList,
                        null,
                        present with { KeepFrame = saved.Count == 0 },
                        absent
                    );
                }
            }

            if (boolean && decision.Clauses is [
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom first }], Body: var firstBody },
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom second }], Body: var secondBody }]
                && first.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False
                && second.Name is ErlangBooleanAtoms.True or ErlangBooleanAtoms.False
                && first.Name != second.Name
                && ReadResult(firstBody, null) is { } firstResult && ReadResult(secondBody, null) is { } secondResult)
            {
                return new(
                    ExactEqualTest,
                    first,
                    firstResult,
                    secondResult
                );
            }

            if (allowUnmatchedLiterals && decision.Clauses[^1].Patterns is [Pattern.Literal { }]
                && ReadLiteralArms(decision.Clauses, keepSelector: true) is { Count: > 0 } literals)
            {
                if (literals.Count == 2 && selectorCall is { Module: null }
                    && HasExhaustiveLiteralResults(selectorCall.Call, literals.Select(arm => arm.Key)))
                {
                    Term firstKey = ((Pattern.Literal)decision.Clauses[0].Patterns[0]).Value;
                    var (match, alternate) = literals[0].Key.Equals(firstKey)
                        ? (literals[0], literals[1])
                        : (literals[1], literals[0]);

                    return new RegisterCase(
                        ExactEqualTest,
                        match.Key,
                        match.Result,
                        alternate.Result
                    );
                }

                var failureLocation = decision.SourceLine == 0
                    ? location
                    : new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine);
                var failure = new RegisterCaseResult(null, null, FailureLocation: failureLocation);

                return literals is [var only]
                    ? new RegisterCase(
                        ExactEqualTest,
                        only.Key,
                        only.Result,
                        failure
                    )
                    : new RegisterCase(
                        SelectValue,
                        null,
                        null,
                        failure,
                        literals
                    );
            }

            RegisterCaseResult otherwise;
            IEnumerable<Clause> branches;
            if (decision.Clauses.Count >= 2 && decision.Clauses[^1] is
                { Guard: null, Patterns: [Pattern.Variable fallback], Body: var unmatched }
                && !saved.ContainsKey(fallback.Name)
                && ReadResult(unmatched, fallback.Name) is { } fallbackResult)
            {
                otherwise = fallbackResult;
                branches = decision.Clauses.SkipLast(1);
                if (decision.Clauses.Count > 2 && ReadLiteralArms(branches) is { } dispatch)
                {
                    return new RegisterCase(
                        SelectValue,
                        null,
                        null,
                        otherwise,
                        dispatch
                    );
                }
            }
            else if (allowUnmatchedLiterals && decision.Clauses.Any(branch => branch.Patterns is [Pattern.Tuple { }]))
            {
                var source = decision.SourceLine == 0
                    ? location
                    : new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine);
                otherwise = new RegisterCaseResult(null, null, FailureLocation: source);
                branches = decision.Clauses;
            }
            else
            {
                return null;
            }

            var tests = new List<RegisterCase>();
            foreach (Clause branch in branches)
            {
                if (ReadTest(branch) is not { } test)
                {
                    return null;
                }

                tests.Add(test);
            }

            return tests.Count == 1 ? tests[0] : tests[0] with { Tests = tests };

            static bool IsEmptyArm(Clause arm) =>
                arm is { Guard: null, Patterns: [Pattern.Literal { Value: Nil } or Pattern.List { Items: [], Tail: null }] };

            List<(Term Key, RegisterCaseResult Result)>? ReadLiteralArms(
                IEnumerable<Clause> clauses,
                bool keepSelector = false
            )
            {
                var arms = new List<(Term Key, RegisterCaseResult Result)>();
                var keys = new HashSet<Term>();
                foreach (Clause candidate in clauses)
                {
                    if (candidate is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom or Integer } branchKey] }
                        || branchKey.Value is Integer integer && (integer.Value < int.MinValue || integer.Value > int.MaxValue)
                        || arms.Count != 0 && arms[0].Key.GetType() != branchKey.Value.GetType()
                        || !keys.Add(branchKey.Value)
                        || ReadResult(candidate.Body, null) is not { } result)
                    {
                        return null;
                    }

                    arms.Add((branchKey.Value, result));
                }

                if (keepSelector)
                {
                    var counts = arms.Where(arm => arm.Result.Constant is not null)
                        .GroupBy(arm => arm.Result.Constant!)
                        .ToDictionary(group => group.Key, group => group.Count());
                    for (int index = 0; index < arms.Count; index++)
                    {
                        var (key, result) = arms[index];
                        if (result.Constant is { } returned && returned.Equals(key) && counts[returned] == 1)
                        {
                            arms[index] = (key, new RegisterCaseResult(null, null));
                        }
                    }
                }

                arms.Sort((left, right) => TermOrder.Compare(left.Key, right.Key, exact: true));

                return arms;
            }

            RegisterCase? ReadTest(Clause branch)
            {
                if (branch is { Guard: null, Patterns: [Pattern.Tuple tuple] }
                    && ReadSingleTupleField(tuple) is { Field: Pattern.Variable projected } access
                    && !saved.ContainsKey(projected.Name)
                    && (ReadResult(branch.Body, projected.Name, projectField: true)
                        ?? (deferField && projected.Name != VariableScopeNames.Wildcard
                            ? ReadCaseTupleCall(branch.Body, null, (projected.Name, access.Index)) : null))
                        is { } projectedResult)
                {
                    bool usesField = branch.Body is Expr.Variable variable && variable.Name == projected.Name
                        || projectedResult.TupleCall is null && projectedResult.Tuple?.UsesSelector == true
                        || projectedResult.NestedCase is { ProjectedSlot: not null } or { Spill: not null }
                        || (projectedResult.Sequence is not null || projectedResult.NestedCase is not null)
                            && LocalFunctionEntries.Walk(branch.Body)
                            .OfType<Expr.Variable>().Any(argument => argument.Name == projected.Name)
                        || projectedResult.TupleCall?.Call.Arguments.OfType<Expr.Variable>()
                            .Any(argument => argument.Name == projected.Name) == true;
                    int fieldRegister = 0;
                    if (projectedResult.NestedCase is { Spill: null, ProjectedSlot: null, Selector.RegisterConstructions: null } nestedCall)
                    {
                        int[] arguments = nestedCall.Selector.Call.Arguments.Select((argument, index) => (argument, index))
                            .Where(item => item.argument is Expr.Variable field && field.Name == projected.Name)
                            .Select(item => item.index).ToArray();
                        if (arguments is [var destination] && destination != 0)
                        {
                            fieldRegister = destination;
                            var moves = nestedCall.Selector.Moves.Where(move => move.Destination != destination).ToList();
                            projectedResult = projectedResult with
                            {
                                NestedCase = nestedCall with { Selector = nestedCall.Selector with { Moves = moves } }
                            };
                        }
                    }

                    if (projectedResult.Tail is { } tail)
                    {
                        int fieldArgument = Array.FindLastIndex(tail.Call.Arguments.ToArray(),
                            argument => argument is Expr.Variable field && field.Name == projected.Name);
                        if (fieldArgument >= 0)
                        {
                            usesField = true;
                            fieldRegister = fieldArgument;
                            var moves = tail.Moves.Where(move => move.Destination != fieldArgument)
                                .Select(move => (move.Destination, move.Source == 0 ? fieldArgument : move.Source)).ToList();
                            if (fieldArgument != 0 && tail.Call.Arguments[0] is Expr.Variable first
                                && first.Name == projected.Name)
                            {
                                moves.Add((0, fieldArgument));
                            }

                            projectedResult = projectedResult with { Tail = tail with { Moves = moves } };
                        }
                    }

                    return new RegisterCase(
                        IsTaggedTuple,
                        access.Tag,
                        projectedResult,
                        otherwise,
                        TupleArity: tuple.Items.Count,
                        Field: usesField ? access.Index : -1,
                        FieldRegister: fieldRegister,
                        FieldSlot: projectedResult.NestedCase?.ProjectedSlot,
                        KnownTupleTag: selectorCall is
                        {
                            Module: "lists",
                            Call: { Function: "keyfind", Arguments: [Expr.Literal { Value: Atom lookupKey }, Expr.Literal { Value: Integer lookupPosition }, _] }
                        } && lookupPosition.Value.IsOne && lookupKey.Equals(access.Tag)
                    );
                }

                if (deferField && branch is { Guard: null, Patterns: [Pattern.Tuple outer] }
                    && ReadSingleTupleField(outer) is { Field: Pattern.Tuple inner } nested
                    && decision.Clauses.Count(candidate => candidate.Patterns is [Pattern.Tuple { }]) == 1
                    && inner.Items.All(item => item is Pattern.Variable { })
                    && inner.Items.OfType<Pattern.Variable>()
                        .Where(item => item.Name != VariableScopeNames.Wildcard).ToArray() is [var field]
                    && !saved.ContainsKey(field.Name)
                    && ReadCaseTupleCall(branch.Body, null, (field.Name, inner.Items.ToList().IndexOf(field))) is { } result)
                {
                    return new RegisterCase(
                        IsTaggedTuple,
                        nested.Tag,
                        result,
                        otherwise,
                        TupleArity: outer.Items.Count,
                        NestedField: (nested.Index, inner.Items.Count)
                    );
                }

                byte opcode;
                Term? key = null;
                string? selector = null;
                if (branch is { Guard: null, Patterns: [Pattern.Literal { Value: Atom or Integer } literal] })
                {
                    opcode = ExactEqualTest;
                    key = literal.Value;
                }
                else if (branch is
                {
                    Patterns: [Pattern.Variable binding], Guard: Expr.Call
                    { DynamicRemote: false, Arguments: [Expr.Variable tested] } guard
                }
                    && binding.Name != VariableScopeNames.Wildcard && binding.Name == tested.Name
                    && !saved.ContainsKey(binding.Name) && owner.ResolvesErlangBif(guard)
                    && TypeGuardOpcode(guard) is byte test && test != 0)
                {
                    opcode = test;
                    selector = binding.Name;
                }
                else
                {
                    return null;
                }

                if (reuseMatchedLiteral && key is not null && branch.Body is Expr.Literal returned
                    && key.Equals(returned.Value))
                {
                    return new RegisterCase(
                        opcode,
                        key,
                        new RegisterCaseResult(null, null),
                        otherwise
                    );
                }

                return ReadResult(branch.Body, selector) is { } match
                    ? new RegisterCase(
                        opcode,
                        key,
                        match,
                        otherwise
                    ) : null;
            }

            RegisterCaseResult? ReadResult(Expr expression, string? bound, bool projectField = false)
            {
                if (bound == VariableScopeNames.Wildcard)
                {
                    bound = null;
                }

                if (expression is Expr.Variable variable)
                {
                    if (variable.Name == bound)
                    {
                        return new RegisterCaseResult(null, null);
                    }

                    return saved.TryGetValue(variable.Name, out int slot) ? new RegisterCaseResult(slot, null) : null;
                }

                if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
                {
                    return new RegisterCaseResult(null, constant);
                }

                if (expression is Expr.RecordConstruction { Wildcard: null } record)
                {
                    expression = RecordExpressions.Create(record);
                }

                if (expression is Expr.Tuple tuple && ReadCaseTuple(tuple, bound, saved) is { } tuplePlan)
                {
                    return new RegisterCaseResult(null, null, Tuple: tuplePlan);
                }

                if (allowUnmatchedLiterals && ReadCaseTupleCall(expression, bound) is { } computed)
                {
                    return computed;
                }

                if (allowUnmatchedLiterals && ReadRegisterCaseCallSequence(expression, bound, saved) is { } sequence)
                {
                    return new RegisterCaseResult(null, null, Sequence: sequence);
                }

                if (allowUnmatchedLiterals && expression is Expr.Case { Value: Expr.Call read } nested
                    && (ReadEntryCall(read, bound is null ? [] : [bound], SavedNames())
                        ?? ReadStraightConstructedCall(read, bound, saved)) is { } selector)
                {
                    if (selector.RegisterConstructions is null && nested.Clauses[^1] is
                        {
                            Guard: null,
                            Patterns: [Pattern.Variable { Name: not VariableScopeNames.Wildcard } fallback],
                            Body: Expr.Variable returned
                        } && fallback.Name == returned.Name && fallback.Name != bound && !saved.ContainsKey(fallback.Name)
                        && nested.Clauses.Take(nested.Clauses.Count - 1).All(branch => branch.Guard is null
                            && branch.Patterns is [var pattern] && ReturnsMatchedValue(pattern, branch.Body)))
                    {
                        return new RegisterCaseResult(null, null, Tail: selector);
                    }

                    var inputs = saved.Keys.Select(name => new Pattern.Variable(name)).ToList();
                    if (bound is not null)
                    {
                        inputs.Add(new Pattern.Variable(bound));
                    }

                    var body = nested with { Value = new Expr.Literal(Nil.Value) };
                    if (ReadUsedEntryParameters(new Clause(inputs, null, body)) is not { } used)
                    {
                        return null;
                    }

                    bool saveField = bound is not null && used.Remove(bound);
                    var live = used.ToDictionary(name => name, name => saved[name], StringComparer.Ordinal);
                    int[] expired = saved.Values.Except(live.Values).Order().ToArray();
                    (int Slot, bool Reuse)? spill = null;
                    int? projectedSlot = null;
                    if (saveField)
                    {
                        if (reservedNestedSlot is int reserved && projectField)
                        {
                            var projectedInputs = new Dictionary<string, int>(saved, StringComparer.Ordinal) { [bound!] = reserved };
                            var names = SavedNames(reserved);
                            names[reserved] = bound!;
                            if ((ReadEntryCall(read, [], names) ?? ReadStraightConstructedCall(read, null, projectedInputs))
                                is not { } fromSlot)
                            {
                                return null;
                            }

                            selector = fromSlot;
                            live.Add(bound!, reserved);
                            projectedSlot = reserved;
                        }
                        else
                        {
                            int? freeSlot = reservedNestedSlot;
                            if (freeSlot is null && selector.RegisterConstructions is null)
                            {
                                freeSlot = expired.Where(slot => selector.SavedInputs?.Any(load => load.Source == slot) != true)
                                    .Min(slot => (int?)slot);
                            }

                            int? reusedSlot = freeSlot ?? FindReusableCallSlot(
                                ref selector,
                                bound!,
                                saved,
                                expired,
                                allowSwapWithPreviousArgument: true
                            );
                            if (reusedSlot is not int spillSlot)
                            {
                                return null;
                            }

                            live.Add(bound!, spillSlot);
                            spill = (spillSlot, freeSlot is null);
                            expired = expired.Where(dead => dead != spillSlot).ToArray();
                        }
                    }

                    var plan = ReadRegisterCase(
                        nested,
                        live,
                        IsBooleanRegisterCaseSelector(selector),
                        allowUnmatchedLiterals: true,
                        selectorCall: selector
                    );

                    if (plan is null)
                    {
                        return null;
                    }

                    return new RegisterCaseResult(null, null, NestedCase: new NestedRegisterCase(
                        selector,
                        plan,
                        expired,
                        spill,
                        projectedSlot
                    ));
                }

                if (expression is Expr.Call tail)
                {
                    string[] names = SavedNames();

                    if (ReadEntryCall(
                        tail,
                        bound is null ? [] : [bound],
                        names,
                        allowControlTransfer: allowUnmatchedLiterals
                    ) is { } prepared)
                    {
                        return new RegisterCaseResult(null, null, prepared);
                    }

                    if (bound is null)
                    {
                        return ReadConsTail(tail, names);
                    }
                }

                return null;
            }

            string[] SavedNames(int reserved = -1)
            {
                var names = new string[Math.Max(reserved, saved.Values.DefaultIfEmpty(-1).Max()) + 1];
                foreach (var (name, index) in saved)
                {
                    names[index] = name;
                }

                return names;
            }

            RegisterCaseResult? ReadCaseTupleCall(
                Expr expression,
                string? bound,
                (string Name, int Field)? deferred = null
            )
            {
                Expr.Tuple tuple;
                Expr.Call call;
                string result;
                if (expression is Expr.Sequence
                    {
                        Items: [
                            Expr.Match
                        {
                            Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding,
                            Value: Expr.Call producer
                        },
                            Expr.Tuple returned
                        ]
                    })
                {
                    if (saved.ContainsKey(binding.Name) || binding.Name == bound || binding.Name == deferred?.Name)
                    {
                        return null;
                    }

                    tuple = returned;
                    call = producer;
                    result = binding.Name;
                }
                else if (expression is Expr.Tuple inline && inline.Items.OfType<Expr.Call>().ToArray() is [var embedded])
                {
                    call = embedded;
                    result = "\0case-call-result";
                    tuple = inline with
                    {
                        Items = inline.Items.Select(item => ReferenceEquals(item, embedded) ? new Expr.Variable(result) : item)
                            .ToArray()
                    };
                }
                else
                {
                    return null;
                }

                if (ReadCaseTuple(
                    tuple,
                    result,
                    saved,
                    deferredName: deferred?.Name
                ) is not { UsesSelector: true } construction
                    || ReadEntryCall(call, bound is null ? [] : [bound], SavedNames()) is not { } prepared)
                {
                    return null;
                }

                return new RegisterCaseResult(
                    null,
                    null,
                    Tuple: construction,
                    TupleCall: prepared,
                    DeferredField: deferred is { } field ? (0, field.Field) : null
                );
            }

            RegisterCaseResult? ReadConsTail(Expr.Call tail, string[] names)
            {
                var arguments = tail.Arguments.ToArray();
                int destination = Array.FindIndex(arguments, argument => argument is Expr.List { Items.Count: 1 });
                if (destination < 0 || destination >= FirstReservedXRegister
                    || arguments[destination] is not Expr.List { Items: [var member] } list
                    || ReadOperand(member) is not { } head
                    || ReadOperand(list.Tail ?? new Expr.Literal(Nil.Value)) is not { } rest)
                {
                    return null;
                }

                const string constructed = "\0case-cons";
                var registers = Enumerable.Repeat(VariableScopeNames.Wildcard, destination + 1).ToArray();
                registers[destination] = constructed;
                arguments[destination] = new Expr.Variable(constructed);

                return ReadEntryCall(tail with { Arguments = arguments }, registers, names) is { } call
                    ? new RegisterCaseResult(
                        null,
                        null,
                        call with { Call = tail },
                        Cons: new(destination, head, rest)
                    )
                    : null;
            }

            CaseTupleOperand? ReadOperand(Expr expression) => expression switch
            {
                Expr.Variable variable when saved.TryGetValue(variable.Name, out int slot) => new(YRegisterTag, slot, null),
                _ when AttributeLiteral.TryNormalizeConstant(expression, out Term constant) => new(0, 0, constant),
                _ => null
            };
        }

        private bool HasExhaustiveLiteralResults(Expr.Call call, IEnumerable<Term> keys)
        {
            var allowed = keys.ToHashSet();
            var results = new HashSet<Term>();
            var visited = new HashSet<(string Name, int Arity)>();

            return ReadCall(call) && results.SetEquals(allowed);

            bool ReadCall(Expr.Call target)
            {
                var signature = (target.Function, target.Arguments.Count);
                if (target is { Module: null, DynamicRemote: false }
                    && localFunctions.TryGetValue(signature, out var definition)
                    && !owner.IsDeclaredNative(signature))
                {
                    // A recursive edge adds no new return value beyond the terminal branches.
                    return !visited.Add(signature) || definition.Clauses.All(branch => ReadResult(branch.Body));
                }

                return owner.ResolvesErlangBif(target) && IsControlTransfer(signature.Function, signature.Count);
            }

            bool ReadResult(Expr expression)
            {
                switch (expression)
                {
                    case Expr.Call target:
                        return ReadCall(target);
                    case Expr.Sequence sequence:
                        return ReadResult(sequence.Items[^1]);
                    case Expr.Block block:
                        return ReadResult(block.Body);
                    case Expr.Case decision:
                        return decision.Clauses.All(branch => ReadResult(branch.Body));
                    case Expr.If conditional:
                        return conditional.Clauses.All(branch => ReadResult(branch.Body));
                    default:
                        if (AttributeLiteral.TryNormalizeConstant(expression, out Term literal) && allowed.Contains(literal))
                        {
                            results.Add(literal);

                            return true;
                        }

                        return false;
                }
            }
        }

        private static bool ReturnsMatchedValue(Pattern pattern, Expr result)
        {
            return (pattern, result) switch
            {
                (Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding, Expr.Variable returned) =>
                    binding.Name == returned.Name,
                (Pattern.Literal { Value: Atom or Integer or Nil } literal, Expr.Literal returned) =>
                    literal.Value.Equals(returned.Value),
                (Pattern.Tuple matched, Expr.Tuple rebuilt) => matched.Items.Count == rebuilt.Items.Count
                    && matched.Items.Zip(rebuilt.Items).All(pair => ReturnsMatchedValue(pair.First, pair.Second)),
                _ => false
            };
        }

        private static bool IsBooleanRegisterCaseSelector(EntryCall selector) =>
            (selector.Module, selector.Call.Function, selector.Call.Arguments.Count)
                is ("lists", "keymember", 3) or ("lists", "member", 2) or ("maps", "is_key", 2);

        private int RegisterCaseSelectorType(EntryCall call, byte opcode)
        {
            return (opcode, call.Module, call.Call.Function, call.Call.Arguments.Count) switch
            {
                (IsTuple, "lists", "keyfind", 3) or (IsTuple, "maps", "find" or "take", 2) => AtomTypeBits | TupleTypeBits,
                (SelectValue, null, _, _) when (InferPairFacts(call.Call) & PairFacts.Integer) != 0 => IntegerTypeBits,
                (ExactEqualTest, null, _, _) => ReadCallResultUnion(call.Call),
                _ => 0
            };
        }

        private void EmitRegisterCase(
            RegisterCase plan,
            int frame,
            BeamSourceLocation previous,
            int selectorType = 0,
            int? preparedFailure = null,
            (RegisterCaseResult Result, int Label)? sharedReturn = null
        )
        {
            int selectorLineCount = owner.lineInstructions;
            bool inheritedFallback = sharedReturn is { } outer && outer.Result == plan.Fallback;
            int? fallbackLabel = inheritedFallback ? sharedReturn!.Value.Label : null;
            if (fallbackLabel is null && preparedFailure is null
                && plan.Fallback == new RegisterCaseResult(plan.Fallback.Slot, plan.Fallback.Constant)
                && ((plan.Tests ?? [plan]).Any(test => test.Match?.NestedCase?.Plan.Fallback == plan.Fallback)
                    || plan.Dispatch?.Any(arm => arm.Result.NestedCase?.Plan.Fallback == plan.Fallback) == true))
            {
                fallbackLabel = Label();
            }

            if (plan.Dispatch is { } dispatch)
            {
                var labels = new int[dispatch.Count];
                var representatives = new Dictionary<RegisterCaseResult, int>();
                for (int index = labels.Length - 1; index >= 0; index--)
                {
                    RegisterCaseResult result = dispatch[index].Result;
                    if (plan.Fallback.FailureLocation is not null && representatives.TryGetValue(result, out int shared))
                    {
                        labels[index] = labels[shared];
                        continue;
                    }

                    labels[index] = Label();
                    representatives.TryAdd(result, index);
                }

                int fallback = fallbackLabel ?? Label();
                owner.EmitMembershipSelect(
                    0,
                    fallback,
                    dispatch.Select((branch, index) => (branch.Key, labels[index])),
                    selectorType
                );
                for (int index = labels.Length - 1; index >= 0; index--)
                {
                    RegisterCaseResult result = dispatch[index].Result;
                    if (plan.Fallback.FailureLocation is null || representatives[result] == index)
                    {
                        Mark(labels[index]);
                        EmitResult(result);
                    }
                }

                if (!inheritedFallback)
                {
                    Mark(fallback);
                    EmitResult(plan.Fallback);
                }

                return;
            }

            if (preparedFailure is null && plan.Fallback.FailureLocation is not null
                && plan.Tests is [
                { Opcode: IsTaggedTuple } tupleTest,
                { Opcode: ExactEqualTest } literalTest
                ])
            {
                int literalBranch = Label();
                int failure = Label();
                owner.Op(IsTuple, LabelTag, literalBranch);
                owner.Operand(XRegisterTag, 0);
                EmitTest(tupleTest, failure, knownTuple: true);
                Mark(literalBranch);
                EmitTest(literalTest, failure);
                Mark(failure);
            }
            else
            {
                var tests = plan.Tests ?? [plan];
                for (int index = 0; index < tests.Count; index++)
                {
                    int? common = index == tests.Count - 1 ? fallbackLabel : null;
                    int next = common ?? preparedFailure ?? Label();
                    EmitTest(tests[index], next);
                    if (common is null)
                    {
                        Mark(next);
                    }
                }
            }

            if (inheritedFallback)
            {
                return;
            }

            if (fallbackLabel is int label)
            {
                Mark(label);
            }

            EmitResult(plan.Fallback);

            void EmitTest(RegisterCase test, int next, bool knownTuple = false)
            {
                if (test.KnownTupleTag && test.Opcode == IsTaggedTuple)
                {
                    if (!knownTuple)
                    {
                        owner.Op(IsTuple, LabelTag, next);
                        owner.Operand(XRegisterTag, 0);
                    }

                    owner.Op(TestArity, LabelTag, next);
                    owner.Operand(XRegisterTag, 0);
                    owner.Operand(BeamWire.UnsignedTag, test.TupleArity);
                }
                else
                {
                    owner.Op(test.Opcode, LabelTag, next);
                    if (knownTuple)
                    {
                        owner.EmitTypedRegister(0, TupleTypeBits);
                    }
                    else if (selectorType == 0 || plan.Tests is not null)
                    {
                        owner.Operand(XRegisterTag, 0);
                    }
                    else
                    {
                        owner.EmitTypedRegister(0, selectorType);
                    }

                    if (test.TupleArity != 0)
                    {
                        owner.Operand(BeamWire.UnsignedTag, test.TupleArity);
                    }

                    if (test.Key is Term key)
                    {
                        owner.LiteralOperand(key);
                    }
                }

                if (test.NestedField is { } nested)
                {
                    owner.Op(GetTupleElement, XRegisterTag, 0);
                    owner.Operand(BeamWire.UnsignedTag, nested.Field);
                    owner.Operand(YRegisterTag, 0);
                    EmitTupleShapeTest(
                        YRegisterTag,
                        0,
                        nested.Arity,
                        next
                    );
                }

                if ((preparedFailure is not null || test.Opcode == IsTuple) && test.Field >= 0
                    && test.Match is { Tuple: { } constructed, TupleCall: null })
                {
                    EmitCaseTuple(constructed, (test.Field, test.FieldRegister));
                    owner.EmitFrameReturn(frame);
                }
                else
                {
                    bool heapChecked = false;
                    if (test.Field >= 0)
                    {
                        if (test.FieldSlot is int fieldSlot)
                        {
                            if (test.Match?.NestedCase?.Selector.RegisterConstructions is { } construction)
                            {
                                int words = construction.Constructions.Sum(item => item.HeapWords);
                                if (words > 0)
                                {
                                    owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                                    owner.Operand(BeamWire.UnsignedTag, Math.Max(1, construction.Live));
                                    heapChecked = true;
                                }
                            }

                            owner.Op(GetTupleElement, XRegisterTag, 0);
                            owner.Operand(BeamWire.UnsignedTag, test.Field);
                            owner.Operand(YRegisterTag, fieldSlot);
                        }
                        else
                        {
                            owner.EmitTuplePart(0, test.Field, test.FieldRegister);
                        }
                    }

                    EmitResult(test.Match!, heapChecked);
                }
            }

            void EmitResult(RegisterCaseResult result, bool heapChecked = false)
            {
                if (result.NestedCase is { } nested)
                {
                    var selector = nested.Selector;
                    if (nested.Spill is { } spill)
                    {
                        EmitSavedCallArguments(
                            new SavedCallStep(selector, spill.Slot, spill.Reuse, []),
                            Enumerable.Range(0, frame).ToArray()
                        );
                    }
                    else if (selector.RegisterConstructions is { } construction)
                    {
                        EmitCallArgumentConstructions(construction, -1, heapChecked: heapChecked);
                    }
                    else
                    {
                        owner.EmitOrderedTailMoves(selector.Moves, selector.Constants, selector.SavedInputs);
                    }

                    if (nested.ExpiredSlots.Length != 0)
                    {
                        owner.EmitSlotKills(nested.ExpiredSlots);
                    }

                    owner.EmitCallLine(selector.Call, previous);
                    Call(selector.Module, selector.Call.Function, selector.Call.Arguments.Count);
                    EmitRegisterCase(
                        nested.Plan,
                        frame,
                        CallLocation(selector.Call, previous),
                        RegisterCaseSelectorType(selector, nested.Plan.Opcode == IsTaggedTuple ? IsTuple : nested.Plan.Opcode),
                        sharedReturn: fallbackLabel is int target && nested.Plan.Fallback == plan.Fallback
                            ? (plan.Fallback, target) : null
                    );

                    return;
                }

                if (result.Sequence is { } sequence)
                {
                    EmitRegisterCaseCallSequence(sequence, previous, frame);

                    return;
                }

                if (result.FailureLocation is { } failure)
                {
                    if (failure != previous || selectorLineCount != owner.lineInstructions)
                    {
                        owner.EmitLine(failure);
                    }

                    owner.Op(CaseEnd, XRegisterTag, 0);

                    return;
                }

                if (result.Tail is { } tail)
                {
                    if (result.Cons is { } cons)
                    {
                        owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                        owner.Operand(BeamWire.UnsignedTag, 0);
                        owner.instructions.WriteByte(PutList);
                        EmitCaseOperand(cons.Head);
                        EmitCaseOperand(cons.Tail);
                        owner.Operand(XRegisterTag, cons.Destination);
                    }

                    owner.EmitOrderedTailMoves(tail.Moves, tail.Constants, tail.SavedInputs);
                    EmitPreparedFrameTail(tail, previous, frame);

                    return;
                }

                if (result.Tuple is { } tuple)
                {
                    int trimmed = 0;
                    Dictionary<int, int>? relocated = null;
                    if (result.TupleCall is { } untrimmedCall && result.KeepFrame)
                    {
                        owner.EmitOrderedTailMoves(untrimmedCall.Moves, untrimmedCall.Constants, untrimmedCall.SavedInputs);
                        owner.EmitCallLine(untrimmedCall.Call, previous);
                        Call(untrimmedCall.Module, untrimmedCall.Call.Function, untrimmedCall.Call.Arguments.Count);
                    }
                    else if (result.TupleCall is { } call)
                    {
                        var live = tuple.Steps.SelectMany(step => step.Operands)
                            .Where(operand => operand.Tag == YRegisterTag).Select(operand => operand.Register).ToHashSet();
                        if (result.DeferredField is { } field)
                        {
                            live.Add(field.Slot);
                        }

                        int hole = frame - 1;
                        while (trimmed < frame)
                        {
                            if (live.Contains(trimmed))
                            {
                                if (call.Module is not null && (result.DeferredField is null || IsPureTupleResultConversion(call)))
                                {
                                    break;
                                }

                                while (hole > trimmed && live.Contains(hole))
                                {
                                    hole--;
                                }

                                if (hole <= trimmed)
                                {
                                    break;
                                }

                                (relocated ??= []).Add(trimmed, hole);
                                live.Remove(trimmed);
                                live.Add(hole);
                                hole--;
                            }

                            trimmed++;
                        }

                        int[] expired = Enumerable.Range(trimmed, frame - trimmed)
                            .Where(slot => !live.Contains(slot)).ToArray();
                        int x0Constant = trimmed == 0 ? -1 : call.Constants.FindIndex(move => move.Destination == 0);
                        owner.EmitOrderedTailMoves(
                            call.Moves,
                            call.Constants,
                            call.SavedInputs,
                            expired,
                            deferX0Constant: x0Constant >= 0
                        );
                        if (relocated is not null)
                        {
                            foreach (var (source, destination) in relocated)
                            {
                                owner.MoveRegister(
                                    YRegisterTag,
                                    source,
                                    YRegisterTag,
                                    destination
                                );
                            }
                        }

                        if (trimmed != 0)
                        {
                            owner.Op(TrimFrame, BeamWire.UnsignedTag, trimmed);
                            owner.Operand(BeamWire.UnsignedTag, frame - trimmed);
                        }

                        if (x0Constant >= 0)
                        {
                            owner.EmitConstant(call.Constants[x0Constant].Value, 0);
                        }

                        owner.EmitCallLine(call.Call, previous);
                        Call(call.Module, call.Call.Function, call.Call.Arguments.Count);
                    }

                    EmitCaseTuple(
                        tuple,
                        trimmed: trimmed,
                        relocated: relocated,
                        deferred: result.DeferredField
                    );
                    owner.EmitFrameReturn(frame - trimmed);

                    return;
                }

                if (result.Slot is int slot)
                {
                    Load(slot);
                }
                else if (result.Constant is Term literal)
                {
                    owner.EmitConstant(literal, 0);
                }
                else if (result.Register is int register)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        register,
                        XRegisterTag,
                        0
                    );
                }

                owner.EmitFrameReturn(frame);
            }
        }

        private static bool IsPureTupleResultConversion(EntryCall call) =>
            call.Module == ErlangModuleNames.Module
            && (call.Call.Function, call.Call.Arguments.Count) is
                (AutoImportNames.AtomToList or AutoImportNames.ListToAtom or AutoImportNames.TupleToList
                    or AutoImportNames.ListToTuple or AutoImportNames.IntegerToList or AutoImportNames.ListToBinary, 1)
                or (AutoImportNames.ListToInteger, 1 or 2)
                or (AutoImportNames.BinaryToList, 1 or 3);

        private void EmitCaseTuple(
            CaseTuplePlan tuple,
            (int Field, int Register)? extraction = null,
            int trimmed = 0,
            IReadOnlyDictionary<int, int>? relocated = null,
            (int Slot, int Field)? deferred = null,
            int extractionSource = 0
        )
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.HeapWords);
            owner.Operand(BeamWire.UnsignedTag, tuple.EntryLive ?? (tuple.UsesSelector ? tuple.SelectorRegister + 1 : extraction is not null ? 1 : 0));
            if (extraction is { } field)
            {
                owner.EmitTuplePart(extractionSource, field.Field, field.Register);
            }

            if (deferred is { } saved)
            {
                owner.Op(GetTupleElement, YRegisterTag, SlotAfterTrim(saved.Slot));
                owner.Operand(BeamWire.UnsignedTag, saved.Field);
                owner.Operand(XRegisterTag, 1);
            }

            foreach (var (destination, operands) in tuple.Steps)
            {
                owner.EmitTupleHeader(XRegisterTag, operands.Length, destination);
                foreach (var operand in operands)
                {
                    EmitCaseOperand(operand.Tag == YRegisterTag
                        ? operand with { Register = SlotAfterTrim(operand.Register) }
                        : operand);
                }
            }

            int SlotAfterTrim(int slot) => (relocated?.GetValueOrDefault(slot, slot) ?? slot) - trimmed;
        }

        private void EmitCaseOperand(CaseTupleOperand operand)
        {
            if (operand.Literal is Term constant)
            {
                owner.LiteralOperand(constant);
            }
            else
            {
                owner.Operand(operand.Tag, operand.Register);
            }
        }

        private readonly record struct CaseTupleOperand(
            int Tag,
            int Register,
            Term? Literal,
            bool OriginalSelector = false
        );

        private sealed record CaseTuplePlan(
            int HeapWords,
            bool UsesSelector,
            List<(int Destination, CaseTupleOperand[] Operands)> Steps,
            int SelectorRegister = 0,
            int? EntryLive = null
        );

        private static CaseTuplePlan? ReadCaseTuple(
            Expr.Tuple tuple,
            string? selector,
            Dictionary<string, int> saved,
            int selectorRegister = 0,
            string? deferredName = null
        )
        {
            int remaining = selector is null ? 0 : CountSelectorUses(tuple);
            bool usesSelector = remaining != 0;
            int nextRegister = Math.Max(usesSelector ? selectorRegister + 1 : 1, deferredName is null ? 1 : 2);
            int words = 0;
            var steps = new List<(int Destination, CaseTupleOperand[] Operands)>();

            return ReadTuple(tuple, root: true) is not null ? new CaseTuplePlan(
                words,
                usesSelector,
                steps,
                selectorRegister
            ) : null;

            int CountSelectorUses(Expr expression) => expression switch
            {
                Expr.Variable variable when variable.Name == selector => 1,
                Expr.Tuple nested => nested.Items.Sum(CountSelectorUses),
                _ => 0
            };

            CaseTupleOperand? ReadTuple(Expr.Tuple value, bool root)
            {
                var operands = new CaseTupleOperand[value.Items.Count];
                for (int index = 0; index < operands.Length; index++)
                {
                    Expr item = value.Items[index];
                    if (AttributeLiteral.TryNormalizeConstant(item, out Term constant))
                    {
                        operands[index] = new CaseTupleOperand(0, 0, constant);
                    }
                    else if (item is Expr.Variable variable && variable.Name == selector)
                    {
                        operands[index] = new CaseTupleOperand(
                            XRegisterTag,
                            selectorRegister,
                            null,
                            OriginalSelector: true
                        );
                    }
                    else if (item is Expr.Variable field && field.Name == deferredName)
                    {
                        operands[index] = new CaseTupleOperand(XRegisterTag, 1, null);
                    }
                    else if (item is Expr.Variable savedInput && saved.TryGetValue(savedInput.Name, out int slot))
                    {
                        operands[index] = new CaseTupleOperand(YRegisterTag, slot, null);
                    }
                    else if (item is Expr.Tuple nested && ReadTuple(nested, root: false) is { } operand)
                    {
                        operands[index] = operand;
                    }
                    else
                    {
                        return null;
                    }
                }

                remaining -= operands.Count(operand => operand.OriginalSelector);
                bool consumesX0 = operands.Any(operand => operand.Tag == XRegisterTag && operand.Register == 0);
                int destination = root || consumesX0 && remaining == 0 ? 0 : nextRegister++;
                if (destination >= FirstReservedXRegister)
                {
                    return null;
                }

                words += operands.Length + 1;
                steps.Add((destination, operands));

                return new CaseTupleOperand(XRegisterTag, destination, null);
            }
        }

        private void EmitCatch(Expr.Catch caught)
        {
            if (AttributeLiteral.TryNormalizeConstant(caught.Operand, out Term constant))
            {
                owner.EmitConstant(constant, 0);

                return;
            }

            int initialSlot = nextSlot;
            var scope = new Dictionary<string, int>(variables, StringComparer.Ordinal);
            int token = Slot();
            int handler = Label();
            EnterCatch(token, handler);
            Expression(caught.Operand);
            EndCatch(token, handler);
            variables = scope;
            nextSlot = initialSlot;
        }

        private void EnterCatch(int token, int handler)
        {
            owner.Op(CatchEnter, YRegisterTag, token);
            owner.Operand(LabelTag, handler);
        }

        private void EndCatch(int token, int handler)
        {
            Mark(handler);
            owner.Op(CatchEnd, YRegisterTag, token);
        }
    }
}
