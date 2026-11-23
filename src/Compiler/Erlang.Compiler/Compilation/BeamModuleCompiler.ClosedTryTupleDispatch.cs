using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedAtomForwarder(FunctionDefinition function)
        {
            if (!CanEmitClosedTryTuple(function) || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom selector }, Pattern.Variable input],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable value] } call
                }
                ]
                || !DistinctBindings(input) || value.Name != input.Name
                || !HasOrdinaryClosedTryTarget(call) || !selector.Equals(ReadClosedForwardedAtom(function, 0)))
            {
                return false;
            }

            owner.MoveToX0(XRegisterTag, 1);
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entries[(call.Function, 1)]);

            return true;
        }

        private bool TryEmitClosedTryTupleDispatch(FunctionDefinition function)
        {
            if (!CanEmitClosedTryTuple(function) || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable selector, Pattern.Variable input, Pattern.Variable option],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Tuple { Items: [Pattern.Variable projected, Pattern.Variable discarded] },
                                Value: Expr.Call
                                {
                                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable chosen, Expr.Variable data]
                                } producer
                            } match,
                                Expr.Try
                            {
                                Clauses.Count: 0, After: null, Catches: [var caught],
                                Body: Expr.Tuple
                                {
                                    Items:
                                        [
                                            Expr.Literal { Value: Atom success },
                                            Expr.Call
                                        {
                                            Module: null, DynamicRemote: false,
                                            Arguments: [Expr.Variable kind, Expr.Variable value]
                                        } dispatch
                                        ]
                                }
                            }
                            ]
                    }
                }
                ]
                || !DistinctBindings(
                    selector,
                    input,
                    option,
                    projected,
                    discarded
                )
                || input.Name != data.Name || option.Name != chosen.Name
                || selector.Name != kind.Name || projected.Name != value.Name
                || !HasOrdinaryClosedTryTarget(producer) || !HasOrdinaryClosedTryTarget(dispatch)
                || ReadClosedForwardedAtom(function, 0) is not Atom knownSelector
                || ReadClosedTupleOrAtomFailure(producer) is not Atom matchFailure
                || !ReadClosedTryCatch(caught, [selector, input, option, projected, discarded], out var tags))
            {
                return false;
            }

            var continuation = ReserveTaggedTryContinuation();
            int badmatch = Label();

            EmitFrameAllocation(3, 1);
            EmitFrameInitialization(0, 1);
            owner.MoveToX0(XRegisterTag, 2);
            owner.EmitCallLine(producer, location);
            owner.EmitFieldCall(2, entries[(producer.Function, 2)]);
            owner.Op(IsTuple, LabelTag, badmatch);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            owner.EmitTupleElement(0, 1);
            owner.Op(TryEnter, YRegisterTag, 0);
            owner.Operand(LabelTag, continuation.Handler);
            owner.EmitConstant(knownSelector, 0);
            owner.EmitCallLine(dispatch, CallLocation(producer, location));
            owner.EmitFieldCall(2, entries[(dispatch.Function, 2)]);
            EmitTaggedTryContinuation(
                success,
                tags,
                1,
                continuation
            );

            Mark(badmatch);
            var previous = CallLocation(dispatch, CallLocation(producer, location));
            owner.EmitArithmeticLine(match.SourceLine, ref previous);
            owner.Op(BadMatch, AtomTag, owner.Intern(matchFailure.Name));

            return true;
        }

        private bool TryEmitGuardedTaggedTryDispatch(FunctionDefinition function)
        {
            if (!CanEmitClosedTryTuple(function) || function.Clauses is not [var guarded, { Guard: null } plain]
                || ReadTaggedTryCall(guarded) is not { } projected || ReadTaggedTryCall(plain) is not { } direct
                || guarded.Guard is not Expr.Call
                {
                    DynamicRemote: false, Function: AutoImportNames.IsBinary, Arguments: [Expr.Variable tested]
                } guard || tested.Name != projected.Input.Name || !owner.ResolvesErlangBif(guard)
                || projected.Call.Arguments[1] is not Expr.Call
                {
                    DynamicRemote: false, Function: AutoImportNames.Element,
                    Arguments: [Expr.Literal { Value: Integer position }, Expr.Call
                    {
                        Arguments: [Expr.Variable producerInput]
                    } producer]
                } projection
                || position.Value <= 0 || position.Value > int.MaxValue
                || !owner.ResolvesErlangBif(projection) || !HasOrdinaryClosedTryTarget(producer)
                || producerInput.Name != projected.Input.Name
                || direct.Call.Arguments[1] is not Expr.Variable directInput || directInput.Name != direct.Input.Name
                || projected.Call.Function != direct.Call.Function)
            {
                return false;
            }

            var projectedContinuation = ReserveTaggedTryContinuation();
            int nonBinary = Label();
            owner.Op(IsBinary, LabelTag, nonBinary);
            owner.Operand(XRegisterTag, 1);
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.Op(TryEnter, YRegisterTag, 1);
            owner.Operand(LabelTag, projectedContinuation.Handler);
            owner.MoveToX0(XRegisterTag, 1);
            owner.EmitCallLine(producer, location);
            owner.EmitFieldCall(1, entries[(producer.Function, 1)]);
            var producerLocation = CallLocation(producer, location);
            owner.EmitCallLine(projection, producerLocation);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Element, 2));
            owner.LiteralOperand(position);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.MoveToX0(YRegisterTag, 0);
            EmitFrameInitialization(0, 1);
            owner.EmitCallLine(projected.Call, CallLocation(projection, producerLocation));
            owner.EmitFieldCall(2, entries[(projected.Call.Function, 2)]);
            EmitTaggedTryContinuation(
                projected.Success,
                projected.Tags,
                2,
                projectedContinuation
            );

            var directContinuation = ReserveTaggedTryContinuation();
            Mark(nonBinary);
            EmitFrameAllocation(2, 1);
            owner.Op(TryEnter, YRegisterTag, 0);
            owner.Operand(LabelTag, directContinuation.Handler);
            owner.EmitCallLine(direct.Call, location);
            owner.EmitFieldCall(2, entries[(direct.Call.Function, 2)]);
            EmitTaggedTryContinuation(
                direct.Success,
                direct.Tags,
                1,
                directContinuation
            );

            return true;
        }

        private (Pattern.Variable Input, Expr.Call Call, Atom Success, (Atom Wrapped, Atom Inner) Tags)? ReadTaggedTryCall(Clause clause)
        {
            if (clause is not
                {
                    Patterns: [Pattern.Variable selector, Pattern.Variable input],
                    Body: Expr.Try
                    {
                        Clauses.Count: 0, After: null, Catches: [var caught],
                        Body: Expr.Tuple
                        {
                            Items:
                                [
                                    Expr.Literal { Value: Atom success },
                                    Expr.Call { Arguments: [Expr.Variable selected, _] } call
                                ]
                        }
                    }
                }
                || !DistinctBindings(selector, input) || selected.Name != selector.Name || !HasOrdinaryClosedTryTarget(call)
                || !ReadClosedTryCatch(caught, [selector, input], out var tags))
            {
                return null;
            }

            return (input, call, success, tags);
        }

        private (int Handler, int Accept, int Unwrapped, int Rethrow) ReserveTaggedTryContinuation() =>
            (Label(), Label(), Label(), Label());

        private void EmitTaggedTryContinuation(
            Atom success,
            (Atom Wrapped, Atom Inner) tags,
            int frame,
            (int Handler, int Accept, int Unwrapped, int Rethrow) continuation
        )
        {
            int token = frame - 1;
            owner.Op(TryEnd, YRegisterTag, token);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitTagTuple(success, XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(continuation.Handler);
            owner.Op(TryCase, YRegisterTag, token);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                token
            );
            owner.instructions.WriteByte(SelectValue);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.Operand(LabelTag, continuation.Rethrow);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(Term.A(ErlangExceptionClasses.Error));
            owner.Operand(LabelTag, continuation.Accept);
            owner.LiteralOperand(Term.A(ErlangExceptionClasses.Exit));
            owner.Operand(LabelTag, continuation.Accept);

            Mark(continuation.Accept);
            EmitCatchTag(1, tags.Wrapped, continuation.Unwrapped);
            owner.EmitTuplePart(1, 1, 0);
            EmitCatchTag(0, tags.Inner, continuation.Unwrapped);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, token);
            EmitCaughtResult();

            Mark(continuation.Unwrapped);
            EmitCaughtResult();

            Mark(continuation.Rethrow);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);

            void EmitCatchTag(int register, Atom tag, int failure)
            {
                owner.Op(IsTaggedTuple, LabelTag, failure);
                owner.Operand(XRegisterTag, register);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.LiteralOperand(tag);
            }

            void EmitCaughtResult()
            {
                owner.MoveToX0(XRegisterTag, 2);
                owner.instructions.WriteByte(BuildStacktrace);
                owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.Operand(YRegisterTag, token);
                owner.Operand(XRegisterTag, 0);
                EmitTagTuple(tags.Inner, XRegisterTag, 0);
                EmitTagTuple(tags.Wrapped, XRegisterTag, 0);
                owner.EmitFrameReturn(frame);
            }

            void EmitTagTuple(Atom tag, int fieldKind, int field)
            {
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(tag);
                owner.Operand(fieldKind, field);
            }
        }

        private static bool ReadClosedTryCatch(
            Clause caught,
            Pattern.Variable[] parameters,
            out (Atom Wrapped, Atom Inner) tags
        )
        {
            tags = default;
            if (caught is not
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Variable kind, Pattern.Variable reason, Pattern.Variable stack] }],
                    Body: Expr.Case
                    {
                        Value: Expr.Variable inspected,
                        Clauses:
                        [
                        {
                            Guard: null,
                            Patterns:
                                [
                                    Pattern.Tuple
                                {
                                    Items:
                                        [
                                            Pattern.Literal { Value: Atom wrapped },
                                            Pattern.Tuple { Items: [Pattern.Literal { Value: Atom inner }, Pattern.Variable unwrapped] }
                                        ]
                                }
                                ],
                            Body: var recognized
                        },
                        { Guard: null, Patterns: [Pattern.Variable original], Body: var fallback }
                        ]
                    }
                }
                || !DistinctBindings([.. parameters, kind, reason, stack, unwrapped])
                || !DistinctBindings([.. parameters, kind, reason, stack, original])
                || inspected.Name != reason.Name
                || !ReadCatchClasses(caught.Guard, kind.Name)
                || !ReturnsTaggedStack(recognized, unwrapped.Name) || !ReturnsTaggedStack(fallback, original.Name))
            {
                return false;
            }

            tags = (wrapped, inner);

            return true;

            bool ReturnsTaggedStack(Expr expression, string expected) => expression is Expr.Tuple
            {
                Items:
                [
                    Expr.Literal { Value: Atom outerTag },
                    Expr.Tuple
                {
                    Items:
                        [
                            Expr.Literal { Value: Atom innerTag },
                            Expr.Tuple { Items: [Expr.Variable captured, Expr.Variable trace] }
                        ]
                }
                ]
            } && outerTag.Equals(wrapped) && innerTag.Equals(inner) && captured.Name == expected && trace.Name == stack.Name;
        }

        private static bool ReadCatchClasses(Expr? guard, string parameter)
        {
            IReadOnlyList<Expr>? alternatives = guard switch
            {
                Expr.GuardAlternatives { Items.Count: 2 } choices => choices.Items,
                Expr.Binary { Operator: ErlangOperators.OrElse } disjunction => [disjunction.Left, disjunction.Right],
                _ => null
            };

            return alternatives is [var first, var second]
                && (ReadsClass(first, ErlangExceptionClasses.Error) && ReadsClass(second, ErlangExceptionClasses.Exit)
                    || ReadsClass(second, ErlangExceptionClasses.Error) && ReadsClass(first, ErlangExceptionClasses.Exit));

            bool ReadsClass(Expr expression, string name) => expression is Expr.Binary
            {
                Operator: ErlangOperators.ExactEqual,
                Left: Expr.Variable variable,
                Right: Expr.Literal { Value: Atom atom }
            } && variable.Name == parameter && atom.Name == name;
        }

        private Atom? ReadClosedTupleOrAtomFailure(Expr.Call root)
        {
            var seen = new HashSet<(string, int)>();
            Atom? failure = null;
            bool allowsTuple = false;
            bool proven = ReadCall(root);

            return proven && allowsTuple ? failure : null;

            bool ReadCall(Expr.Call call)
            {
                if (!HasOrdinaryClosedTryTarget(call))
                {
                    return false;
                }

                var signature = (call.Function, call.Arguments.Count);

                // A recursive tail path has no independent result. Check every
                // exit from each visited function before accepting the summary.
                return !seen.Add(signature) || localFunctions[signature].Clauses.All(clause => Read(clause.Body));
            }

            bool Read(Expr expression)
            {
                switch (expression)
                {
                    case Expr.Tuple { Items.Count: 2 }:
                        allowsTuple = true;

                        return true;
                    case Expr.Literal { Value: Atom atom }:
                        if (failure is not null && !failure.Equals(atom))
                        {
                            return false;
                        }

                        failure = atom;

                        return true;
                    case Expr.Sequence { Items: [.., var last] }:
                        return Read(last);
                    case Expr.Case decision:
                        return decision.Clauses.All(clause => Read(clause.Body));
                    case Expr.Call call:
                        return ReadCall(call);
                    default:
                        return false;
                }
            }
        }

        private Atom? ReadClosedForwardedAtom(FunctionDefinition function, int register)
        {
            var active = new HashSet<(string, int, int)>();

            return Read(function, register);

            Atom? Read(FunctionDefinition target, int argument)
            {
                if (!CanEmitClosedTryTuple(target) || !active.Add((target.Name, target.Arity, argument)))
                {
                    return null;
                }

                Atom? result = null;
                bool proven = HasOnlyLocalCalls(
                    target,
                    target.Arity,
                    (caller, call) =>
                    {
                        Atom? value = call.Arguments[argument] switch
                        {
                            Expr.Literal { Value: Atom constant } => constant,
                            Expr.Variable variable when ReadForwardedArgument(caller, call, variable.Name) is int index => Read(caller, index),
                            _ => null
                        };
                        if (value is null || result is not null && !result.Equals(value))
                        {
                            return false;
                        }

                        result = value;

                        return true;
                    }
                );
                active.Remove((target.Name, target.Arity, argument));

                return proven ? result : null;
            }
        }

        private static int? ReadForwardedArgument(FunctionDefinition caller, Expr.Call call, string name)
        {
            if (caller.Clauses is not [{ Guard: null } clause]
                || clause.Patterns.Any(pattern => pattern is not Pattern.Variable { })
                || clause.Patterns.OfType<Pattern.Variable>().Count(pattern => pattern.Name == name) != 1
                || name == VariableScopeNames.Wildcard)
            {
                return null;
            }

            Expr expression = clause.Body is Expr.Sequence { Items: [.., var last] } ? last : clause.Body;
            if (expression is Expr.Try @try)
            {
                expression = @try.Body;
            }

            if (!ReferenceEquals(expression, call)
                && !(expression is Expr.Tuple tuple && tuple.Items.Any(item => ReferenceEquals(item, call))))
            {
                return null;
            }

            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                if (clause.Patterns[index] is Pattern.Variable variable && variable.Name == name)
                {
                    return index;
                }
            }

            return null;
        }

        private bool CanEmitClosedTryTuple(FunctionDefinition function) => owner.SsaTypeInferenceEnabled
            && !owner.ModuleOptimizationDisabled && owner.OnLoadFunction is null && recursiveClosure is null
            && !owner.IsDeclaredNative((function.Name, function.Arity));

        private bool HasOrdinaryClosedTryTarget(Expr.Call call)
        {
            var signature = (call.Function, call.Arguments.Count);

            return call is { Module: null, DynamicRemote: false } && entries.ContainsKey(signature)
                && localFunctions.ContainsKey(signature) && !owner.IsDeclaredNative(signature);
        }
    }
}
