"""Artifact-only proposed instrumentation; does not build, run, or change Main."""
from pathlib import Path
import json,hashlib,shutil
r=Path(__file__).resolve().parents[1];a=r/'artifacts';d=a/'hash-checkpoint-diagnostic-source';d.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
rows=[]
def save(path,transform):
 raw=(r/path).read_bytes();old=raw.decode('utf-8-sig');new=transform(old)
 p=d/Path(path).name;p.write_bytes(new.encode())
 rows.append({'MainPath':path,'BeforeRawSha256':sha(raw),'CandidatePath':str(p.relative_to(a)).replace('\\','/'),'CandidateSha256':sha(p.read_bytes())})
def module(s):
 start=s.index('    public ValueTask<Term> Call(');end=s.index('    private static async ValueTask<Term> AwaitHaltBoundary',start)
 before=s[start:end];part=before
 part=part.replace('        context.Runtime.ThrowIfHalted();','        HashDiagnosticCheckpoints.Mark(10);\n        context.Runtime.ThrowIfHalted();\n        HashDiagnosticCheckpoints.Mark(11);',1)
 part=part.replace('        if (body is null)\n','        HashDiagnosticCheckpoints.Mark(12);\n        if (body is null)\n',1)
 # Native image-null branch only. Exact Called -> SavedCalls -> body -> Complete -> Guard order retained.
 part=part.replace('            invocation = context.Runtime.ProcessTracing.Called(','            HashDiagnosticCheckpoints.Mark(13);\n            invocation = context.Runtime.ProcessTracing.Called(',1)
 part=part.replace('            context.SavedCalls.Record(module, function, arguments.Length);','            HashDiagnosticCheckpoints.Mark(14);\n            context.SavedCalls.Record(module, function, arguments.Length);\n            HashDiagnosticCheckpoints.Mark(15);',1)
 old='                return GuardHaltBoundary(context.Runtime.ProcessTracing.Complete(context, invocation, body(context, arguments)), context);'
 assert part.count(old)==1
 part=part.replace(old,'''                HashDiagnosticCheckpoints.Mark(16);
                ValueTask<Term> diagnosticBody = body(context, arguments);
                HashDiagnosticCheckpoints.Mark(17);
                ValueTask<Term> diagnosticCompletion = context.Runtime.ProcessTracing.Complete(context, invocation, diagnosticBody);
                HashDiagnosticCheckpoints.Mark(18);
                ValueTask<Term> diagnosticBoundary = GuardHaltBoundary(diagnosticCompletion, context);
                HashDiagnosticCheckpoints.Mark(19);
                return diagnosticBoundary;''')
 # Guard records contain no allocations; normal source/image branch otherwise untouched.
 part=part.replace('    {\n        if (pending.IsCompletedSuccessfully)','    {\n        HashDiagnosticCheckpoints.Mark(20);\n        if (pending.IsCompletedSuccessfully)',1)
 # Current files use CRLF; normalize copy first in caller below.
 return s[:start]+part+s[end:]
def hashes(s):
 old='            return ValueTask.FromResult<Term>(Term.I((long)(ErlangNativeHash.Portable2(arguments[0]) % range)));'
 assert s.count(old)==1
 return s.replace(old,'''            HashDiagnosticCheckpoints.Mark(30);
            uint diagnosticHash = ErlangNativeHash.Portable2(arguments[0]);
            HashDiagnosticCheckpoints.Mark(31);
            ValueTask<Term> diagnosticResult = ValueTask.FromResult<Term>(Term.I((long)(diagnosticHash % range)));
            HashDiagnosticCheckpoints.Mark(32);
            return diagnosticResult;''')
save('src/Erlang.Runtime/ModuleRegistry.cs',lambda s:module(s.replace('\r\n','\n')))
save('src/Erlang.Runtime/ErlangHashRegistrations.cs',lambda s:hashes(s.replace('\r\n','\n')))
collector='''namespace Erlang;
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
'''
(d/'HashDiagnosticCheckpoints.cs').write_bytes(collector.encode())
def host(s):
 s=s.replace('\r\n','\n')
 anchor='                await runtime.Modules.Call('
 assert s.count(anchor)>=1
 s=s.replace(anchor,'                HashDiagnosticCheckpoints.Begin(1);\n                HashDiagnosticCheckpoints.Mark(1);\n'+anchor,1)
 anchor='                int startingThread = Environment.CurrentManagedThreadId;'
 assert s.count(anchor)==1
 s=s.replace(anchor,'                HashDiagnosticCheckpoints.Mark(2);\n                HashDiagnosticCheckpoints.End();\n                HashDiagnosticCheckpoints.Begin(2);\n'+anchor)
 s=s.replace('                long before = GC.GetAllocatedBytesForCurrentThread();','                long before = GC.GetAllocatedBytesForCurrentThread();\n                HashDiagnosticCheckpoints.Mark(3);')
 s=s.replace('                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;','                HashDiagnosticCheckpoints.Mark(4);\n                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;\n                HashDiagnosticCheckpoints.End();')
 return s
save('tests/Erlang.Tests/ErlangHashHostChecks.cs',host)
program=(r/'tests/Erlang.Tests/Program.cs').read_bytes();text=program.decode('utf-8-sig').replace('\r\n','\n')
# Preserve all registrations; stop only execution after the original hash fixture.
anchor='Console.WriteLine($"{tests.Count - failed}/{tests.Count} passed");'
assert text.count(anchor)==1
pos=text.index(anchor);prefix=text[:pos];closing='    }\n}\n';assert prefix.endswith(closing)
prefix=prefix[:-len(closing)]+'    }\n    if (test.Name == "erlang/hash-iterative-deep-terms-and-no-binary-payload-copy") break;\n}\n'
suffix='''Console.WriteLine($"{results.Count - failed}/{results.Count} executed; {tests.Count} registered");
await File.WriteAllTextAsync(args[0], JsonSerializer.Serialize(new { Registered = tests.Count, Executed = results.Count, Failed = failed, Tests = results, Checkpoints = HashDiagnosticCheckpoints.Snapshot(), NativeExecuted = false, FullValidateExecuted = false }, new JsonSerializerOptions { WriteIndented = true }));
return failed == 0 ? 0 : 1;
'''
(d/'Program.cs').write_bytes((prefix+suffix).encode())
ledger={'Status':'PROPOSED_SOURCE_ONLY_AWAITING_ROOT_REVIEW_AND_HEAVY_GRANT','AllMain6526RegistrationsPreserved':True,'ExecutionStopsAfterOriginalHashFixture':True,'ExpectedExecutedPrefix':60,'FullValidation':False,'BudgetUnchanged':4096,'WarmupCallsUnchanged':1,'HashPrimitiveAndArithmeticUnchanged':True,'NoAllocatingMetadataInsideMeasuredInterval':True,'CollectorArrayAllocatedBeforeWarmup':128,'FilterActualMeasuredThreadOnly':True,'Phases':{'1':'original warmup','2':'original measured call'},'StageNames':{'1':'warmup caller before','2':'warmup caller after','3':'caller after first allocation counter','4':'caller before second allocation counter','10':'Call entry','11':'after halt precheck','12':'after function/image lookup','13':'before Called','14':'after Called','15':'after SavedCalls','16':'before delegate','17':'after delegate','18':'after Complete','19':'after GuardHaltBoundary','20':'GuardHaltBoundary entry','30':'after hash range validation/before Portable2','31':'after Portable2','32':'after Term.I + ValueTask.FromResult'},'IsolatedRuntimeAssemblyRequired':True,'NoMainSourceEdits':True,'NoBuildExecuted':True,'Changes':rows,'MainProgramRawSha256':sha(program),'DiagnosticOnlyFiles':[{'Path':p.name,'Sha256':sha(p.read_bytes())} for p in sorted(d.iterdir()) if p.is_file()]}
(a/'hash-checkpoint-diagnostic-proposed-ledger.json').write_bytes((json.dumps(ledger,indent=2)+'\n').encode())
print('Source-only proposal prepared; do not build until Root reviews.')
