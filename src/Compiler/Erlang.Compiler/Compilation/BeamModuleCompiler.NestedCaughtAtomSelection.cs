using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedCaughtAtomSelection(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [global::Erlang.Pattern.Variable member, AliasPattern alias,
                            global::Erlang.Pattern.Variable sender, global::Erlang.Pattern.Variable state],
                        Body: Expr.Try
                        {
                            Body: Expr.Sequence
                            {
                                Items:
                            [
                                Expr.Match
                            {
                                Pattern: global::Erlang.Pattern.Variable decision,
                                Value: Expr.Try
                                {
                                    Body: Expr.Call
                                    {
                                        Arguments:
                                            [var table, Expr.Tuple { Items: [Expr.Variable lookupKey, var keySuffix] }, var position]
                                    } lookup,
                                    Clauses: [],
                                    Catches:
                                        [
                                        {
                                            Guard: null,
                                            Patterns: [global::Erlang.Pattern.Tuple
                                            {
                                                Items:
                                                [
                                                    global::Erlang.Pattern.Literal { Value: Atom caughtClass },
                                                    global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                                    global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }
                                                ]
                                            }],
                                            Body: var defaultResult
                                        }
                                        ],
                                    After: null
                                }
                            },
                                Expr.If { Clauses: [var accepted, var rejected] }
                            ]
                            },
                            Clauses: [],
                            Catches: [var directCatch, var wrappedCatch],
                            After: null
                        }
                    }
                ]
                || !TryReadPatternAlias(alias, out var arguments, out var shape)
                || shape is not global::Erlang.Pattern.List
                {
                    Items: [global::Erlang.Pattern.Variable head],
                    Tail: global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }
                }
                || !DistinctBindings(
                    member,
                    arguments,
                    sender,
                    state,
                    head,
                    decision
                ) || head.Name != lookupKey.Name
                || !AttributeLiteral.TryNormalizeConstant(table, out Term lookupTable)
                || !AttributeLiteral.TryNormalizeConstant(keySuffix, out Term keyConstant)
                || !AttributeLiteral.TryNormalizeConstant(position, out Term lookupPosition)
                || !AttributeLiteral.TryNormalizeConstant(defaultResult, out Term defaultValue)
                || !TryResolveOrdinaryCallTarget(lookup, out string? lookupModule) || lookupModule is null)
            {
                return false;
            }

            var keys = new List<Atom>();
            if (accepted.Guard is not { } guard || !ReadAtomGuardKeys(guard, decision.Name, keys)
                || keys.Count < 2 || keys.Distinct().Count() != keys.Count
                || defaultValue is Atom atom && keys.Contains(atom)
                || accepted.Body is not
                Expr.Tuple
                {
                    Items:
                [
                    Expr.Literal { Value: Atom reply },
                    Expr.Call
                {
                    Arguments:
                    [
                        Expr.Literal { Value: Atom target },
                        Expr.Variable functionName,
                        Expr.List { Items: [Expr.Variable choice], Tail: Expr.Variable rest }
                    ]
                } invoke,
                    Expr.Variable kept
                ]
                } || functionName.Name != member.Name || choice.Name != decision.Name
                || rest.Name != arguments.Name || kept.Name != state.Name
                || !TryResolveOrdinaryCallTarget(invoke, out string? invokeModule) || invokeModule is null
                || rejected is not
                {
                    Guard: Expr.Literal { Value: Atom { Name: "true" } },
                    Body: Expr.Sequence
                    {
                        Items:
                    [
                        Expr.Match
                    {
                        Pattern: global::Erlang.Pattern.Variable resource,
                        Value: Expr.Call
                        {
                            Module: null, DynamicRemote: false, Arguments:
                            [
                                Expr.Literal { Value: Atom fallbackTarget }, Expr.Variable fallbackName,
                                Expr.Variable fallbackArguments, Expr.Variable fallbackSender
                            ]
                        } prepare
                    },
                        Expr.Match
                    {
                        Pattern: global::Erlang.Pattern.Variable ignored,
                        Value: Expr.Call { Arguments: [Expr.Variable effectInput] } effect
                    },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom deferred }, Expr.Variable deferredState] }
                    ]
                    }
                } || !fallbackTarget.Equals(target) || fallbackName.Name != member.Name
                || fallbackArguments.Name != arguments.Name || fallbackSender.Name != sender.Name
                || effectInput.Name != resource.Name || deferredState.Name != state.Name
                || !entries.TryGetValue((prepare.Function, 4), out int prepareEntry)
                || !TryResolveOrdinaryCallTarget(effect, out string? effectModule))
            {
                return false;
            }

            if (directCatch is not
                {
                    Guard: null,
                    Patterns: [global::Erlang.Pattern.Tuple
                    {
                        Items:
                    [
                        global::Erlang.Pattern.Literal { Value: Atom { Name: ErlangExceptionClasses.Throw } throwClass },
                        global::Erlang.Pattern.Variable directReason,
                        global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    ]
                    }],
                    Body: Expr.Tuple
                    {
                        Items:
                        [Expr.Literal { Value: Atom directTag }, Expr.Variable directValue, Expr.Variable directState]
                    }
                }
                || wrappedCatch is not
                {
                    Guard: null,
                    Patterns: [global::Erlang.Pattern.Tuple
                    {
                        Items:
                    [
                        global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        global::Erlang.Pattern.Variable wrappedReason,
                        global::Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    ]
                    }],
                    Body: Expr.Tuple
                    {
                        Items:
                    [
                        Expr.Literal { Value: Atom wrappedTag },
                        Expr.Tuple
                    {
                        Items:
                        [
                            Expr.Literal { Value: Atom wrapped },
                            Expr.Tuple { Items: [Expr.Literal { Value: Atom inner }, Expr.Variable exception] }
                        ]
                    },
                        Expr.Variable wrappedState
                    ]
                    }
                } || !directTag.Equals(reply) || !wrappedTag.Equals(reply)
                || directValue.Name != directReason.Name || exception.Name != wrappedReason.Name
                || directState.Name != state.Name || wrappedState.Name != state.Name
                || !DistinctBindings(
                    member,
                    arguments,
                    sender,
                    state,
                    head,
                    decision,
                    resource,
                    directReason,
                    wrappedReason
                ) || new[]
                {
                    member.Name, arguments.Name, sender.Name, state.Name, head.Name,
                    decision.Name, resource.Name, directReason.Name, wrappedReason.Name
                }.Contains(ignored.Name))
            {
                return false;
            }

            const int argumentsCount = 4;
            const int stateSlot = 0;
            const int senderSlot = 1;
            const int listSlot = 2;
            const int memberSlot = 3;
            const int innerToken = 4;
            const int outerToken = 5;
            const int frame = 6;
            int functionFailure = owner.nextLabel - 2;
            int selected = Label();
            int innerHandler = Label();
            int fallback = Label();
            int returned = Label();
            int reraise = Label();
            int outerHandler = Label();
            int wrap = Label();

            owner.EmitNonemptyList(1, functionFailure);
            EmitFrameAllocation(argumentsCount, frame);
            EmitFrameInitialization(innerToken, outerToken);
            for (int register = argumentsCount - 1; register >= 0; register--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    memberSlot - register
                );
            }

            owner.Op(GetHead, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);

            // The list check and head extraction precede the outer try.
            // Once it is installed, both branch calls and inner reraises remain protected.
            Enter(outerToken, outerHandler);
            Heap(3, 1);
            owner.Op(PutTuple, XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(keyConstant);
            Enter(innerToken, innerHandler);
            owner.EmitConstant(lookupPosition, 2);
            owner.EmitConstant(lookupTable, 0);
            owner.EmitCallLine(lookup, location);
            Call(lookupModule, lookup.Function, 3);
            owner.Op(TryEnd, YRegisterTag, innerToken);
            var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            owner.EmitMembershipSelect(0, fallback, keys.Order(order).Select(key => ((Term)key, selected)));

            Mark(selected);
            Heap(ConsHeapWords, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, listSlot);
            owner.Operand(XRegisterTag, 2);
            Restore(memberSlot, 1);
            EmitFrameInitialization(senderSlot, innerToken);
            owner.EmitConstant(target, 0);
            owner.EmitCallLine(invoke, location);
            Call(invokeModule, invoke.Function, 3);
            Heap(4, 1);
            Reply(XRegisterTag, 0);
            owner.Op(Jump, LabelTag, returned);

            Mark(innerHandler);
            owner.Op(TryCase, YRegisterTag, innerToken);
            owner.Op(ExactEqualTest, LabelTag, reraise);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(caughtClass);

            // The closed default cannot satisfy the selected atom alternatives,
            // so the matching catch passes directly to the original fallback.
            Mark(fallback);
            Restore(senderSlot, 3);
            Restore(listSlot, 2);
            Restore(memberSlot, 1);
            EmitFrameInitialization(senderSlot, innerToken);
            owner.EmitConstant(target, 0);
            owner.EmitCallLine(prepare, location);
            owner.EmitFieldCall(4, prepareEntry);
            owner.EmitCallLine(effect, CallLocation(prepare, location));
            Call(effectModule, effect.Function, 1);
            Heap(3, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(deferred.Name)), (YRegisterTag, stateSlot));
            Mark(returned);
            owner.Op(TryEnd, YRegisterTag, outerToken);
            owner.EmitFrameReturn(frame);

            Mark(reraise);
            owner.Op(Raise, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);

            Mark(outerHandler);
            owner.Op(TryCase, YRegisterTag, outerToken);
            owner.Op(ExactEqualTest, LabelTag, wrap);
            owner.EmitTypedRegister(0, AtomTypeBits);
            owner.LiteralOperand(throwClass);
            Heap(4, 2);
            Reply(XRegisterTag, 1);
            owner.EmitFrameReturn(frame);

            Mark(wrap);
            Heap(10, 2);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(inner.Name)), (XRegisterTag, 1));
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(wrapped.Name)), (XRegisterTag, 0));
            Reply(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            return true;

            void Enter(int token, int handler)
            {
                owner.Op(TryEnter, YRegisterTag, token);
                owner.Operand(LabelTag, handler);
            }

            void Heap(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void Restore(int slot, int register) => owner.MoveRegister(
                YRegisterTag,
                slot,
                XRegisterTag,
                register
            );

            void Reply(int tag, int register) => owner.EmitTagMatchTuple(
                0,
                (AtomTag, owner.Intern(reply.Name)),
                (tag, register),
                (YRegisterTag, stateSlot)
            );
        }
    }
}
