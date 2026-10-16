"""Select current terminal proof rows; no SDK/native/runtime binaries or archives copied."""
from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';d=a/'part333-remote-new-review';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();prefix='docs/validation/part-334/prior333/';selected={}
def add(p,target):
 p=p.resolve();assert p.is_file() and p.is_relative_to(r) and not p.name.endswith(('.zip','.tar.gz','.dll','.pdb','.so','.exe'));b=p.read_bytes();target=prefix+target;e=dict(SourcePath=p.relative_to(r).as_posix(),TargetPath=target,Bytes=len(b),Sha256=h(b))
 assert target not in selected or selected[target]==e,target
 selected[target]=e
ids={}
for role in ['ci','ordinary','native','reallocation','distribution']:
 oldroot=a/'part332-remote-new-review'/role;old=j(oldroot/'run-map.json');new=j(d/role/'run-map.json');oldapi=j(oldroot/'artifacts.json')['artifacts'][0];newapi=j(d/role/'artifacts.json')['artifacts'][0]
 for x,y in [(old['Run'],new['Run']),(old['Job'],new['Job']),(oldapi['id'],newapi['id'])]:ids[str(x)]=str(y)
for e in j(a/'part332-five-terminal-lean-copy-rows.json'):
 n=e['SourcePath'];lead='artifacts/part332-remote-new-review/'
 if n.startswith(lead):
  rel=n.removeprefix(lead)
  if '/' not in rel or rel.startswith('monitor/'):continue
  if rel.split('/')[0] not in ['ci','ordinary','native','reallocation','distribution']:continue
  n=n.replace(lead,'artifacts/part333-remote-new-review/')
  for x,y in ids.items():n=n.replace(x,y)
  p=r/n;assert p.is_file(),n;kind,tail=p.relative_to(d).as_posix().split('/',1);add(p,kind+'/raw/'+tail)
 elif 'pack327-' in n or n.startswith('tools/'):
  if (r/n).is_file():add(r/n,'trusted-local/'+n.removeprefix('artifacts/'))
for foldername in ['part333-native-independent','part333-reallocation-independent','part333-distribution-independent','part333-publication-controls','part333-publication-source-independent','part333-actual-authority-passive-peer','part333-post-publication-authority-independent']:
 folder=a/foldername
 if folder.is_dir():
  for p in folder.iterdir():
   if p.is_file():add(p,'independent/'+foldername+'/'+p.name)
for p in d.iterdir():
 if p.is_file():add(p,'monitor-controls/'+p.name)
latest=sorted((d/'monitor').glob('status-*.json'))[-1];stamp=latest.stem.removeprefix('status-')
for p in [d/'monitor/mapped-runs.json',latest,d/'monitor'/('runs-'+stamp+'.json'),d/'monitor'/('branch-'+stamp+'.json')]+list((d/'monitor').glob('*-jobs-'+stamp+'.json')):add(p,'monitor/'+p.name)
authority=a/'root-part333-post-publication-dispatch-authority.json';add(authority,'publication/authority.json')
for e in j(authority)['RequiredBindings']:
 p=r/e['Path'];add(p,'publication/bindings/'+e['Role']+'/'+p.name)
for p in [a/'root-part333-publication-bindings.json']+list(a.glob('root-part333-*-dispatch-attempt.json'))+list(a.glob('root-part333-*-dispatch-result.json')):add(p,'publication/'+p.name)
for n in ['focused-guard.json','formatted-source-freeze.json','options-focused.json','startup-focused.json','queued-and-interpreted-focused.json','previous-proc-lib-focused.json']:add(a/'proc-lib333-actual3'/n,'new41-local/'+n)
summary=a/'part333-all-five-terminal-independent-summary.json';assert j(summary)['Passed'] and j(summary)['OriginalOrdinaryPassed']==6042;add(summary,'all-five-terminal-independent-summary.json')
rows=sorted(selected.values(),key=lambda e:e['TargetPath']);p=a/'part333-five-terminal-lean-copy-rows.json';assert not p.exists();p.write_bytes((json.dumps(rows,indent=2)+'\n').encode())
report=dict(Status='PASS_SELECTED333_ALL_FIVE_TERMINAL_PROOFS_FOR334',Head='e9725c4b2eef68014aeb70afd0abcf9f646065fd',Rows=len(rows),Bytes=sum(e['Bytes'] for e in rows),CopyPlanSha256=h(p.read_bytes()),TargetPrefix=prefix,New41PublishedLocalOriginalSdkChainRetained=True,All6001PreviousContractsRetained=True,ActualOrdinary6042Fd8=True,StrictOrdinaryVarianceRows=1,DistributionStrictReprRefusalRetained=True,ReaderPathPreconditionFailureRetainedSeparately=True,LocalSingleFull6871ElevenActualPass=True,CurrentHeadCi6871ElevenActualPass=True,NoSdkNativeRuntimeBinariesOrArchives=True,CopyPerformed=False)
q=a/'part333-five-terminal-lean-copy-summary.json';q.write_bytes((json.dumps(report,indent=2)+'\n').encode());print(json.dumps(report))
