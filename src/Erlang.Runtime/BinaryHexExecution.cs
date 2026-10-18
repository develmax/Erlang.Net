namespace Erlang;

internal static class BinaryHexExecution
{
    private static readonly object encodeReturn = new();
    private static readonly object decodeReturn = new();
    private static readonly object encodeComprehension = new();
    private static readonly object decodeComprehension = new();
    private static readonly object encodeRemainder = new();
    private static readonly object decodeRemainder = new();

    internal static async ValueTask<Term> Encode(
        ProcessContext context,
        Term value,
        Term casing,
        bool unary
    )
    {
        if (value is not BitString { IsBinary: true } data || casing is not Atom { Name: BinaryWrapperNames.Uppercase or BinaryWrapperNames.Lowercase } which)
            throw BinaryWrapperTrace.Public(
                context,
                BinaryModuleNames.EncodeHex,
                unary ? [value] : [value, casing],
                line: BinaryWrapperSource.ErrorInfo
            );
        data.PinForMatch();
        using var tail = context.EnterSourceTailCall(BinaryModuleNames.Module, BinaryModuleNames.EncodeHex);
        byte[] output;
        BitString accumulator;
        int firstBytes = data.ByteLength / BinaryHexSource.InputBlockBytes * BinaryHexSource.InputBlockBytes;
        using (context.EnterGeneratedFunction(
            BinaryModuleNames.Module,
            BinaryHexNames.EncodeFirst,
            BinaryHexSource.EncodeFirstArity,
            Nil.Value,
            encodeReturn
        ))
        {
            if (data.ByteLength > BinaryWrapperSource.MaximumBinaryBytes / BinaryWrapperSource.HexWidth)
                throw context.CaptureSourceFailure(new ErlangException(ErlangErrorReasons.SystemLimit));
            output = new byte[data.ByteLength * BinaryWrapperSource.HexWidth];
            accumulator = BitString.InitializeWritable(firstBytes * BinaryWrapperSource.HexWidth);
            using var source = context.EnterSourceLocation(BinaryWrapperTrace.Location(BinaryHexSource.EncodeComprehensionLine));
            using var helper = context.EnterGeneratedFunction(
                BinaryModuleNames.Module,
                BinaryHexNames.EncodeComprehension,
                BinaryHexSource.EncodeComprehensionArity,
                BinaryWrapperTrace.Location(BinaryHexSource.EncodeComprehensionLine),
                encodeComprehension
            );
            string digits = which.Name == BinaryWrapperNames.Uppercase ? BinaryWrapperNames.UpperDigits : BinaryWrapperNames.LowerDigits;
            for (int index = 0; index < firstBytes; index++)
            {
                await context.ReduceAsync();
                EncodeByte(
                    data,
                    output,
                    digits,
                    index
                );
            }
            accumulator = BitString.FromValidatedAppend(
                accumulator,
                BitString.FromOwnedBytes(output.AsSpan(0, firstBytes * BinaryWrapperSource.HexWidth).ToArray())
            );
        }
        using (context.EnterGeneratedFunction(
            BinaryModuleNames.Module,
            BinaryHexNames.EncodeRemainder,
            BinaryHexSource.EncodeRemainderArity,
            BinaryWrapperTrace.Location(BinaryHexSource.EncodeAppendLine),
            encodeRemainder
        ))
        {
            string digits = which.Name == BinaryWrapperNames.Uppercase ? BinaryWrapperNames.UpperDigits : BinaryWrapperNames.LowerDigits;
            for (int index = firstBytes; index < data.ByteLength; index++)
            {
                await context.ReduceAsync();
                EncodeByte(
                    data,
                    output,
                    digits,
                    index
                );
                accumulator = BitString.FromValidatedAppend(
                    accumulator,
                    BitString.FromOwnedBytes(output.AsSpan(0, (index + 1) * BinaryWrapperSource.HexWidth).ToArray())
                );
            }
        }

        return accumulator;
    }

    internal static async ValueTask<Term> Decode(ProcessContext context, Term value)
    {
        if (value is not BitString data || data.ByteLength % BinaryWrapperSource.HexWidth != 0)
            throw BinaryWrapperTrace.Public(context, BinaryModuleNames.DecodeHex, [value]);
        data.PinForMatch();
        using var owner = context.EnterSourceLocation(BinaryWrapperTrace.Location(BinaryWrapperSource.DecodeCall));
        if (!data.IsBinary)
        {
            using var helper = context.EnterGeneratedFunction(
                BinaryModuleNames.Module,
                BinaryHexNames.DecodeFirst,
                BinaryHexSource.DecodeFirstArity,
                BinaryWrapperTrace.Location(BinaryWrapperSource.DecodeMatch),
                decodeReturn
            );

            throw context.CaptureSourceFailure(new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), value)));
        }
        byte[] output = new byte[data.ByteLength / BinaryWrapperSource.HexWidth];
        int firstBytes = data.ByteLength / BinaryHexSource.InputBlockBytes * BinaryHexSource.InputBlockBytes;
        BitString accumulator;
        bool valid = true;
        using (context.EnterGeneratedFunction(
            BinaryModuleNames.Module,
            BinaryHexNames.DecodeFirst,
            BinaryHexSource.DecodeFirstArity,
            Nil.Value,
            decodeReturn
        ))
        {
            accumulator = BitString.InitializeWritable(firstBytes / BinaryWrapperSource.HexWidth);
            using var source = context.EnterSourceLocation(BinaryWrapperTrace.Location(BinaryHexSource.DecodeComprehensionLine));
            using var helper = context.EnterGeneratedFunction(
                BinaryModuleNames.Module,
                BinaryHexNames.DecodeComprehension,
                BinaryHexSource.DecodeComprehensionArity,
                BinaryWrapperTrace.Location(BinaryHexSource.DecodeComprehensionLine),
                decodeComprehension
            );
            for (int index = 0; index < firstBytes; index += BinaryWrapperSource.HexWidth)
            {
                await context.ReduceAsync();
                int high = Digit(data.ByteSpan[index]);
                int low = Digit(data.ByteSpan[index + 1]);
                if (high < 0 || low < 0)
                {
                    valid = false;
                    break;
                }
                output[index / BinaryWrapperSource.HexWidth] = (byte)((high << BinaryWrapperSource.NibbleBits) | low);
            }
            if (valid)
                accumulator = BitString.FromValidatedAppend(
                    accumulator,
                    BitString.FromOwnedBytes(output.AsSpan(0, firstBytes / BinaryWrapperSource.HexWidth).ToArray())
                );
        }
        if (!valid)
            throw BinaryWrapperTrace.Public(context, BinaryModuleNames.DecodeHex, [value]);
        using (context.EnterGeneratedFunction(
            BinaryModuleNames.Module,
            BinaryHexNames.DecodeRemainder,
            BinaryHexSource.DecodeRemainderArity,
            BinaryWrapperTrace.Location(BinaryHexSource.DecodeAppendLine),
            decodeRemainder
        ))
        {
            for (int index = firstBytes; index < data.ByteLength; index++)
            {
                await context.ReduceAsync();
                int digit = Digit(data.ByteSpan[index]);
                if (digit < 0)
                {
                    valid = false;
                    break;
                }
                int outputIndex = index / BinaryWrapperSource.HexWidth;
                if (index % BinaryWrapperSource.HexWidth == 0)
                    output[outputIndex] = (byte)(digit << BinaryWrapperSource.NibbleBits);
                else
                    output[outputIndex] |= (byte)digit;
                int bits = (index + 1) * BinaryWrapperSource.NibbleBits;
                int bytes = (bits + BinaryWrapperSource.BitsPerByte - 1) / BinaryWrapperSource.BitsPerByte;
                accumulator = BitString.FromValidatedAppend(
                    accumulator,
                    new BitString(output.AsSpan(0, bytes), bits)
                );
            }
        }
        if (!valid)
            throw BinaryWrapperTrace.Public(context, BinaryModuleNames.DecodeHex, [value]);

        return accumulator;
    }

    private static void EncodeByte(
        BitString data,
        byte[] output,
        string digits,
        int index
    )
    {
        byte input = data.ByteSpan[index];
        output[index * BinaryWrapperSource.HexWidth] = (byte)digits[input >> BinaryWrapperSource.NibbleBits];
        output[index * BinaryWrapperSource.HexWidth + 1] = (byte)digits[input & BinaryWrapperSource.NibbleMask];
    }

    private static int Digit(byte value)
    {
        int digit = BinaryWrapperNames.UpperDigits.IndexOf((char)value);

        return digit >= 0 ? digit : BinaryWrapperNames.LowerDigits.IndexOf((char)value);
    }
}
