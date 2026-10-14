from pathlib import Path
import shutil,json,hashlib,xml.etree.ElementTree as E
r=Path(__file__).resolve().parents[1];a=r/'artifacts';old=a/'HashPrefixAllocationDiagnostic';d=a/'HashCorpusAllocationDiagnostic';d.mkdir(exist_ok=True)
for name in ['ErlangHashHostChecks.cs','HashPrefixDiagnosticMetadata.cs']:shutil.copyfile(old/name,d/name)
shutil.copyfile(r/'tests/Erlang.Tests/Program.cs',d/'Program.cs')
p=d/'HashPrefixDiagnosticMetadata.cs';s=p.read_text()
s=s.replace('    public static readonly List<object> Rows = new();','''    public static readonly List<object> Rows = new();
    static HashPrefixDiagnosticMetadata()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => File.WriteAllText(Environment.GetEnvironmentVariable("ERLANG_HASH_DIAGNOSTIC_REPORT") ?? throw new InvalidOperationException("Missing diagnostic report path"), System.Text.Json.JsonSerializer.Serialize(new { Measurements = Rows, FullValidateExecuted = false, NativeExecuted = false }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }''')
p.write_bytes(s.encode())
t=E.parse(old/'HashPrefixAllocationDiagnostic.csproj');t.write(d/'HashCorpusAllocationDiagnostic.csproj',encoding='utf-8')
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
freeze=json.loads((a/'hash-prefix-diagnostic-source-freeze.json').read_bytes())
assert all(h(Path(x['Path']))==x['Sha256'] for x in freeze['LinkedActualSourceAndDllBindings'])
freeze['Status']='ENTIRE_CURRENT_MAIN_PROGRAM_SOURCE_PREBUILD_ONLY';freeze['ProgramByteExact']=h(d/'Program.cs')==h(r/'tests/Erlang.Tests/Program.cs')
freeze['ExpectedOutcomes']=6526;freeze['ProgramExecutionLoopAndTimeoutsUnchanged']=True
freeze['OriginalMeasurementIntervalUnchanged']=True
freeze['ChangedHelperMetadataOnly']='ProcessExit diagnostic serialization after original Main report; no Program source edits'
freeze['Files']=[{'Path':p.name,'Sha256':h(p)} for p in sorted(d.iterdir()) if p.is_file()]
(a/'hash-corpus-diagnostic-source-freeze.json').write_bytes((json.dumps(freeze,indent=2)+'\n').encode())
print('Entire Main Program byteexact',freeze['ProgramByteExact'])
