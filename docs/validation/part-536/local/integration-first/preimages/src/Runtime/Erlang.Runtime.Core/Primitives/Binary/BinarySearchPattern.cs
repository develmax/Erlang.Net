namespace Erlang;

internal sealed class BinarySearchPattern
{
    private readonly BinarySearchNode[] nodes;
    internal string Tag { get; }

    internal BinarySearchPattern(string tag, BinarySearchNode[] nodes)
    {
        Tag = tag;
        this.nodes = (BinarySearchNode[])nodes.Clone();
    }

    internal async ValueTask<int> At(
        BitString subject,
        int position,
        int end,
        BinarySearchWork work
    )
    {
        int index = 0;
        int longest = 0;
        for (int cursor = position; cursor < end; cursor++)
        {
            await work.Step();
            if (!nodes[index].Next(subject.ByteSpan[cursor], out index))
                break;
            if (nodes[index].Terminal)
                longest = cursor - position + 1;
        }

        return longest;
    }
}
