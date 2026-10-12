namespace Erlang;

internal static class NativePrintfOutput
{
    public static byte[] Encode(ReadOnlySpan<byte> source)
    {
        int newLines = source.Count(NativePrintfOutputParameters.LineFeed);
        var output = new byte[checked(source.Length + newLines)];
        int written = 0;
        foreach (byte value in source)
        {
            if (value == NativePrintfOutputParameters.LineFeed)
                output[written++] = NativePrintfOutputParameters.CarriageReturn;
            output[written++] = value;
        }

        return output;
    }
}
