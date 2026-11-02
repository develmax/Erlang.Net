namespace Erlang;

public sealed partial class BitString
{
    internal int CompareBits139(BitString other)
    {
        // Validate both actual owned descriptors before any early comparison.
        int leftBits = BitLength;
        int rightBits = other.BitLength;
        IDisposable? leftRead = null, rightRead = null;
        Exception? primary = null;
        try
        {
            leftRead = backing.RetainNativeCopyRead524();
            rightRead = other.backing.RetainNativeCopyRead524();
            ReadOnlySpan<byte> left = ByteSpan;
            ReadOnlySpan<byte> right = other.ByteSpan;
            int commonBits = Math.Min(leftBits, rightBits);
            int wholeBytes = commonBits / BitStringLayout.BitsPerByte;
            int compared = left[..wholeBytes].SequenceCompareTo(right[..wholeBytes]);
            if (compared != 0)
                return Math.Sign(compared);
            int tailBits = commonBits % BitStringLayout.BitsPerByte;
            if (tailBits != 0)
            {
                // Only shared high bits participate, never padding or the
                // longer operand's later valid bits in this same byte.
                int shift = BitStringLayout.BitsPerByte - tailBits;
                compared = (left[wholeBytes] >> shift).CompareTo(right[wholeBytes] >> shift);
                if (compared != 0)
                    return compared;
            }

            return leftBits.CompareTo(rightBits);
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            Exception? rightCleanup = null, leftCleanup = null;
            try
            {
                rightRead?.Dispose();
            }
            catch (Exception error)
            {
                rightCleanup = error;
            }
            try
            {
                leftRead?.Dispose();
            }
            catch (Exception error)
            {
                leftCleanup = error;
            }
            // Both releases are attempted before any error-container allocation.
            if (rightCleanup is not null || leftCleanup is not null)
            {
                if (primary is not null)
                    throw rightCleanup is not null && leftCleanup is not null
                        ? new AggregateException(primary, rightCleanup, leftCleanup)
                        : new AggregateException(primary, rightCleanup ?? leftCleanup!);
                if (rightCleanup is not null && leftCleanup is not null)
                    throw new AggregateException(rightCleanup, leftCleanup);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(rightCleanup ?? leftCleanup!).Throw();
            }
        }
    }
}
