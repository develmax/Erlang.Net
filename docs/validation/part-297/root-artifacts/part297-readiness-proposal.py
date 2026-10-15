"""Root-only proposal after actual terminal Main6609; no build/test execution."""
from pathlib import Path
import json
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes())
assert j(a/'root-part297-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part297-full-validation.log').read_bytes()
t=j(a/'tests.json');assert (t['Passed'],t['Failed'])==(6609,0)
q=r/'docs/module-readiness-scope.json';s=j(q);runtime=next(c for c in s['Components'] if c['Id']=='Runtime');assert not any('Part297:' in x for x in runtime.get('Implemented',[]))
target=r/'docs/validation/part-297/tests.json';raw=(a/'tests.json').read_bytes()
if target.exists():assert target.read_bytes()==raw
else:assert not target.parent.exists()
s['ComparisonTestReport']='validation/part-297/tests.json'
runtime.setdefault('Implemented',[]).append('Part297: Main6609/full retains6608 old ordered names plus exact-image BEAM entry/closure HOST; source16. Stable prepared/standalone owners, owned named-entry calls and closure leases preserve old/current/pending identity. All declared lambdas validate at publication, including unexported targets; blanket FunT rejection removed after negative controls. Fake managed bindings and injected pending metadata only; no native loading/inventory/automatic retirement or original on_load-closure parity claim. Registry502/erlang344 of351 unchanged.')
target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw);q.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');print('Prepared actual6609 comparison and partial Runtime scope; root regenerates readiness')
