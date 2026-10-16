from pathlib import Path
import hashlib
import json
import subprocess

r = Path.cwd()
c = r / 'artifacts/proc-lib338-candidate'
path = 'src/Erlang.Otp.Behaviours/ProcLibCrashReportBuilder.cs'
raw = (c / path).read_bytes()
text = raw.decode()
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
baseline = subprocess.check_output(['git', 'show', 'add206c03c801cbd68c51d23dbbdb8e36f2d581f:src/Erlang.Runtime/ProcessInfoDefaultCatalog.cs'])
assert b'MissingPhysicalKeys' in baseline
for key in ['HeapSize', 'StackSize']:
    assert ('ProcessInfoResourceNames.' + key).encode() in baseline
assert 'query = Cons.From(requested.Where(key => !UnavailablePhysicalKey(key)));' in text
assert 'result = Cons.From(requested.Select(key => UnavailablePhysicalKey(key)' in text
assert '? Term.Tuple(key, ProcessInfoAtoms.Undefined)' in text
assert ': Find(key, result)' in text
assert '=> key is Atom { Name: ProcLibCrashReportNames.HeapSize or ProcLibCrashReportNames.StackSize };' in text
for source in ['ProcessInfoDefaultCatalog.cs', 'ProcessInfoAtoms.cs', 'ProcessRuntime.ProcessInfo.cs']:
    source_path = 'src/Erlang.Runtime/' + source
    original = subprocess.check_output(['git', 'show', 'add206c03c801cbd68c51d23dbbdb8e36f2d581f:' + source_path])
    assert (c / source_path).read_bytes() == original
guard = dict(Status='SOURCE_REVIEW_ACCEPTS_EXPLICIT_UNAVAILABLE_LOCAL_CRASH_REPORT_METRICS',
             Path=path, Sha256=sha(raw),
             UnavailableFields=['heap_size', 'stack_size'],
             PublicProcessInfoChanged=False, PhysicalMetricsInvented=False,
             OriginalFieldOrderPreserved=True, LocalOnly=True,
             RemoteRpcStillSeparateDependency=True,
             SdkOrNativeExecuted=False,
             OriginalPhysicalParityClaimed=False)
out = r / 'artifacts/root-338-physical-bridge-source-review.json'
assert not out.exists()
out.write_text(json.dumps(guard, indent=2) + '\n')
print(json.dumps(guard))
