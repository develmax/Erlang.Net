"""Preserve actual failed full run and exact private DLLs before any repair."""
from pathlib import Path
import hashlib,json,shutil
r=Path.cwd();a=r/'artifacts';main=a/'root-part328-main1';d=a/'part328-main1-failure';assert not d.exists()
j=lambda p:json.loads(p.read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert sha(main/'source-freeze.json')=='1C6D9F182962B2AA04831C4187255E43433EFFA9A336A20CCCB11AADA98C7E7F'
rows=j(main/'source-resource-closure.json')['Files'];assert len(rows)==2393
for row in rows:
 q=r/row['Path'];assert q.stat().st_size==row['Bytes'] and sha(q)==row['Sha256']
t=j(a/'tests.json');ex=j(main/'full-exit.json');assert ex['ExitCode']==1 and t['Passed']==6733 and t['Failed']==5
failed=[x for x in t['Tests'] if x['Status']!='Passed'];assert len(failed)==5
g=j(a/'undefined-lambda328-root-focus2/managed-run/actual.json');added={'oracle/compiled-module/'+x['Case'] for x in g['Cases']}|{'host_restored_local_fun_publication_and_lambda_dispatch'}
assert len(added)==9 and all(x['Status']=='Passed' for x in t['Tests'] if x['Name'] in added)
assert [x['Name'] for x in t['Tests'] if x['Name'] not in added]==[x['Name'] for x in j(r/'docs/validation/part-327/final-main/tests.json')['Tests']]
d.mkdir();copies=[(a/'tests.json',d/'tests.json'),(r/'docs/supported-mfas.json',d/'raw-supported-mfas.json')]+[(main/name,d/name) for name in ('source-freeze.json','source-resource-closure.json','full-attempt.json','full-exit.json','full-validation.log','registry-before-full.json')]
for source,target in copies:shutil.copyfile(source,target)
binary=r/'tests/Erlang.Tests/bin/Debug/net10.0';shutil.copytree(binary,d/'bin')
records=[{'Path':p.relative_to(d).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(d.rglob('*')) if p.is_file()]
receipt={'Status':'FAILED_ACTUAL_MAIN328_6733_OF_6738_FIVE_LEGACY_FACTORY_CONSTRUCTION_ASSERTIONS','Passed':6733,'Failed':5,'ExitCode':1,'FailedTests':failed,'AllNewNineTestsPassed':True,'AllPrevious6729OrderedNamesPreserved':True,'CurrentIntegrationAndPackageSuffixExecuted':False,'ExistingIntegrationArtifactsArePreviousRunNotCurrentProof':True,'PostFullNativeExecuted':False,'SourceClosureRowsVerifiedBeforeCapture':2393,'RawSourceStillMain1Exact':True,'SourceFreezeSha256':sha(main/'source-freeze.json'),'PrivateFailedDllSnapshot':True,'Files':records,'NoRetryPerformed':True}
(d/'capture.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'CaptureSha256':sha(d/'capture.json'),'PrivateFiles':len(records),'FailedTests':[x['Name'] for x in failed]}))
