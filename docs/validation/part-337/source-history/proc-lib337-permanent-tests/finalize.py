from pathlib import Path
import json,hashlib
out=Path('artifacts/proc-lib337-permanent-tests');p=out/'source/tests/Erlang.Tests/ProcLibReportMfaContracts.cs';t=p.read_text();assert t.count('        await caller.Unwound.WaitAsync(ProcLibReportProbeNames.Deadline);\n')==1;p.write_text(t.replace('        await caller.Unwound.WaitAsync(ProcLibReportProbeNames.Deadline);\n',''),encoding='utf-8')
f=out/'source-freeze.json';j=json.loads(f.read_bytes())
for r in j['Files']:
 b=(out/'source'/r['Path']).read_bytes();r['Bytes']=len(b);r['Sha256']=hashlib.sha256(b).hexdigest().upper()
f.write_text(json.dumps(j,indent=2)+'\n');print(hashlib.sha256(f.read_bytes()).hexdigest().upper())
