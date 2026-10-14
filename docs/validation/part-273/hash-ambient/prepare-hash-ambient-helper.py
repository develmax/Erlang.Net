from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';p=a/'HashAmbientActualMainFocus';p.mkdir(exist_ok=True);closure=a/'part273-hash-failure-preserved/actual-main-test-closure'
refs=''.join(f'<Reference Include="{n}"><HintPath>{closure/n}.dll</HintPath></Reference>' for n in ['Erlang.Runtime','Erlang.Terms','Erlang.Serialization','Erlang.Otp.Behaviours'])
(p/'HashAmbientActualMainFocus.csproj').write_bytes(('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup>'+refs+'</ItemGroup></Project>\n').encode())
s='''using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Erlang;

string closure = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) => File.Exists(Path.Combine(closure, name.Name + ".dll")) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(closure, name.Name + ".dll")) : null;
Assembly tests = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(closure, "Erlang.Tests.dll"));
MethodInfo original = tests.GetType("Erlang.Tests.ErlangHashHostChecks", true)!.GetMethod("Resources", BindingFlags.Public | BindingFlags.Static)!;
var rows = new List<object>();
foreach (int count in new[] { 0, 16, 32, 64, 128, 256 })
{
    var ambient = Enumerable.Range(0, count).Select(_ => new AsyncLocal<object>()).ToArray();
    foreach (var local in ambient) local.Value = new object();
    object beforeOriginal = ContextSnapshot();
    string? originalFailure = null;
    try { await (Task)original.Invoke(null, null)!; }
    catch (Exception exception) { originalFailure = exception.ToString(); }
    object diagnostic = await Measure();
    rows.Add(new { AmbientCount = count, ExactOriginalResourceFailure = originalFailure, CallerContextBeforeOriginal = beforeOriginal, Diagnostic = diagnostic });
    foreach (var local in ambient) local.Value = null;
}
Console.WriteLine(JsonSerializer.Serialize(new { ActualMainTestsMvid = tests.ManifestModule.ModuleVersionId, Rows = rows, NativeExecuted = false, FullValidationRerun = false, Budget = 4096, OriginalWarmupCalls = 1, NoMinimumSelection = true }, new JsonSerializerOptions { WriteIndented = true }));

static object ContextSnapshot()
{
    object? context = typeof(Thread).GetField("_executionContext", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(Thread.CurrentThread);
    object? map = context?.GetType().GetField("_localValues", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(context);
    var entries = new List<object>();
    void Entry(object? pair)
    {
        if (pair is null) return;
        object? key = pair.GetType().GetProperty("Key")?.GetValue(pair);
        object? value = pair.GetType().GetProperty("Value")?.GetValue(pair);
        if (key is not null) entries.Add(new { KeyType = key.GetType().FullName, ValueType = value?.GetType().FullName });
    }
    if (map is IDictionary dictionary)
        foreach (DictionaryEntry entry in dictionary) entries.Add(new { KeyType = entry.Key.GetType().FullName, ValueType = entry.Value?.GetType().FullName });
    else if (map is not null)
        foreach (FieldInfo field in map.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            object? value = field.GetValue(map);
            if (value is Array array) foreach (object? pair in array) Entry(pair);
            else Entry(value);
        }
    return new { Thread = Environment.CurrentManagedThreadId, ContextType = context?.GetType().FullName, MapType = map?.GetType().FullName, Count = entries.Count, Entries = entries, AmbientValuesPrinted = false };
}

static async Task<object> Measure()
{
    await using var runtime = new ProcessRuntime();
    object? row = null;
    var process = runtime.Spawn(async context =>
    {
        var binary = new BitString(new byte[4 * 1024 * 1024]);
        await runtime.Modules.Call(context, ErlangModuleNames.Module, "phash2", binary, Term.I(4294967296));
        object capturedBefore = ContextSnapshot();
        int startingThread = Environment.CurrentManagedThreadId;
        long before = GC.GetAllocatedBytesForCurrentThread();
        ValueTask<Term> pending = runtime.Modules.Call(context, ErlangModuleNames.Module, "phash2", binary, Term.I(4294967296));
        bool synchronous = pending.IsCompletedSuccessfully;
        if (!synchronous) throw new InvalidOperationException("Original measurement must complete synchronously");
        Term result = pending.GetAwaiter().GetResult();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        int endingThread = Environment.CurrentManagedThreadId;
        object capturedAfter = ContextSnapshot();
        row = new { Bytes = allocated, Budget = 4096, Result = result.GetType().Name, StartingThread = startingThread, EndingThread = endingThread, Synchronous = synchronous, CapturedBefore = capturedBefore, CapturedAfter = capturedAfter };
        return Nil.Value;
    });
    Term reason = await process.Completion;
    return new { Reason = reason.ToString(), Measurement = row };
}
''';(p/'Program.cs').write_bytes(s.encode());sha=lambda b:hashlib.sha256(b).hexdigest().upper();x={'Stage':'BeforeHelperTargetExecution','ActualMainTestClosure':str(closure),'CapturedFailedFullReceiptSha256':sha((a/'part273-hash-failure-preserved/receipt.json').read_bytes()),'Files':[{'Path':n,'Sha256':sha((p/n).read_bytes())} for n in ['Program.cs','HashAmbientActualMainFocus.csproj']],'AmbientCounts':[0,16,32,64,128,256],'MainChanges':False,'NativeExecuted':False};(a/'part273-hash-ambient-helper-source-freeze.json').write_bytes((json.dumps(x,indent=2)+'\n').encode());print('Prepared actual Main helper frozen')
