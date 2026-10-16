"""Operational pointer only; previous handoff snapshots and immutable proof reports remain unchanged."""
from pathlib import Path
import hashlib,json,datetime
r=Path.cwd();d=r/'artifacts/part332-remote-new-review';p=d/'handoff.json';old=p.read_bytes();h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda q:json.loads(q.read_bytes())
history=d/('handoff-before-'+h(old)[:12]+'.json')
if not history.exists():history.write_bytes(old)
v=json.loads(old);latest=sorted((d/'monitor').glob('status-*.json'))[-1];snapshot=j(latest);v['Runs']=snapshot['Runs'];v['ObservedUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat();v['PreviousHandoff']=dict(Path=history.relative_to(r).as_posix(),Sha256=h(old));v['LatestStatusSnapshot']=dict(Path=latest.relative_to(r).as_posix(),Sha256=h(latest.read_bytes()))
guards={'ci':d/'ci/independent-ci-guard.json','native':r/'artifacts/part332-native-independent/guard.json','reallocation':r/'artifacts/part332-reallocation-independent/guard.json','distribution':r/'artifacts/part332-distribution-independent/summary.json','ordinary':d/'ordinary-semantic-independent-guard.json'}
v['IndependentlyAuditedScopes']=[dict(Kind=k,Path=q.relative_to(r).as_posix(),Sha256=h(q.read_bytes()),Status=j(q)['Status']) for k,q in guards.items() if q.exists()]
v['RemainingScopes']=[k for k,q in guards.items() if not q.exists()];v['CurrentOriginalCompleted']=not v['RemainingScopes'];v['Status']='ALL_FIVE_TYPED_ACTUAL_COMPLETED' if v['CurrentOriginalCompleted'] else 'PARTIAL_ACTUAL_TERMINAL_AUDITS_REMAINING_'+','.join(v['RemainingScopes'])
v['NextAction']='Do not repeat POST. Inspect RemainingScopes and latest GET snapshot. Reuse existing terminal raw folders; never redownload a captured role. Poll >=120 seconds, back off to5min while corpus runs; download only newly terminal missing role, join before passive extraction, audit typed outcomes.'
v.pop('DistributionCapture',None)
if v['CurrentOriginalCompleted']:v['PlannedCountIsNotObservedResult']=False
p.write_bytes((json.dumps(v,indent=2)+'\n').encode());print(json.dumps(dict(HandoffSha256=h(p.read_bytes()),RemainingScopes=v['RemainingScopes'])))
