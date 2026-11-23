using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record RawPayloadField(
            BitSegment Segment,
            int Size,
            int Register,
            long Maximum
        );

        private sealed record RawPayloadArithmetic(
            Expr.Binary Expression,
            RawPayloadField? Argument,
            Integer? Constant,
            long Lower,
            long Upper,
            long ResultLower,
            long ResultUpper,
            int Live
        );

        private bool TryEmitRawPositionedPayload(FunctionDefinition function)
        {
            if (!CanUseEntryTypeFacts() || recursiveClosure is not null
                || owner.SourceModule?.DeclaredNifs is { Count: > 0 }
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [BitPattern { Segments.Count: > 1 } prefix],
                        Body: Expr.Sequence
                        {
                            Items:
                            [
                                Expr.Match { Pattern: Pattern.Variable size, Value: Expr.Binary width },
                                Expr.Match
                            {
                                Pattern: BitPattern { Segments: [var payload, var remainder] },
                                Value: Expr.Variable source
                            } extraction,
                                Expr.Variable result
                            ]
                        }
                    }
                ]
                || !IsOpenAlignedBinary(prefix.Segments[^1], out var suffix)
                || !IsOpenAlignedBinary(remainder, out var ignored) || !IsWildcard(ignored)
                || payload.Value is not Pattern.Variable selected || selected.Name != result.Name
                || payload.Specification is not
                {
                    Type: BitSegmentTypes.Binary,
                    Unit: BitSyntaxDefaults.BinaryUnit,
                    Size: Expr.Variable requested
                }
                || source.Name != suffix.Name || size.Name != requested.Name
                || !DistinctBindings(suffix, selected, size))
            {
                return false;
            }

            var fields = new Dictionary<string, RawPayloadField>(StringComparer.Ordinal);
            const int signedWidth = sizeof(long) * BitSyntaxDefaults.BinaryUnit - 1;
            int prefixWidth = 0;
            foreach (var field in prefix.Segments.SkipLast(1))
            {
                if (field.Value is not Pattern.Variable binding || IsWildcard(binding)
                    || binding.Name == suffix.Name || binding.Name == size.Name || binding.Name == selected.Name
                    || field.Specification is not { Type: BitSegmentTypes.Integer, Signed: false, Endian: BitByteOrders.Big or BitByteOrders.Little }
                    || FixedSize(field.Specification) is not int count
                    || (long)count * field.Specification.Unit is not (> 0 and < signedWidth and var bits)
                    || fields.Count + 1 >= FirstReservedXRegister
                    || !fields.TryAdd(binding.Name, new(
                        field.Specification,
                        count,
                        fields.Count + 1,
                        (1L << (int)bits) - 1
                    )))
                {
                    return false;
                }

                prefixWidth += (int)bits;
            }

            if (prefixWidth % BitSyntaxDefaults.BinaryUnit != 0
                || ReadRawPayloadArithmetic(width, fields) is not { } arithmetic
                || !HasBinaryLocalArgument(function, binaryRecursion: null, materializedBitstringOnly: true)
                || !HasBinaryLocalArgument(function, binaryRecursion: null))
            {
                return false;
            }

            const int context = 0;
            const int originalPosition = 1;
            int functionInfo = owner.nextLabel - 2;
            int invalidPayload = Label();
            int invalidPrefix = Label();
            EmitBitMatchStart(context, functionInfo, knownBinary: true);
            owner.Op(GetBinaryPosition, XRegisterTag, context);
            owner.Operand(XRegisterTag, originalPosition);
            owner.Operand(BeamWire.UnsignedTag, 1);
            EmitBitMatchHeader(
                context,
                invalidPrefix,
                3 + 6 * fields.Count,
                prefixWidth,
                BitSyntaxDefaults.BitStringUnit
            );
            foreach (RawPayloadField field in fields.Values.OrderBy(field => field.Register))
            {
                EmitBitSegment(
                    field.Segment,
                    field.Size,
                    field.Register,
                    XRegisterTag,
                    field.Register
                );
            }

            var (register, steps) = arithmetic;
            var previous = location;
            foreach (RawPayloadArithmetic step in steps)
            {
                owner.EmitArithmeticLine(step.Expression.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, step.Live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, step.Expression.Operator, 2));
                owner.EmitTypedRegister(
                    register,
                    IntegerTypeBits,
                    step.Lower,
                    step.Upper
                );
                if (step.Argument is { } argument)
                {
                    owner.EmitTypedRegister(
                        argument.Register,
                        IntegerTypeBits,
                        0,
                        argument.Maximum
                    );
                }
                else
                {
                    owner.LiteralOperand(step.Constant!);
                }

                owner.Operand(XRegisterTag, register);
            }

            owner.Op(GetBinarySegment, LabelTag, invalidPayload);
            owner.EmitTypedRegister(context, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(BeamWire.UnsignedTag, register + 1);
            var final = steps[^1];
            owner.EmitTypedRegister(
                register,
                IntegerTypeBits,
                final.ResultLower,
                final.ResultUpper
            );
            owner.Operand(BeamWire.UnsignedTag, payload.Specification.Unit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, register);
            owner.MoveToX0(XRegisterTag, register);
            owner.instructions.WriteByte(Return);

            Mark(invalidPayload);
            MaterializeCurrentSuffix();
            owner.EmitArithmeticLine(extraction.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            Mark(invalidPrefix);
            owner.Op(SetBinaryPosition, XRegisterTag, context);
            owner.Operand(XRegisterTag, originalPosition);
            MaterializeCurrentSuffix();
            JumpTo(functionInfo);

            return true;

            void MaterializeCurrentSuffix()
            {
                owner.Op(GetBinaryTail, XRegisterTag, context);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }
        }

        private static bool IsOpenAlignedBinary(BitPatternSegment field, out Pattern.Variable name)
        {
            if (field is
                {
                    Value: Pattern.Variable variable,
                    Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit, Size: null }
                })
            {
                name = variable;

                return true;
            }

            name = null!;

            return false;
        }

        private static (int Register, RawPayloadArithmetic[] Steps)? ReadRawPayloadArithmetic(
            Expr.Binary width,
            IReadOnlyDictionary<string, RawPayloadField> fields
        )
        {
            var operations = new Stack<Expr.Binary>();
            Expr value = width;
            while (value is Expr.Binary { Operator: ErlangOperators.Plus or ErlangOperators.Minus } operation)
            {
                operations.Push(operation);
                value = operation.Left;
            }

            if (operations.Count == 0 || value is not Expr.Variable binding
                || !fields.TryGetValue(binding.Name, out var initial))
            {
                return null;
            }

            var steps = new List<RawPayloadArithmetic>();
            var used = new HashSet<string>(StringComparer.Ordinal) { binding.Name };
            BigInteger lower = BigInteger.Zero;
            BigInteger upper = initial.Maximum;
            int live = initial.Register + 1;
            foreach (Expr.Binary operation in operations.Reverse())
            {
                if (operation.Right is Expr.Variable argument && argument.Name != binding.Name
                    && fields.TryGetValue(argument.Name, out var field))
                {
                    used.Add(argument.Name);
                    live = Math.Max(live, field.Register + 1);
                    steps.Add(new(
                        operation,
                        field,
                        null,
                        0,
                        0,
                        0,
                        0,
                        live
                    ));
                }
                else if (operation.Right is Expr.Literal { Value: Integer literal })
                {
                    steps.Add(new(
                        operation,
                        null,
                        literal,
                        0,
                        0,
                        0,
                        0,
                        live
                    ));
                }
                else
                {
                    return null;
                }
            }

            if (used.Count != fields.Count)
            {
                return null;
            }

            steps.Reverse();
            for (int index = 0; index < steps.Count; index++)
            {
                RawPayloadArithmetic step = steps[index];
                BigInteger argumentLower = step.Constant is { } constant ? constant.Value : BigInteger.Zero;
                BigInteger argumentUpper = step.Constant is { } fixedValue ? fixedValue.Value : step.Argument!.Maximum;
                bool add = step.Expression.Operator == ErlangOperators.Plus;
                BigInteger nextLower = add ? lower + argumentLower : lower - argumentUpper;
                BigInteger nextUpper = add ? upper + argumentUpper : upper - argumentLower;
                if (nextLower < long.MinValue || nextUpper > long.MaxValue)
                {
                    return null;
                }

                steps[index] = step with
                {
                    Lower = (long)lower,
                    Upper = (long)upper,
                    ResultLower = (long)nextLower,
                    ResultUpper = (long)nextUpper
                };
                lower = nextLower;
                upper = nextUpper;
            }

            return (initial.Register, steps.ToArray());
        }
    }
}
