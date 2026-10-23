using System.Numerics;
using System.Runtime.CompilerServices;

namespace Erlang;

public sealed partial class Mailbox
{
    internal BigInteger CaptureOwnedPayloadBytes()
    {
        lock (gate)
            return (BigInteger)messages.Count * Unsafe.SizeOf<Entry>();
    }
}
