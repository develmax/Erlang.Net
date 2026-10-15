from pathlib import Path
import hashlib,json
r=Path.cwd();d=r/'artifacts/root-part316-main1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,x:p.write_bytes((json.dumps(x,indent=2)+'\n').encode())
for source,dest in [(r/'artifacts/tests.json',d/'tests-first-failed.json'),(r/'docs/supported-mfas.json',d/'raw-supported-mfas-first-failed.json')]:assert dest.exists() and dest.read_bytes()==source.read_bytes()
t=read(d/'tests-first-failed.json');assert t['Passed']==6653 and t['Failed']==1
failed=[e for e in t['Tests'] if e['Status']!='Passed'];assert len(failed)==1 and failed[0]['Name']=='runtime/halt-dedicated-raw-process-numeric-errors-abort-and-real-dump'
for name in ['mfa/erlang:alloc_info/1','mfa/erlang:alloc_sizes/1']:assert any(e['Name']==name and e['Status']=='Passed' for e in t['Tests'])
registry=read(d/'raw-supported-mfas-first-failed.json');assert len(registry)==505 # Previous successful exported document, not produced by failed run.
actual_mfas=[e for e in t['Tests'] if e['Name'].startswith('mfa/')];assert len(actual_mfas)==507 and len({e['Name'] for e in actual_mfas})==507 and all(e['Status']=='Passed' for e in actual_mfas)
cor=r/'artifacts/allocator-public-sys-profile316-count-correction1';p=cor/'overlay/tests/Erlang.Tests/HaltSubprocessRegistrationCounts.cs';p.parent.mkdir(parents=True)
source=r/'tests/Erlang.Tests/HaltSubprocessRegistrationCounts.cs';before=source.read_bytes();assert b'DefaultCoreAndOtp = 505;' in before and b'WithOptionalIo = 507;' in before
after=before.replace(b'DefaultCoreAndOtp = 505;',b'DefaultCoreAndOtp = 507;').replace(b'WithOptionalIo = 507;',b'WithOptionalIo = 509;');p.write_bytes(after)
write(cor/'correction.json',{'Status':'SOURCE_COUNT_FIX_FOR_TWO_ACTUALLY_REGISTERED_AND_DIRECTLY_TESTED_PUBLIC_MFAS','BeforeSha256':h(before),'AfterSha256':h(after),'PreviousDefault':505,'ActualDefaultTestMfaRows':507,'SavedRegistryDocumentHistoricalRows':505,'FailedRunDidNotRegenerateRegistryDocument':True,'OptionalIoExistingAdds':2,'UpdatedOptionalIo':509,'FirstFullPassed':6653,'FirstFullFailed':1,'FirstReportSha256':h((d/'tests-first-failed.json').read_bytes()),'FailedCase':failed[0],'ProductionChanged':False,'OriginalFixtureExpectationsChanged':False,'OnlyChangedPath':'tests/Erlang.Tests/HaltSubprocessRegistrationCounts.cs'})
source.write_bytes(after)
out=r/'artifacts/root-part316-main2';out.mkdir();f=read(d/'source-freeze.json');cl=read(d/'source-resource-closure.json');name='tests/Erlang.Tests/HaltSubprocessRegistrationCounts.cs'
for e in cl['Files']:
 if e['Path']==name:e.update(Bytes=len(after),Sha256=h(after))
f['Files'].append({'Path':name,'Bytes':len(after),'Sha256':h(after),'Added':False,'ParentSha256':h(before),'CountFixAfterFirstFailure':True});f['Files'].sort(key=lambda e:e['Path']);f.update(SourcePaths=26,CSharpPaths=19,FirstFullFailure={'SourceFreezeSha256':h((d/'source-freeze.json').read_bytes()),'TestsSha256':h((d/'tests-first-failed.json').read_bytes()),'Passed':6653,'Failed':1},CountCorrectionSha256=h((cor/'correction.json').read_bytes()))
write(out/'source-resource-closure.json',cl);f['SourceClosureSha256']=f['FinalSourceClosureSha256']=h((out/'source-resource-closure.json').read_bytes());write(out/'source-freeze.json',f)
for e in cl['Files']:assert h((r/e['Path']).read_bytes())==e['Sha256'],e['Path']
print(json.dumps({'FreezeSha256':h((out/'source-freeze.json').read_bytes()),'ClosureSha256':f['FinalSourceClosureSha256'],'SourcePaths':26,'CSharpPaths':19}))
