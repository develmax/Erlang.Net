using System.Globalization;
using System.Text;

namespace DistributionPacketContrasts;

internal static class PacketErlangString
{
    internal static string Quote(string value)
    {
        _ = new UTF8Encoding(false, true).GetByteCount(value);
        var result = new StringBuilder(value.Length + PacketStringSyntax.QuoteDelimiters);
        result.Append(PacketStringSyntax.Quote);
        foreach (char character in value)
        {
            if (character == PacketStringSyntax.Quote || character == PacketStringSyntax.Escape)
                result.Append(PacketStringSyntax.Escape).Append(character);
            else if (char.IsControl(character))
                result.Append(PacketStringSyntax.HexPrefix)
                    .Append(((int)character).ToString(PacketStringSyntax.HexFormat, CultureInfo.InvariantCulture))
                    .Append(PacketStringSyntax.HexEnd);
            else
                result.Append(character);
        }
        result.Append(PacketStringSyntax.Quote);

        return result.ToString();
    }
}
