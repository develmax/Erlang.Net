namespace Erlang;

internal sealed class ProcessTermSlab506(int words)
{
    internal Term?[] Cells { get; } = new Term?[words];
    private readonly bool[] used = new bool[words];

    internal int Reserve(int length)
    {
        int run = 0;
        for (int index = 0; index < used.Length; index++)
        {
            run = used[index] ? 0 : run + 1;
            if (run != length)
                continue;
            int offset = index + 1 - length;
            Array.Fill(
                used,
                true,
                offset,
                length
            );

            return offset;
        }

        return -1;
    }

    internal void Release(int offset, int length)
    {
        Array.Clear(Cells, offset, length);
        Array.Fill(
            used,
            false,
            offset,
            length
        );
    }
}
