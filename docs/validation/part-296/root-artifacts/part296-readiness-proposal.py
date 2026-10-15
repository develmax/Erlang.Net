"""Root-only proposal; runs no test/build. Terminal actual Main6608 is mandatory."""
from pathlib import Path
import json
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes())
assert j(a/'root-part296-full-exit.json')['ExitCode']==0
t=j(a/'tests.json');assert (t['Passed'],t['Failed'])==(6608,0) and b'Validation passed' in (a/'root-part296-full-validation.log').read_bytes()
q=r/'docs/module-readiness-scope.json';s=j(q);components={x['Id']:x for x in s['Components']}
assert not any('Part296:' in x for c in components.values() for x in c.get('Implemented',[]))
target=r/'docs/validation/part-296/tests.json';raw=(a/'tests.json').read_bytes()
if target.exists():assert target.read_bytes()==raw
else:assert not target.parent.exists()
s['ComparisonTestReport']='validation/part-296/tests.json'
components['Runtime'].setdefault('Implemented',[]).append('Part296: Main6608/full preserves all6606 old ordered names plus2 HOSTs. Exact current/pending caller-image reservation owns code/source permission through initializer and cleanup; generation/owner identity is rechecked at atomic publication. Abandonment/self-join and diagnostic actor-flow isolation are managed prerequisites. Native loading, installed inventory and automatic retirement/initializer lifecycle integration remain unfinished; registry502/erlang344 of351 unchanged.')
components['Compiler'].setdefault('Implemented',[]).append('Part296: exact-image named source entry checks managed binding before fallback clauses; owned call lease spans awaited body. Eight named-entry routes and old/current/fallback/kill-finally drain pass HOST contracts. Runtime friends actual Compiler only; no public test hook. Fake internal bindings, no load_nif/native ABI/BEAM parity claim.')
prefix='runtime/nif-source-named-entry-'
assert prefix not in components['Compiler']['TestPrefixes'];components['Compiler']['TestPrefixes'].append(prefix)
target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw);q.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');print('Prepared actual6608 comparison and partial Runtime/Compiler scope; root regenerates readiness')
