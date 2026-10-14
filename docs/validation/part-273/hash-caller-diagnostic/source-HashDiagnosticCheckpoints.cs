namespace Erlang;
public static class HashDiagnosticCheckpoints
{
    private struct Row
    {
        public int Phase, Stage, Thread, Gen0, Gen1, Gen2;
        public long Allocated;
    }
    private static readonly Row[] rows = new Row[128];
    private static int count, phase, owner;
    public static void Begin(int selectedPhase)
    {
        owner = Environment.CurrentManagedThreadId;
        phase = selectedPhase;
    }
    public static void End() => phase = 0;
    public static void Mark(int stage)
    {
        int selectedPhase = phase;
        int thread = Environment.CurrentManagedThreadId;
        if (selectedPhase == 0 || thread != owner) return;
        int index = count;
        if (index >= rows.Length) throw new InvalidOperationException("Diagnostic checkpoint capacity exceeded");
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
        rows[index] = new Row { Phase = selectedPhase, Stage = stage, Thread = thread, Allocated = allocated, Gen0 = gen0, Gen1 = gen1, Gen2 = gen2 };
        count = index + 1;
    }
    public static object[] Snapshot()
    {
        var output = new object[count];
        for (int index = 0; index < count; index++)
        {
            Row value = rows[index];
            output[index] = new { value.Phase, value.Stage, value.Thread, value.Allocated, value.Gen0, value.Gen1, value.Gen2 };
        }
        return output;
    }
}
