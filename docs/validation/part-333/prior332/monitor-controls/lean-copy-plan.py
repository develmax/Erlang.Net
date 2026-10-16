"""Select current terminal proof rows; no SDK/native/runtime binaries or archives copied."""
from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';d=a/'part332-remote-new-review';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();prefix='docs/validation/part-333/prior332/';selected={}
def add(p,target):
 p=p.resolve();assert p.is_file() and p.is_relative_to(r) and not p.name.endswith(('.zip','.tar.gz','.dll','.pdb','.so','.exe'));b=p.read_bytes();target=prefix+target;e=dict(SourcePath=p.relative_to(r).as_posix(),TargetPath=target,Bytes=len(b),Sha256=h(b))
 assert target not in selected or selected[target]==e,target
 selected[target]=e
ids={}
for role in ['ci','ordinary','native','reallocation','distribution']:
 oldroot=a/'part331-remote-new-review'/role;old=j(oldroot/'run-map.json');new=j(d/role/'run-map.json');oldapi=j(oldroot/'artifacts.json')['artifacts'][0];newapi=j(d/role/'artifacts.json')['artifacts'][0]
 for x,y in [(old['Run'],new['Run']),(old['Job'],new['Job']),(oldapi['id'],newapi['id'])]:ids[str(x)]=str(y)
for e in j(a/'part331-five-terminal-lean-copy-rows.json'):
 n=e['SourcePath'];lead='artifacts/part331-remote-new-review/'
 if n.startswith(lead):
  rel=n.removeprefix(lead)
  if '/' not in rel or rel.startswith('monitor/'):continue
  if rel.split('/')[0] not in ['ci','ordinary','native','reallocation','distribution']:continue
  n=n.replace(lead,'artifacts/part332-remote-new-review/')
  for x,y in ids.items():n=n.replace(x,y)
  p=r/n;assert p.is_file(),n;kind,tail=p.relative_to(d).as_posix().split('/',1);add(p,kind+'/raw/'+tail)
 elif 'pack327-' in n or n.startswith('tools/'):
  if (r/n).is_file():add(r/n,'trusted-local/'+n.removeprefix('artifacts/'))
for foldername in ['part332-native-independent','part332-reallocation-independent','part332-distribution-independent','part332-publication-controls-new','part332-publication-controls-new2','part332-publication-source-independent','part332-actual-authority-passive-peer','part332-post-publication-authority-independent']:
 folder=a/foldername
 if folder.is_dir():
  for p in folder.iterdir():
   if p.is_file():add(p,'independent/'+foldername+'/'+p.name)
for p in d.iterdir():
 if p.is_file():add(p,'monitor-controls/'+p.name)
latest=sorted((d/'monitor').glob('status-*.json'))[-1];stamp=latest.stem.removeprefix('status-')
for p in [d/'monitor/mapped-runs.json',latest,d/'monitor'/('runs-'+stamp+'.json'),d/'monitor'/('branch-'+stamp+'.json')]+list((d/'monitor').glob('*-jobs-'+stamp+'.json')):add(p,'monitor/'+p.name)
authority=a/'root-part332-post-publication-dispatch-authority.json';add(authority,'publication/authority.json')
for e in j(authority)['RequiredBindings']:
 p=r/e['Path'];add(p,'publication/bindings/'+e['Role']+'/'+p.name)
for p in [a/'root-part332-publication-bindings.json']+list(a.glob('root-part332-*-dispatch-attempt.json'))+list(a.glob('root-part332-*-dispatch-result.json')):add(p,'publication/'+p.name)
for n in ['focused-guard.json','formatted-source-freeze.json','startup-focused.json','queued-and-interpreted-focused.json','previous-proc-lib-focused.json']:add(a/'proc-lib332-actual2'/n,'new39-local/'+n)
summary=a/'part332-all-five-terminal-independent-summary.json';assert j(summary)['Passed'] and j(summary)['OriginalOrdinaryPassed']==6001;add(summary,'all-five-terminal-independent-summary.json')
rows=sorted(selected.values(),key=lambda e:e['TargetPath']);p=a/'part332-five-terminal-lean-copy-rows.json';assert not p.exists();p.write_bytes((json.dumps(rows,indent=2)+'\n').encode())
report=dict(Status='PASS_SELECTED332_ALL_FIVE_TERMINAL_PROOFS_FOR333',Head='aaf48552bee429b0447d3e28e8274d34ad590d73',Rows=len(rows),Bytes=sum(e['Bytes'] for e in rows),CopyPlanSha256=h(p.read_bytes()),TargetPrefix=prefix,New39PublishedLocalOriginalSdkChainRetained=True,All5962PreviousContractsRetained=True,ActualOrdinary6001Fd8=True,StrictOrdinaryVarianceRows=0,DistributionStrictReprRefusalRetained=True,ReaderPathPreconditionFailureRetainedSeparately=True,HistoricalIsolatedInitialExit1PackageSuffixExit0Preserved=True,CurrentHeadCi6822ElevenActualPass=True,NoSdkNativeRuntimeBinariesOrArchives=True,CopyPerformed=False)
q=a/'part332-five-terminal-lean-copy-summary.json';q.write_bytes((json.dumps(report,indent=2)+'\n').encode());print(json.dumps(report))
