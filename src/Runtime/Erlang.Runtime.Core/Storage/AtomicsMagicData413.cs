using System.Runtime.InteropServices;

namespace Erlang;

// Force OTP aligned magic data and atomic64 payload alignment on both word widths.
[StructLayout(LayoutKind.Explicit)]
internal struct AtomicsMagicData413
{
    [FieldOffset(0)] internal ulong Alignment;
    [FieldOffset(0)] internal AtomicsMagicPayload413 Value;
}
