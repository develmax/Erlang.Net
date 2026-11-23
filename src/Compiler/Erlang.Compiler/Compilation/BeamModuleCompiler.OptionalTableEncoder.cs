using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TableEncoderPair(Expr.Call Call, Pattern.Variable Bytes, Pattern.Variable Length);

        private bool TryEmitOptionalTableEncoder(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable input, Pattern.Variable tags],
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
                                    Value: Expr.Variable data
                                } read,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable callback,
                                    Value: Expr.Call
                                    {
                                        Module: not null, DynamicRemote: false, Arguments: [Expr.Variable selector]
                                    } select
                                },
                                    var first,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable optionalBytes, Pattern.Variable optionalLength] },
                                    Value: Expr.Case
                                    {
                                        Value: Expr.Variable condition,
                                        Clauses:
                                                [
                                                { Guard: null, Patterns: [Pattern.Literal { Value: Atom absent }], Body: var empty },
                                                {
                                                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                                    Body: Expr.Sequence
                                                    {
                                                        Items:
                                                                [
                                                                    Expr.Match
                                                                {
                                                                    Pattern: Pattern.Tuple
                                                                    {
                                                                        Items: [Pattern.Variable open, Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                                                                    },
                                                                    Value: Expr.Apply
                                                                    {
                                                                        Function: Expr.Variable applied,
                                                                        Arguments: [Expr.Literal { Value: Atom parameter }, Expr.Variable argument, var options]
                                                                    } invoke
                                                                } unpack,
                                                                    Expr.Call
                                                                {
                                                                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable payload, var openTags]
                                                                } encodeOpen
                                                                ]
                                                    }
                                                }
                                                ]
                                    }
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable dataPair,
                                    Value: Expr.List
                                    {
                                        Items: [Expr.Variable requiredPart, Expr.Variable optionalPart], Tail: null
                                    }
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable total,
                                    Value: Expr.Binary
                                    {
                                        Operator: ErlangOperators.Plus, Left: Expr.Variable requiredSize, Right: Expr.Variable optionalSize
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
                || ReadTableEncoderPair(first) is not { } mandatory
                || mandatory.Call.Arguments is not [Expr.Variable encodedShared, var requiredTags]
                || !DistinctBindings(
                    input,
                    tags,
                    key,
                    field,
                    callback,
                    mandatory.Bytes,
                    mandatory.Length,
                    optionalBytes,
                    optionalLength,
                    open,
                    dataPair,
                    total
                )
                || data.Name != input.Name || key.Name != selector.Name || key.Name != encodedShared.Name
                || field.Name != condition.Name || field.Name != argument.Name || callback.Name != applied.Name
                || open.Name != payload.Name || tags.Name != outerTags.Name || dataPair.Name != pieces.Name || total.Name != size.Name
                || mandatory.Bytes.Name != requiredPart.Name || optionalBytes.Name != optionalPart.Name
                || mandatory.Length.Name != requiredSize.Name || optionalLength.Name != optionalSize.Name
                || !AttributeLiteral.TryNormalizeConstant(requiredTags, out Term encodeTag)
                || !AttributeLiteral.TryNormalizeConstant(openTags, out Term openTag)
                || !AttributeLiteral.TryNormalizeConstant(options, out Term callbackOptions)
                || !AttributeLiteral.TryNormalizeConstant(empty, out Term absentPair)
                || absentPair is not TupleTerm { Items.Count: 2 } emptyTuple || emptyTuple.Items[1] is not (Integer or FloatTerm)
                || !entries.TryGetValue((mandatory.Call.Function, 2), out int requiredEncoder)
                || !entries.TryGetValue((encodeOpen.Function, 2), out int optionalEncoder)
                || !entries.TryGetValue((finish.Function, 3), out int outputEncoder)
                || !ProducesNumericPair(mandatory.Call) || !ProducesNumericPair(encodeOpen))
            {
                return false;
            }

            int hasField = Label();
            int joined = Label();
            int badClosure = Label();
            int badInput = Label();
            CheckTuple(3, badInput);
            EmitFrameAllocation(arity: 2, size: 4);
            EmitFrameInitialization(0, 1);
            Move(
                XRegisterTag,
                1,
                YRegisterTag,
                3
            );
            Project(
                XRegisterTag,
                0,
                1,
                YRegisterTag,
                2
            );
            Project(
                XRegisterTag,
                0,
                2,
                YRegisterTag,
                1
            );
            Move(
                YRegisterTag,
                2,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(select, location);
            Call(select.Module, select.Function, 1);
            owner.Op(Swap, YRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.EmitConstant(encodeTag, 1);
            BeamSourceLocation previous = CallLocation(select, location);
            owner.EmitCallLine(mandatory.Call, previous);
            EmitLocalCall(requiredEncoder, 2);
            Move(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.Op(ExactEqualTest, LabelTag, hasField);
            owner.Operand(YRegisterTag, 1);
            owner.LiteralOperand(absent);
            owner.EmitConstant(absentPair, 0);
            owner.Op(Jump, LabelTag, joined);

            Mark(hasField);
            owner.EmitConstant(callbackOptions, 2);
            Move(
                YRegisterTag,
                1,
                XRegisterTag,
                1
            );
            Move(
                YRegisterTag,
                2,
                XRegisterTag,
                3
            );
            EmitFrameInitialization(1, 3);
            owner.EmitConstant(parameter, 0);
            previous = CallLocation(mandatory.Call, previous);
            owner.EmitArithmeticLine(invoke.SourceLine, ref previous);
            owner.Op(CallFun, BeamWire.UnsignedTag, 3);
            CheckTuple(2, badClosure);
            Project(
                XRegisterTag,
                0,
                0,
                XRegisterTag,
                0
            );
            owner.EmitConstant(openTag, 1);
            owner.EmitCallLine(encodeOpen, previous);
            EmitLocalCall(optionalEncoder, 2);

            Mark(joined);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
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
            Project(
                XRegisterTag,
                0,
                0,
                XRegisterTag,
                3
            );
            Project(
                XRegisterTag,
                0,
                1,
                XRegisterTag,
                0
            );
            owner.Op(PutList, XRegisterTag, 3);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 3);
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 3);
            owner.Operand(XRegisterTag, 1);
            previous = CallLocation(encodeOpen, previous);
            owner.EmitArithmeticLine(sum.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2));
            EmitNumericPairLength(2, mandatory.Call);
            EmitNumericPairLength(0, encodeOpen);
            owner.Operand(XRegisterTag, 2);
            Move(
                YRegisterTag,
                3,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, outputEncoder);
            owner.Operand(BeamWire.UnsignedTag, 4);

            Mark(badClosure);
            previous = location;
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(badInput);
            previous = location;
            owner.EmitArithmeticLine(read.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void CheckTuple(int arity, int failure)
            {
                owner.Op(IsTuple, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.Op(TestArity, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, arity);
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

            void EmitLocalCall(int entry, int arity)
            {
                owner.Op(LocalCall, BeamWire.UnsignedTag, arity);
                owner.Operand(LabelTag, entry);
            }
        }

        private void EmitNumericPairLength(int register, Expr.Call call)
        {
            if (ReadNumericPairLengthType(call) != 0)
            {
                owner.EmitTypedRegister(register, NumberTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, register);
            }
        }

        private int ReadNumericPairLengthType(Expr.Call call) =>
            (ReadJoinedNumericPairFacts(call) & (PairFacts.Length | PairFacts.NumericLength)) != 0 ? NumberTypeBits : 0;

        private bool ProducesNumericPair(Expr.Call call)
        {
            bool previous = inferNumericPairs;
            inferNumericPairs = true;
            try
            {
                return (InferPairFacts(call) & (PairFacts.Length | PairFacts.NumericLength)) != 0;
            }
            finally
            {
                inferNumericPairs = previous;
            }
        }

        private static TableEncoderPair? ReadTableEncoderPair(Expr expression) =>
            expression is Expr.Match
            {
                Pattern: Pattern.Tuple { Items: [Pattern.Variable bytes, Pattern.Variable length] },
                Value: Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 2 } call
            } ? new(call, bytes, length) : null;
    }
}
