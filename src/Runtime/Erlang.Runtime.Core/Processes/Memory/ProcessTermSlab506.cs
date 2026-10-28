namespace Erlang;

internal sealed class ProcessTermSlab506(int words) : IDisposable
{
    private readonly bool[] used = new bool[words];
    private readonly Dictionary<int, int> candidates519 = new();
    private int firstFree519;
    private int freeWords519 = words;
    internal NativeTermCellBuffer526 Cells { get; } = new(words);

    internal long ReservationCellVisits519 { get; private set; }

    internal int Reserve(int length)
    {
        ObjectDisposedException.ThrowIf(Cells.Disposed526, this);
        if (freeWords519 < length)
            return -1;

        int start = candidates519.TryGetValue(length, out int saved)
            ? Math.Max(firstFree519, saved)
            : firstFree519;
        int run = 0;
        for (int index = start; index < used.Length; index++)
        {
            unchecked
            {
                ReservationCellVisits519++;
            }
            run = used[index] ? 0 : run + 1;
            if (run != length)
                continue;

            int offset = index + 1 - length;
            // Dictionary allocation must finish before committing the bitmap.
            // Prior positions cannot acquire a fit until Release clears hints.
            candidates519[length] = index + 1;
            Array.Fill(
                used,
                true,
                offset,
                length
            );
            freeWords519 -= length;
            if (offset == firstFree519)
            {
                firstFree519 = index + 1;
                while (firstFree519 < used.Length)
                {
                    unchecked
                    {
                        ReservationCellVisits519++;
                    }
                    if (!used[firstFree519])
                        break;
                    firstFree519++;
                }
            }

            return offset;
        }

        candidates519[length] = used.Length;

        return -1;
    }

    internal void Release(int offset, int length)
    {
        Cells.Clear526(offset, length);
        Array.Fill(
            used,
            false,
            offset,
            length
        );
        freeWords519 += length;
        firstFree519 = Math.Min(firstFree519, offset);
        // A release can bridge holes for every length, not only this length.
        candidates519.Clear();
    }

    public void Dispose() => Cells.Dispose();
}
