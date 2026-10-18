namespace Erlang;

internal static class QueueHigherHistory
{
    internal static void Add(List<QueueHigherPending> operations, Term value, QueueHigherEmission kind, int line)
    {
        int run = operations.Count != 0 && operations[^1].ReturnLine == line ? operations[^1].RunLength + 1 : 1;
        operations.Add(new(value, kind, line, run));
    }

    internal static QueueHigherFrame[] Read(List<QueueHigherPending> operations, string module, string helper)
    {
        var frames = new List<QueueHigherFrame>();
        int index = operations.Count - 1;
        while (index >= 0 && frames.Count < BacktraceDepthParameters.MaximumDepth - 1)
        {
            QueueHigherPending item = operations[index];
            frames.Add(new(module, helper, 2, item.ReturnLine));
            index -= item.RunLength;
        }

        return frames.ToArray();
    }
}
