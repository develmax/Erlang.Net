using System.Buffers;
using System.Text;

namespace Erlang;

internal static class AtomNameValidation
{
    internal static void Validate(string name)
    {
        ReadOnlySpan<char> remaining = name.AsSpan();
        int count = 0;
        while (!remaining.IsEmpty)
        {
            if (count == AtomNameLimits.MaximumCodePoints)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            if (Rune.DecodeFromUtf16(remaining, out _, out int consumed) != OperationStatus.Done)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            count++;
            remaining = remaining[consumed..];
        }
    }
}
