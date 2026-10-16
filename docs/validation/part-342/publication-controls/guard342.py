"""DRAFT independent actual342 full guard. Root/producer must peer the final input adapter before execution."""
import argparse,sys
from pathlib import Path
sys.dont_write_bytecode=True
from common342 import *
p=argparse.ArgumentParser()
for name in ('candidate','full-dir','main','input-contract','baseline','binding-review','output'):p.add_argument('--'+name,type=Path,required=True)
for name in ('input-contract-sha256','baseline-sha256','binding-review-sha256'):p.add_argument('--'+name,required=True)
a=p.parse_args();c=a.candidate.resolve(strict=True);d=a.full_dir.resolve(strict=True);main=a.main.resolve(strict=True)
assert c!=main and not a.output.exists() and sha(a.input_contract)==a.input_contract_sha256.upper() and sha(a.baseline)==a.baseline_sha256.upper()
b=read(a.baseline);i=read(a.input_contract);assert b['ParentHead']==i['ParentHead']==PARENT
# Actual producer file names and SHA values are supplied only after source/full receipts exist.
assert set(i['Inputs'])=={'SourceFreeze','SourceClosure','PackageResources','FullExit','Tests','Integrations','Registry','PackageOutputs','AddedTests'}
paths={}
for name,r in i['Inputs'].items():
 q=checked(d,r['Path']);assert sha(q)==r['Sha256'];paths[name]=q
producer=read(checked(d,i['ProducerGuard']['Path']));assert sha(d/i['ProducerGuard']['Path'])==i['ProducerGuard']['Sha256']=='1BACFA5518D2BE5A72F97976AE7C0D490DC3EE0A1AF584BD5612F81EF58B56A7'
assert producer['Status']=='PASS_ACTUAL_ISOLATED342_FULL' and producer['ParentHead']==PARENT and producer['SingleUninterruptedFullPass'] is True
for field,name in (('SourceFreezeSha256','SourceFreeze'),('SourceClosureSha256','SourceClosure'),('FullExitSha256','FullExit'),('TestReportSha256','Tests'),('PackageResourceManifestSha256','PackageResources'),('PackageOutputsManifestSha256','PackageOutputs'),('RawRegistrySha256','Registry')):assert producer[field]==sha(paths[name])
assert producer['InitialLaunchPreSdkFailure'] is True and producer['PreviousSandboxFullFailed'] is True
for history in i['History']:assert sha(checked(main,history['Path']))==history['Sha256']==producer[history['ProducerField']]
first=read(main/'artifacts/runtime342-full-actual1/full-exit.json');second=read(main/'artifacts/runtime342-full-actual2/full-exit.json');assert first['ExitCode']==second['ExitCode']==1 and first['Settled'] and second['Settled'] and not first['TimedOut'] and not second['TimedOut']
assert not (main/'artifacts/runtime342-full-actual1/tests.json').exists()
failed=read(main/'artifacts/runtime342-full-actual2/tests.json');assert failed['Passed']==7184 and failed['Failed']==16 and len(failed['Tests'])==7200
f=read(paths['SourceFreeze']);assert f['ParentHead']==PARENT
owned=exact(c,paths['SourceFreeze']);closure=exact(c,paths['SourceClosure']);assert owned<=closure
assert len(rows(paths['PackageResources']))==6;exact(c,paths['PackageResources'])
# Frozen full sources/closure must not include generated mutable registry metadata.
assert 'docs/supported-mfas.json' not in closure
exit=read(paths['FullExit']);assert exit['ExitCode']==0 and exit['Settled'] is True and exit['TimedOut'] is False
assert read(paths['Integrations'])==INTEGRATIONS
packages=rows(paths['PackageOutputs']);assert len(packages)==2 and len({x['Path'] for x in packages})==2
assert all(x['Path'].endswith('.nupkg') and x['Bytes']>0 for x in packages);exact(c,paths['PackageOutputs'])
t=read(paths['Tests']);names=[x['Name'] for x in t['Tests']];assert t['Failed']==0 and t['Passed']==len(names)==len(set(names)) and all(x['Status']=='Passed' for x in t['Tests'])
previous_path=checked(main,b['Published340Tests']);assert sha(previous_path)==b['Published340TestsSha256'];previous=read(previous_path)
previous341=read(checked(main,b['Actual341TestsPath']));ci341=read(checked(main,b['Actual341CiGuardPath']))
assert sha(main/b['Actual341TestsPath'])==b['Actual341TestsSha256']==ci341['TestReportSha256'] and sha(main/b['Actual341CiGuardPath'])==b['Actual341CiGuardSha256']
assert ci341['Head']==PARENT and ci341['Passed']==previous341['Passed']==7194 and ci341['Failed']==previous341['Failed']==0 and ci341['Integrations']==11
old=[x['Name'] for x in previous341['Tests']];assert all(x['Status']=='Passed' for x in previous341['Tests'])
assert old==[b['Reporting341AddedTest']]+[x['Name'] for x in previous['Tests']];assert previous['Passed']==7193 and previous['Failed']==0 and len(old)==len(set(old))==7194
assert [n for n in names if n in set(old)]==old
added=read(paths['AddedTests']);assert added['SourceFreezeSha256']==sha(paths['SourceFreeze']) and len(added['Names'])==len(set(added['Names']))
assert [n for n in names if n not in set(old)]==added['Names']==producer['AddedTestNames'];assert producer['Passed']==t['Passed'] and producer['Failed']==0;assert set(added['Names']).isdisjoint(old)
base_registry=read(checked(main,b['RegistryPath']));assert sha(main/b['RegistryPath'])==b['RegistrySha256'];actual=read(paths['Registry'])
assert len(actual)==len({key(x) for x in actual})==len(base_registry)==587
assert [key(x) for x in actual]==[key(x) for x in base_registry]
for oldrow,newrow in zip(base_registry,actual):assert {k:v for k,v in oldrow.items() if k!='Limits'}=={k:v for k,v in newrow.items() if k!='Limits'}
assert len({x['Module'] for x in actual})==14 and sum(x['Module']=='proc_lib' for x in actual)==39
l=ledger(c,b);assert {key(x) for x in l['Entries']}=={key(x) for x in actual}
# Every actual current reporting delta must be declared by independent reviewed metadata binding supplement.
assert sha(a.binding_review)==a.binding_review_sha256.upper()
review=read(a.binding_review);assert review['Passed'] is True and review['SourceFreezeSha256']==sha(paths['SourceFreeze'])
allowed=set(review['Protected341ChangedPaths']);changed=[]
for r in b['ProtectedReporting341Files']:
 assert sha(checked(main,r['Path']))==r['Sha256']
 # Generated registry's Limits are restored by finalizer after full; non-Limits identity already checked.
 if r['Path']=='docs/supported-mfas.json':continue
 candidate_path=c/r['Path']
 if candidate_path.exists():effective=checked(c,r['Path'])
 else:
  assert r['Path'] not in owned
  effective=checked(main,r['Path'])
 if sha(effective)!=r['Sha256']:
  changed.append(r['Path'])
  if r['Path'] in review['EolOnlyProtectedPaths']:assert effective.read_bytes().replace(b'\r\n',b'\n')==checked(main,r['Path']).read_bytes().replace(b'\r\n',b'\n')
assert set(changed)==allowed
save(a.output,dict(Status='PASS_INDEPENDENT_ACTUAL342_FULL',Passed=t['Passed'],Failed=0,AddedTestNames=added['Names'],AllPrevious7194OrderedNamesPreserved=True,Previous341ActualCiGuardSha256=b['Actual341CiGuardSha256'],Registry=587,ProcLibMfas=39,ImplementedModules=14,CompatibilityCounts=b['CompatibilityCounts'],CompletedFullExit0=True,ProducerCompletionGuardSha256=i['ProducerGuard']['Sha256'],InitialLaunchPreSdkFailurePreserved=True,InitialLaunchExit1Preserved=True,SandboxFull7184Passed16FailedPreserved=True,FailureHistoryBindings=i['History'],BindingUpdateReviewSha256=sha(a.binding_review),SingleUninterruptedFullPass=True,IntegrationResults=INTEGRATIONS,SourceFreezeSha256=sha(paths['SourceFreeze']),SourceClosureSha256=sha(paths['SourceClosure']),SourceClosureRows=len(closure),OwnedPaths=len(owned),PackageResources6=True,PackageOutputs2=True,Reporting341ChangedPaths=changed,InputContractSha256=sha(a.input_contract),BaselineSha256=sha(a.baseline),MainEdited=False))
