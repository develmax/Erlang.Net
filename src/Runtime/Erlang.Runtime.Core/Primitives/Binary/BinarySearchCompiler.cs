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
        var nodes = new List<BinarySearchBuilderNode> { new() };
        foreach (BitString word in words)
        {
            int current = 0;
            for (int i = 0; i < word.ByteLength; i++)
            {
                await work.Step();
                byte value = word.ByteSpan[i];
                if (!nodes[current].Edges.TryGetValue(value, out int next))
                {
                    next = nodes.Count;
                    nodes[current].Edges.Add(value, next);
                    nodes.Add(new());
                }
                current = next;
            }
            nodes[current].Terminal = true;
        }
        var frozen = new BinarySearchNode[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
        {
            await work.Step();
            frozen[i] = new(nodes[i]);
        }
        await context.ChargeAsync(0);

        return new(words.Count == 1 ? BinarySearchNames.SinglePattern : BinarySearchNames.MultiplePatterns, frozen);
    }

    private static BitString Require(BitString value)
    {
        if (!value.IsBinary || value.BitLength == 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return value;
    }
}
