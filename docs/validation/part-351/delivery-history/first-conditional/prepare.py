"""Prepare conditional351 proof inventory only; never mutate Main/candidate or launch validation."""
from pathlib import Path
import hashlib,json
R=Path.cwd();A=R/'artifacts';O=A/'part351-docs-delivery-plan';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
rows={}
def add(p,target):
 p=p.resolve();assert p.is_file();b=p.read_bytes();assert target.startswith('docs/validation/part-351/')
 row=dict(SourcePath=p.relative_to(R).as_posix(),TargetPath=target,Bytes=len(b),Sha256=sha(b))
 if target in rows:assert rows[target]==row
 else:rows[target]=row
P='docs/validation/part-351/'
for row in read(A/'priority351-prefull-metadata-proposal/copy-plan.json')['Files']:
 if row['TargetPath'].startswith(P):
  p=R/row['SourcePath'];assert p.stat().st_size==row['Bytes'] and sha(p.read_bytes())==row['Sha256'];add(p,row['TargetPath'])
seed=A/'spawn-priority351-full-source2';freeze=read(seed/'source-freeze.json');assert len(freeze['Files'])==43
for row in freeze['Files']:
 p=seed/'source'/row['Path'];assert p.stat().st_size==row['Bytes'] and sha(p.read_bytes())==row['Sha256'];add(p,P+'tested-source/'+row['Path'])
# Explicit bounded folders only; no layout/build binaries or speculative terminal full outputs.
folders=['spawn-priority351-core-build-actual1','spawn-priority351-core-build-actual2','spawn-priority351-source-independent','spawn-priority351-styled-independent','spawn-priority351-permanent-independent','spawn-priority351-full-preflight-independent','spawn-priority351-full-source1','spawn-priority351-full-source2','spawn-priority351-full-base','spawn-priority351-full-runner-proposal','priority351-verified-contract-source-review','priority351-prefull-metadata-proposal']
for folder in folders:
 for p in sorted((A/folder).iterdir()):
  if p.is_file() and p.suffix in ['.json','.py','.log','.txt']:
   add(p,P+'preparation/'+folder+'/'+p.name)
for p in sorted(A.glob('root-*351*.py')):add(p,P+'root-preparation/'+p.name)
add(A/'part351-root-changelog-draft.md',P+'history/root-draft-before-full.md')
for folder in ['part350-ordinary-independent','successor350-ci-independent']:
 for p in sorted((A/folder).iterdir()):
  if p.is_file() and p.suffix in ['.json','.py','.log','.stdout','.stderr']:add(p,P+'published350/'+folder+'/'+p.name)
# Preserve all API/download/raw text and extracted JSON. ZIP excluded, still digest-bound by manifest/API receipts.
subsets=[]
for kind in ['ordinary','ci']:
 d=A/'part350-remote-new-review'/kind;manifest=read(d/'download-manifest.json');included=[];omitted=[]
 for row in manifest:
  rel=row['Path'].replace('\\','/');p=d/rel;assert p.stat().st_size==row['Bytes'] and sha(p.read_bytes())==row['Sha256']
  if p.suffix.lower() in ['.json','.log','.txt','.etf','.stdout','.stderr']:
   add(p,P+'published350/raw-'+kind+'/'+rel);included.append(row)
  else:omitted.append(row)
 for name in ['download-manifest.json','run-map.json']:add(d/name,P+'published350/raw-'+kind+'/'+name)
 subsets.append(dict(Kind=kind,Included=included,Omitted=omitted,Disclosure='Declared raw text/JSON subset. API ZIP omitted; its exact hash/size and all extracted members remain verified by passive350 readers. No runtime binaries copied.'))
subset=O/'published350-subset.json';assert not subset.exists();subset.write_text(json.dumps(subsets,indent=2)+'\n',newline='\n');add(subset,P+'published350/subset.json')
plan=dict(Status='CONDITIONAL351_PROOF_INVENTORY_FULL_AND_INDEPENDENT_GUARD_PENDING',ParentHead='c1f219f31726fc93e537080f400651d300c47d72',SourceFreezeSha256=sha((seed/'source-freeze.json').read_bytes()),SourceClosureSha256=sha((seed/'source-resource-closure.json').read_bytes()),Files=list(rows.values()),FilesCount=len(rows),ExpectedArithmeticOnly=dict(Tests=7297,OldTests=7286,NewTests=11,Integrations=11,Packages=2,BoundedChildren=12),CurrentCompatibilityCounts=[10,24,553],RuntimeSourceBindings=74,Registry=587,FutureSixPredicatePromotionsExcluded=True,ActualOriginal350=dict(Passed=6313,FdPassed=8,StrictRawEquality='REFUSED_PREDECLARED_MAP_STDOUT_VARIANCE'),ActualCI350=dict(Passed=7286,Integrations=11),Full351PassedClaim=False,MainModified=False,CandidateModified=False,SdkExecuted=False,Network=False)
p=O/'proof-copy-plan.json';assert not p.exists();p.write_text(json.dumps(plan,indent=2)+'\n',newline='\n');print(json.dumps(dict(Files=len(rows),Bytes=sum(x['Bytes'] for x in rows.values()),Sha256=sha(p.read_bytes()))))
