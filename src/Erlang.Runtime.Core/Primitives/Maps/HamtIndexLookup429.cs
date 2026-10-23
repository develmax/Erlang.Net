using System.Numerics;

namespace Erlang;

internal static class HamtIndexLookup429
{
    internal static bool TryGet(
        HamtIndex429 index,
        Term key,
        ulong hash,
        out Term? value
    )
    {
        HamtViewNode current = index.Root;
        int level = 0;
        while (current.Leaf is null)
        {
            if (current.IsCollision)
            {
                foreach (var child in current.Children)
                    if (child.Leaf is { } collision && collision.Key.Equals(key))
                    {
                        value = collision.Value;

                        return true;
                    }
                value = null;

                return false;
            }
            int digit = (int)((hash >> (level * HamtViewLayout.NibbleBits)) & HamtViewLayout.NibbleMask);
            uint bit = 1U << digit;
            if ((current.Bitmap & bit) == 0)
            {
                value = null;

                return false;
            }
            int offset = BitOperations.PopCount((uint)current.Bitmap & (bit - 1));
            current = current.Children[offset];
            level++;
        }
        var pair = current.Leaf.Value;
        if (pair.Key.Equals(key))
        {
            value = pair.Value;

            return true;
        }
        value = null;

        return false;
    }
}
