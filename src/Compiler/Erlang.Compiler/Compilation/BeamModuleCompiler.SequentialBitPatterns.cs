using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedBinarySplit(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: { } guard,
                    Patterns: [Pattern.Variable { }, Pattern.Variable { }],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match { Pattern: BitPattern { Segments: [var prefix, var suffix] }, Value: Expr.Variable source } match,
                            Expr.Tuple { Items: [Expr.Variable first, Expr.Variable second] }
                        ]
                    }
                } clause
                ]
                || !HasUnconditionalParameters(clause)
                || prefix is not
                {
                    Value: Pattern.Variable part,
                    Specification: { Type: BitSegmentTypes.Binary, Size: Expr.Variable size, Unit: BitSyntaxDefaults.BinaryUnit }
                }
                || suffix is not
                {
                    Value: Pattern.Variable tail,
                    Specification: { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit }
                }
                || !DistinctBindings(part, tail, new Pattern.Variable(source.Name), new Pattern.Variable(size.Name))
                || !(first.Name == part.Name && second.Name == tail.Name || first.Name == tail.Name && second.Name == part.Name))
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int input = Array.IndexOf(parameters, source.Name);
            int count = Array.IndexOf(parameters, size.Name);
            if (input < 0 || count < 0
                || PredicateConjuncts(guard).Select(test => ReadEntryTypeGuard(clause, test)).ToArray() is not [var left, var right]
                || !(left == (IsBinary, input) && right == (IsInteger, count)
                    || left == (IsInteger, count) && right == (IsBinary, input)))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            if (EmitEntryGuardPrefix(clause, guard, functionInfo) is not null)
            {
                throw new InvalidOperationException(Unsupported);
            }

            int failure = Label();
            int context = function.Arity;
            int live = context + 1;
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern("no_fail"));
            owner.Operand(BeamWire.UnsignedTag, context);
            owner.Operand(XRegisterTag, input);
            owner.Operand(XRegisterTag, context);
            owner.Op(GetBinarySegment, LabelTag, failure);
            owner.EmitTypedRegister(context, MatchableTypeBits);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.EmitTypedRegister(count, IntegerTypeBits);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, count);
            owner.Op(GetBinaryTail, XRegisterTag, context);
            owner.Operand(XRegisterTag, input);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, context);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(XRegisterTag, first.Name == part.Name ? count : input);
            owner.Operand(XRegisterTag, second.Name == part.Name ? count : input);
            owner.instructions.WriteByte(Return);

            Mark(failure);
            var previous = location;
            owner.EmitArithmeticLine(match.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, input);

            return true;
        }
        private void EmitSequentialBitPattern(BitPattern pattern, int value, int failure, bool knownBinary)
        {
            int context = Slot();
            Load(value);
            EmitBitMatchStart(0, failure, knownBinary);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, context);

            for (int index = 0; index < pattern.Segments.Count; index++)
            {
                var segment = pattern.Segments[index];
                BitSegment specification = segment.Specification;
                if (specification is { Type: BitSegmentTypes.Binary, Size: null })
                {
                    if (index != pattern.Segments.Count - 1)
                    {
                        throw new NotSupportedException(Unsupported);
                    }

                    Load(context);
                    EmitBitMatchHeader(0, failure, 3, 0, specification.Unit);
                    if (!IsWildcard(segment.Value))
                    {
                        int tail = Slot();
                        owner.Op(GetBinaryTail, XRegisterTag, 0);
                        owner.Operand(YRegisterTag, tail);
                        owner.Operand(BeamWire.UnsignedTag, 1);
                        Pattern(segment.Value, tail, failure);
                    }
                    return;
                }
                int destination = ReadSequentialBitSegment(specification, context, failure);
                Pattern(segment.Value, destination, failure);
            }

            Load(context);
            EmitBitMatchHeader(0, failure, 2, 0, null);
        }

        private int ReadSequentialBitSegment(BitSegment specification, int context, int failure)
        {
            byte opcode = specification.Type switch
            {
                BitSegmentTypes.Integer => GetBinaryInteger,
                BitSegmentTypes.Float => GetBinaryFloat,
                BitSegmentTypes.Binary => GetBinarySegment,
                BitSegmentTypes.Utf8 => GetBinaryUtf8,
                BitSegmentTypes.Utf16 => GetBinaryUtf16,
                BitSegmentTypes.Utf32 => GetBinaryUtf32,
                _ => throw new NotSupportedException(Unsupported)
            };
            bool unicode = opcode is GetBinaryUtf8 or GetBinaryUtf16 or GetBinaryUtf32;
            int? size = unicode || specification.Size is null ? null : ValueSlot(specification.Size, failure);
            int destination = Slot();
            Load(context);
            owner.Op(opcode, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            if (!unicode)
            {
                if (size is int slot)
                {
                    owner.Operand(YRegisterTag, slot);
                }
                else
                {
                    owner.LiteralOperand(specification.Type switch
                    {
                        BitSegmentTypes.Binary => Term.A("all"),
                        BitSegmentTypes.Float => Term.I(BitSyntaxDefaults.FloatWidth),
                        _ => Term.I(BitSyntaxDefaults.IntegerWidth)
                    });
                }
                owner.Operand(BeamWire.UnsignedTag, specification.Unit);
            }
            int flags = specification.Type is BitSegmentTypes.Binary or BitSegmentTypes.Utf8 ? 0
                : specification.Endian switch
                {
                    BitByteOrders.Little => 2,
                    BitByteOrders.Native => 16,
                    _ => 0
                };
            if (opcode == GetBinaryInteger && specification.Signed)
            {
                flags |= 4;
            }
            owner.Operand(BeamWire.UnsignedTag, flags);
            owner.Operand(YRegisterTag, destination);

            return destination;
        }
    }
}
