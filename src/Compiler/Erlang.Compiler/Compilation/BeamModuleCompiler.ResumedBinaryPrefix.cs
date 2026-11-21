using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record BinaryPrefixClause(
            BitPatternSegment First,
            BitPatternSegment Second,
            int FirstWidth,
            int SecondWidth,
            Pattern.Variable Tail,
            Expr Body
        );

        private sealed record BinaryPrefixSwitch(
            BinaryPrefixClause Forwarded,
            Integer Selector,
            Atom ResultTag,
            Expr.Call Forward
        );

        private bool TryEmitResumedBinaryPrefix(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.OnLoadFunction is not null || recursiveClosure is not null
                || owner.IsDeclaredNative((function.Name, 1))
                || ReadBinaryPrefixSwitch(function) is not { } plan
                || !entries.TryGetValue((plan.Forward.Function, 1), out int target))
            {
                return false;
            }

            bool resumes = plan.Forward.Function == function.Name;
            if (resumes)
            {
                if (!HasProvenBinaryPrefixCallers(function, plan))
                {
                    return false;
                }
            }
            else if (!localFunctions.TryGetValue((plan.Forward.Function, 1), out var continuation)
                || owner.IsDeclaredNative((continuation.Name, 1))
                || ReadBinaryPrefixSwitch(continuation) is not { } loop
                || loop.Forward.Function != continuation.Name || !HasProvenBinaryPrefixCallers(continuation, loop))
            {
                return false;
            }

            const int input = 0;
            const int discriminant = 2;
            int context = resumes ? 0 : 1;
            int saved = resumes ? 1 : 0;
            int restoreLive = resumes ? 1 : 2;
            int functionInfo = owner.nextLabel - 2;
            int alternate = Label();
            int restore = Label();
            if (resumes)
            {
                owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern(ResumeMatchContext));
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(XRegisterTag, input);
                owner.Operand(XRegisterTag, context);
            }
            else
            {
                EmitBitMatchStart(context, functionInfo);
            }

            owner.Op(GetBinaryPosition, XRegisterTag, context);
            owner.Operand(XRegisterTag, saved);
            owner.Operand(BeamWire.UnsignedTag, restoreLive);
            int skippedPrefix = resumes ? 0 : plan.Forwarded.FirstWidth;
            int width = resumes ? plan.Forwarded.FirstWidth : plan.Forwarded.SecondWidth;
            var tested = resumes ? plan.Forwarded.First : plan.Forwarded.Second;
            EmitBitMatchHeader(
                context,
                restore,
                skippedPrefix == 0 ? 9 : 11,
                skippedPrefix + width,
                BitSyntaxDefaults.BitStringUnit
            );
            if (skippedPrefix != 0)
            {
                EmitBinaryPrefixSkip(skippedPrefix);
            }

            EmitBitSegment(
                tested.Specification,
                width,
                2,
                XRegisterTag,
                discriminant
            );
            owner.Op(ExactEqualTest, LabelTag, alternate);
            if (resumes)
            {
                owner.Operand(XRegisterTag, discriminant);
                owner.LiteralOperand(Term.I(0));
                EmitBinaryPrefixSuffix(
                    context,
                    restore,
                    plan.Forwarded.SecondWidth,
                    tailLive: 1
                );
                EmitTaggedRegisterResult(plan.ResultTag);
                owner.instructions.WriteByte(Return);
                Mark(alternate);
                EmitBinaryPrefixSuffix(
                    context,
                    restore,
                    plan.Forwarded.SecondWidth,
                    tailLive: null
                );
                EmitTailLocal(target, arity: 1, frame: null);
            }
            else
            {
                owner.EmitTypedRegister(
                    discriminant,
                    IntegerTypeBits,
                    0,
                    (1L << width) - 1
                );
                owner.LiteralOperand(plan.Selector);
                EmitBinaryPrefixSuffix(
                    context,
                    restore,
                    0,
                    tailLive: null
                );
                owner.MoveToX0(XRegisterTag, context);
                EmitTailLocal(target, arity: 1, frame: null);
                Mark(alternate);
                EmitBinaryPrefixSuffix(
                    context,
                    restore,
                    0,
                    tailLive: 2
                );
                EmitTaggedRegisterResult(plan.ResultTag);
                owner.instructions.WriteByte(Return);
            }

            Mark(restore);
            owner.Op(SetBinaryPosition, XRegisterTag, context);
            owner.Operand(XRegisterTag, saved);
            owner.Op(GetBinaryTail, XRegisterTag, context);
            owner.Operand(XRegisterTag, input);
            owner.Operand(BeamWire.UnsignedTag, restoreLive);
            JumpTo(functionInfo);

            return true;
        }

        private bool HasProvenBinaryPrefixCallers(FunctionDefinition loop, BinaryPrefixSwitch recursive)
        {
            bool seeded = false;

            return HasOnlyLocalCalls(
                loop,
                1,
                (caller, invocation) =>
                {
                    if (caller.Name == loop.Name)
                    {
                        return ReferenceEquals(recursive.Forward, invocation);
                    }

                    if (owner.IsDeclaredNative((caller.Name, 1))
                        || ReadBinaryPrefixSwitch(caller) is not { } outer
                        || !ReferenceEquals(outer.Forward, invocation))
                    {
                        return false;
                    }

                    seeded = true;

                    return true;
                }
            ) && seeded;
        }

        private static BinaryPrefixSwitch? ReadBinaryPrefixSwitch(FunctionDefinition function)
        {
            if (function.Arity != 1 || function.Clauses is not [var first, var second]
                || ReadPrefix(first) is not { } forwarded || ReadPrefix(second) is not { } returned
                || forwarded.FirstWidth != returned.FirstWidth || forwarded.SecondWidth != returned.SecondWidth
                || forwarded.Body is not Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable argument]
                } forward || argument.Name != forwarded.Tail.Name
                || returned.Body is not Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom tag }, Expr.Variable output]
                } || output.Name != returned.Tail.Name)
            {
                return null;
            }

            if (forward.Function == function.Name)
            {
                return forwarded.FirstWidth == 1
                    && forwarded.First.Value is Pattern.Literal { Value: Integer greater } && greater.Value.IsOne
                    && returned.First.Value is Pattern.Literal { Value: Integer zero } && zero.Value.IsZero
                    && IsWildcard(forwarded.Second.Value) && IsWildcard(returned.Second.Value)
                    ? new(
                        forwarded,
                        greater,
                        tag,
                        forward
                    ) : null;
            }

            return IsWildcard(forwarded.First.Value) && IsWildcard(returned.First.Value)
                && forwarded.Second.Value is Pattern.Literal { Value: Integer selector }
                && selector.Value >= 0 && selector.Value < (1 << forwarded.SecondWidth)
                && returned.Second.Value is Pattern.Variable ignored && ignored.Name != returned.Tail.Name
                ? new(
                    forwarded,
                    selector,
                    tag,
                    forward
                ) : null;

            static BinaryPrefixClause? ReadPrefix(Clause clause)
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns:
                        [
                            BitPattern
                        {
                            Segments:
                                [
                                    var first, var second,
                                {
                                    Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: null },
                                    Value: Pattern.Variable tail
                                }
                                ]
                        }
                        ]
                    }
                    || !DistinctBindings(tail) || Width(first) is not int leading || Width(second) is not int trailing
                    || leading + trailing != BitSyntaxDefaults.BinaryUnit)
                {
                    return null;
                }

                return new(
                    first,
                    second,
                    leading,
                    trailing,
                    tail,
                    clause.Body
                );
            }

            static int? Width(BitPatternSegment segment) => segment.Specification is
            {
                Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit,
                Signed: false, Endian: BitByteOrders.Big
            } && FixedSize(segment.Specification) is int bits && bits is > 0 and < BitSyntaxDefaults.BinaryUnit
                ? bits : null;
        }

        private void EmitBinaryPrefixSkip(int width)
        {
            owner.Operand(AtomTag, owner.Intern(SkipBits));
            owner.Operand(BeamWire.UnsignedTag, width);
        }

        private void EmitBinaryPrefixSuffix(
            int context,
            int failure,
            int skipped,
            int? tailLive
        )
        {
            int operands = 3 + (skipped == 0 ? 0 : 2) + (tailLive.HasValue ? 4 : 0);
            EmitBitMatchHeader(
                context,
                failure,
                operands,
                skipped,
                BitSyntaxDefaults.BinaryUnit
            );
            if (skipped != 0)
            {
                EmitBinaryPrefixSkip(skipped);
            }

            if (tailLive is int live)
            {
                owner.Operand(AtomTag, owner.Intern(ReadBinaryTail));
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
                owner.Operand(XRegisterTag, 0);
            }
        }
    }
}
