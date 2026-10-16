"""Draft passive343 full guard. Execute only after terminal actual producer and external binding review exist."""
from pathlib import Path
import argparse,sys
sys.dont_write_bytecode=True
from common343 import *
p=argparse.ArgumentParser()
for n in ('candidate','main','full-dir','baseline','contract','binding-review','output'):p.add_argument('--'+n,type=Path,required=True)
for n in ('baseline-sha256','contract-sha256','binding-review-sha256'):p.add_argument('--'+n,required=True)
a=p.parse_args();c=a.candidate.resolve(strict=True);main=a.main.resolve(strict=True);d=a.full_dir.resolve(strict=True)
assert c!=main and not a.output.exists()
for name in ('baseline','contract','binding_review'):assert sha(getattr(a,name))==getattr(a,name+'_sha256').upper()
b=read(a.baseline);i=read(a.contract);assert b['ParentHead']==i['ParentHead']==PARENT
assert set(i['Inputs'])=={'ProducerGuard','SourceFreeze','SourceClosure','PackageResources','FullExit','Tests','Integrations','Registry','PackageOutputs','AddedTests','Preflight'}
paths={}
for name,row in i['Inputs'].items():
 path=checked(d,row['Path']);assert sha(path)==row['Sha256'];paths[name]=path
g=read(paths['ProducerGuard']);assert g['Status']=='PASS_ACTUAL_ISOLATED343_FULL' and g['SingleUninterruptedFullPass'] is True and g['ParentHead']==PARENT
assert i['FailureHistory']==[] # Canonical firstfull expected. If actual producer records a failure, make explicit reviewed adapter revision.
assert not any('Failure' in name or 'PreviousLaunch' in name or 'SandboxFull' in name for name in g)
for field,name in (('SourceFreezeSha256','SourceFreeze'),('SourceClosureSha256','SourceClosure'),('PackageResourceManifestSha256','PackageResources'),('FullExitSha256','FullExit'),('TestReportSha256','Tests'),('RawRegistrySha256','Registry'),('PackageOutputsManifestSha256','PackageOutputs'),('PreflightSha256','Preflight')):assert g[field]==sha(paths[name])
f=read(paths['SourceFreeze']);assert f['ParentHead']==PARENT
owned=exact(c,paths['SourceFreeze']);closure=exact(c,paths['SourceClosure']);assert len(owned)==43 and len(closure)==2739 and owned<=closure
assert g['OwnedPaths']==len(owned) and g['SourceClosureRows']==len(closure) and 'docs/supported-mfas.json' not in closure
assert len(rows(paths['PackageResources']))==6;exact(c,paths['PackageResources'])
e=read(paths['FullExit']);assert e['ExitCode']==0 and e['Settled'] is True and e['TimedOut'] is False
integrations=read(paths['Integrations']);assert integrations==g['IntegrationResults']==INTEGRATIONS
packages=rows(paths['PackageOutputs']);assert len(packages)==len({x['Path'] for x in packages})==2 and all(x['Path'].endswith('.nupkg') and x['Bytes']>0 for x in packages);exact(c,paths['PackageOutputs'])
t=read(paths['Tests']);names=[x['Name'] for x in t['Tests']];assert t['Passed']==len(names)==len(set(names)) and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
oldpath=checked(main,b['PreviousTestsPath']);assert sha(oldpath)==b['PreviousTestsSha256']==g['Previous7200TestReportSha256'];oldreport=read(oldpath);old=[x['Name'] for x in oldreport['Tests']];assert oldreport['Passed']==len(old)==7200 and oldreport['Failed']==0
assert [name for name in names if name in set(old)]==old
added=read(paths['AddedTests']);assert added['SourceFreezeSha256']==sha(paths['SourceFreeze']);assert len(added['Names'])==len(set(added['Names']))==24
assert [name for name in names if name not in set(old)]==added['Names']==g['AddedTestNames'];assert t['Passed']==g['Passed']==len(old)+len(added['Names']) and g['Failed']==0
basepath=checked(main,b['RegistryPath']);assert sha(basepath)==b['RegistrySha256'];before=read(basepath);raw=read(paths['Registry']);assert len(before)==len(raw)==len({key(x) for x in raw})==587 and [key(x) for x in before]==[key(x) for x in raw]
for x,y in zip(before,raw):assert {k:v for k,v in x.items() if k!='Limits'}=={k:v for k,v in y.items() if k!='Limits'}
assert len({x['Module'] for x in raw})==14 and sum(x['Module']=='proc_lib' for x in raw)==39
assert g['Registry']==587 and g['ProcLibMfas']==39 and g['ImplementedModules']==14
oldledgerpath=checked(main,b['LedgerPath']);assert sha(oldledgerpath)==b['LedgerSha256'];oldledger=read(oldledgerpath);current=ledger(c,b);assert g['CompatibilityCounts']==b['CompatibilityCounts']
assert [key(x) for x in oldledger['Entries']]==[key(x) for x in current['Entries']]
for x,y in zip(oldledger['Entries'],current['Entries']):
 assert x['Evidence']==y['Evidence'][:len(x['Evidence'])]
 assert all(ev['Kind']=='SourceReview' for ev in y['Evidence'][len(x['Evidence']):])
 if key(x)==('erlang','length',1):assert x['Status']=='Known differences' and y['Status']=='Needs verification' and y['Limits']
 else:assert all(x[name]==y[name] for name in ('Status','Scope','Limits'))
for x in current['Entries']:
 for ev in x['Evidence']:assert sha(checked(c/'docs',ev['Path']))==ev['Sha256']
review=read(a.binding_review);assert review['Passed'] is True and review['SourceFreezeSha256']==sha(paths['SourceFreeze']);changed=[]
for row in b['ProtectedBaselineFiles']:
 assert sha(checked(main,row['Path']))==row['Sha256']
 if row['Path']=='docs/supported-mfas.json':continue
 candidate=c/row['Path']
 if candidate.exists():effective=checked(c,row['Path'])
 else:assert row['Path'] not in owned;effective=checked(main,row['Path'])
 if sha(effective)!=row['Sha256']:
  changed.append(row['Path'])
  if row['Path'] in review['EolOnlyProtectedPaths']:assert effective.read_bytes().replace(b'\r\n',b'\n')==checked(main,row['Path']).read_bytes().replace(b'\r\n',b'\n')
assert set(changed)==set(review['ProtectedBaselineChangedPaths'])
assert sha(checked(main,b['LengthCorrectionReviewPath']))==b['LengthCorrectionReviewSha256']
save(a.output,dict(Status='PASS_INDEPENDENT_ACTUAL343_FULL',ParentHead=PARENT,Passed=t['Passed'],Failed=0,AddedTestNames=added['Names'],AllPrevious7200OrderedNamesPreserved=True,Registry=587,ProcLibMfas=39,ImplementedModules=14,CompatibilityCounts=b['CompatibilityCounts'],OnlyLengthClassificationMovedToPending=True,Other586StatusScopeLimitsPreserved=True,CompletedFullExit0=True,SingleUninterruptedFullPass=True,ProducerCompletionGuardSha256=sha(paths['ProducerGuard']),SourceFreezeSha256=sha(paths['SourceFreeze']),SourceClosureSha256=sha(paths['SourceClosure']),SourceClosureRows=len(closure),OwnedPaths=len(owned),PackageResources6=True,PackageOutputs2=True,IntegrationResults=integrations,InputContractSha256=sha(a.contract),BaselineSha256=sha(a.baseline),BindingReviewSha256=sha(a.binding_review),ProtectedBaselineChangedPaths=changed,Historical342LedgerUnchanged=True,MainEdited=False,SdkExecutedByReviewer=False,OriginalComparisonClaimed=False))
