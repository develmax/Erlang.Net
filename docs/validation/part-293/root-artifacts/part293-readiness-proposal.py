"""Root-only proposal; author never executes. Actual terminal Main6603 is mandatory."""
from pathlib import Path
import argparse,hashlib,json
p=argparse.ArgumentParser();p.add_argument('--full-exit',required=True);args=p.parse_args()
r=Path.cwd().resolve();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper()
assert j(Path(args.full_exit))['ExitCode']==0
t=j(a/'tests.json');assert t['Passed']==6603 and t['Failed']==0
assert b'Validation passed' in (a/'root-part293-full-validation.log').read_bytes()
q=r/'docs/module-readiness-scope.json';before=q.read_bytes();s=json.loads(before);components={x['Id']:x for x in s['Components']};assert 'Runtime' in components
assert not any('Part293:' in x for x in components['Runtime'].get('Implemented',[]))
target=r/'docs/validation/part-293/tests.json';raw=(a/'tests.json').read_bytes()
if target.exists():assert target.read_bytes()==raw
else:assert not target.parent.exists(),'Refuse unrelated partial archive'
ledger={'ComparisonPreseed':{'Path':target.relative_to(r).as_posix(),'Bytes':len(raw),'Sha256':h(raw)},'BeforeScopeSha256':h(before),'ReadinessGeneratedByThisScript':False,'PreviousReadinessOutputs':[{'Path':'docs/'+n,'Sha256':h((r/'docs'/n).read_bytes())} for n in ['module-readiness.json','MODULE_READINESS.md']]}
s['ComparisonTestReport']='validation/part-293/tests.json'
components['Runtime'].setdefault('Implemented',[]).append('Part293: Main6603/full and four additional HOST groups cover bounded all-existing-writer admission, atomic monitor/unlink publication, public-FREE-independent exit cursors and owned nonblocking same-controller replies. Historical6599 ordered names,502 MFAs and erlang344/351 presence retained. Isolated fixture-budget and real legacy-monitor-mask failures/fixes are preserved. Changed-runtime original comparison and new pressure observations remain pending; no physical/native busy-policy or whole distribution parity claim.')
target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw);q.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
ledger['AfterScopeSha256']=h(q.read_bytes());(a/'root-part293-readiness-metadata-update.json').write_text(json.dumps(ledger,indent=2)+'\n',encoding='utf-8');print(json.dumps(ledger))
