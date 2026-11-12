using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private void EmitSequentialBitPattern(BitPattern pattern, int value, int failure)
        {
            int context = Slot();
            Load(value);
            EmitBitMatchStart(0, failure);
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
