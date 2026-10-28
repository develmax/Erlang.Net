using System.Numerics;

namespace Erlang;

// Policy arithmetic only. No backing allocation or public resource accounting.
internal sealed class PinnedHeapSizeClasses508
{
    private readonly ulong[] classes;
    internal BigInteger MaximumNativeSmall { get; }

    internal PinnedHeapSizeClasses508(int wordBits)
    {
        if (wordBits is not (32 or 64))
            throw new ArgumentException(ResourcePolicy508Names.WordWidthRequired, nameof(wordBits));
        MaximumNativeSmall = (BigInteger.One << (wordBits - ResourcePolicy508Names.SmallTagBits
            - ResourcePolicy508Names.SignedValueBit)) - 1;
        int tableLength = wordBits == 64 ? ResourcePolicy508Names.TableLength64 : ResourcePolicy508Names.TableLength32;
        ulong maximum = wordBits == 64
            ? (1UL << ResourcePolicy508Names.MaximumByteAddressBits64) / sizeof(ulong)
            : uint.MaxValue / sizeof(uint);
        var sizes = new List<ulong> { ResourcePolicy508Names.FirstHeapClass, ResourcePolicy508Names.SecondHeapClass };
        while (sizes.Count < ResourcePolicy508Names.FibonacciClassCount)
            sizes.Add(checked(sizes[^1] + sizes[^2] + 1));
        while (sizes.Count < tableLength)
        {
            ulong next = checked(sizes[^1] + sizes[^1] / ResourcePolicy508Names.GrowthDivisor);
            if (next > maximum)
            {
                // erl_gc.c discards one prior valid entry as well as the
                // overflow candidate when establishing num_heap_sizes.
                sizes.RemoveAt(sizes.Count - 1);
                break;
            }
            sizes.Add(next);
        }
        classes = sizes.ToArray();
    }

    internal ulong Next(ulong requested, bool oneClassLarger = false)
    {
        // The C fast path ignores offset for requests below the first class.
        if (requested < classes[0])
            return classes[0];
        int index = Array.BinarySearch(classes, requested);
        index = index < 0 ? ~index : index;
        if (oneClassLarger)
            index++;
        if (index >= classes.Length)
            throw new ArgumentOutOfRangeException(nameof(requested), ResourcePolicy508Names.NoHeapSize);

        return classes[index];
    }

    internal ulong NonnegativeSmall(Term value)
    {
        if (value is not Integer number || number.Value < 0 || number.Value > MaximumNativeSmall)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (ulong)number.Value;
    }
}
