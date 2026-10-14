namespace Erlang;
public static class HashDiagnosticCheckpoints
{
    private struct Row
    {
        public int Phase, Stage, Thread, Gen0, Gen1, Gen2;
        public long Allocated;
        public int[]? CastTable;
        public int CastTableLength;
    }
    private static readonly Row[] rows = new Row[128];
    private static readonly Func<int[]> readCastTable = CreateCastTableGetter();
    private static Func<int[]> CreateCastTableGetter()
    {
        Type type = typeof(object).Assembly.GetType("System.Runtime.CompilerServices.CastHelpers", throwOnError: true)!;
        var field = type.GetField("s_table", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CoreLib CastHelpers.s_table not found");
        if (field.FieldType != typeof(int[])) throw new InvalidOperationException("Unexpected CastHelpers.s_table type");
        var method = new System.Reflection.Emit.DynamicMethod("ReadCurrentCastTable", typeof(int[]), Type.EmptyTypes, typeof(HashDiagnosticCheckpoints).Module, skipVisibility: true);
        var il = method.GetILGenerator();
        il.Emit(System.Reflection.Emit.OpCodes.Ldsfld, field);
        il.Emit(System.Reflection.Emit.OpCodes.Ret);
        var getter = (Func<int[]>)method.CreateDelegate(typeof(Func<int[]>));
        _ = getter(); // Prepare dynamic method before original warmup and measurement.
        return getter;
    }
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
        int[]? table = readCastTable();
        int tableLength = table?.Length ?? 0;
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
        rows[index] = new Row { Phase = selectedPhase, Stage = stage, Thread = thread, Allocated = allocated, Gen0 = gen0, Gen1 = gen1, Gen2 = gen2, CastTable = table, CastTableLength = tableLength };
        count = index + 1;
    }
    public static object[] Snapshot()
    {
        var output = new object[count];
        var tableIdentities = new List<int[]?>();
        for (int index = 0; index < count; index++)
        {
            Row value = rows[index];
            int tableId = tableIdentities.FindIndex(table => ReferenceEquals(table, value.CastTable));
            if (tableId < 0) { tableId = tableIdentities.Count; tableIdentities.Add(value.CastTable); }
            bool tableChanged = index > 0 && !ReferenceEquals(rows[index - 1].CastTable, value.CastTable);
            output[index] = new { value.Phase, value.Stage, value.Thread, value.Allocated, value.Gen0, value.Gen1, value.Gen2, CastTableId = tableId, value.CastTableLength, CastTableChangedSincePrevious = tableChanged };
        }
        return output;
    }
}
