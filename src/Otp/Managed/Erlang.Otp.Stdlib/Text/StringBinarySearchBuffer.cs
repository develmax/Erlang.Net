using System.Text;

namespace Erlang;

internal sealed class StringBinarySearchBuffer(IReadOnlyList<Term> needle)
{
    private object? backing;
    private ReadOnlyMemory<byte> bytes;
    private long bitOffset;
    private byte[]? completePattern;
    private byte[]? initialPattern;

    internal IReadOnlyList<Term> Needle => needle;

    internal ReadOnlyMemory<byte> Read(BitString binary)
    {
        BinaryBacking current = binary.ResourceBacking577;
        // Preserve the original read lease and stale/ownership checks on every search.
        using IDisposable? read = current.RetainNativeCopyRead524();
        OrdinaryBinaryBacking? ordinary = current.NativeStorage524 as OrdinaryBinaryBacking;
        bool immutable = current.CanCaptureImmutableOrdinary
            || ordinary is not null && !ordinary.IsWritable && ordinary.WritableBinding is null;
        if (!immutable || binary.OwnedHeapBits is not null)
            return binary.ByteSpan.ToArray();

        object identity = ordinary is not null ? ordinary : current;
        ResourceBitsRead577 range = binary.CaptureResourceBits577();
        if (range.BitLength < 0 || range.BitLength % BitStringLayout.BitsPerByte != 0)
            return binary.ByteSpan.ToArray();

        int length = range.BitLength / BitStringLayout.BitsPerByte;
        long relative = range.BitOffset - bitOffset;
        if (ReferenceEquals(identity, backing) && relative >= 0
            && relative % BitStringLayout.BitsPerByte == 0
            && relative / BitStringLayout.BitsPerByte <= bytes.Length - length)
        {
            return bytes.Slice(
                checked((int)(relative / BitStringLayout.BitsPerByte)),
                length
            );
        }

        // A guest-to-host return can copy an immutable suffix into another owner.
        // Prove its bytes before reusing the snapshot; arbitrary replacement tails
        // must never be inferred from their lengths alone.
        ReadOnlyMemory<byte> captured = backing is not null && length <= bytes.Length
            && binary.ByteSpan.SequenceEqual(bytes.Span[(bytes.Length - length)..])
                ? bytes.Slice(bytes.Length - length)
                : binary.ByteSpan.ToArray();
        backing = identity;
        bitOffset = range.BitOffset;
        bytes = captured;

        return captured;
    }

    internal byte[] Pattern(bool continuation)
    {
        byte[]? cached = continuation ? initialPattern : completePattern;
        if (cached is not null)
            return cached;

        // Preserve lazy full-versus-initial validation and its exception order.
        string text = string.Concat((continuation ? needle.Take(1) : needle)
            .Cast<Integer>().Select(cp => char.ConvertFromUtf32((int)cp.Value)));
        byte[] encoded = Encoding.UTF8.GetBytes(text);
        if (continuation)
            initialPattern = encoded;
        else
            completePattern = encoded;

        return encoded;
    }
}
