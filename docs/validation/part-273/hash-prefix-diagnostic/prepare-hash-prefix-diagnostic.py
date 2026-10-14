from pathlib import Path
import hashlib, json, shutil, xml.etree.ElementTree as ET
r=Path(__file__).resolve().parents[1]; a=r/'artifacts'; d=a/'HashPrefixAllocationDiagnostic'
d.mkdir(exist_ok=True)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
program=(r/'tests/Erlang.Tests/Program.cs').read_bytes()
text=program.decode('utf-8-sig'); anchor='Test("erlang/hash-iterative-deep-terms-and-no-binary-payload-copy", Erlang.Tests.ErlangHashHostChecks.Resources);'
assert text.count(anchor)==1
prefix=text[:text.index(anchor)+len(anchor)]
suffix='''
var outcomes = new List<object>();
foreach (var test in tests)
{
    string? error = null;
    try { await test.Body(); } catch (Exception exception) { error = exception.ToString(); }
    outcomes.Add(new { test.Name, Error = error });
    Console.WriteLine(JsonSerializer.Serialize(new { test.Name, Error = error }));
}
File.WriteAllText(args[0], JsonSerializer.Serialize(new { PrefixOutcomes = outcomes, Measurements = Erlang.Tests.HashPrefixDiagnosticMetadata.Rows, FullValidation = false, NativeExecuted = false }, new JsonSerializerOptions { WriteIndented = true }));
void Test(string name, Func<Task> body) => tests.Add((name, body));
'''
(d/'Program.cs').write_bytes((prefix+suffix).encode())
source=(r/'tests/Erlang.Tests/ErlangHashHostChecks.cs').read_bytes(); s=source.decode('utf-8-sig')
before='                int startingThread = Environment.CurrentManagedThreadId;'
after='                int endingThread = Environment.CurrentManagedThreadId;'
assert s.count(before)==s.count(after)==1
s=s.replace(before,'                object diagnosticBefore = HashPrefixDiagnosticMetadata.Capture();\n'+before)
s=s.replace(after,after+'\n                object diagnosticAfter = HashPrefixDiagnosticMetadata.Capture();\n                HashPrefixDiagnosticMetadata.Rows.Add(new { allocated, synchronous, startingThread, endingThread, ResultType = result.GetType().Name, Before = diagnosticBefore, After = diagnosticAfter });')
(d/'ErlangHashHostChecks.cs').write_bytes(s.encode())
# Metadata allocation occurs strictly outside the original allocation interval.
metadata='''using System.Reflection;
namespace Erlang.Tests;
internal static class HashPrefixDiagnosticMetadata
{
    public static readonly List<object> Rows = new();
    public static object Capture()
    {
        int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
        object? context = typeof(Thread).GetField("_executionContext", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(Thread.CurrentThread);
        FieldInfo? field = context?.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).SingleOrDefault(item => item.FieldType.Name.Contains("AsyncLocalValueMap", StringComparison.Ordinal));
        if (context is not null && field is null) throw new InvalidOperationException("ExecutionContext local-values map field unresolved");
        object? map = field?.GetValue(context);
        var types = new List<object>();
        void Pair(object? pair)
        {
            if (pair is null) return;
            object? key = pair.GetType().GetProperty("Key")?.GetValue(pair);
            object? value = pair.GetType().GetProperty("Value")?.GetValue(pair);
            if (key is not null) types.Add(new { KeyType = key.GetType().FullName, ValueType = value?.GetType().FullName });
        }
        if (map is System.Collections.IDictionary dictionary)
            foreach (System.Collections.DictionaryEntry item in dictionary) types.Add(new { KeyType = item.Key.GetType().FullName, ValueType = item.Value?.GetType().FullName });
        else if (map is not null)
            foreach (var item in map.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                object? value = item.GetValue(map);
                if (value is Array array) foreach (object? pair in array) Pair(pair); else Pair(value);
            }
        return new { Gen0 = gen0, Gen1 = gen1, Gen2 = gen2, Thread = Environment.CurrentManagedThreadId, MapType = map?.GetType().FullName, LocalValuesField = field?.Name, Count = types.Count, Entries = types, AmbientValuesPrinted = false };
    }
}
'''
(d/'HashPrefixDiagnosticMetadata.cs').write_bytes(metadata.encode())
root=ET.Element('Project',Sdk='Microsoft.NET.Sdk'); props=ET.SubElement(root,'PropertyGroup')
for key,value in {'OutputType':'Exe','AssemblyName':'Erlang.Tests','EnableDefaultCompileItems':'false'}.items():ET.SubElement(props,key).text=value
items=ET.SubElement(root,'ItemGroup')
rows=[]
for p in sorted((r/'tests/Erlang.Tests').glob('*.cs')):
 if p.name not in ['Program.cs','ErlangHashHostChecks.cs']:
  ET.SubElement(items,'Compile',Include=str(p),Link=p.name); rows.append({'Path':str(p),'Sha256':sha(p.read_bytes())})
for name in ['Program.cs','ErlangHashHostChecks.cs','HashPrefixDiagnosticMetadata.cs']: ET.SubElement(items,'Compile',Include=name)
closure=r/'tests/Erlang.Tests/bin/Debug/net10.0'
for p in sorted(closure.glob('*.dll')):
 if p.name=='Erlang.Tests.dll': continue
 ref=ET.SubElement(items,'Reference',Include=p.stem);ET.SubElement(ref,'HintPath').text=str(p)
 # Bind actual DLL bytes; no project builds or prototype runtime sources.
 rows.append({'Path':str(p),'Sha256':sha(p.read_bytes())})
original=ET.fromstring((r/'tests/Erlang.Tests/Erlang.Tests.csproj').read_text())
for resource in original.iter('EmbeddedResource'):
 include=resource.get('Include'); assert include
 ET.SubElement(items,'EmbeddedResource',Include=str(r/'tests/Erlang.Tests'/include),Link=include)
ET.indent(root);(d/'HashPrefixAllocationDiagnostic.csproj').write_bytes(ET.tostring(root,encoding='utf-8')+b'\n')
ledger={'Status':'SOURCE_PREPARED_NOT_EXECUTED','MainProgramSha256':sha(program),'OriginalHashCheckSha256':sha(source),'PrefixThroughOriginalHashRegistrationExact':True,'PrefixRegistrations':prefix.count('Test('),'OriginalMeasurementIntervalUnchanged':True,'ExtraWarmupCalls':0,'BudgetUnchanged':True,'GcCountersOutsideMeasurement':True,'ContextMetadataOutsideMeasurement':True,'DoesNotProveHistoricalPaddingCause':True,'LinkedActualSourceAndDllBindings':rows,'Files':[{'Path':p.name,'Sha256':sha(p.read_bytes())} for p in sorted(d.iterdir()) if p.is_file() and p.suffix in ['.cs','.csproj']]}
(a/'hash-prefix-diagnostic-source-freeze.json').write_bytes((json.dumps(ledger,indent=2)+'\n').encode())
print('Prepared only; Root must confirm actual DLL bindings before build/run.')
