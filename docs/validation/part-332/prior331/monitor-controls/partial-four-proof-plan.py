from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';d=a/'part331-remote-new-review';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper()
def row(p):return dict(Path=p.relative_to(r).as_posix(),Bytes=p.stat().st_size,Sha256=h(p.read_bytes()))
paths={'ci':d/'ci/independent-ci-guard.json','native':a/'part331-native-new-independent3/guard.json','reallocation':a/'part331-reallocation-new-independent3/guard.json','distribution':a/'part331-distribution-independent/summary.json'}
for role,p in paths.items():v=j(p);assert v['Status'].startswith('PASS') and v['Head']=='6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d'
latest=sorted((d/'monitor').glob('status-*.json'))[-1];snapshot=j(latest);ordinary=next(x for x in snapshot['Runs'] if x['Kind']=='ordinary');assert ordinary['Status']!='completed'
report=dict(Status='PASS_FOUR_ACTUAL331_TERMINAL_SCOPES_ORDINARY_PENDING',Head='6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d',CompletedScopes=[dict(Role=k,**row(p)) for k,p in paths.items()],Ordinary=dict(Run=ordinary['RunId'],Status=ordinary['Status'],OriginalCasesCompared=False,CompletionClaimed=False),AllFiveCompletionClaimed=False,LatestSnapshot=row(latest),DistributionStrictReprRefusalRetained=True,ReviewPlumbingHistory=row(d/'review-adaptation-history.json'))
summary=a/'part331-four-terminal-ordinary-pending-summary.json';assert not summary.exists();summary.write_bytes((json.dumps(report,indent=2)+'\n').encode())
selected={};prefix='docs/validation/part-332/prior331/'
def add(p,target):
 p=p.resolve();assert p.is_file() and p.is_relative_to(r) and not p.name.endswith(('.zip','.tar.gz','.dll','.pdb','.so','.exe'));target=prefix+target
 b=p.read_bytes();selected[target]=dict(SourcePath=p.relative_to(r).as_posix(),TargetPath=target,Bytes=len(b),Sha256=h(b))
ids={}
for role in paths:
 old=j(a/'part329-remote-new-review'/role/'run-map.json');new=j(d/role/'run-map.json');oldapi=j(a/'part329-remote-new-review'/role/'artifacts.json')['artifacts'][0];newapi=j(d/role/'artifacts.json')['artifacts'][0]
 for x,y in [(old['Run'],new['Run']),(old['Job'],new['Job']),(oldapi['id'],newapi['id'])]:ids[str(x)]=str(y)
for e in j(a/'part329-five-terminal-lean-copy-rows.json'):
 n=e['SourcePath'];lead='artifacts/part329-remote-new-review/'
 if n.startswith(lead):
  rel=n.removeprefix(lead)
  if '/' not in rel or rel.startswith(('ordinary/','monitor/')):continue
  n=n.replace(lead,'artifacts/part331-remote-new-review/')
  for x,y in ids.items():n=n.replace(x,y)
  p=r/n;assert p.is_file(),n;kind,tail=p.relative_to(d).as_posix().split('/',1);add(p,kind+'/raw/'+tail)
 elif 'pack327-' in n or n.startswith('tools/'):
  if (r/n).is_file():add(r/n,'trusted-local/'+n.removeprefix('artifacts/'))
for name in ['part331-native-new-independent','part331-native-new-independent2','part331-native-new-independent3','part331-reallocation-new-independent2','part331-reallocation-new-independent3','part331-distribution-independent']:
 folder=a/name
 if folder.is_dir():
  for p in folder.iterdir():
   if p.is_file():add(p,'independent/'+name+'/'+p.name)
for p in d.iterdir():
 if p.is_file():add(p,'monitor-controls/'+p.name)
stamp=latest.stem.removeprefix('status-')
for p in [d/'monitor/mapped-runs.json',latest,d/'monitor'/('runs-'+stamp+'.json'),d/'monitor'/('branch-'+stamp+'.json')]+list((d/'monitor').glob('*-jobs-'+stamp+'.json')):add(p,'monitor/'+p.name)
add(summary,'four-terminal-ordinary-pending-summary.json')
authority=a/'root-part331-post-publication-dispatch-authority.json'
if authority.is_file():
 add(authority,'publication/authority.json')
 for e in j(authority)['RequiredBindings']:
  p=r/e['Path'];add(p,'publication/bindings/'+e['Role']+'/'+p.name)
rows=sorted(selected.values(),key=lambda e:e['TargetPath']);p=a/'part331-four-terminal-lean-copy-rows.json';assert not p.exists();p.write_bytes((json.dumps(rows,indent=2)+'\n').encode())
print(json.dumps(dict(Summary=row(summary),CopyPlan=row(p),Rows=len(rows),Bytes=sum(x['Bytes'] for x in rows),OrdinaryPending=True,AllFiveCompletionClaimed=False)))
