namespace Erlang;

// Direct port of pinned do_binary_match_compile/create_bmdata/ac_add... /
// ac_compute_failure_functions. All retained executable tables are native.
internal static class BinaryArenaCompiler598
{
    internal static async ValueTask<BinaryProgramArena598> Build598(List<BitString> words, BinarySearchWork work)
    {
        if (words.Count == 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
        long total = 0;
        try { foreach (var word in words) total = checked(total + word.ByteLength); }
        catch (OverflowException) { throw new ErlangException(ErlangErrorReasons.SystemLimit); }
        var kind = words.Count == 1 ? BinaryArenaKind598.BoyerMoore : BinaryArenaKind598.AhoCorasick;
        var arena = new BinaryProgramArena598(kind, total, words[0].ByteLength);
        try
        {
            if (kind == BinaryArenaKind598.BoyerMoore) await Bm598(arena, words[0], work);
            else await Ac598(arena, words, total, work);
            return arena;
        }
        catch (Exception original)
        {
            try { arena.FreeActualArena598(); }
            catch (Exception cleanup) { throw new AggregateException(original, cleanup); }
            throw;
        }
    }

    private static async Task Bm598(BinaryProgramArena598 arena, BitString word, BinarySearchWork work)
    {
        int m = word.ByteLength;
        arena.BeginBm598(m);
        for (int i = 0; i < m; i++)
        {
            await work.Step();
            arena.WriteNeedle598(i, word.ByteSpan[i]);
        }
        if (m == 1) return;
        for (int i = 0; i < BinaryArenaNames598.Alphabet; i++)
        {
            await work.Step(); arena.SetBad598(i, m);
        }
        for (int i = 0; i < m - 1; i++)
        {
            await work.Step(); arena.SetBad598(arena.Needle598(i), m - i - 1);
        }
        using var suffix = new BinaryScratchWords598(m);
        suffix.Write598(m - 1, m);
        int f = 0, g = m - 1;
        for (int i = m - 2; i >= 0; --i)
        {
            await work.Step();
            if (i > g && suffix.Read598(i + m - 1 - f) < i - g)
                suffix.Write598(i, suffix.Read598(i + m - 1 - f));
            else
            {
                if (i < g) g = i;
                f = i;
                while (g >= 0 && arena.Needle598(g) == arena.Needle598(g + m - 1 - f))
                {
                    await work.Step(); --g;
                }
                suffix.Write598(i, f - g);
            }
        }
        for (int i = 0; i < m; i++)
        {
            await work.Step(); arena.SetGood598(i, m);
        }
        int j = 0;
        for (int i = m - 1; i >= -1; --i)
        {
            await work.Step();
            if (i == -1 || suffix.Read598(i) == i + 1)
                while (j < m - 1 - i)
                {
                    await work.Step();
                    if (arena.Good598(j) == m) arena.SetGood598(j, m - 1 - i);
                    ++j;
                }
        }
        for (int i = 0; i <= m - 2; ++i)
        {
            await work.Step(); arena.SetGood598(m - 1 - (int)suffix.Read598(i), m - 1 - i);
        }
    }

    private static async Task Ac598(BinaryProgramArena598 arena, List<BitString> words, long total, BinarySearchWork work)
    {
        arena.BeginAc598();
        nint root = arena.AcRoot598;
        foreach (var word in words)
        {
            uint number = arena.NextPattern598();
            nint node = root;
            for (int i = 0; i < word.ByteLength; i++)
            {
                await work.Step();
                byte item = word.ByteSpan[i];
                nint child = arena.Transition598(node, item);
                if (child == 0)
                {
                    child = arena.NewAcNode598(checked((uint)i + 1), root);
                    arena.SetTransition598(node, item, child);
                }
                node = child;
            }
            if (arena.Final598(node) == 0) arena.SetFinal598(node, checked((int)number));
        }
        using var queue = new BinaryScratchWords598(total);
        long head = 0, tail = 0;
        for (int symbol = 0; symbol < BinaryArenaNames598.Alphabet; symbol++)
        {
            await work.Step();
            nint child = arena.Transition598(root, (byte)symbol);
            if (child == 0) continue;
            arena.SetFailure598(child, root);
            queue.Write598(tail++, child);
        }
        while (head < tail)
        {
            nint parent = queue.Read598(head++);
            for (int symbol = 0; symbol < BinaryArenaNames598.Alphabet; symbol++)
            {
                await work.Step();
                byte item = (byte)symbol;
                nint child = arena.Transition598(parent, item);
                if (child == 0) continue;
                queue.Write598(tail++, child);
                nint r = arena.Failure598(parent);
                while (r != 0 && arena.Transition598(r, item) == 0)
                {
                    await work.Step(); r = arena.Failure598(r);
                }
                if (r == 0)
                {
                    nint next = arena.Transition598(root, item);
                    arena.SetFailure598(child, next == 0 ? root : next);
                }
                else
                {
                    nint failure = arena.Transition598(r, item);
                    arena.SetFailure598(child, failure);
                    if (arena.Final598(child) == 0 && arena.Final598(failure) != 0) arena.SetFinal598(child, -1);
                }
            }
        }
        arena.SetFailure598(root, root);
    }
}
