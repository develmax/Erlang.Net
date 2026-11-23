using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCountedTermFold(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload] }],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable unwrapped]
                    } unwrap
                },
                {
                    Patterns: [Pattern.Variable value],
                    Guard: Expr.Call { Function: AutoImportNames.IsTuple, Arguments: [Expr.Variable tested] } tupleGuard,
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                                [
                                    Expr.Call { Function: AutoImportNames.TupleToList, Arguments: [Expr.Variable input] } conversion
                                ]
                    } fromTuple
                },
                {
                    Guard: null,
                    Patterns: [Pattern.List
                    {
                        Items: [Pattern.Variable first, Pattern.Variable second], Tail: Pattern.Variable tail
                    }],
                    Body: Expr.Sequence
                    {
                        Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable combined,
                                    Value: Expr.Binary
                                    {
                                        Operator: ErlangOperators.Plus,
                                        Left: Expr.Binary
                                        {
                                            Operator: ErlangOperators.Multiply,
                                            Left: Expr.Literal { Value: Integer baseValue }, Right: Expr.Variable multiplier
                                        } multiply,
                                        Right: Expr.Variable addend
                                    } combine
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable head, Pattern.Variable headSize] },
                                    Value: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable raw]
                                    } encode
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable mapped, Pattern.Variable mappedSize] },
                                    Value: Expr.Call
                                    {
                                        Module: "lists", Function: "mapfoldl", DynamicRemote: false,
                                        Arguments:
                                                [
                                                    Expr.FunctionReference { Arity: 2 } callback,
                                                    Expr.Literal { Value: Integer initial }, Expr.Variable remaining
                                                ]
                                    } fold
                                },
                                    Expr.Tuple
                                {
                                    Items:
                                            [
                                                Expr.List { Items: [Expr.Variable item], Tail: Expr.Variable rest },
                                                Expr.Binary
                                            {
                                                Operator: ErlangOperators.Plus, Left: Expr.Variable left, Right: Expr.Variable right
                                            } length
                                            ]
                                }
                                ]
                    }
                }
                ]
                || unwrap.Function != function.Name || fromTuple.Function != function.Name
                || payload.Name == VariableScopeNames.Wildcard || value.Name == VariableScopeNames.Wildcard
                || payload.Name != unwrapped.Name || value.Name != tested.Name || value.Name != input.Name
                || !owner.ResolvesErlangBif(tupleGuard) || !owner.ResolvesErlangBif(conversion)
                || !DistinctBindings(
                    first,
                    second,
                    tail,
                    combined,
                    head,
                    headSize,
                    mapped,
                    mappedSize
                )
                || multiplier.Name != first.Name || addend.Name != second.Name || combined.Name != raw.Name
                || !initial.Value.IsZero || remaining.Name != tail.Name
                || head.Name != item.Name || mapped.Name != rest.Name
                || left.Name != headSize.Name || right.Name != mappedSize.Name
                || baseValue.Value <= 1 || baseValue.Value > int.MaxValue
                || !entries.TryGetValue((function.Name, 1), out int entry)
                || !entries.TryGetValue((encode.Function, 1), out int coder)
                || !entries.TryGetValue((callback.Name, 2), out int folder)
                || !HasCountedTermPair(encode)
                || !HasCountedTermCallback(callback, encode.Function))
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int tuple = Label();
            int list = Label();

            owner.EmitRecordTest(
                tuple,
                XRegisterTag,
                0,
                tag,
                2
            );
            GetField(
                XRegisterTag,
                0,
                1,
                0
            );
            EmitTail();
            Mark(tuple);

            owner.Op(IsTuple, LabelTag, list);
            owner.Operand(XRegisterTag, 0);
            EmitFrameAllocation(arity: 1, size: 0);
            owner.EmitCallLine(conversion, location);
            Call(ErlangModuleNames.Module, conversion.Function, 1);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 0);
            Mark(list);

            GetList(0, 1, 2);
            GetList(2, 0, 2);
            BeamSourceLocation previous = location;
            EmitArithmetic(multiply);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(baseValue);
            owner.Operand(XRegisterTag, 1);
            EmitArithmetic(combine);
            owner.EmitTypedRegister(1, NumberTypeBits);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            EmitFrameAllocation(arity: 3, size: 1);
            Move(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(encode, previous);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, coder);
            owner.EmitClosureAllocation(0, 1, 1);
            owner.EmitDeclaredFunctionReference(callback, folder, 1);
            Move(
                YRegisterTag,
                0,
                XRegisterTag,
                2
            );
            Move(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.MoveToX0(XRegisterTag, 1);
            owner.EmitConstant(initial, 1);
            owner.EmitCallLine(fold, CallLocation(encode, previous));
            Call(fold.Module, fold.Function, 3);

            GetField(
                YRegisterTag,
                0,
                1,
                1
            );
            GetField(
                XRegisterTag,
                0,
                1,
                2
            );
            previous = CallLocation(fold, previous);
            EmitArithmetic(length);
            owner.EmitTypedRegister(1, IntegerTypeBits, 1);
            owner.EmitTypedRegister(2, NumberTypeBits);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 2);
            GetField(
                XRegisterTag,
                0,
                0,
                0
            );
            GetField(
                YRegisterTag,
                0,
                0,
                2
            );
            owner.Op(PutList, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (XRegisterTag, 1));
            owner.EmitFrameReturn(1);

            return true;

            void EmitTail()
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(LabelTag, entry);
            }

            void GetList(int source, int firstTarget, int remainingTarget)
            {
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(XRegisterTag, source);
                owner.Op(BeamEmissionFormat.GetList, XRegisterTag, source);
                owner.Operand(XRegisterTag, firstTarget);
                owner.Operand(XRegisterTag, remainingTarget);
            }

            void GetField(
                int tag,
                int source,
                int field,
                int target
            )
            {
                owner.Op(GetTupleElement, tag, source);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(XRegisterTag, target);
            }

            void Move(
                int fromTag,
                int from,
                int toTag,
                int to
            ) =>
                owner.MoveRegister(
                    fromTag,
                    from,
                    toTag,
                    to
                );

            void EmitArithmetic(Expr.Binary operation)
            {
                owner.EmitArithmeticLine(operation.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation.Operator, 2));
            }
        }

        private bool HasCountedTermPair(Expr.Call encode)
        {
            if (owner.IsDeclaredNative((encode.Function, 1))
                || !localFunctions.TryGetValue((encode.Function, 1), out var entry)
                || entry.Clauses is not
                [
                {
                    Body: Expr.Tuple { Items: [_, Expr.Literal { Value: Integer shortCount }] }
                },
                {
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false, Arguments: [_, _, Expr.Literal { Value: Integer longCount }]
                    } advance
                }
                ] || shortCount.Value <= 0 || longCount.Value <= 0
                || owner.IsDeclaredNative((advance.Function, 3))
                || !localFunctions.TryGetValue((advance.Function, 3), out var helper)
                || helper.Clauses is not
                [
                {
                    Patterns: [_, Pattern.Variable list, Pattern.Variable count],
                    Body: Expr.Tuple { Items: [Expr.Variable yielded, Expr.Variable finalCount] }
                },
                {
                    Patterns: [_, _, Pattern.Variable counter],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                                [
                                    _, _,
                                    Expr.Binary
                                {
                                    Operator: ErlangOperators.Plus, Left: Expr.Variable length,
                                    Right: Expr.Literal { Value: Integer increment }
                                }
                                ]
                    } recur
                }
                ]
                || yielded.Name != list.Name || finalCount.Name != count.Name || length.Name != counter.Name
                || increment.Value <= 0 || recur.Function != helper.Name)
            {
                return false;
            }

            return true;
        }

        private bool HasCountedTermCallback(Expr.FunctionReference reference, string coder)
        {
            if (owner.IsDeclaredNative((reference.Name, 2))
                || !localFunctions.TryGetValue((reference.Name, 2), out var callback)
                || callback.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable value, Pattern.Variable count],
                        Body: Expr.Sequence
                        {
                            Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable item, Pattern.Variable length] },
                                    Value: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable input]
                                    } call
                                },
                                    Expr.Tuple
                                {
                                    Items:
                                            [
                                                Expr.Variable emitted,
                                                Expr.Binary
                                            {
                                                Operator: ErlangOperators.Plus,
                                                Left: Expr.Variable before, Right: Expr.Variable extra
                                            }
                                            ]
                                }
                                ]
                        }
                    }
                ]
                || !DistinctBindings(
                    value,
                    count,
                    item,
                    length
                ))
            {
                return false;
            }

            return call.Function == coder && input.Name == value.Name && emitted.Name == item.Name
                && before.Name == count.Name && extra.Name == length.Name;
        }
    }
}
