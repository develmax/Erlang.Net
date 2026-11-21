using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record BinaryAccumulatorArm(
            Expr.Binary Shift,
            Expr.Binary Merge,
            Expr.Call Recur,
            BitSegment Head,
            int Bound,
            Expr.Binary? Increment
        );

        private bool TryEmitPositionedBinaryAccumulator(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled)
            {
                return false;
            }

            Expr.Call? wrapper = ReadBinaryAccumulatorEntry(function);
            if (wrapper is null && function.Arity != 3)
            {
                return false;
            }

            var signature = (wrapper?.Function ?? function.Name, 3);
            if (owner.IsDeclaredNative(signature)
                || owner.IsDeclaredNative((function.Name, function.Arity))
                || !localFunctions.TryGetValue(signature, out var loop)
                || ReadBinaryAccumulatorLoop(loop) is not { } plan
                || !entries.TryGetValue(signature, out int entry))
            {
                return false;
            }

            bool hasEntry = false;
            if (!HasOnlyLocalCalls(
                loop,
                3,
                (caller, call) =>
            {
                if (ReferenceEquals(call, plan.Continue.Recur) || ReferenceEquals(call, plan.Finish.Recur))
                {
                    return true;
                }

                if (!ReferenceEquals(ReadBinaryAccumulatorEntry(caller), call))
                {
                    return false;
                }

                hasEntry = true;

                return true;
            }
            ) || !hasEntry)
            {
                return false;
            }

            // beam_ssa_bsm forwards the context through a closed local call if
            // the callee matches the argument before any other use. Both
            // recursive edges pass the tail at the current input position.
            int functionInfo = owner.nextLabel - 2;
            if (wrapper is not null)
            {
                int unaligned = Label();
                EmitBitMatchStart(context: 1, failure: functionInfo);
                EmitBitMatchHeader(
                    1,
                    unaligned,
                    3,
                    0,
                    BitSyntaxDefaults.BinaryUnit
                );
                owner.EmitConstant(Term.I(0), 2);
                owner.MoveToX0(XRegisterTag, 1);
                owner.EmitConstant(Term.I(0), 1);
                EmitTail(entry);
                Mark(unaligned);
                EmitRestoredFailure(context: 1, live: 2);

                return true;
            }

            int alternate = Label();
            int empty = Label();
            int invalid = Label();
            int byteWidth = BitSyntaxDefaults.BinaryUnit;
            int highBit = BitSyntaxDefaults.NumericUnit;
            int lowWidth = byteWidth - highBit;
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern(BeamBinaryNames.Resume));
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Op(GetBinaryPosition, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 3);
            EmitHead(
                plan.Continue.Head,
                alternate,
                highBit,
                BitSyntaxDefaults.BitStringUnit
            );
            owner.Op(ExactEqualTest, LabelTag, alternate);
            owner.EmitTypedRegister(
                4,
                IntegerTypeBits,
                0,
                1
            );
            owner.LiteralOperand(Term.I(1));
            EmitHead(
                plan.Continue.Head,
                alternate,
                lowWidth,
                byteWidth
            );
            EmitCountGuard(empty);
            BeamSourceLocation previous = location;
            EmitShiftedHead(
                plan.Continue,
                lowWidth,
                2,
                XRegisterTag,
                2,
                allocate: false
            );
            var increment = plan.Continue.Increment!;
            owner.EmitArithmeticLine(increment.SourceLine, ref previous);
            EmitBinaryOp(increment, live: 3);
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                0,
                plan.Continue.Bound - 1
            );
            owner.LiteralOperand(Term.I(1));
            owner.Operand(XRegisterTag, 1);
            EmitTail(entry);

            Mark(alternate);
            RestorePosition();
            EmitHead(
                plan.Finish.Head,
                empty,
                byteWidth,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitCountGuard(empty);
            previous = location;
            EmitShiftedHead(
                plan.Finish,
                byteWidth,
                1,
                YRegisterTag,
                0,
                allocate: true
            );
            owner.EmitConstant(Term.I(0), 2);
            owner.EmitConstant(Term.I(0), 1);
            owner.EmitCallLine(plan.Finish.Recur, previous);
            owner.Op(LocalCall, BeamWire.UnsignedTag, 3);
            owner.Operand(LabelTag, entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Op(Deallocate, BeamWire.UnsignedTag, 1);
            owner.instructions.WriteByte(Return);

            Mark(empty);
            RestorePosition();
            EmitBitMatchHeader(
                0,
                invalid,
                2,
                0,
                null
            );
            owner.EmitConstant(Nil.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(invalid);
            EmitRestoredFailure(context: 0, live: 3);

            return true;

            void EmitHead(
                BitSegment segment,
                int failure,
                int width,
                int alignment
            )
            {
                EmitBitMatchHeader(
                    0,
                    failure,
                    9,
                    width,
                    alignment
                );
                EmitBitSegment(
                    segment,
                    width,
                    4,
                    XRegisterTag,
                    4
                );
            }

            void EmitCountGuard(int failure)
            {
                owner.Op(IsGreaterOrEqual, LabelTag, failure);
                owner.LiteralOperand(Term.I(plan.Continue.Bound - 1));
                owner.EmitTypedRegister(1, IntegerTypeBits, 0);
            }

            void EmitShiftedHead(
                BinaryAccumulatorArm arm,
                int width,
                int intermediate,
                int destinationTag,
                int destination,
                bool allocate
            )
            {
                owner.EmitArithmeticLine(arm.Shift.SourceLine, ref previous);
                EmitBinaryOp(arm.Shift, live: 5);
                owner.EmitTypedRegister(2, IntegerTypeBits);
                owner.LiteralOperand(Term.I(lowWidth));
                owner.Operand(XRegisterTag, intermediate);
                if (allocate)
                {
                    EmitFrameAllocation(arity: 5, size: 1);
                    EmitFrameInitialization(0, 1);
                }

                owner.EmitArithmeticLine(arm.Merge.SourceLine, ref previous);
                EmitBinaryOp(arm.Merge, live: 5);
                owner.EmitTypedRegister(intermediate, IntegerTypeBits);
                owner.EmitTypedRegister(
                    4,
                    IntegerTypeBits,
                    0,
                    (1L << width) - 1
                );
                owner.Operand(destinationTag, destination);
            }

            void EmitBinaryOp(Expr.Binary operation, int live)
            {
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, operation.Operator, 2));
            }

            void EmitTail(int label)
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 3);
                owner.Operand(LabelTag, label);
            }

            void RestorePosition()
            {
                owner.Op(SetBinaryPosition, XRegisterTag, 0);
                owner.Operand(XRegisterTag, 3);
            }

            void EmitRestoredFailure(int context, int live)
            {
                owner.Op(GetBinaryTail, XRegisterTag, context);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Op(Jump, LabelTag, functionInfo);
            }
        }

        private static Expr.Call? ReadBinaryAccumulatorEntry(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [BitPattern { Segments: [var segment] }],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable input, Expr.Literal { Value: Integer count }, Expr.Literal { Value: Integer total }]
                    } call
                }
                ] || !count.Value.IsZero || !total.Value.IsZero || !IsAccumulatorTail(segment, input.Name))
            {
                return null;
            }

            return call;
        }

        private static (BinaryAccumulatorArm Continue, BinaryAccumulatorArm Finish)? ReadBinaryAccumulatorLoop(FunctionDefinition function)
        {
            if (function.Clauses is not
                [
                    var continuation,
                    var finish,
                {
                    Guard: null,
                    Patterns: [BitPattern { Segments.Count: 0 }, Pattern.Variable count, Pattern.Variable total],
                    Body: Expr.List { Items.Count: 0, Tail: null } or Expr.Literal { Value: Nil }
                }
                ]
                || count.Name == total.Name && count.Name != VariableScopeNames.Wildcard
                || ReadBinaryAccumulatorArm(continuation, function.Name, recursive: true) is not { } left
                || ReadBinaryAccumulatorArm(finish, function.Name, recursive: false) is not { } right
                || left.Bound != right.Bound)
            {
                return null;
            }

            return (left, right);
        }

        private static BinaryAccumulatorArm? ReadBinaryAccumulatorArm(
            Clause clause,
            string function,
            bool recursive
        )
        {
            if (clause is not
                {
                    Patterns: [BitPattern { Segments: var segments }, Pattern.Variable count, Pattern.Variable total],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.Less, Left: Expr.Variable tested, Right: Expr.Literal { Value: Integer bound }
                    },
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable result,
                                Value: Expr.Binary
                                {
                                    Operator: ErlangOperators.BitwiseOr,
                                    Left: Expr.Binary
                                    {
                                        Operator: ErlangOperators.ShiftLeft, Left: Expr.Variable input,
                                        Right: Expr.Literal { Value: Integer width }
                                    } shift,
                                    Right: Expr.Variable extracted
                                } merge
                            },
                                var output
                            ]
                    }
                }
                || tested.Name != count.Name || input.Name != total.Name || bound.Value <= 1 || bound.Value > int.MaxValue
                || width.Value != BitSyntaxDefaults.BinaryUnit - BitSyntaxDefaults.NumericUnit)
            {
                return null;
            }

            int segmentCount = recursive ? 3 : 2;
            int headWidth = recursive ? (int)width.Value : BitSyntaxDefaults.BinaryUnit;
            if (segments.Count != segmentCount
                || recursive && (segments[0].Value is not Pattern.Literal { Value: Integer marker }
                    || marker.Value != 1 || !IsUnsignedAccumulatorSegment(segments[0].Specification, BitSyntaxDefaults.NumericUnit))
                || segments[^2].Value is not Pattern.Variable head || head.Name != extracted.Name
                || !IsUnsignedAccumulatorSegment(segments[^2].Specification, headWidth)
                || segments[^1].Value is not Pattern.Variable tail || !IsAccumulatorTail(segments[^1], tail.Name)
                || !DistinctBindings(
                    head,
                    tail,
                    count,
                    total,
                    result
                ))
            {
                return null;
            }

            if (!recursive)
            {
                if (output is not Expr.List { Items: [Expr.Variable item], Tail: Expr.Call rest }
                    || item.Name != result.Name || !IsAccumulatorReentry(rest, function, tail.Name)
                    || rest.Arguments is not [_, Expr.Literal { Value: Integer initialCount }, Expr.Literal { Value: Integer initialTotal }]
                    || !initialCount.Value.IsZero || !initialTotal.Value.IsZero)
                {
                    return null;
                }

                return new(
                    shift,
                    merge,
                    rest,
                    segments[^2].Specification,
                    (int)bound.Value,
                    null
                );
            }

            if (output is not Expr.Call recur || !IsAccumulatorReentry(recur, function, tail.Name)
                || recur.Arguments is not
                    [
                        _,
                        Expr.Binary
                    {
                        Operator: ErlangOperators.Plus, Left: Expr.Variable added, Right: Expr.Literal { Value: Integer one }
                    } increment,
                        Expr.Variable updated
                    ]
                || added.Name != count.Name || one.Value != 1 || updated.Name != result.Name)
            {
                return null;
            }

            return new(
                shift,
                merge,
                recur,
                segments[^2].Specification,
                (int)bound.Value,
                increment
            );
        }

        private static bool IsAccumulatorReentry(Expr.Call call, string function, string tail) =>
            call is { Module: null, DynamicRemote: false, Arguments: [Expr.Variable input, _, _] }
            && call.Function == function && input.Name == tail;

        private static bool IsAccumulatorTail(BitPatternSegment segment, string binding) =>
            segment is
            {
                Value: Pattern.Variable { Name: not VariableScopeNames.Wildcard } value,
                Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: null }
            } && value.Name == binding;

        private static bool IsUnsignedAccumulatorSegment(BitSegment segment, int size) =>
            segment is
            {
                Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit, Signed: false, Endian: BitByteOrders.Big
            } && FixedSize(segment) == size;
    }
}
