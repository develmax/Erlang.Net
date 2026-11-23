using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedListMembershipLoop(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns:
                            [
                                Pattern.List { Items: [Pattern.Variable item], Tail: Pattern.Variable rest },
                                Pattern.List { Items: [Pattern.Variable key], Tail: Pattern.Variable keys },
                                Pattern.Variable tail
                            ],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: ListModuleNames.Module, Function: ListModuleNames.Member, DynamicRemote: false,
                            Arguments: [Expr.Variable compared, Expr.Variable options]
                        } member,
                        Clauses:
                                [
                                {
                                    Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } }],
                                    Body: Expr.Call { Arguments: [Expr.Literal rejected] } reject
                                },
                                {
                                    Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }],
                                    Body: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false,
                                        Arguments: [Expr.Variable nextItem, Expr.Variable nextKey, Expr.Variable nextTail]
                                    } recurse
                                }
                                ]
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable remaining, var empty, Pattern.Variable expected],
                    Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } yes }
                },
                {
                    Guard: null,
                    Patterns:
                            [
                                Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                Pattern.Variable { Name: VariableScopeNames.Wildcard }
                            ],
                    Body: Expr.Call { Arguments: [Expr.Literal wrong] } fallback
                }
                ]
                || !DistinctBindings(
                    item,
                    rest,
                    key,
                    keys,
                    tail
                )
                || compared.Name != item.Name || options.Name != key.Name
                || nextItem.Name != rest.Name || nextKey.Name != keys.Name || nextTail.Name != tail.Name
                || recurse.Function != function.Name || !entries.TryGetValue((function.Name, 3), out int entry)
                || remaining.Name == VariableScopeNames.Wildcard || remaining.Name != expected.Name || !IsEmptyListPattern(empty)
                || ReadStackFreeThrow(reject, requireCallerHandler: true) is not { } rejectRaise
                || ReadStackFreeThrow(fallback, requireCallerHandler: true) is not { } fallbackRaise
                || !HasClosedListMembershipInput(function, recurse))
            {
                return false;
            }

            int absent = Label();
            int exhausted = Label();
            int mismatch = Label();
            owner.EmitNonemptyList(0, exhausted);
            owner.EmitListParts(
                0,
                XRegisterTag,
                3,
                XRegisterTag,
                4
            );
            owner.EmitNonemptyList(1, exhausted);
            EmitFrameAllocation(arity: 5, size: 3);
            owner.MoveRegister(
                XRegisterTag,
                4,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                2
            );
            owner.EmitListParts(
                1,
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.MoveToX0(XRegisterTag, 3);
            owner.EmitCallLine(member, location);
            Call(member.Module, member.Function, 2);
            owner.Op(ExactEqualTest, LabelTag, absent);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(yes);
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
                2
            );
            owner.MoveToX0(YRegisterTag, 1);
            EmitTailLocal(entry, 3, 3);

            Mark(absent);
            EmitRaise(
                rejectRaise,
                rejected.Value,
                CallLocation(member, location),
                frame: 3
            );

            Mark(exhausted);
            owner.Op(IsNil, LabelTag, mismatch);
            owner.EmitTypedRegister(1, ProperListTypeBits);
            owner.Op(ExactEqualTest, LabelTag, mismatch);
            owner.EmitTypedRegister(2, ConsTypeBits);
            owner.EmitTypedRegister(0, ProperListTypeBits);
            owner.EmitConstant(yes, 0);
            owner.instructions.WriteByte(Return);

            Mark(mismatch);
            EmitRaise(
                fallbackRaise,
                wrong.Value,
                location,
                frame: null
            );

            return true;

            void EmitRaise(
                Expr.Call call,
                Term reason,
                BeamSourceLocation previous,
                int? frame
            )
            {
                owner.EmitConstant(reason, 1);
                owner.EmitConstant(Nil.Value, 2);
                owner.EmitConstant(Term.A(ErlangExceptionClasses.Throw), 0);
                owner.EmitCallLine(call, previous);
                owner.Op(frame.HasValue ? TailExternalCallWithFrame : TailExternalCall, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, 3));
                if (frame is int size)
                {
                    owner.Operand(BeamWire.UnsignedTag, size);
                }
            }
        }

        private bool HasClosedListMembershipInput(FunctionDefinition function, Expr.Call self)
        {
            var signature = (function.Name, function.Arity);
            if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature))
            {
                return false;
            }

            bool found = false;
            foreach (var caller in localFunctions.Values)
            {
                foreach (var expression in LocalFunctionEntries.ClauseExpressions(caller.Clauses).SelectMany(LocalFunctionEntries.Walk))
                {
                    if (expression is Expr.FunctionReference reference && reference.Name == signature.Name && reference.Arity == signature.Arity)
                    {
                        return false;
                    }

                    if (expression is not Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 3 } call
                        || call.Function != signature.Name || ReferenceEquals(call.Arguments, self.Arguments))
                    {
                        continue;
                    }

                    if (caller.Clauses is not
                        [
                        {
                            Patterns:
                                    [
                                        Pattern.Variable argument,
                                        Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] }
                                    ],
                            Guard: Expr.Call
                            {
                                Function: AutoImportNames.IsTuple, DynamicRemote: false,
                                Arguments: [Expr.Variable tested]
                            } guard,
                            Body: Expr.Call
                            {
                                Arguments:
                                        [
                                            Expr.Call
                                        {
                                            Function: AutoImportNames.TupleToList, DynamicRemote: false,
                                            Arguments: [Expr.Variable source]
                                        } convert,
                                            Expr.Variable list,
                                            Expr.Variable ending
                                        ]
                            } forwarded
                        },
                            _
                        ] || !ReferenceEquals(forwarded.Arguments, call.Arguments)
                        || !DistinctBindings(argument, first, second) || tested.Name != argument.Name || source.Name != argument.Name
                        || list.Name != first.Name || ending.Name != second.Name
                        || !owner.ResolvesErlangBif(guard) || !owner.ResolvesErlangBif(convert)
                        || !HasClosedPairInput(caller, IsListPair))
                    {
                        return false;
                    }

                    found = true;
                }
            }

            return found;

            static bool IsListPair(Expr expression) =>
                AttributeLiteral.TryNormalizeConstant(expression, out Term value)
                && value is TupleTerm { Items: [var prefix, Cons tail] }
                && IsProper(prefix) && IsProper(tail);

            static bool IsProper(Term value)
            {
                while (value is Cons cell)
                {
                    value = cell.Tail;
                }

                return value is Nil;
            }
        }
    }
}
