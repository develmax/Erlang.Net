from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';d=a/'part331-remote-new-review';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();prefix='docs/validation/part-332/prior331/'
base=j(a/'part331-four-terminal-lean-copy-rows.json');selected={x['TargetPath']:x for x in base}
for e in base:
 p=r/e['SourcePath'];assert p.stat().st_size==e['Bytes'] and h(p.read_bytes())==e['Sha256']
def add(p,target):
 p=p.resolve();assert p.is_file() and p.is_relative_to(r) and not p.name.endswith(('.zip','.tar.gz','.dll','.pdb','.so','.exe'));target=prefix+target;b=p.read_bytes();e=dict(SourcePath=p.relative_to(r).as_posix(),TargetPath=target,Bytes=len(b),Sha256=h(b))
 assert target not in selected or selected[target]==e,target
 selected[target]=e
for p in (d/'ordinary').rglob('*'):
 if p.is_file() and p.suffix!='.zip':add(p,'ordinary/raw/'+p.relative_to(d/'ordinary').as_posix())
for p in d.iterdir():
 if p.is_file():add(p,'monitor-controls/'+p.name)
latest=sorted((d/'monitor').glob('status-*.json'))[-1];stamp=latest.stem.removeprefix('status-')
for p in [latest,d/'monitor'/('runs-'+stamp+'.json'),d/'monitor'/('branch-'+stamp+'.json')]+list((d/'monitor').glob('*-jobs-'+stamp+'.json')):add(p,'monitor/'+p.name)
for n in ['focus-fixtures.json','formatted-source-freeze.json','guard.json']:add(a/'proc-lib331-actual3'/n,'new-proc-lib-managed/'+n)
summary=a/'part331-all-five-terminal-independent-summary.json';v=j(summary);assert v['Passed'] and v['OriginalOrdinaryPassed']==5962 and v['NewOrdinaryOriginalAndManagedCases']==21 and v['StrictOrdinaryRawEqualityPassed']
add(summary,'all-five-terminal-independent-summary.json')
rows=sorted(selected.values(),key=lambda e:e['TargetPath']);p=a/'part331-five-terminal-lean-copy-rows.json';assert not p.exists();p.write_bytes((json.dumps(rows,indent=2)+'\n').encode())
report=dict(Status='PASS_COMPLETE_SELECTED331_TERMINAL_PROOFS_FOR332',Rows=len(rows),Bytes=sum(x['Bytes'] for x in rows),CopyPlanSha256=h(p.read_bytes()),TargetPrefix=prefix,AllPartialFourRowsPreservedUnchanged=True,PriorOrdinaryPendingSnapshotHistorical=True,AllFiveTypedActualPass=True,OrdinaryOriginal5962AndFd8=True,NewProcLibOriginalAndSdkCases=21,StrictOrdinaryVarianceRows=0,DistributionStrictReprRefusalRetained=True,ReviewAdaptationFailuresRetainedAndClassified=True,NoSdkNativeRuntimeBinariesOrArchives=True,CopyPerformed=False)
q=a/'part331-five-terminal-lean-copy-summary.json';q.write_bytes((json.dumps(report,indent=2)+'\n').encode());print(json.dumps(report))
