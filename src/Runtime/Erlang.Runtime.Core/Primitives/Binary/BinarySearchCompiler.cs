namespace Erlang;

internal static class BinarySearchCompiler
{
    internal static async ValueTask<BinarySearchPattern> Compile(ProcessContext context, Term input)
    {
        await context.ChargeAsync(0);
        var work = new BinarySearchWork(context);
        var words = new List<BitString>();
        if (input is BitString binary)
            words.Add(Require(binary));
        else if (input is Cons)
        {
            Term tail = input;
            while (tail is Cons item)
            {
                await work.Step();
                if (item.Head is not BitString word)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                words.Add(Require(word));
                tail = item.Tail;
            }
            if (tail is not Nil)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        else
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (words.Count == 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BinaryProgramOwner598 owner = context.Runtime.BinaryPrograms598(context);
        using BinaryProgramOperation598 operation = owner.EnterBuild598();
        BinaryProgramArena598 arena = await BinaryArenaCompiler598.Build598(words, work);
        BinaryProgramBacking598 backing;
        try
        {
            backing = owner.Register598(arena);
        }
        catch (Exception original)
        {
            try
            {
                arena.FreeActualArena598();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
        try
        {
            return new BinarySearchPattern(backing);
        }
        catch
        {
            backing.ReleaseProducer598();
            throw;
        }
    }

    private static BitString Require(BitString value)
    {
        if (!value.IsBinary || value.BitLength == 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return value;
    }
}
