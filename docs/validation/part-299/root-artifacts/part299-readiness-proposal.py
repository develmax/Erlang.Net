"""Root-only metadata proposal; requires actual terminal Main validation."""
from pathlib import Path
import json
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes())
assert j(a/'root-part299-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part299-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-298/tests.json')
assert t['Passed']==6610 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests']) and [x['Name'] for x in t['Tests']]==[x['Name'] for x in old['Tests']]
scope=r/'docs/module-readiness-scope.json';s=j(scope)
assert not any(c['Id']=='NifNativeBackend' for c in s['Components'])
runtime=next(c for c in s['Components'] if c['Id']=='Runtime')
runtime.setdefault('Implemented',[]).append('Part299: actual6610/full retains all test names and registry502. Internal bounded trusted native callback/owner/initialization/pending-exception foundation; standalone native tool managed prerequisites join validation. Windows trusted fixture and compiled-source routes are separately recorded tool gates. Public load_nif/general ABI/automatic native lifecycle/BEAM and old-image native replacement remain incomplete; no Linux/original13 execution claim.')
s['Components'].append({'Id':'NifNativeBackend','Project':'tools/Erlang.NifNativeBackend','Status':'Partially compatible','Implemented':['Part299: actual managed prerequisite integration plus separately gated Windows trusted fixture and compiled-source native checks; reviewed992-input seal and manual Linux SDK/original13 infrastructure. C veneer is bounded ABI forwarding only; original13 is a separate fixture/typed gate.'],'Remaining':['Linux native execution and original13 terminal proof pending; no general load_nif/ABI, arbitrary libraries, native threads/resources/dirty scheduling, automatic lifecycle, BEAM/old-image native replacement or performance parity.'],'TestPrefixes':[]})
s['ComparisonTestReport']='validation/part-299/tests.json'
target=r/'docs/validation/part-299/tests.json';raw=(a/'tests.json').read_bytes()
if target.exists():assert target.read_bytes()==raw
else:assert not target.parent.exists()
target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
scope.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('Prepared actual6610 comparison and partial Runtime/native-tool scope; root regenerates readiness')
