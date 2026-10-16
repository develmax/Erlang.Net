from pathlib import Path
import hashlib,json,shutil
r=Path.cwd();a=r/'artifacts';b=r/'docs/validation/part-340';d=a/'connected-remote-completion-full-actual4'
j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
ready=j(a/'root-part340-readiness/module-readiness.json')
assert (ready['LocalPassedTests'],ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'],ready['ImplementedApiModules'])==(7193,587,576,11,14)
modules={x['Name']:x for x in ready['Modules']}
assert [(modules[m]['RegisteredMfas'],modules[m]['ReferenceDeclarations']) for m in ['proc_lib','rpc','erpc']]==[(39,39),(2,35),(3,33)]
for name in ['MODULE_READINESS.md','module-readiness.json']:
 shutil.copyfile(a/'root-part340-readiness'/name,r/'docs'/name);shutil.copyfile(r/'docs'/name,b/name)
shutil.copyfile(r/'docs/supported-mfas.json',b/'final-candidate/supported-mfas.json')
source=j(d/'source-freeze.json')['Files']
for row in source:
 p=r/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256']
 out=b/'source'/row['Path'];out.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,out)
suffixes={'.json','.txt','.log','.cs','.csproj','.py','.ps1','.erl','.md'}
def archive(name,label):
 folder=a/name;assert folder.is_dir(),folder
 for p in sorted(folder.rglob('*')):
  if p.is_file() and not {'bin','obj','__pycache__'}.intersection(p.parts) and p.suffix in suffixes:
   out=b/label/p.relative_to(folder);out.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,out)
for name in ['connected-negative-fixtures340-proposal','connected-negative-fixtures340-proposal2','connected-negative-fixtures340-actual1','connected-completion-focused-result','connected-native-contract-host-actual1','connected-native-contract-host-actual2','connected-native-contract-host-proposal','connected-native-contract-host-source-peer','connected-remote-completion-actual1','connected-remote-rpc-focused-result','connected-remote-rpc-actual1','connected-remote-rpc-actual2','connected-remote-rpc-actual3','connected-remote-rpc-actual4','connected-remote-spawn-alias-actual1','connected-remote-spawn-alias-actual2','connected-remote-spawn-alias-independent','connected-remote-rpc-independent','runtime-nodes339-proposal','erlang-apply339-boundary-proposal','connected-remote-rpc-permanent-proposal','connected-remote-rpc-permanent-independent','connected-remote-fun-scope-independent','part340-delivery-helpers-draft']:
 archive(name,'development/'+name)
for name in ['connected-remote-completion-full','connected-remote-completion-full-actual2','connected-remote-completion-full-actual3']:
 folder=a/name
 for p in folder.iterdir():
  if p.is_file() and p.suffix in suffixes:
   out=b/'previous-full-launches'/name/p.name;out.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,out)
rootnames=['root-review340-full.py','root-part340-full-independent-guard.json','root-document340.py','root-finalize340.py','root-part340-documentation.json','root-part340-finalized.json','root-part340-promotion.json','root-prepare340-promotion3.py','root-part340-promotion-plan3.json','root-peer340-negatives.py','root-340-negative-fixtures-source-review.json','root-review340-plans.py','root-340-plans-source-review.json','root-review340-catalog.py','root-340-catalog-source-review.json','root-dispatch340.ps1','part340-development-notes.md']
for name in rootnames:
 p=a/name;assert p.is_file();out=b/'review'/name;out.parent.mkdir(exist_ok=True);shutil.copyfile(p,out)
for path in ['artifacts/root-part340-promotion.json','artifacts/root-part340-finalized.json']:
 assert j(r/path)['FullGuardSha256']==h(d/'completion-guard.json')
rows=[dict(Path=p.relative_to(r).as_posix(),Bytes=p.stat().st_size,Sha256=h(p)) for p in sorted(b.rglob('*')) if p.is_file() and p.name not in ['evidence-index.json','evidence-receipt.json']]
index=b/'evidence-index.json';assert not index.exists();index.write_text(json.dumps(rows,indent=2)+'\n')
receipt=dict(Status='COMPLETED340_LOCAL_FULL_AND_PRIOR338_339_AUDITS',Passed=7193,Failed=0,Integrations=11,Registry=587,ProcLib=39,ImplementedModules=14,Previous338Original=6216,Previous338Fd=8,Native339Observed=12,ConnectedSdkHostContracts=23,FullGuardSha256=h(d/'completion-guard.json'),IndexSha256=h(index),Files=len(rows),Bytes=sum(x['Bytes'] for x in rows),Current340OriginalVerified=False,Current340CiVerified=False,FreshReceiverAnonymousFunctionGapExplicit=True)
(b/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
metadata=['AGENTS.md','CHANGELOG.md','docs/PROGRESS.md','docs/COMPATIBILITY.md','docs/PROC_LIB_COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/semantic-differences.md','docs/DECISIONS.md','docs/dependency-decisions.md','docs/DEPENDENCY_PRIORITIES.md','docs/NEXT_STEPS.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md','docs/changelog/0053.md','docs/supported-mfas.json']
paths=sorted([x['Path'] for x in source]+metadata+[p.relative_to(r).as_posix() for p in b.rglob('*') if p.is_file()])
assert len(paths)==len(set(paths)) and not any('LICENSE.local' in x or '__pycache__' in x for x in paths)
plan=dict(ParentHead='36e6da6dfc2696adce6bff8808ca96bbd46596b6',Paths=paths,Metadata=metadata,Files=[dict(Path=p,Bytes=(r/p).stat().st_size,Sha256=h(r/p)) for p in paths])
out=a/'root-part340-stage-plan.json';assert not out.exists();out.write_text(json.dumps(plan,indent=2)+'\n')
print(json.dumps(dict(**receipt,StagePaths=len(paths),StagePlanSha256=h(out))))
