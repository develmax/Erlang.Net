"""Passive actual same-head356 CI; package payloads are absent from the CI artifact."""
import collections,json,re
import common356 as m

c,e,run,job,artifact,log=m.transport('ci')
v=m.blobs(['docs/validation/part-356/tests.json','docs/validation/part-356/integration-results.json','docs/validation/part-356/full-passive-report.json','docs/validation/part-356/composition-plan.json','docs/validation/part-356/registry-before.json','docs/supported-mfas.json','docs/mfa-compatibility.json','docs/module-readiness.json'])
get=lambda name:json.loads(v[name])
remote=m.J(e/'tests.json');names=[x['Name'] for x in remote['Tests']]
assert remote['Passed']==len(names)==len(set(names))==7345 and remote['Failed']==0 and all(x['Status']=='Passed' for x in remote['Tests'])
local=get('docs/validation/part-356/tests.json');assert names==[x['Name'] for x in local['Tests']]
previous=json.loads(m.blobs(['docs/validation/part-354/full/raw/artifacts/tests.json'])['docs/validation/part-354/full/raw/artifacts/tests.json']);assert previous['Passed']==7343 and names[:7343]==[x['Name'] for x in previous['Tests']]
peer=get('docs/validation/part-356/full-passive-report.json');assert peer['FullAccepted'] and peer['Passed']==7345 and peer['Failed']==0 and peer['Old7343ExactOrderedNamesPrefix'] and len(peer['Packages'])==2 and all(x['CrcAndSafeUniquePathsVerified'] for x in peer['Packages'])
expected=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed')
assert m.J(e/'integration-results.json')==get('docs/validation/part-356/integration-results.json')==peer['IntegrationResults']==expected
assert v['docs/supported-mfas.json']==v['docs/validation/part-356/registry-before.json']
ledger=get('docs/mfa-compatibility.json');counts=dict(collections.Counter(x['Status'] for x in ledger['Entries']));assert counts=={'Verified compatible':16,'Known differences':30,'Needs verification':541} and len(ledger['Entries'])==587
guards=ledger['VerifiedSourceFiles'];assert len(guards)==len({x['Path'] for x in guards})==127
source=m.blobs([x['Path'] for x in guards])
for row in guards:assert m.D(source[row['Path']])==row['Sha256'],row['Path']
evidence=m.blobs({'docs/'+x['Path'] for entry in ledger['Entries'] for x in entry['Evidence']})
for entry in ledger['Entries']:
    for row in entry['Evidence']:assert set(row)=={'Kind','Path','Sha256','Scope'} and row['Scope'].strip() and m.D(evidence['docs/'+row['Path']])==row['Sha256']
ready=get('docs/module-readiness.json');assert (ready['RegisteredMfas'],ready['LocalPassedTests'],ready['VerifiedCompatibleMfas'],ready['KnownDifferenceMfas'],ready['NeedsVerificationMfas'])==(587,7345,16,30,541) and ready['VerifiedSourceFiles']==guards
plan=get('docs/validation/part-356/composition-plan.json');cs=[x for x in plan['StableClosure'] if x['Path'].endswith('.cs') and x['Path'] in plan['OwnedPaths']];published=m.blobs([x['Path'] for x in cs])
for row in cs:assert m.D(published[row['Path']])==row['Sha256'],row['Path']
packages=re.findall(r"Successfully created package '([^'\r\n]+\.nupkg)'",log);assert len(packages)==2 and {x.replace('\\','/').rsplit('/',1)[-1] for x in packages}=={'Erlang.Net.CSharp.0.1.0.nupkg','Erlang.Net.Tool.0.1.0.nupkg'}
nativeplans=[]
for path,workflow,key,n in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1300),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1325)]:
    data=m.blobs([path,workflow]);body=json.loads(data[path]);assert b'\r' not in data[path] and not data[path].startswith(b'\xef\xbb\xbf') and len(body['Files'])==n
    payload=m.blobs([x['Path'] for x in body['Files']])
    for row in body['Files']:
        assert row['HashMode'] in ('TextLF','BinaryExact');raw=payload[row['Path']];raw=raw.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else raw;assert len(raw)==row['Bytes'] and m.D(raw)==row['Sha256']
    assert re.findall(key.encode()+rb': ([A-F0-9]{64})',data[workflow])==[m.D(data[path]).encode()];nativeplans.append(dict(Path=path,Rows=n,Sha256=m.D(data[path])))
m.W(m.Path(__file__).parent/'ci-report.json',dict(Status='PASS_PASSIVE_SAME_HEAD356_CI7345_ELEVEN_INTEGRATIONS',Head=m.HEAD,Run=run['id'],Job=job['id'],Artifact=artifact['id'],Passed=7345,Failed=0,All7343PreviousOrderedNamesPreserved=True,NewTestNames=names[7343:],Integrations=11,PackageCreationLogEntries=packages,RemotePackagePayloadRehashed=False,RemotePackagePayloadAbsentFromArtifact=True,AcceptedSeparateLocalPackageReceipts=2,RuntimeSourceGuards=127,OwnedCSharpRawSourceBindings=len(cs),CompatibilityCounts=counts,NativePlans=nativeplans,ActualTestsSha256=m.H(e/'tests.json'),CaptureManifestSha256=m.H(c/'download-manifest.json'),RawJobLogSha256=m.H(c/('job-'+str(job['id'])+'.log')),SdkOrNativeExecuted=False,NetworkUsed=False,ReaderSha256=m.H(m.Path(__file__))))
print('PASS actual CI7345 / eleven integrations / two package creation log entries; package payloads absent.')
