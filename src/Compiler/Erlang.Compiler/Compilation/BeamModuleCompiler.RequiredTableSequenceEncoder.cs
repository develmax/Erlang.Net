using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRequiredTableSequenceEncoder(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                        Body: Expr.Sequence
                        {
                            Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple
                                    {
                                        Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable key, Pattern.Variable field]
                                    },
                                    Value: Expr.Variable original
                                } tuple,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable callback,
                                    Value: Expr.Call
                                    {
                                        DynamicRemote: false, Module: not null, Arguments: [Expr.Variable selector]
                                    } select
                                },
                                    var first,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable secondBytes, Pattern.Variable secondLength] },
                                    Value: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable value, var secondTags, Expr.Variable selected]
                                    } second
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable pairs,
                                    Value: Expr.List
                                    {
                                        Items: [Expr.Variable firstPart, Expr.Variable secondPart], Tail: null
                                    }
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable combined,
                                    Value: Expr.Binary
                                    {
                                        Operator: ErlangOperators.Plus, Left: Expr.Variable firstSize, Right: Expr.Variable secondSize
                                    } sum
                                },
                                    Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable outerTags, Expr.Variable pieces, Expr.Variable size]
                                } finish
                                ]
                        }
                    }
                ]
                || ReadTableEncoderPair(first) is not { } firstPair
                || firstPair.Call.Arguments is not [Expr.Variable firstInput, var firstTags]
                || !DistinctBindings(
                    input,
                    tags,
                    key,
                    field,
                    callback,
                    firstPair.Bytes,
                    firstPair.Length,
                    secondBytes,
                    secondLength,
                    pairs,
                    combined
                )
                || original.Name != input.Name || selector.Name != key.Name || firstInput.Name != key.Name
                || field.Name != value.Name || callback.Name != selected.Name || firstPart.Name != firstPair.Bytes.Name
                || secondPart.Name != secondBytes.Name || firstSize.Name != firstPair.Length.Name || secondSize.Name != secondLength.Name
                || outerTags.Name != tags.Name || pieces.Name != pairs.Name || size.Name != combined.Name
                || !AttributeLiteral.TryNormalizeConstant(firstTags, out Term firstOptions)
                || !AttributeLiteral.TryNormalizeConstant(secondTags, out Term secondOptions)
                || !TryResolveOrdinaryCallTarget(select, out string? remote) || remote is null
                || !entries.TryGetValue((firstPair.Call.Function, 2), out int firstEntry)
                || !entries.TryGetValue((second.Function, 3), out int secondEntry)
                || !entries.TryGetValue((finish.Function, 3), out int finishEntry)
                || !ProducesNumericPair(firstPair.Call) || !ProducesNumericPair(second))
            {
                return false;
            }

            int malformed = Label();
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                3,
                malformed
            );
            EmitFrameAllocation(2, 3);
            Move(
                XRegisterTag,
                1,
                YRegisterTag,
                2
            );
            Project(
                XRegisterTag,
                0,
                1,
                YRegisterTag,
                1
            );
            Project(
                XRegisterTag,
                0,
                2,
                YRegisterTag,
                0
            );
            owner.MoveToX0(YRegisterTag, 1);
            BeamSourceLocation previous = location;
            owner.EmitCallLine(select, previous);
            Call(remote, select.Function, 1);
            previous = CallLocation(select, previous);
            owner.Op(Swap, YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(firstOptions, 1);
            Local(firstPair.Call, firstEntry, 2);
            Move(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Move(
                YRegisterTag,
                1,
                XRegisterTag,
                2
            );
            Move(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.MoveToX0(XRegisterTag, 1);
            owner.EmitConstant(secondOptions, 1);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            Local(second, secondEntry, 3);

            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            Project(
                YRegisterTag,
                0,
                0,
                XRegisterTag,
                1
            );
            Project(
                YRegisterTag,
                0,
                1,
                XRegisterTag,
                2
            );
            owner.EmitTuplePart(0, 0, 3);
            owner.EmitTuplePart(0, 1, 0);
            owner.Op(PutList, XRegisterTag, 3);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 3);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 3);
            owner.Operand(XRegisterTag, 1);
            owner.EmitArithmeticLine(sum.SourceLine, ref previous);
            owner.GcArithmetic(
                GcBifTwo,
                ErlangOperators.Plus,
                3,
                2,
                (XRegisterTag, 2, ReadNumericPairLengthType(firstPair.Call)),
                (XRegisterTag, 0, ReadNumericPairLengthType(second))
            );
            owner.MoveToX0(YRegisterTag, 1);
            EmitTailLocal(finishEntry, 3, 2);

            Mark(malformed);
            owner.EmitArithmeticLine(tuple.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void Move(
                int sourceTag,
                int source,
                int targetTag,
                int target
            ) =>
                owner.MoveRegister(
                    sourceTag,
                    source,
                    targetTag,
                    target
                );

            void Project(
                int sourceTag,
                int source,
                int field,
                int targetTag,
                int target
            )
            {
                owner.Op(GetTupleElement, sourceTag, source);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(targetTag, target);
            }

            void Local(Expr.Call call, int entry, int arity)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.Op(LocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, entry);
            }
        }
    }
}
