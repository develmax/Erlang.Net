namespace Erlang;

internal static class BinaryArenaSearch598
{
    internal static async ValueTask<BinarySearchHit?> FindFirst598(
        BinaryProgramArena598 arena,
        BitString subject,
        int start,
        int end,
        BinarySearchWork work,
        BinaryArenaReader598 reader
    )
    {
        reader.Require598(arena);

        return arena.Kind598 == BinaryArenaKind598.BoyerMoore
            ? await BmFirst598(
                arena,
                subject,
                start,
                end,
                work
            )
            : await AcFirst598(
                arena,
                subject,
                start,
                end,
                work
            );
    }

    private static async ValueTask<BinarySearchHit?> BmFirst598(
        BinaryProgramArena598 arena,
        BitString subject,
        int start,
        int end,
        BinarySearchWork work
    )
    {
        int length = arena.BmLength598, last = length - 1;
        long position = start;
        while (position <= (long)end - length)
        {
            await work.Step();
            int index = last;
            while (index >= 0 && arena.Needle598(index) == subject.ByteSpan[checked((int)position + index)])
            {
                await work.Step();
                --index;
            }
            if (index < 0) return new BinarySearchHit(checked((int)position), length);
            if (length == 1)
            {
                position++;
                continue;
            }
            long shift = Math.Max(arena.Good598(index), arena.Bad598(subject.ByteSpan[checked((int)position + index)]) - length + 1 + index);
            if (shift <= 0) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
            position = checked(position + shift);
        }

        return null;
    }

    private static async ValueTask<BinarySearchHit?> AcFirst598(
        BinaryProgramArena598 arena,
        BitString subject,
        int start,
        int end,
        BinarySearchWork work
    )
    {
        nint q = arena.AcRoot598, candidate = 0;
        int at = start, candidateStart = 0;
        while (at < end)
        {
            await work.Step();
            byte symbol = subject.ByteSpan[at];
            while (arena.Transition598(q, symbol) == 0 && arena.Failure598(q) != q)
            {
                await work.Step();
                q = arena.Failure598(q);
            }
            nint transition = arena.Transition598(q, symbol);
            if (transition != 0) q = transition;
            ++at;
            long qstart = (long)at - arena.Depth598(q);
            if (qstart < start) throw new InvalidOperationException(BinaryArenaNames598.Invalid);
            if (candidate != 0 && qstart > candidateStart) break;
            if (arena.Final598(q) != 0)
            {
                nint r = q;
                while (arena.Final598(r) < 0)
                {
                    await work.Step();
                    r = arena.Failure598(r);
                }
                int rstart = checked(at - (int)arena.Depth598(r));
                if (candidate == 0 || rstart < candidateStart
                    || (rstart == candidateStart && arena.Depth598(candidate) < arena.Depth598(q)))
                {
                    candidateStart = rstart;
                    candidate = r;
                }
            }
        }

        return candidate == 0 ? null : new BinarySearchHit(candidateStart, checked((int)arena.Depth598(candidate)));
    }
}
