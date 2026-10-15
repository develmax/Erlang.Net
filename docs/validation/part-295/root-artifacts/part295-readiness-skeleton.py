"""Root-reviewed skeleton. Only actual terminal final Main count may be supplied."""
from pathlib import Path
import argparse,json
p=argparse.ArgumentParser();p.add_argument('--passed',required=True,type=int);p.add_argument('--full-exit',required=True);args=p.parse_args()
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes());assert j(Path(args.full_exit))['ExitCode']==0
t=j(a/'tests.json');assert t['Passed']==args.passed and t['Failed']==0 and args.passed>6603
assert b'Validation passed' in (a/'root-part295-full-validation.log').read_bytes()
target=r/'docs/validation/part-295/tests.json';raw=(a/'tests.json').read_bytes()
if target.exists():assert target.read_bytes()==raw
else:assert not target.parent.exists()
q=r/'docs/module-readiness-scope.json';s=j(q);component=next(x for x in s['Components'] if x['Id']=='Runtime');assert not any('Part295:' in x for x in component.get('Implemented',[]))
s['ComparisonTestReport']='validation/part-295/tests.json'
component.setdefault('Implemented',[]).append(f'Part295: actual Main{args.passed}/full retains historical6603 ordered tests and adds image-attachment/code-permission/callback-cleanup HOST coverage. Dormant lazy binding attachment and17 serialized registry writer entries are managed prerequisites, not native loading/dispatch. Registry502/erlang344 of351 presence unchanged. New changed-source original and same-head CI remain pending.')
prefix='host/BEAM-on-load-cleanup-fault-'
assert prefix not in component['TestPrefixes'];component['TestPrefixes'].append(prefix)
target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw);q.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');print('Prepared actual Part295 report comparison; root regenerates readiness separately')
