namespace Erlang;

internal static class NativeDistributionInput457
{
    // io.c forwards raw driver bytes for DISTRIBUTION, bypassing ordinary
    // line buffering and {Port,{data,Data}} formatting for every output variant.
    internal static byte[] Copy(ErlDriverOutput420 output)
    {
        if (output.Header is null || output.Body is null || !Enum.IsDefined(output.Kind))
            throw new InvalidDataException(DriverParentWireNames422.Invalid);
        byte[][] segments;
        if (output.Kind == ErlDriverOutputKind420.Vector)
        {
            if (output.Segments is not { Length: > 0 } vector || output.Body.Length != 0 || vector.Any(segment => segment is null))
                throw new InvalidDataException(DriverParentWireNames422.Invalid);
            segments = vector;
        }
        else
        {
            if (output.Segments is not null) throw new InvalidDataException(DriverParentWireNames422.Invalid);
            segments = [output.Body];
        }
        int count = output.Header.Length;
        foreach (var segment in segments) count = checked(count + segment.Length);
        byte[] packet = new byte[count];
        output.Header.CopyTo(packet, 0);
        int offset = output.Header.Length;
        foreach (var segment in segments)
        {
            segment.CopyTo(packet, offset);
            offset += segment.Length;
        }

        return packet;
    }
}
