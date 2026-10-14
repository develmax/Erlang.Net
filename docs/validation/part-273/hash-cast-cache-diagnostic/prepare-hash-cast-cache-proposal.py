from pathlib import Path
import shutil,json,hashlib
r=Path(__file__).resolve().parents[1];a=r/'artifacts';old=a/'hash-caller-checkpoint-source';d=a/'hash-cast-cache-checkpoint-source';d.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
for p in old.iterdir():
 if p.is_file():shutil.copyfile(p,d/p.name)
p=d/'HashDiagnosticCheckpoints.cs';s=p.read_text()
s=s.replace('        public long Allocated;','        public long Allocated;\n        public int[]? CastTable;\n        public int CastTableLength;')
s=s.replace('    private static readonly Row[] rows = new Row[128];','''    private static readonly Row[] rows = new Row[128];
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
    }''')
s=s.replace('        long allocated = GC.GetAllocatedBytesForCurrentThread();','        int[]? table = readCastTable();\n        int tableLength = table?.Length ?? 0;\n        long allocated = GC.GetAllocatedBytesForCurrentThread();')
s=s.replace('Gen2 = gen2 };','Gen2 = gen2, CastTable = table, CastTableLength = tableLength };')
s=s.replace('        var output = new object[count];','        var output = new object[count];\n        var tableIdentities = new List<int[]?>();')
s=s.replace('            output[index] = new { value.Phase, value.Stage, value.Thread, value.Allocated, value.Gen0, value.Gen1, value.Gen2 };','''            int tableId = tableIdentities.FindIndex(table => ReferenceEquals(table, value.CastTable));
            if (tableId < 0) { tableId = tableIdentities.Count; tableIdentities.Add(value.CastTable); }
            bool tableChanged = index > 0 && !ReferenceEquals(rows[index - 1].CastTable, value.CastTable);
            output[index] = new { value.Phase, value.Stage, value.Thread, value.Allocated, value.Gen0, value.Gen1, value.Gen2, CastTableId = tableId, value.CastTableLength, CastTableChangedSincePrevious = tableChanged };''')
p.write_bytes(s.encode())
ledger={'Status':'SOURCE_ONLY_NONMUTATING_CACHE_GETTER_PROPOSAL','MainModified':False,'BuildExecuted':False,'NativeExecuted':False,'CacheMutated':False,'Getter':'Reflection.Emit DynamicMethod direct ldsfld,ret; field metadata and delegate construction/JIT before original warmup','Sampling':'preallocated struct rows keep table reference and length; ordinal identity comparison and JSON allocated after measured interval','NativeAddressesPrinted':False,'TableContentsPrinted':False,'OriginalWarmupCalls':1,'OriginalBudget':4096,'SourceJitShapeCanPerturbCacheState':True,'ActualObservedTableReplacementNotClaimed':True,'Sources':[{'Url':'https://raw.githubusercontent.com/dotnet/runtime/v10.0.11/src/coreclr/vm/castcache.cpp','RelevantLines':'15-36,70-87','Finding':'CreateCastCache uses managed int array sized (bucketCount+1)*sizeof(entry)/4; replacement publishes new table reference'},{'Url':'https://raw.githubusercontent.com/dotnet/runtime/v10.0.11/src/coreclr/vm/castcache.h','RelevantLines':'50-68,171-176,227-240','Finding':'DWORD then two pointer-sized fields; x64 alignment gives24-byte entries. Growth doubles buckets, x64 source documents98304 bytes for4096 entries.'},{'Url':'https://raw.githubusercontent.com/dotnet/runtime/v10.0.11/src/coreclr/System.Private.CoreLib/src/System/Runtime/CompilerServices/CastHelpers.cs','RelevantLines':'12-13,404-456','Finding':'StelemRef nonexact element type uses static cast table and cache-miss type-test helper'}],'X64Cache512SizeInference':{'PayloadBytes':513*24,'IntArrayLength':513*24//4,'ArrayHeaderBytes':24,'TotalBytes':513*24+24,'ExactMeasuredStoreWindowExcess':12336,'InferenceOnly':True},'Files':[{'Path':p.name,'Sha256':sha(p.read_bytes())} for p in sorted(d.iterdir()) if p.is_file()]}
(a/'hash-cast-cache-checkpoint-proposed-ledger.json').write_bytes((json.dumps(ledger,indent=2)+'\n').encode())
print('Nonmutating cache getter source proposal prepared; no build/run.')
