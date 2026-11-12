// Derived from Erlang/OTP 29.1.1 dets_utils.erl (Apache-2.0).
// Copyright Ericsson AB 2001-2026. Upstream notices are retained in Otp/dets_utils.erl.
namespace Erlang;

/// <summary>DETS buddy addresses are relative to the file's allocation base.</summary>
internal sealed class DetsBuddyAllocator
{
    private readonly SortedSet<long>[] free;

    internal DetsBuddyAllocator(long allocationBase)
    {
        Base = allocationBase;
        free = Enumerable.Range(0, DetsV9Layout.MaximumCollectionExponent + 1)
            .Select(_ => new SortedSet<long>()).ToArray();
        free[DetsV9Layout.MaximumCollectionExponent].Add(allocationBase);
    }

    internal long Base { get; }

    internal static DetsBuddyAllocator Restore(long allocationBase, IEnumerable<(int Exponent, long Address)> blocks)
    {
        var allocator = new DetsBuddyAllocator(allocationBase);
        foreach (var level in allocator.free)
        {
            level.Clear();
        }
        long previousEnd = allocationBase;
        foreach (var block in blocks.OrderBy(block => block.Address))
        {
            if (block.Exponent is < 0 or > DetsV9Layout.MaximumCollectionExponent
                || block.Address < previousEnd || (block.Address - allocationBase) % (1L << block.Exponent) != 0
                || block.Address + (1L << block.Exponent) > allocationBase + DetsV9Layout.AddressSpaceBytes)
            {
                throw new DetsFileException(DetsFileReasons.BadFreelists);
            }
            allocator.free[block.Exponent].Add(block.Address);
            previousEnd = block.Address + (1L << block.Exponent);
        }

        return allocator;
    }

    internal DetsBuddyAllocator Clone() => Restore(Base, FreeBlocks);

    internal IEnumerable<(int Exponent, long Address)> FreeBlocks
    {
        get
        {
            for (int exponent = 0; exponent < free.Length; exponent++)
            {
                foreach (long address in free[exponent])
                {
                    yield return (exponent, address);
                }
            }
        }
    }

    internal (long Address, int Exponent) Allocate(long bytes)
    {
        int exponent = Exponent(bytes);
        int available = exponent;
        while (available < free.Length && free[available].Count == 0)
        {
            available++;
        }
        if (available == free.Length)
        {
            throw new DetsFileException(DetsFileReasons.NoSpace);
        }
        long address = free[available].Min;
        if (address + (1L << exponent) > DetsV9Layout.AddressSpaceBytes - DetsV9Layout.FreeListReserveBytes)
        {
            throw new DetsFileException(DetsFileReasons.NoSpace);
        }
        free[available].Remove(address);
        while (available > exponent)
        {
            available--;
            free[available].Add(address + (1L << available));
        }

        return (address, exponent);
    }

    internal void Release(long address, int exponent)
    {
        if (exponent is < 0 or > DetsV9Layout.MaximumCollectionExponent || address < Base
            || (address - Base) % (1L << exponent) != 0
            || address - Base >= DetsV9Layout.AddressSpaceBytes)
        {
            throw new DetsFileException(DetsFileReasons.BadFreelists);
        }
        while (exponent < DetsV9Layout.MaximumCollectionExponent)
        {
            long buddy = Base + ((address - Base) ^ (1L << exponent));
            if (!free[exponent].Remove(buddy))
            {
                break;
            }
            address = Math.Min(address, buddy);
            exponent++;
        }
        if (!free[exponent].Add(address))
        {
            throw new DetsFileException(DetsFileReasons.BadFreelists);
        }
    }

    internal static int Exponent(long bytes)
    {
        if (bytes <= 0 || bytes > DetsV9Layout.AddressSpaceBytes)
        {
            throw new DetsFileException(DetsFileReasons.NoSpace);
        }
        int exponent = 0;
        long size = 1;
        while (size < bytes)
        {
            size <<= 1;
            exponent++;
        }

        return exponent;
    }
}
