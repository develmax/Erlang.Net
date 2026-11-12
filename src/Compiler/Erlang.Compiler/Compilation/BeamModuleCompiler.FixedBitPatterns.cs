using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const string EnsureExactly = "ensure_exactly";
        private const string EnsureAtLeast = "ensure_at_least";
        private const string SkipBits = "skip";
        private const string ReadBinaryTail = "get_tail";
        private const string SignedBits = "signed";

        private bool TryEmitDirectFixedBitPattern(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [BitPattern { Segments: [var segment] }], Body: Expr.Variable result },
                { Guard: null, Patterns: [Pattern.Variable { }], Body: Expr.Literal fallback }
            ] || segment.Value is not Pattern.Variable binding || binding.Name != result.Name
                || segment.Specification.Type != BitSegmentTypes.Integer || IsByte(segment.Specification)
                || FixedSize(segment.Specification) is not int size)
            {
                return false;
            }

            int failure = Label();
            EmitBitMatchStart(0, failure);
            EmitBitMatchHeader(0, failure, 8, size * segment.Specification.Unit, null);
            EmitBitSegment(segment.Specification, size, 1, XRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitConstant(fallback.Value, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private void EmitFixedBitPattern(BitPattern pattern, int value, int failure)
        {
            for (int index = 0; index < pattern.Segments.Count; index++)
            {
                var segment = pattern.Segments[index].Specification;
                if (FixedSize(segment) is null
                    && !(index == pattern.Segments.Count - 1 && segment is { Type: BitSegmentTypes.Binary, Size: null }))
                {
                    EmitSequentialBitPattern(pattern, value, failure);
                    return;
                }
            }

            int fixedWidth = 0;
            int operandCount = 0;
            int? tailUnit = null;
            var sizes = new List<int>();
            foreach (var segment in pattern.Segments)
            {
                BitSegment specification = segment.Specification;
                if (FixedSize(specification) is int size)
                {
                    int width = size * specification.Unit;
                    if (fixedWidth > int.MaxValue - width || tailUnit is not null)
                    {
                        throw new NotSupportedException(Unsupported);
                    }

                    fixedWidth += width;
                    operandCount += IsWildcard(segment.Value) ? 2 : 6;
                    sizes.Add(size);
                }
                else if (specification is { Type: BitSegmentTypes.Binary, Size: null }
                    && tailUnit is null)
                {
                    tailUnit = specification.Unit;
                    operandCount += IsWildcard(segment.Value) ? 0 : 4;
                }
                else
                {
                    throw new NotSupportedException(Unsupported);
                }
            }

            Load(value);
            bool receiving = owner.receiveScratchRegisters is not null;
            int context = receiving ? 0 : 1;
            EmitBitMatchStart(context, failure);
            EmitBitMatchHeader(context, failure, operandCount + (tailUnit is null ? 2 : 3), fixedWidth, tailUnit);
            var captures = new List<(Pattern Value, int Slot)>();
            int? receiveTail = null;
            for (int index = 0; index < pattern.Segments.Count; index++)
            {
                var segment = pattern.Segments[index];
                var specification = segment.Specification;
                bool tail = index >= sizes.Count;
                if (IsWildcard(segment.Value))
                {
                    if (!tail)
                    {
                        owner.Operand(AtomTag, owner.Intern(SkipBits));
                        owner.Operand(BeamWire.UnsignedTag, sizes[index] * specification.Unit);
                    }
                    continue;
                }

                int live = owner.receiveScratchRegisters is null ? 2 : ReceiveGuardLiveRegisters;
                int slot = Slot();
                if (tail)
                {
                    owner.Operand(AtomTag, owner.Intern(ReadBinaryTail));
                    owner.Operand(BeamWire.UnsignedTag, live);
                    owner.Operand(BeamWire.UnsignedTag, specification.Unit);
                    owner.Operand(receiving ? XRegisterTag : YRegisterTag, receiving ? context : slot);
                    if (receiving)
                    {
                        receiveTail = slot;
                    }
                }
                else
                {
                    EmitBitSegment(specification, sizes[index], live, YRegisterTag, slot);
                }
                captures.Add((segment.Value, slot));
            }

            if (receiveTail is int destination)
            {
                owner.MoveRegister(XRegisterTag, context, YRegisterTag, destination);
            }

            foreach (var capture in captures)
            {
                Pattern(capture.Value, capture.Slot, failure);
            }
        }

        private static bool IsWildcard(Pattern pattern) =>
            pattern is Pattern.Variable { Name: VariableScopeNames.Wildcard };

        private static int? FixedSize(BitSegment specification)
        {
            if (specification.Type is not (BitSegmentTypes.Integer or BitSegmentTypes.Binary))
            {
                return null;
            }

            if (specification.Size is Expr.Literal { Value: Integer size }
                && size.Value >= 0 && size.Value * specification.Unit <= int.MaxValue)
            {
                return (int)size.Value;
            }

            return specification is { Type: BitSegmentTypes.Integer, Size: null } ? BitSyntaxDefaults.IntegerWidth : null;
        }

        private void EmitBitMatchStart(int context, int failure)
        {
            owner.Op(StartBinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, Math.Max(1, ReceiveGuardLiveRegisters));
            owner.Operand(XRegisterTag, context);
        }

        private void EmitBitMatchHeader(
            int context,
            int failure,
            int count,
            int width,
            int? tailUnit
        )
        {
            owner.Op(BinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, context);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, count);
            owner.Operand(AtomTag, owner.Intern(tailUnit is null ? EnsureExactly : EnsureAtLeast));
            owner.Operand(BeamWire.UnsignedTag, width);
            if (tailUnit is int unit)
            {
                owner.Operand(BeamWire.UnsignedTag, unit);
            }
        }

        private void EmitBitSegment(
            BitSegment segment,
            int size,
            int live,
            int destinationTag,
            int destination
        )
        {
            var flags = new List<Term>();
            if (segment.Type == BitSegmentTypes.Integer)
            {
                if (segment.Signed)
                {
                    flags.Add(Term.A(SignedBits));
                }
                if (segment.Endian != BitByteOrders.Big)
                {
                    flags.Add(Term.A(segment.Endian));
                }
            }

            owner.Operand(AtomTag, owner.Intern(segment.Type));
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.LiteralOperand(Cons.From(flags));
            owner.Operand(BeamWire.UnsignedTag, size);
            owner.Operand(BeamWire.UnsignedTag, segment.Unit);
            owner.Operand(destinationTag, destination);
        }
    }
}
