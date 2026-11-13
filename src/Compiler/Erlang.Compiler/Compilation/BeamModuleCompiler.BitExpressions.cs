using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int BinaryConstructionSegmentOperands = 6;
    private const int MaximumBinaryConstructionUnit = 256;
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
        byte[]? Bytes
    );

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDirectBits(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Body: Expr.Bits bits } clause]
                || !HasUnconditionalParameters(clause) || AttributeLiteral.TryNormalizeConstant(bits, out _)
                || !bits.Segments.All(segment => IsCallOperand(segment.Value)
                    && (segment.Size is null || IsCallOperand(segment.Size))))
            {
                return false;
            }

            var segments = ReadBinaryConstruction(bits);
            int live = segments.Where(segment => segment.Bytes is null)
                .SelectMany(segment => new[] { segment.Value, segment.Size }).OfType<Expr.Variable>()
                .Select(variable => ArgumentRegister(clause, variable.Name) + 1).DefaultIfEmpty(0).Max();
            EmitBinaryConstructionHeader(bits, segments, live, null);
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

                segments.Insert(0, new(
                    BinaryPrivateAppendSegment,
                    1,
                    BitSyntaxDefaults.BitStringUnit,
                    BitByteOrders.Big,
                    new Expr.Literal(Nil.Value),
                    new Expr.Literal(Term.A(BitSizeAtoms.All)),
                    null
                ));
                slots.Insert(0, (prefix, null));
            }

            EmitBinaryConstructionHeader(bits, segments, lastValueInX0 ? 1 : 0, failure);
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

        private static List<BinaryConstructionSegment> ReadBinaryConstruction(Expr.Bits bits)
        {
            var segments = new List<BinaryConstructionSegment>();
            for (int index = 0; index < bits.Segments.Count; index++)
            {
                BitSegment segment = bits.Segments[index];
                Term encoded = Nil.Value;
                bool constant = segment.Endian != BitByteOrders.Native
                    && AttributeLiteral.TryNormalizeConstant(new Expr.Bits([segment]), out encoded)
                    && encoded is BitString;
                if (constant && encoded is BitString { IsBinary: true } bytes)
                {
                    if (bytes.BitLength != 0)
                    {
                        segments.Add(new(
                            BinaryStringSegment,
                            0,
                            BitSyntaxDefaults.BinaryUnit,
                            BitByteOrders.Big,
                            segment.Value,
                            new Expr.Literal(Term.I(bytes.BitLength / BitSyntaxDefaults.BinaryUnit)),
                            bytes.ToArray()
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
                segments.Add(new(kind, constant ? 0 : index + 1, unicode ? 0 : segment.Unit, segment.Endian, segment.Value, size, null));
            }

            return segments;
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

            BigInteger unit = 0;
            foreach (var segment in segments)
            {
                BigInteger width = segment.Kind switch
                {
                    BitSegmentTypes.Utf8 => 8,
                    BitSegmentTypes.Utf16 => 16,
                    BitSegmentTypes.Utf32 => 32,
                    _ when segment.Size is Expr.Literal { Value: Integer size } => size.Value * segment.Unit,
                    _ => segment.Unit
                };
                unit = BigInteger.GreatestCommonDivisor(unit, width);
            }
            owner.Op(CreateBinary, LabelTag, failure ?? 0);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Operand(BeamWire.UnsignedTag, unit > 0 && unit <= MaximumBinaryConstructionUnit ? unit : 1);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, segments.Count * BinaryConstructionSegmentOperands);
        }

        private bool EmitBinaryConstructionSegment(BinaryConstructionSegment segment)
        {
            owner.Operand(AtomTag, owner.Intern(segment.Kind));
            owner.Operand(BeamWire.UnsignedTag, segment.Number);
            owner.Operand(BeamWire.UnsignedTag, segment.Unit);
            owner.LiteralOperand(segment.Endian == BitByteOrders.Big || segment.Kind is BitSegmentTypes.Utf8 or BitSegmentTypes.Binary or BinaryAppendSegment
                ? Nil.Value : Term.List(Term.A(segment.Endian)));
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
