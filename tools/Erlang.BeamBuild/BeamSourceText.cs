using System.Text;

namespace Erlang.BeamBuild;

internal static class BeamSourceText
{
    internal static string Read(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble))
            throw new InvalidDataException(BeamBuildNames.Invalid);

        return new UTF8Encoding(false, true).GetString(bytes);
    }
}
