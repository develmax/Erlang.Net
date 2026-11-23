using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int BinaryConstructionSegmentOperands = 6;
    private const int MaximumBinaryConstructionUnit = 256;
    private const int WideIntegerSignFillBits = 64;
    private const int MaximumOptimizedIntegerBits = 1 << 24;
    private const string BinaryStringSegment = "string";
    private const string BinaryAppendSegment = "append";
    private const string BinaryPrivateAppendSegment = "private_append";

    private static bool ContainsComputedBits(Expr expression) =>
        expression is Expr.BinaryComprehension
        || expression is Expr.Bits bits && !AttributeLiteral.TryNormalizeConstant(bits, out _)
        || expression is not Expr.Fun { SelfName: not null }
            && LocalFunctionEntries.Children(expression).Any(ContainsComputedBits);

    private sealed record BinaryConstructionSegment(
        string Kind,
        int Number,
        int Unit,
        string Endian,
        Expr Value,
        Expr Size,
        byte[]? Bytes,
        int? PartialBitLength = null
    );

    private void BeginBinaryConstruction(
        int failure,
        int live,
        BigInteger unit,
        int destination,
        int segmentCount,
        int heapWords = 0
    )
    {
        Op(CreateBinary, LabelTag, failure);
        Operand(BeamWire.UnsignedTag, heapWords);
        Operand(BeamWire.UnsignedTag, live);
        Operand(BeamWire.UnsignedTag, unit);
        Operand(XRegisterTag, destination);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, segmentCount * BinaryConstructionSegmentOperands);
    }

    private void EmitBinarySegmentPrefix(
        string kind,
        int number,
        int unit,
        string endian
    )
    {
        Operand(AtomTag, Intern(kind));
        Operand(BeamWire.UnsignedTag, number);
        Operand(BeamWire.UnsignedTag, unit);
        LiteralOperand(endian == BitByteOrders.Big
            || kind is BitSegmentTypes.Utf8 or BitSegmentTypes.Binary or BinaryAppendSegment
            ? Nil.Value : Term.List(Term.A(endian)));
    }

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDirectBits(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Bits bits } clause]
                || !HasUnconditionalParameters(clause) || AttributeLiteral.TryNormalizeConstant(bits, out _))
            {
                return false;
            }

            if (TryEmitDirectMeasuredRecord(clause, bits))
            {
                return true;
            }
            if (!bits.Segments.All(segment => IsCallOperand(segment.Value)
                && (segment.Size is null || IsCallOperand(segment.Size))))
            {
                return false;
            }

            var segments = ReadBinaryConstruction(bits);
            int live = segments.Where(segment => segment.Bytes is null)
                .SelectMany(segment => new[] { segment.Value, segment.Size }).OfType<Expr.Variable>()
                .Select(variable => ArgumentRegister(clause, variable.Name) + 1).DefaultIfEmpty(0).Max();
            EmitBinaryConstructionHeader(
                bits,
                segments,
                live,
                null
            );
            foreach (var segment in segments)
            {
                if (!EmitBinaryConstructionSegment(segment))
                {
                    EmitDirectOperand(clause, segment.Value);
                    EmitDirectOperand(clause, segment.Size);
                }
            }
            owner.instructions.WriteByte(Return);

            return true;
        }

        private void EmitBits(Expr.Bits bits, int? failure, int? writablePrefix = null)
        {
            var segments = ReadBinaryConstruction(bits);
            var slots = new List<(int? Value, int? Size)>(segments.Count);
            // A final call can feed bs_create_bin directly if no size expression
            // needs to run between the call and construction.
            bool lastValueInX0 = failure is null && writablePrefix is null
                && segments.LastOrDefault() is
                {
                    Bytes: null, Value: Expr.Call, Size: Expr.Literal
                };
            for (int index = 0; index < segments.Count; index++)
            {
                var segment = segments[index];
                if (failure is null && index + 1 < segments.Count
                    && TryEmitMeasuredRecordSegments(segment, segments[index + 1], out var measured))
                {
                    slots.Add((measured.Size, null));
                    slots.Add((measured.Value, null));
                    index++;
                    continue;
                }
                if (lastValueInX0 && index == segments.Count - 1)
                {
                    Expression(segment.Value);
                    slots.Add((null, null));
                    continue;
                }

                slots.Add(segment.Bytes is null
                    ? (GuardSlot(segment.Value, failure), GuardSlot(segment.Size, failure))
                    : (null, null));
            }

            if (writablePrefix is int prefix)
            {
                for (int index = 0; index < segments.Count; index++)
                {
                    var segment = segments[index];
                    segments[index] = segment with
                    {
                        Kind = segment.Kind == BinaryAppendSegment ? BitSegmentTypes.Binary : segment.Kind,
                        Number = segment.Number == 0 ? 0 : segment.Number + 1
                    };
                }

                segments.Insert(
                    0,
                    new(
                        BinaryPrivateAppendSegment,
                        1,
                        BitSyntaxDefaults.BitStringUnit,
                        BitByteOrders.Big,
                        new Expr.Literal(Nil.Value),
                        new Expr.Literal(Term.A(BitSizeAtoms.All)),
                        null
                    )
                );
                slots.Insert(0, (prefix, null));
            }

            EmitBinaryConstructionHeader(
                bits,
                segments,
                lastValueInX0 ? 1 : 0,
                failure
            );
            for (int index = 0; index < segments.Count; index++)
            {
                var segment = segments[index];
                if (!EmitBinaryConstructionSegment(segment))
                {
                    if (lastValueInX0 && index == segments.Count - 1)
                    {
                        owner.Operand(XRegisterTag, 0);
                    }
                    else
                    {
                        GuardOperand(segment.Value, slots[index].Value);
                    }
                    GuardOperand(segment.Size, slots[index].Size);
                }
            }
        }

        private bool TryEmitMeasuredRecordSegments(
            BinaryConstructionSegment measurement,
            BinaryConstructionSegment data,
            out (int Size, int Value) slots)
        {
            slots = default;
            if (ReadMeasuredRecordSegments(measurement, data) is not { } measured
                || !variables.ContainsKey(measured.Input.Name))
            {
                return false;
            }

            // Both accesses read the same immutable record. The first access
            // still performs its original validation before the size operation.
            int value = ValueSlot(measured.Field, null);
            int size = Slot();
            owner.EmitCallLine(measured.Call, owner.lastEmittedLine);
            EmitSizeInstruction(measured.Call.Function, 0, ReceiveGuardLiveRegisters);
            owner.Operand(YRegisterTag, value);
            owner.Operand(YRegisterTag, size);
            slots = (size, value);

            return true;
        }

        private (Expr.Call Call, Expr.RecordAccess Field, Expr.Variable Input)? ReadMeasuredRecordSegments(
            BinaryConstructionSegment measurement, BinaryConstructionSegment data)
        {
            if (measurement is not
                {
                    Kind: BitSegmentTypes.Integer, Size: Expr.Literal,
                    Value: Expr.Call
                    {
                        DynamicRemote: false, Function: AutoImportNames.ByteSize or AutoImportNames.BitSize,
                        Arguments: [Expr.RecordAccess { Value: Expr.Variable input } read]
                    } source
                }
                || data is not
                {
                    Kind: BitSegmentTypes.Binary, Size: Expr.Literal,
                    Value: Expr.RecordAccess { Value: Expr.Variable other } repeated
                }
                || input.Name != other.Name || read.Name != repeated.Name || read.Arity != repeated.Arity
                || read.Index != repeated.Index)
            {
                return null;
            }

            Expr.Call call = owner.ResolveImportedCall(source, entries);

            return owner.ResolvesErlangBif(call) ? (call, read, input) : null;
        }

        private bool TryEmitDirectMeasuredRecord(Clause clause, Expr.Bits bits)
        {
            if (clause.Patterns is not [Pattern.Variable parameter]
                || bits.Segments.Count != 2)
            {
                return false;
            }
            var segments = ReadBinaryConstruction(bits);
            if (segments is not [var size, var value]
                || ReadMeasuredRecordSegments(size, value) is not { } measured
                || measured.Input.Name != parameter.Name)
            {
                return false;
            }

            int failure = Label();
            EmitDetailRecordTest(
                0,
                measured.Field.Arity,
                Term.A(measured.Field.Name),
                failure
            );
            owner.EmitTuplePart(0, measured.Field.Index, 0);
            owner.EmitCallLine(measured.Call, location);
            EmitSizeInstruction(measured.Call.Function, 0, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            EmitBinaryConstructionHeader(
                bits,
                segments,
                2,
                null,
                CallLocation(measured.Call, location)
            );
            EmitBinaryConstructionSegment(size);
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            EmitDirectOperand(clause, size.Size);
            EmitBinaryConstructionSegment(value);
            owner.EmitTypedRegister(0, MatchableTypeBits);
            EmitDirectOperand(clause, value.Size);
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.Op(BadRecord, XRegisterTag, 0);

            return true;
        }

        private List<BinaryConstructionSegment> ReadBinaryConstruction(Expr.Bits bits)
        {
            var segments = new List<BinaryConstructionSegment>();
            bool optimize = owner.SsaTypeInferenceEnabled && !owner.ModuleOptimizationDisabled;
            for (int index = 0; index < bits.Segments.Count; index++)
            {
                BitSegment segment = bits.Segments[index];
                Term encoded = Nil.Value;
                bool constant = segment.Endian != BitByteOrders.Native
                    && AttributeLiteral.TryNormalizeConstant(new Expr.Bits([segment]), out encoded)
                    && encoded is BitString;
                if (constant && encoded is BitString bytes
                    && (bytes.IsBinary || optimize && bytes.BitLength < WideIntegerSignFillBits))
                {
                    if (optimize && bytes.IsBinary
                        && TryLowerWideBigEndianInteger(
                            segment,
                            bytes,
                            index + 1,
                            segments
                        ))
                    {
                        continue;
                    }

                    if (bytes.BitLength != 0)
                    {
                        segments.Add(new(
                            BinaryStringSegment,
                            0,
                            BitSyntaxDefaults.BinaryUnit,
                            BitByteOrders.Big,
                            segment.Value,
                            new Expr.Literal(Term.I(bytes.BitLength / BitSyntaxDefaults.BinaryUnit)),
                            bytes.ToArray(),
                            bytes.IsBinary ? null : bytes.BitLength
                        ));
                    }
                    continue;
                }
                if (segment.IsStringLiteral)
                {
                    throw new NotSupportedException(Unsupported);
                }

                bool unicode = BitUnicode.IsUtf(segment.Type);
                Expr size = segment.Size ?? new Expr.Literal(segment.Type switch
                {
                    BitSegmentTypes.Integer => Term.I(BitSyntaxDefaults.IntegerWidth),
                    BitSegmentTypes.Float => Term.I(BitSyntaxDefaults.FloatWidth),
                    BitSegmentTypes.Binary => Term.A(BitSizeAtoms.All),
                    _ => Term.A(CommonAtomNames.Undefined)
                });
                string kind = index == 0 && segment.Type == BitSegmentTypes.Binary
                    && size is Expr.Literal { Value: Atom { Name: BitSizeAtoms.All } } && !constant
                    ? BinaryAppendSegment
                    : segment.Type;
                segments.Add(new(
                    kind,
                    constant ? 0 : index + 1,
                    unicode ? 0 : segment.Unit,
                    segment.Endian,
                    segment.Value,
                    size,
                    null
                ));
            }

            segments = PackPartialBinaryStrings(segments);
            CoalesceBinaryConstructionStrings(segments);

            return segments;
        }

        private static bool TryLowerWideBigEndianInteger(
            BitSegment segment,
            BitString encoded,
            int number,
            List<BinaryConstructionSegment> segments
        )
        {
            if (segment is not { Type: BitSegmentTypes.Integer, Endian: BitByteOrders.Big }
                || encoded.BitLength is < WideIntegerSignFillBits or > MaximumOptimizedIntegerBits
                || !AttributeLiteral.TryNormalizeConstant(segment.Value, out Term value)
                || value is not Integer integer)
            {
                return false;
            }

            // OTP rounds the significant magnitude up to a whole byte before
            // splitting off a sufficiently long sign-filled prefix.
            BigInteger significant = integer.Value.Sign < 0 ? ~integer.Value : integer.Value;
            long bits = significant.GetBitLength();
            long suffixWidth = (bits + BitSyntaxDefaults.BinaryUnit - 1)
                / BitSyntaxDefaults.BinaryUnit * BitSyntaxDefaults.BinaryUnit;
            if (suffixWidth > encoded.BitLength - WideIntegerSignFillBits)
            {
                return false;
            }

            int suffixBits = (int)suffixWidth;
            int prefixBits = encoded.BitLength - suffixBits;
            int unit = suffixBits == 0 ? segment.Unit : BitSyntaxDefaults.BitStringUnit;
            segments.Add(new(
                BitSegmentTypes.Integer,
                number,
                unit,
                BitByteOrders.Big,
                new Expr.Literal(Term.I(integer.Value.Sign < 0 ? -1 : 0)),
                new Expr.Literal(Term.I(prefixBits / unit)),
                null
            ));
            if (suffixBits != 0)
            {
                var suffix = encoded.SliceBits(prefixBits, suffixBits);
                segments.Add(new(
                    BinaryStringSegment,
                    0,
                    BitSyntaxDefaults.BinaryUnit,
                    BitByteOrders.Big,
                    segment.Value,
                    new Expr.Literal(Term.I(suffixBits / BitSyntaxDefaults.BinaryUnit)),
                    suffix.ToArray()
                ));
            }

            return true;
        }

        private static List<BinaryConstructionSegment> PackPartialBinaryStrings(List<BinaryConstructionSegment> segments)
        {
            if (!segments.Any(segment => segment.PartialBitLength.HasValue))
            {
                return segments;
            }

            var result = new List<BinaryConstructionSegment>(segments.Count);
            for (int index = 0; index < segments.Count; index++)
            {
                BinaryConstructionSegment segment = segments[index];
                if (segment.Bytes is null)
                {
                    result.Add(segment);
                    continue;
                }

                int start = index;
                bool containsPartial = segment.PartialBitLength.HasValue;
                while (index + 1 < segments.Count && segments[index + 1].Bytes is not null)
                {
                    index++;
                    containsPartial |= segments[index].PartialBitLength.HasValue;
                }

                if (!containsPartial)
                {
                    result.AddRange(segments.GetRange(start, index - start + 1));
                    continue;
                }

                // The SSA binary-construction pass joins literal bitstrings
                // before the code generator divides each run into bytes and a tail.
                var parts = new (Term, Term?, BitSegment)[index - start + 1];
                for (int item = start; item <= index; item++)
                {
                    BinaryConstructionSegment literal = segments[item];
                    var bits = new BitString(literal.Bytes!, literal.PartialBitLength);
                    parts[item - start] = (bits, null, new(
                        new Expr.Literal(bits),
                        null,
                        BitSegmentTypes.Binary,
                        BitSyntaxDefaults.BitStringUnit
                    ));
                }

                var packed = BitConstruction.Create(parts);
                int trailingBits = packed.BitLength % BitSyntaxDefaults.BinaryUnit;
                int alignedBits = packed.BitLength - trailingBits;
                if (alignedBits != 0)
                {
                    result.Add(segment with
                    {
                        Size = new Expr.Literal(Term.I(alignedBits / BitSyntaxDefaults.BinaryUnit)),
                        Bytes = packed.SliceBits(0, alignedBits).ToArray(),
                        PartialBitLength = null
                    });
                }

                if (trailingBits != 0)
                {
                    byte residue = packed.SliceBits(alignedBits, trailingBits).ToArray()[0];
                    result.Add(new(
                        BitSegmentTypes.Integer,
                        0,
                        BitSyntaxDefaults.BitStringUnit,
                        BitByteOrders.Big,
                        new Expr.Literal(Term.I(residue >> (BitSyntaxDefaults.BinaryUnit - trailingBits))),
                        new Expr.Literal(Term.I(trailingBits)),
                        null
                    ));
                }
            }

            return result;
        }

        private static void CoalesceBinaryConstructionStrings(List<BinaryConstructionSegment> segments)
        {
            int output = 0;
            for (int index = 0; index < segments.Count; index++)
            {
                var segment = segments[index];
                if (segment.Bytes is { } first && index + 1 < segments.Count && segments[index + 1].Bytes is not null)
                {
                    var run = new List<byte>(first);
                    while (index + 1 < segments.Count && segments[index + 1].Bytes is { } next)
                    {
                        run.AddRange(next);
                        index++;
                    }

                    segment = segment with
                    {
                        Bytes = run.ToArray(),
                        Size = new Expr.Literal(Term.I(run.Count))
                    };
                }

                segments[output++] = segment;
            }

            segments.RemoveRange(output, segments.Count - output);
        }

        private void EmitBinaryConstructionHeader(
            Expr.Bits bits,
            List<BinaryConstructionSegment> segments,
            int live,
            int? failure,
            BeamSourceLocation? previousLine = null
        )
        {
            if (failure is null)
            {
                var previous = previousLine ?? location;
                owner.EmitArithmeticLine(bits.SourceLine, ref previous);
            }

            BigInteger unit = BinaryConstructionResultUnit(segments);
            if (segments is [{ Kind: BinaryAppendSegment or BinaryPrivateAppendSegment } append, ..])
            {
                unit = BigInteger.Max(unit, append.Unit);
            }

            owner.BeginBinaryConstruction(
                failure ?? 0,
                live,
                unit,
                0,
                segments.Count
            );
        }

        private static BigInteger BinaryConstructionResultUnit(List<BinaryConstructionSegment> segments)
        {
            const int utf16Unit = UtfSegmentLimits.Utf16BytesPerCharacter * BitSyntaxDefaults.BinaryUnit;
            const int utf32Width = UtfSegmentLimits.Utf32BytesPerScalar * BitSyntaxDefaults.BinaryUnit;
            BigInteger fixedBits = 0;
            BigInteger variableUnit = MaximumBinaryConstructionUnit;
            foreach (var segment in segments)
            {
                switch (segment)
                {
                    case { Kind: BitSegmentTypes.Utf8 }:
                        fixedBits += BitSyntaxDefaults.BinaryUnit;
                        variableUnit = BigInteger.GreatestCommonDivisor(variableUnit, BitSyntaxDefaults.BinaryUnit);
                        break;
                    case { Kind: BitSegmentTypes.Utf16 }:
                        fixedBits += utf16Unit;
                        variableUnit = BigInteger.GreatestCommonDivisor(variableUnit, utf16Unit);
                        break;
                    case { Kind: BitSegmentTypes.Utf32 }:
                        fixedBits += utf32Width;
                        break;
                    case { Size: Expr.Literal { Value: Integer size } } when size.Value >= 0:
                        fixedBits += size.Value * segment.Unit;
                        break;
                    case
                    {
                        Size: Expr.Literal { Value: Atom { Name: BitSizeAtoms.All } },
                        Value: Expr.Literal { Value: BitString bits }
                    }:
                        fixedBits += bits.BitLength;
                        break;
                    default:
                        variableUnit = BigInteger.GreatestCommonDivisor(variableUnit, segment.Unit);
                        break;
                }
            }

            return BigInteger.GreatestCommonDivisor(fixedBits, variableUnit);
        }

        private bool EmitBinaryConstructionSegment(BinaryConstructionSegment segment)
        {
            owner.EmitBinarySegmentPrefix(
                segment.Kind,
                segment.Number,
                segment.Unit,
                segment.Endian
            );
            if (segment.Bytes is not { } bytes)
            {
                return false;
            }

            owner.Operand(BeamWire.UnsignedTag, owner.InternBinaryString(bytes));
            owner.LiteralOperand(((Expr.Literal)segment.Size).Value);

            return true;
        }
    }
}
