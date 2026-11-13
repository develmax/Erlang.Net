using System.Numerics;
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
        private const int MaximumEqualityCommandBits = 31;
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

        private bool TryEmitRegisterBinaryRebuild(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Bits bits } clause]
                || clause.Patterns.OfType<BitPattern>().ToArray() is not [var pattern]
                || pattern.Segments.Count < 2
                || pattern.Segments[^1] is not
                {
                    Value: Pattern.Variable { Name: not VariableScopeNames.Wildcard } tail,
                    Specification: { Type: BitSegmentTypes.Binary, Size: null, Unit: BitSyntaxDefaults.BinaryUnit }
                }
                || AttributeLiteral.TryNormalizeConstant(bits, out _))
            {
                return false;
            }

            int source = -1;
            int context = function.Arity;
            int live = context + 1;
            var registers = new Dictionary<string, (int Register, bool Binary)>(StringComparer.Ordinal);
            for (int index = 0; index < clause.Patterns.Count; index++)
            {
                switch (clause.Patterns[index])
                {
                    case BitPattern:
                        source = index;
                        break;
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Variable variable when registers.TryAdd(variable.Name, (index, false)):
                        break;
                    default:
                        return false;
                }
            }

            var prefix = pattern.Segments.Take(pattern.Segments.Count - 1).ToArray();
            var sizes = new int[prefix.Length];
            int width = 0;
            int operands = 7; // ensure_at_least and get_tail
            bool captured = false;
            for (int index = 0; index < prefix.Length; index++)
            {
                var (value, spec) = prefix[index];
                if (FixedSize(spec) is not int size || spec.Signed || spec.Endian != BitByteOrders.Big
                    || spec.Type == BitSegmentTypes.Binary && spec.Unit != BitSyntaxDefaults.BinaryUnit
                    || width > int.MaxValue - size * spec.Unit)
                {
                    return false;
                }

                width += size * spec.Unit;
                sizes[index] = size;
                if (IsWildcard(value))
                {
                    operands += 2;
                    continue;
                }

                if (captured || value is not Pattern.Variable binding
                    || !registers.TryAdd(binding.Name, (source, spec.Type == BitSegmentTypes.Binary)))
                {
                    return false;
                }

                captured = true;
                operands += 6;
            }

            if (live >= FirstReservedXRegister || width % BitSyntaxDefaults.BinaryUnit != 0
                || !registers.TryAdd(tail.Name, (context, true))
                || !bits.Segments.All(segment => IsRebuildOperand(segment.Value)
                    && (segment.Size is null || IsRebuildOperand(segment.Size))))
            {
                return false;
            }

            var construction = ReadBinaryConstruction(bits);
            int functionInfo = owner.nextLabel - 2;
            int restore = Label();
            owner.Op(StartBinaryMatch, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, source);
            owner.Operand(BeamWire.UnsignedTag, context);
            owner.Operand(XRegisterTag, context);
            owner.Op(GetBinaryPosition, XRegisterTag, context);
            owner.Operand(XRegisterTag, source);
            owner.Operand(BeamWire.UnsignedTag, live);
            EmitBitMatchHeader(context, restore, operands, width, BitSyntaxDefaults.BinaryUnit);
            for (int index = 0; index < prefix.Length; index++)
            {
                var (value, spec) = prefix[index];
                if (IsWildcard(value))
                {
                    owner.Operand(AtomTag, owner.Intern(SkipBits));
                    owner.Operand(BeamWire.UnsignedTag, sizes[index] * spec.Unit);
                }
                else
                {
                    EmitBitSegment(spec, sizes[index], live, XRegisterTag, source);
                }
            }

            owner.Operand(AtomTag, owner.Intern(ReadBinaryTail));
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.BinaryUnit);
            owner.Operand(XRegisterTag, context);
            EmitBinaryConstructionHeader(bits, construction, live, failure: null);
            foreach (var segment in construction)
            {
                if (!EmitBinaryConstructionSegment(segment))
                {
                    EmitRebuildOperand(segment.Value);
                    EmitRebuildOperand(segment.Size);
                }
            }

            owner.instructions.WriteByte(Return);
            Mark(restore);
            owner.Op(SetBinaryPosition, XRegisterTag, context);
            owner.Operand(XRegisterTag, source);
            owner.Op(GetBinaryTail, XRegisterTag, context);
            owner.Operand(XRegisterTag, source);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Op(Jump, LabelTag, functionInfo);

            return true;

            bool IsRebuildOperand(Expr expression) => expression is Expr.Variable variable
                ? registers.ContainsKey(variable.Name)
                : expression is Expr.Literal;

            void EmitRebuildOperand(Expr expression)
            {
                if (expression is Expr.Variable variable)
                {
                    var (register, binary) = registers[variable.Name];
                    if (binary)
                    {
                        owner.EmitTypedRegister(register, MatchableTypeBits, unit: BitSyntaxDefaults.BinaryUnit);
                    }
                    else
                    {
                        owner.Operand(XRegisterTag, register);
                    }
                }
                else
                {
                    owner.LiteralOperand(((Expr.Literal)expression).Value);
                }
            }
        }
        private bool TryEmitFixedBitTuple(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Guard: null, Patterns: [BitPattern pattern], Body: Expr.Tuple result },
                { Guard: null, Patterns: [Pattern.Variable { }], Body: var fallback }
                ] || !AttributeLiteral.TryNormalizeConstant(fallback, out Term constant))
            {
                return false;
            }

            int width = 0;
            var registers = new Dictionary<string, int>(StringComparer.Ordinal);
            int nextRegister = 1;
            for (int index = 0; index < pattern.Segments.Count; index++)
            {
                var (value, specification) = pattern.Segments[index];
                bool tail = index == pattern.Segments.Count - 1
                    && specification is { Type: BitSegmentTypes.Binary, Size: null };
                if (!tail)
                {
                    if (FixedSize(specification) is not int size
                        || size * specification.Unit > int.MaxValue - width)
                    {
                        return false;
                    }

                    width += size * specification.Unit;
                    if (ReadFixedBitEquality(pattern.Segments[index], size * specification.Unit) is not null)
                    {
                        continue;
                    }
                }

                if (IsWildcard(value))
                {
                    continue;
                }

                if (value is not Pattern.Variable capture || !registers.TryAdd(capture.Name, tail ? 0 : nextRegister++))
                {
                    return false;
                }
            }

            if (registers.Count == 0 || nextRegister >= FirstReservedXRegister
                || result.Items.Any(item => item is Expr.Variable variable
                    ? !registers.ContainsKey(variable.Name)
                    : !AttributeLiteral.TryNormalizeConstant(item, out _)))
            {
                return false;
            }

            int failure = Label();
            EmitFixedBitPattern(pattern, 0, failure, registers);
            owner.Op(TestHeap, BeamWire.UnsignedTag, result.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, nextRegister);
            owner.EmitTupleHeader(XRegisterTag, result.Items.Count);
            foreach (Expr item in result.Items)
            {
                if (item is Expr.Variable variable)
                {
                    owner.Operand(XRegisterTag, registers[variable.Name]);
                }
                else
                {
                    AttributeLiteral.TryNormalizeConstant(item, out Term literal);
                    owner.LiteralOperand(literal);
                }
            }

            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitConstant(constant, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private void EmitFixedBitPattern(
            BitPattern pattern,
            int value,
            int failure,
            IReadOnlyDictionary<string, int>? registerCaptures = null
        )
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
            var equalities = new Dictionary<int, (int Width, BigInteger Value, int Count)>();
            for (int index = 0; index < pattern.Segments.Count; index++)
            {
                var segment = pattern.Segments[index];
                BitSegment specification = segment.Specification;
                if (FixedSize(specification) is int size)
                {
                    int width = size * specification.Unit;
                    if (fixedWidth > int.MaxValue - width || tailUnit is not null)
                    {
                        throw new NotSupportedException(Unsupported);
                    }

                    fixedWidth += width;
                    sizes.Add(size);
                    if (ReadFixedBitEquality(segment, width) is { } literal)
                    {
                        int start = index;
                        while (index + 1 < pattern.Segments.Count)
                        {
                            var next = pattern.Segments[index + 1];
                            if (FixedSize(next.Specification) is not int nextSize)
                            {
                                break;
                            }

                            int nextWidth = nextSize * next.Specification.Unit;
                            if (nextWidth > 64 - width || fixedWidth > int.MaxValue - nextWidth
                                || ReadFixedBitEquality(next, nextWidth) is not { } suffix)
                            {
                                break;
                            }

                            literal = (literal << nextWidth) | suffix;
                            width += nextWidth;
                            fixedWidth += nextWidth;
                            sizes.Add(nextSize);
                            index++;
                        }

                        equalities.Add(start, (width, literal, index - start + 1));
                        operandCount += 4 * ((width + MaximumEqualityCommandBits - 1) / MaximumEqualityCommandBits);
                    }
                    else
                    {
                        operandCount += IsWildcard(segment.Value) ? 2 : 6;
                    }
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

            if (registerCaptures is null)
            {
                Load(value);
            }

            bool receiving = owner.receiveScratchRegisters is not null;
            int context = receiving || registerCaptures is not null ? 0 : 1;
            EmitBitMatchStart(context, failure);
            EmitBitMatchHeader(context, failure, operandCount + (tailUnit is null ? 2 : 3), fixedWidth, tailUnit);
            var captures = new List<(Pattern Value, int Slot)>();
            int? receiveTail = null;
            int registerLive = 1;
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

                if (equalities.TryGetValue(index, out var equality))
                {
                    EmitFixedBitEquality(equality.Width, equality.Value);
                    index += equality.Count - 1;
                    continue;
                }

                int live = owner.receiveScratchRegisters is null ? 2 : ReceiveGuardLiveRegisters;
                int slot = registerCaptures is null ? Slot() : registerCaptures[((Pattern.Variable)segment.Value).Name];
                if (registerCaptures is not null)
                {
                    live = registerLive;
                }

                if (tail)
                {
                    owner.Operand(AtomTag, owner.Intern(ReadBinaryTail));
                    owner.Operand(BeamWire.UnsignedTag, live);
                    owner.Operand(BeamWire.UnsignedTag, specification.Unit);
                    owner.Operand(receiving || registerCaptures is not null ? XRegisterTag : YRegisterTag, receiving ? context : slot);
                    if (receiving)
                    {
                        receiveTail = slot;
                    }
                }
                else
                {
                    EmitBitSegment(specification, sizes[index], live, registerCaptures is null ? YRegisterTag : XRegisterTag, slot);
                }

                if (registerCaptures is null)
                {
                    captures.Add((segment.Value, slot));
                }
                else
                {
                    registerLive = Math.Max(registerLive, slot + 1);
                }
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

        private static BigInteger? ReadFixedBitEquality(BitPatternSegment segment, int width)
        {
            // A literal outside the field's value range cannot match, even if its low bits fit.
            if (segment is not
                {
                    Value: Pattern.Literal { Value: Integer expected },
                    Specification: { Type: BitSegmentTypes.Integer, Endian: BitByteOrders.Big } specification
                }
                || width is <= 0 or > 64)
            {
                return null;
            }

            BigInteger limit = BigInteger.One << (specification.Signed ? width - 1 : width);
            BigInteger minimum = specification.Signed ? -limit : BigInteger.Zero;
            return expected.Value >= minimum && expected.Value < limit
                ? expected.Value & ((BigInteger.One << width) - 1)
                : null;
        }

        private void EmitFixedBitEquality(int width, BigInteger expected)
        {
            while (width > 0)
            {
                int bits = Math.Min(width, MaximumEqualityCommandBits);
                width -= bits;
                BigInteger value = (expected >> width) & ((BigInteger.One << bits) - 1);
                owner.Operand(AtomTag, owner.Intern(ErlangOperators.ExactEqual));
                owner.Operand(AtomTag, NilAtomIndex);
                owner.Operand(BeamWire.UnsignedTag, bits);
                owner.Operand(BeamWire.UnsignedTag, value);
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
