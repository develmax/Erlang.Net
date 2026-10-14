using System.Globalization;
using System.Text;

namespace NodeIdentityContrasts;

internal static class NodeErlangString
{
    internal static string Quote(string value)
    {
        _ = new UTF8Encoding(false, true).GetByteCount(value);
        var result = new StringBuilder(value.Length + NodeStringSyntax.QuoteDelimiters);
        result.Append(NodeStringSyntax.Quote);
        foreach (char character in value)
        {
            if (character == NodeStringSyntax.Quote || character == NodeStringSyntax.Escape)
                result.Append(NodeStringSyntax.Escape).Append(character);
            else if (char.IsControl(character))
                result.Append(NodeStringSyntax.HexPrefix)
                    .Append(((int)character).ToString(NodeStringSyntax.HexFormat, CultureInfo.InvariantCulture))
                    .Append(NodeStringSyntax.HexEnd);
            else
                result.Append(character);
        }
        result.Append(NodeStringSyntax.Quote);

        return result.ToString();
    }
}
