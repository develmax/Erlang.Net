using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record BinarySplitBoundary(
            Expr.Call TagRead,
            Expr.Call LengthRead,
            Expr.Call OriginalSize,
            Expr.Call RemainingSize,
            Expr.Binary Difference,
            Expr.Match Split,
            Expr.Call? Recur,
            BitString? Empty
        );

        private bool TryEmitBinarySplitBoundary(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.OnLoadFunction is not null
                || recursiveClosure is not null || owner.IsDeclaredNative((function.Name, 1))
                || ReadBinarySplitBoundary(function) is not { } plan)
            {
                return false;
            }

            bool cons = plan.Recur is not null;
            int failure = Label();
            if (plan.Empty is { } empty)
            {
                int nonempty = failure;
                failure = Label();
                owner.Op(ExactEqualTest, LabelTag, nonempty);
                owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
                owner.LiteralOperand(empty);
                owner.EmitConstant(Nil.Value, 0);
                owner.instructions.WriteByte(Return);
                Mark(nonempty);
            }

            int source = cons ? 1 : 0;
            EmitFrameAllocation(1, source + 1);
            if (cons)
            {
                EmitFrameInitialization(0, 1);
            }

            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                source
            );
            owner.EmitCallLine(plan.TagRead, location);
            owner.EmitFieldCall(1, entries[(plan.TagRead.Function, 1)]);
            owner.EmitTupleElement(1, 0);
            var previous = CallLocation(plan.TagRead, location);
            owner.EmitCallLine(plan.LengthRead, previous);
            owner.EmitFieldCall(1, entries[(plan.LengthRead.Function, 1)]);
            previous = CallLocation(plan.LengthRead, previous);
            owner.EmitCallLine(plan.OriginalSize, previous);
            EmitSizeInstruction(plan.OriginalSize.Function, failure: 0, live: 1);
            if (cons)
            {
                owner.EmitTypedRegister(
                    source,
                    MatchableTypeBits,
                    registerTag: YRegisterTag,
                    unit: BitSyntaxDefaults.BitStringUnit
                );
            }
            else
            {
                owner.Operand(YRegisterTag, source);
            }

            owner.Operand(XRegisterTag, 1);
            owner.EmitTupleElement(1, 0);
            previous = CallLocation(plan.OriginalSize, previous);
            owner.EmitCallLine(plan.RemainingSize, previous);
            EmitSizeInstruction(plan.RemainingSize.Function, failure: 0, live: 2);
            owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(XRegisterTag, 0);
            previous = CallLocation(plan.RemainingSize, previous);
            owner.EmitArithmeticLine(plan.Difference.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, plan.Difference.Operator, 2));
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.Operand(XRegisterTag, 0);

            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern("no_fail"));
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, source);
            owner.Operand(XRegisterTag, 1);
            owner.Op(GetBinarySegment, LabelTag, failure);
            owner.EmitTypedRegister(1, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                -MaximumBitSize,
                MaximumBitSize
            );
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(cons ? YRegisterTag : XRegisterTag, 0);

            if (plan.Recur is { } recur)
            {
                owner.Op(GetBinaryTail, XRegisterTag, 1);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    YRegisterTag,
                    source
                );
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.EmitCallLine(recur, previous);
                owner.EmitFieldCall(1, entries[(function.Name, 1)]);
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Op(PutList, YRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }
            else
            {
                EmitBitMatchHeader(
                    1,
                    failure,
                    7,
                    0,
                    BitSyntaxDefaults.BinaryUnit
                );
                owner.Operand(AtomTag, owner.Intern(ReadBinaryTail));
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
                owner.Operand(XRegisterTag, 1);
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 1);
            }

            owner.EmitFrameReturn(1);
            Mark(failure);
            owner.EmitArithmeticLine(plan.Split.SourceLine, ref previous);
            owner.Op(BadMatch, YRegisterTag, source);

            return true;
        }

        private BinarySplitBoundary? ReadBinarySplitBoundary(FunctionDefinition function)
        {
            if (function.Arity != 1 || function.Clauses is not [.., { Guard: null, Patterns: [Pattern.Variable input] } continuation]
                || function.Clauses.Count is not (1 or 2)
                || continuation.Body is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match { Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, _] } } first,
                        var second,
                        Expr.Match
                    {
                        Pattern: Pattern.Variable size,
                        Value: Expr.Binary
                        {
                            Operator: ErlangOperators.Minus, Left: Expr.Call full, Right: Expr.Call remaining
                        } difference
                    },
                        Expr.Match
                    {
                        Pattern: BitPattern
                        {
                            Segments:
                                [
                                {
                                    Value: Pattern.Variable prefix,
                                    Specification:
                                    {
                                        Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: Expr.Variable width
                                    }
                                },
                                {
                                    Value: Pattern.Variable tail,
                                    Specification:
                                    {
                                        Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: null
                                    }
                                }
                                ]
                        },
                        Value: Expr.Variable source
                    } split,
                        var output
                    ]
                }
                || ReadCheckedBinaryNext(first, input.Name, tag) is not { } tagRead
                || ReadCheckedBinaryNext(second, tagRead.Remainder.Name, tag) is not { } lengthRead
                || !DistinctBindings(
                    input,
                    tagRead.Remainder,
                    lengthRead.Remainder,
                    size,
                    prefix,
                    tail
                )
                || width.Name != size.Name || source.Name != input.Name
                || !ReadsByteSize(full, input.Name) || !ReadsByteSize(remaining, lengthRead.Remainder.Name)
                || !localFunctions.TryGetValue((tagRead.Call.Function, 1), out var dispatch)
                || !localFunctions.TryGetValue((lengthRead.Call.Function, 1), out var consume)
                || !entries.ContainsKey((dispatch.Name, 1)) || !entries.ContainsKey((consume.Name, 1))
                || !ProvesResumedTaggedBinaryPair(dispatch, tag) || !ProvesLengthConsumerPair(consume, tag))
            {
                return null;
            }

            if (function.Clauses.Count == 1 && output is Expr.Tuple { Items: [Expr.Variable a, Expr.Variable b] }
                && a.Name == prefix.Name && b.Name == tail.Name)
            {
                return new(
                    tagRead.Call,
                    lengthRead.Call,
                    full,
                    remaining,
                    difference,
                    split,
                    null,
                    null
                );
            }

            if (function.Clauses.Count != 2 || function.Clauses[0] is not
                {
                    Guard: null,
                    Patterns: [BitPattern { Segments.Count: 0 } or Pattern.Literal { Value: BitString { BitLength: 0 } }],
                    Body: Expr.List { Items.Count: 0, Tail: null } or Expr.Literal { Value: Nil }
                }
                || output is not Expr.List
                {
                    Items: [Expr.Variable head],
                    Tail: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable next] } recur
                }
                || head.Name != prefix.Name || next.Name != tail.Name || recur.Function != function.Name
                || !entries.ContainsKey((function.Name, 1))
                || !HasBinaryLocalArgument(function, recur, projectedBinaryTuple: (4, 2)))
            {
                return null;
            }

            return new(
                tagRead.Call,
                lengthRead.Call,
                full,
                remaining,
                difference,
                split,
                recur,
                new BitString([])
            );

            bool ReadsByteSize(Expr.Call call, string name) =>
                call is { Function: AutoImportNames.ByteSize, DynamicRemote: false, Arguments: [Expr.Variable argument] }
                    && argument.Name == name && owner.ResolvesErlangBif(call);
        }

        private bool ProvesLengthConsumerPair(FunctionDefinition consumer, Atom tag)
        {
            return !owner.IsDeclaredNative((consumer.Name, 1)) && ReadBinaryLengthConsumer(consumer) is { } read
                && read.ResultTag.Equals(tag) && localFunctions.TryGetValue((read.Forward.Function, 1), out var continuation)
                && !owner.IsDeclaredNative((continuation.Name, 1)) && continuation.Clauses is
                [
                {
                    Guard: null, Patterns: [BitPattern end],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom endTag }, Expr.Variable tail] }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable { }],
                    Body: Expr.Sequence
                    {
                        Items: [_, _, Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 1 } recur]
                    }
                }
                ]
                && recur.Function == continuation.Name && endTag.Equals(tag) && ReadIndefiniteBinaryEnd(end, tail) is not null;
        }
    }
}
