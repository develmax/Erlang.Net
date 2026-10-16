from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';d=a/'part329-remote-new-review';h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());selected={}
prefix='docs/validation/part-331/prior329/'
def add(p,target):
 p=p.resolve();assert p.is_relative_to(r) and p.is_file();name=p.relative_to(r).as_posix();assert not p.name.endswith(('.zip','.tar.gz','.dll','.pdb','.so','.exe'))
 target=prefix+target;assert target not in selected or selected[target]['SourcePath']==name
 b=p.read_bytes();selected[target]=dict(SourcePath=name,TargetPath=target,Bytes=len(b),Sha256=h(b))
ids={'38865058842':'38899129114','116855838511':'116966742891','12031022190':'12044794046','38865257906':'38899260501','116856489621':'116967179110','12031689572':'12046826413','38865264886':'38899284241','116856512555':'116967248782','12030958282':'12045812047','38865272148':'38899307374','116856534998':'116967324775','12030614599':'12045009296','38865278645':'38899333818','116856555712':'116967412748','12031650623':'12045597089'}
prior=j(a/'pack328-five-terminal-selected-copy-rows.json');raw_count=0
for e in prior:
 old=e['SourcePath'];lead='artifacts/part328-remote-beam310/'
 if old.startswith(lead):
  n=old.replace(lead,'artifacts/part329-remote-new-review/')
  for before,after in ids.items():n=n.replace(before,after)
  q=r/n
  # Prior snapshot stamps are intentionally replaced by one final current snapshot below.
  if '/monitor/' in n:continue
  assert q.is_file(),n
  rel=q.relative_to(d).as_posix()
  if '/' not in rel:continue
  kind,tail=rel.split('/',1)
  add(q,kind+'/raw/'+tail);raw_count+=1
 elif 'pack327-' in old or old.startswith('tools/'):
  q=r/old
  if q.is_file():add(q,'trusted-local/'+old.removeprefix('artifacts/'))
for folder,target in [(a/'part329-native-new-independent','native/independent'),(a/'part329-reallocation-new-independent','reallocation/independent'),(a/'part329-distribution-independent','distribution/independent'),(a/'part329-new-publication-review','publication-independent')]:
 for p in folder.iterdir():
  if p.is_file() and p.suffix in ['.py','.json','.ps1','.md','.stdout','.stderr']:add(p,target+'/'+p.name)
for p in d.iterdir():
 if p.is_file():add(p,'monitor-controls/'+p.name)
latest=sorted((d/'monitor').glob('status-*.json'))[-1];stamp=latest.stem.removeprefix('status-')
for p in [d/'monitor/mapped-runs.json',latest,d/'monitor'/('runs-'+stamp+'.json'),d/'monitor'/('branch-'+stamp+'.json')]+list((d/'monitor').glob('*-jobs-'+stamp+'.json')):add(p,'monitor/'+p.name)
for n in ['non-tail-stack329-managed-eight-actual1.json','non-tail-stack329-managed-eight-actual-guard1.json','non-tail-stack329-actual-case-ledger1.json','non-tail-stack329-green-actual-guard1.json']:
 add(a/n,'new-eight-managed/'+n)
add(a/'part329-all-five-terminal-independent-summary.json','all-five-terminal-independent-summary.json')
rows=sorted(selected.values(),key=lambda e:e['TargetPath']);assert len(rows)==len({e['SourcePath'] for e in rows})
p=a/'part329-five-terminal-lean-copy-rows.json';assert not p.exists();p.write_bytes((json.dumps(rows,indent=2)+'\n').encode())
report=dict(Status='PASS_COMPLETE_SELECTED329_TERMINAL_RAW_AND_TYPED_PROOFS_FOR331',Rows=len(rows),Bytes=sum(e['Bytes'] for e in rows),RowsFromPriorReviewedRawSelectionPattern=raw_count,CopyPlanSha256=h(p.read_bytes()),TargetPrefix=prefix,NoSdkNativeRuntimeBinariesOrArchives=True,OrdinaryRawOriginal5941AndManagedEightRetained=True,DistributionStrictReprRefusalRetained=True,CopyPerformed=False)
q=a/'part329-five-terminal-lean-copy-summary.json';q.write_bytes((json.dumps(report,indent=2)+'\n').encode());print(json.dumps(report))
