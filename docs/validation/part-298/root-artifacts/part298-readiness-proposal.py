"""Root-only proposal after terminal actual Main6610; no tests/builds run."""
from pathlib import Path
import json
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes())
assert j(a/'root-part298-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part298-full-validation.log').read_bytes()
t=j(a/'tests.json');assert (t['Passed'],t['Failed'])==(6610,0)
q=r/'docs/module-readiness-scope.json';s=j(q);runtime=next(c for c in s['Components'] if c['Id']=='Runtime');assert not any('Part298:' in x for x in runtime.get('Implemented',[]))
target=r/'docs/validation/part-298/tests.json';raw=(a/'tests.json').read_bytes()
if target.exists():assert target.read_bytes()==raw
else:assert not target.parent.exists()
s['ComparisonTestReport']='validation/part-298/tests.json'
runtime.setdefault('Implemented',[]).append('Part298: Main6610/full retains6609 old ordered names plus current-image installed-inventory HOST; source6. Ordered immutable inventory publishes atomically with ready bindings and invalidates/reuses metadata snapshots by immutable identity. Pending/old images stay isolated; fake owners retire explicitly in HOST. No public NIF loading/ABI/taints/nif_version or automatic native lifetime claim. Registry502/erlang344 of351 unchanged; same-head CI/changed-source original pending.')
target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw);q.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');print('Prepared actual6610 comparison/partial Runtime scope; root regenerates readiness')
