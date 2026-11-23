using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const string ListSliceFunction = "sublist";
        private const string ResumeMatchContext = "resume";

        private sealed record ResumedBitListDecodePlan(
            int Entry,
            BitPattern Prefix,
            int BitCount,
            Integer Terminal,
            Integer Width,
            Expr.Binary Available,
            Expr.Binary Decreased,
            Expr.Call Slice,
            Expr.Call Recur
        );

        private ResumedBitListDecodePlan? ReadResumedBitListDecode(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || owner.IsDeclaredNative((function.Name, 3)) || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Integer terminal }, Pattern.Variable padding, BitPattern final],
                    Body: Expr.Call
                    {
                        Module: ListModuleNames.Module, Function: ListSliceFunction, DynamicRemote: false,
                        Arguments: [
                                Expr.List { Tail: null, Items: var collected },
                                Expr.Binary
                        {
                            Operator: ErlangOperators.Minus, Left: Expr.Literal { Value: Integer width },
                            Right: Expr.Variable unused
                        } available
                            ]
                    } slice
                },
                {
                    Guard: null,
                    Patterns: [Pattern.Variable length, Pattern.Variable keepPadding, BitPattern prefix],
                    Body: Expr.List
                    {
                        Items: var bitValues,
                        Tail: Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [
                                    Expr.Binary
                            {
                                Operator: ErlangOperators.Minus, Left: Expr.Variable remaining,
                                Right: Expr.Literal { Value: Integer decrement }
                            } decreased,
                                    Expr.Variable withPadding,
                                    Expr.Variable next
                                ]
                        } recur
                    }
                }
                ]
                || !terminal.Value.IsOne || !decrement.Value.IsOne
                || collected.Count != BitSyntaxDefaults.BinaryUnit || width.Value != collected.Count
                || !DistinctBindings(padding) || !DistinctBindings(length, keepPadding)
                || unused.Name != padding.Name || remaining.Name != length.Name || withPadding.Name != keepPadding.Name
                || recur.Function != function.Name
                || !ReadParts(
                    final,
                    collected,
                    [padding.Name],
                    out var discarded
                ) || discarded != VariableScopeNames.Wildcard
                || !ReadParts(
                    prefix,
                    bitValues,
                    [length.Name, keepPadding.Name],
                    out var tail
                ) || tail != next.Name || tail == length.Name
                || !entries.TryGetValue((function.Name, 3), out int entry))
            {
                return null;
            }

            bool contextWitnessSeen = false;
            if (!HasOnlyLocalCalls(
                function,
                3,
                (caller, call) =>
                {
                    if (ReferenceEquals(caller, function))
                    {
                        return ReferenceEquals(call, recur);
                    }

                    if (ReadMeasuredBinaryDecode(caller) is not { } plan || !ReferenceEquals(plan.Decode, call))
                    {
                        return false;
                    }

                    contextWitnessSeen = true;

                    return true;
                }
            ) || !contextWitnessSeen)
            {
                return null;
            }

            return new(
                entry,
                prefix,
                collected.Count,
                terminal,
                width,
                available,
                decreased,
                slice,
                recur
            );

            bool ReadParts(
                BitPattern pattern,
                IReadOnlyList<Expr> values,
                IReadOnlyList<string> forbidden,
                out string tailName
            )
            {
                tailName = string.Empty;
                if (values.Count != collected.Count || pattern.Segments.Count != collected.Count + 1
                    || pattern.Segments[^1] is not
                    {
                        Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: null },
                        Value: Pattern.Variable remainder
                    })
                {
                    return false;
                }

                var scoped = new HashSet<string>(forbidden, StringComparer.Ordinal);
                if (!scoped.Add(remainder.Name))
                {
                    return false;
                }


                for (int index = 0; index < collected.Count; index++)
                {
                    var segment = pattern.Segments[index];
                    if (segment.Value is not Pattern.Variable item || item.Name == VariableScopeNames.Wildcard
                        || values[index] is not Expr.Variable listed || listed.Name != item.Name
                        || segment.Specification is not
                        {
                            Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit,
                            Signed: false, Endian: BitByteOrders.Big
                        }
                        || FixedSize(segment.Specification) != 1 || !scoped.Add(item.Name))
                    {
                        return false;
                    }
                }

                tailName = remainder.Name;

                return true;
            }
        }

        private bool TryEmitResumedBitListDecode(FunctionDefinition function)
        {
            if (ReadResumedBitListDecode(function) is not { } plan)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            int more = Label();
            int restore = Label();
            const int matchContext = 2;
            const int firstBit = 3;
            int live = plan.BitCount + firstBit;
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern(ResumeMatchContext));
            owner.Operand(BeamWire.UnsignedTag, firstBit);
            owner.Operand(XRegisterTag, matchContext);
            owner.Operand(XRegisterTag, matchContext);
            owner.Op(GetBinaryPosition, XRegisterTag, matchContext);
            owner.Operand(XRegisterTag, firstBit);
            owner.Operand(BeamWire.UnsignedTag, firstBit);
            EmitBitMatchHeader(
                matchContext,
                restore,
                3 + plan.BitCount * 6,
                plan.BitCount,
                BitSyntaxDefaults.BinaryUnit
            );
            for (int i = 0; i < plan.BitCount; i++)
            {
                EmitBitSegment(
                    plan.Prefix.Segments[i].Specification,
                    1,
                    firstBit + i,
                    XRegisterTag,
                    firstBit + i
                );
            }

            owner.Op(ExactEqualTest, LabelTag, more);
            owner.EmitTypedRegister(0, IntegerTypeBits);
            owner.LiteralOperand(plan.Terminal);
            var previous = location;
            owner.EmitArithmeticLine(plan.Available.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, plan.Available.Operator, 2));
            owner.LiteralOperand(plan.Width);
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                byte.MinValue,
                byte.MaxValue
            );
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, plan.BitCount * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, live);
            for (int i = plan.BitCount - 1; i >= 0; i--)
            {
                owner.Op(PutList, XRegisterTag, firstBit + i);
                owner.Operand(i == plan.BitCount - 1 ? AtomTag : XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }

            owner.EmitCallLine(plan.Slice, previous);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(plan.Slice.Module!, plan.Slice.Function, 2));
            Mark(more);
            previous = location;
            owner.EmitArithmeticLine(plan.Decreased.SourceLine, ref previous);
            owner.GcArithmetic(
                GcBifTwo,
                plan.Decreased.Operator,
                live,
                0,
                (XRegisterTag, 0, IntegerTypeBits),
                (BeamWire.IntegerTag, 1, 0)
            );
            EmitFrameAllocation(live, plan.BitCount);
            for (int slot = 0; slot < plan.BitCount; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    live - slot - 1,
                    YRegisterTag,
                    slot
                );
            }

            owner.EmitCallLine(plan.Recur, previous);
            owner.EmitFieldCall(3, plan.Entry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, plan.BitCount * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            for (int slot = 0; slot < plan.BitCount; slot++)
            {
                owner.Op(PutList, YRegisterTag, slot);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }

            owner.EmitFrameReturn(plan.BitCount);
            Mark(restore);
            owner.Op(SetBinaryPosition, XRegisterTag, matchContext);
            owner.Operand(XRegisterTag, firstBit);
            owner.Op(GetBinaryTail, XRegisterTag, matchContext);
            owner.Operand(XRegisterTag, matchContext);
            owner.Operand(BeamWire.UnsignedTag, firstBit);
            JumpTo(failure);

            return true;
        }
    }
}
