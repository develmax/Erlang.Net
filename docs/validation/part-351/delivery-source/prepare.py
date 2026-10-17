from pathlib import Path
import json,hashlib
R=Path.cwd();A=R/'artifacts';O=A/'part351-docs-delivery-plan2';read=lambda p:json.loads(p.read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest().upper();rows={}
def add(p,target):
 b=p.read_bytes();row=dict(SourcePath=p.relative_to(R).as_posix(),TargetPath=target,Bytes=len(b),Sha256=sha(b))
 if target in rows:assert rows[target]['Sha256']==row['Sha256']
 else:rows[target]=row
P='docs/validation/part-351/';old=A/'part351-docs-delivery-plan/proof-copy-plan.json';original=read(old)
for row in original['Files']:
 p=R/row['SourcePath'];assert p.stat().st_size==row['Bytes'] and sha(p.read_bytes())==row['Sha256'];target=row['TargetPath']
 if target.startswith(P+'tested-source/'):target=target.replace(P+'tested-source/',P+'source-history/first-full/',1)
 add(p,target)
for n in ['proof-copy-plan.json','source-freeze.json','changelog-draft.md','prepare.py','finalize.py']:add(A/'part351-docs-delivery-plan'/n,P+'delivery-history/first-conditional/'+n)
S=A/'spawn-priority351-full-source4';freeze=read(S/'source-freeze.json');assert len(freeze['Files'])==49
for row in freeze['Files']:
 p=S/'source'/row['Path'];assert p.stat().st_size==row['Bytes'] and sha(p.read_bytes())==row['Sha256'];add(p,P+'tested-source/'+row['Path'])
folders=['spawn-priority351-full-actual1','spawn-priority351-full-failure-independent','spawn-priority351-host-corrections-proposal1','spawn-priority351-port-retired-proposal','spawn-priority351-port-retired-independent','spawn-priority351-corrections-focus-actual1','spawn-priority351-corrections-focused-independent','spawn-priority351-corrected-preparation','spawn-priority351-corrected-preflight-independent','spawn-priority351-full-source3','spawn-priority351-full-source4','spawn-priority351-port-restricted','spawn-priority351-port-approved','priority351-corrected-full-preparation-plan','priority351-corrected-metadata-proposal','priority351-corrected-metadata-proposal2']
for name in folders:
 for p in sorted((A/name).iterdir()):
  if p.is_file() and p.suffix in ['.json','.py','.log','.txt','.diff']:add(p,P+'correction-history/'+name+'/'+p.name)
 # Only narrow source proposals contain source subtrees; no runtime build/layout snapshots.
 if name in ['spawn-priority351-host-corrections-proposal1','spawn-priority351-port-retired-proposal']:
  for p in sorted((A/name/'source').rglob('*.cs')):add(p,P+'correction-history/'+name+'/source/'+p.relative_to(A/name/'source').as_posix())
for row in read(A/'priority351-corrected-metadata-proposal2/copy-plan.json')['Files']:
 if row['TargetPath'].startswith(P):
  p=R/row['SourcePath'];assert sha(p.read_bytes())==row['Sha256'];add(p,row['TargetPath'])
for p in sorted(A.glob('root-*351*.py')):add(p,P+'root-preparation/'+p.name)
# Current reporting-only350 CI is a Root audit, not an independently authored CI review.
d=A/'reporting350-ci-add05/terminal-ci';manifest=read(d/'download-manifest.json');included=[];omitted=[]
for row in manifest:
 p=d/row['Path'];assert p.stat().st_size==row['Bytes'] and sha(p.read_bytes())==row['Sha256']
 if p.suffix in ['.json','.log','.txt']:add(p,P+'reporting350-ci/raw/'+row['Path'].replace('\\','/'));included.append(row)
 else:omitted.append(row)
for n in ['download-manifest.json','root-audit.json']:add(d/n,P+'reporting350-ci/'+n)
subset=O/'reporting350-ci-subset.json';subset.write_text(json.dumps(dict(Status='DECLARED_TEXT_JSON_SUBSET_WITH_ROOT_AUDIT_NOT_INDEPENDENT_CI_CLAIM',Included=included,Omitted=omitted,ApiZipOmittedWithHashRetained=True),indent=2)+'\n',newline='\n');add(subset,P+'reporting350-ci/subset.json')
plan=dict(Status='CONDITIONAL_CORRECTED351_DOCS_AND_PROOF_FULL2_PENDING',ParentHead='add05fc61d8c6f5f024ffcdad1ea19aaea4e3725',SourceFreezeSha256=sha((S/'source-freeze.json').read_bytes()),SourceClosureSha256=sha((S/'source-resource-closure.json').read_bytes()),Files=list(rows.values()),FilesCount=len(rows),Bytes=sum(x['Bytes'] for x in rows.values()),ExpectedArithmeticOnly=dict(Tests=7297,Integrations=11,Packages=2,BoundedChildren=12,Owned=49,Closure=3006),CurrentCompatibilityCounts=[10,24,553],RuntimeSourceBindings=74,Old319InventoryRetainedAsHistory=True,CurrentSourceSnapshots49=True,OriginalFailedFullPreserved=True,CurrentFocus59Passed=True,Full2SuccessClaimed=False,FutureSixPredicatePromotionsExcluded=True,MainChanged=False,CandidateChanged=False,SdkExecuted=False,Network=False)
p=O/'proof-copy-plan.json';assert not p.exists();p.write_text(json.dumps(plan,indent=2)+'\n',newline='\n');print(json.dumps(dict(Files=len(rows),Bytes=plan['Bytes'],PlanSha256=sha(p.read_bytes()))))
