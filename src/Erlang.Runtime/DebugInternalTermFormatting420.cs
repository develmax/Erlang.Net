using System.Text;
namespace Erlang;

internal static class DebugInternalTermFormatting420
{
    internal static ReadOnlyMemory<byte> Bytes(ProcessRuntime runtime, Term value)
    {
        byte[] bytes = new TermDisplayWriter(runtime).Write(value);
        // display/1 adds exactly this terminator after the complete term. printf
        // %T embeds the term itself; retain every content byte, including escapes.
        if (bytes.Length == 0 || bytes[^1] != TermDisplayParameters.LineFeed)
            throw new InvalidDataException(DebugInternalTermFormatting420Names.MissingTerminator);

        return bytes.AsMemory(0, bytes.Length - 1);
    }

    internal static string Text(ProcessRuntime runtime, Term value) => Encoding.UTF8.GetString(Bytes(runtime, value).Span);
}
