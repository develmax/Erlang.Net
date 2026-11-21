using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCountedListPairAccumulator(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil }, ..] and var emptyArguments,
                    Body: Expr.Tuple
                    {
                        Items: [Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable unsorted] } finish,
                            Expr.Variable finalCount]
                    }
                },
                {
                    Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest }, ..] and var arguments,
                    Body: Expr.Sequence { Items: var body }
                }
                ]
                || arguments.Count is not (3 or 4) || emptyArguments.Count != arguments.Count
                || emptyArguments[^2] is not Pattern.Variable emptyItems || emptyItems.Name != unsorted.Name
                || emptyArguments[^1] is not Pattern.Variable emptyCount || emptyCount.Name != finalCount.Name
                || !DistinctBindings(emptyItems, emptyCount)
                || arguments[^2] is not Pattern.Variable items || arguments[^1] is not Pattern.Variable count
                || !TryResolveOrdinaryCallTarget(finish, out string? finishModule)
                || finishModule is not (null or ListModuleNames.Module)
                || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            int finalEntry = 0;
            if (finishModule is null && !entries.TryGetValue((finish.Function, 1), out finalEntry))
            {
                return false;
            }

            bool withCallback = arguments.Count == 4;
            var callback = withCallback ? arguments[1] as Pattern.Variable : null;
            if (withCallback && (callback is null || emptyArguments[1] is not Pattern.Variable emptyCallback
                    || emptyCallback.Name == emptyItems.Name || emptyCallback.Name == emptyCount.Name)
                || body.Count != (withCallback ? 3 : 2)
                || body[^2] is not Expr.Match
                {
                    Pattern: Pattern.Tuple { Items: [Pattern.Variable encoded, Pattern.Variable encodedCount] },
                    Value: Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable input, var options] } encode
                } pairMatch
                || !TryResolveOrdinaryCallTarget(encode, out string? encodeModule)
                || body[^1] is not Expr.Call { Module: null, DynamicRemote: false } recur
                || recur.Function != function.Name || recur.Arguments.Count != function.Arity
                || recur.Arguments[0] is not Expr.Variable tail || tail.Name != rest.Name
                || recur.Arguments[^2] is not Expr.List { Items: [Expr.Variable newItem], Tail: Expr.Variable oldItems }
                || newItem.Name != encoded.Name || oldItems.Name != items.Name
                || recur.Arguments[^1] is not Expr.Binary
                {
                    Operator: ErlangOperators.Plus, Left: Expr.Variable oldCount, Right: Expr.Variable newCount
                } sum || oldCount.Name != count.Name || newCount.Name != encodedCount.Name
                || !DistinctBindings(
                    head,
                    rest,
                    items,
                    count,
                    encoded,
                    encodedCount
                )
                || !AttributeLiteral.TryNormalizeConstant(options, out Term optionValue)
                || !HasOnlyLocalCalls(
                    function,
                    function.Arity,
                    (_, call) =>
                    ReferenceEquals(call, recur) || call.Arguments[^1] is Expr.Literal { Value: Integer or FloatTerm }
                ))
            {
                return false;
            }

            int encodeEntry = 0;
            if (encodeModule is null)
            {
                if (!entries.TryGetValue((encode.Function, 2), out encodeEntry)
                    || !ProducesNumericPair(encode) && (withCallback || !ReturnsTuple(
                        encode,
                        2,
                        [],
                        exact: true
                    )))
                {
                    return false;
                }
            }
            else if (withCallback || finishModule != ListModuleNames.Module || finish.Function != ListModuleNames.Reverse)
            {
                return false;
            }

            Expr.Apply? apply = null;
            Atom? selector = null;
            if (withCallback)
            {
                if (recur.Arguments[1] is not Expr.Variable callbackArgument || callbackArgument.Name != callback!.Name
                    || body[0] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable payload, Pattern.Variable { Name: VariableScopeNames.Wildcard }] },
                        Value: Expr.Apply
                        {
                            Function: Expr.Variable usedCallback,
                            Arguments: [Expr.Literal { Value: Atom field }, Expr.Variable selected, var context]
                        } applyCall
                    }
                    || !AttributeLiteral.TryNormalizeConstant(context, out Term contextValue) || contextValue is not Nil
                    || !DistinctBindings(
                        head,
                        rest,
                        items,
                        count,
                        encoded,
                        encodedCount,
                        callback,
                        payload
                    )
                    || usedCallback.Name != callback.Name || selected.Name != head.Name || input.Name != payload.Name)
                {
                    return false;
                }

                apply = applyCall;
                selector = field;
            }
            else if (input.Name != head.Name)
            {
                return false;
            }

            // The closed loop starts with literal nonnegative counts and adds only proven lengths.
            bool naturalCounts = !withCallback && encodeModule is null
                && (InferPairFacts(encode) & PairFacts.Length) != 0
                && HasOnlyLocalCalls(
                    function,
                    function.Arity,
                    (_, call) => ReferenceEquals(call, recur)
                        || call.Arguments[^1] is Expr.Literal { Value: Integer seed } && seed.Value.Sign >= 0
                );
            int arity = function.Arity;
            int failure = owner.nextLabel - 2;
            int empty = Label();
            int badMatch = withCallback || encodeModule is not null ? Label() : 0;

            owner.Op(IsNonemptyList, LabelTag, empty);
            owner.Operand(XRegisterTag, 0);
            EmitFrameAllocation(arity, arity);
            Save(arity - 1, 1);
            Save(arity - 2, 2);
            if (withCallback)
            {
                Save(1, 3);
            }

            owner.Op(GetList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, withCallback ? 1 : 0);
            owner.Operand(YRegisterTag, 0);
            BeamSourceLocation previous = location;
            if (apply is not null)
            {
                owner.EmitConstant(Nil.Value, 2);
                Restore(3, 3);
                owner.EmitConstant(selector!, 0);
                owner.EmitArithmeticLine(apply.SourceLine, ref previous);
                owner.Op(CallFun, BeamWire.UnsignedTag, 3);
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    2,
                    badMatch
                );
                owner.EmitTuplePart(0, 0, 0);
            }

            owner.EmitConstant(optionValue, 1);
            owner.EmitCallLine(encode, previous);
            if (encodeModule is null)
            {
                owner.EmitFieldCall(2, encodeEntry);
            }
            else
            {
                Call(encodeModule, encode.Function, 2);
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    2,
                    badMatch
                );
            }

            owner.EmitTuplePart(0, 1, 1);
            previous = CallLocation(encode, previous);
            owner.EmitArithmeticLine(sum.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, sum.Operator, 2));
            if (naturalCounts)
            {
                owner.EmitTypedRegister(
                    1,
                    IntegerTypeBits,
                    lower: 0,
                    registerTag: YRegisterTag
                );
                owner.EmitTypedRegister(1, IntegerTypeBits, lower: 0);
            }
            else
            {
                owner.EmitTypedRegister(1, NumberTypeBits, registerTag: YRegisterTag);
                if (encodeModule is null)
                {
                    EmitNumericPairLength(1, encode);
                }
                else
                {
                    owner.Operand(XRegisterTag, 1);
                }
            }

            owner.Operand(XRegisterTag, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(0, 0, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(YRegisterTag, 2);
            owner.Operand(XRegisterTag, withCallback ? 2 : 0);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                arity - 1
            );
            if (withCallback)
            {
                Restore(3, 1);
            }
            else
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    1
                );
            }

            Restore(0, 0);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, arity);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, arity);

            Mark(empty);
            owner.EmitNilTest(XRegisterTag, 0, failure);
            EmitFrameAllocation(arity, 1);
            Save(arity - 1, 0);
            owner.MoveToX0(XRegisterTag, arity - 2);
            owner.EmitCallLine(finish, location);
            if (finishModule is null)
            {
                owner.EmitFieldCall(1, finalEntry);
            }
            else
            {
                Call(finishModule, finish.Function, 1);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(0, (XRegisterTag, 0), (YRegisterTag, 0));
            owner.EmitFrameReturn(1);

            if (badMatch != 0)
            {
                Mark(badMatch);
                owner.EmitArithmeticLine(apply?.SourceLine ?? pairMatch.SourceLine, ref previous);
                owner.Op(BadMatch, XRegisterTag, 0);
            }

            return true;

            void Save(int source, int target) => owner.MoveRegister(
                XRegisterTag,
                source,
                YRegisterTag,
                target
            );

            void Restore(int source, int target) => owner.MoveRegister(
                YRegisterTag,
                source,
                XRegisterTag,
                target
            );
        }
    }
}
