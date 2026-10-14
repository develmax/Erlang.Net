from pathlib import Path
import json,hashlib,re,zipfile,subprocess
r=Path('.');a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda b:hashlib.sha256(b).hexdigest().upper()
head='f26e1a15320553eb9d725764ec2260f3c262e1da'
m=j(a/'part-270-differential-metadata.json');p=j(a/'part-270-ordinary-latest-provenance.json')
d=j(a/'part-270-differential/differential.json');old=j(r/'docs/validation/part-270/external269/part-269-differential/differential.json')
assert m['Run']['head_sha']==p['Run']['head_sha']==head
assert m['Run']['id']==p['Run']['id']==38596894388 and p['Job']['id']==115965831492
assert m['Run']['conclusion']==p['Job']['conclusion']=='success' and p['Job']['labels']==['ubuntu-latest']
assert all(x['status']=='completed' and x['conclusion']=='success' for x in p['Job']['steps'])
assert d['Complete'] and d['VersionVerified'] and not d['InvestigationOnly']
assert d['Planned']==d['Executed']==d['Passed']==5785 and d['Failed']==0 and d['InfrastructureError'] is None
assert not d['ReferenceCoverageComplete'] and d['OriginalOtpDefectExclusions']==old['OriginalOtpDefectExclusions'] and len(d['OriginalOtpDefectExclusions'])==6
assert d['Results']==old['Results'] and len(d['Results'])==778
assert d['DiagnosticResults']==old['DiagnosticResults'] and len(d['DiagnosticResults'])==159
modules=d['ModuleResults'];assert len(modules)==4848 and len(old['ModuleResults'])==4837
differences=[]
for index,(v,b) in enumerate(zip(modules[:4837],old['ModuleResults'])):
 if v==b:continue
 delta={k:{'Actual':v.get(k),'Historical':b.get(k)} for k in v.keys()|b.keys() if v.get(k)!=b.get(k)}
 assert v['Name']==b['Name']=='erlang-term-display-map'
 assert set(delta)<={'NativeAndSdkStandardOutputExactlyMatch','NativeStandardOutputHex'}
 assert v['Passed'] and b['Passed'] and v['StandardStreamsMatch'] and b['StandardStreamsMatch']
 assert v['ExpectedStandardOutputAlternatives']==b['ExpectedStandardOutputAlternatives']
 allowed=[b['ExpectedStandardOutputHex']]+b['ExpectedStandardOutputAlternatives']
 assert v['NativeStandardOutputHex'] in allowed and b['NativeStandardOutputHex'] in allowed
 assert v['NativeAndSdkStandardOutputExactlyMatch']==(v['NativeStandardOutputHex']==v['SdkStandardOutputHex'])
 differences.append({'Index':index,'Delta':delta,'Current':v,'Historical':b})
assert len(differences)<=1
source=subprocess.check_output(['git','show',head+':tools/Erlang.Differential/IdentifierOrderCases.cs']).decode()
declared=re.findall(r'"(identifier-order-[^"]+)"',source)
assert len(declared)==len(set(declared))==11
assert [v['Name'] for v in modules[4837:]]==declared
assert all(v['Passed'] and v['Actual']==v['Expected']==v['FixtureExpected']=='{ok,done}' for v in modules[4837:])
for row in modules+d['DiagnosticResults']:
 assert row['Passed'] and sha(row['Source'].encode())==row['SourceSha256']
fd=j(a/'part-270-differential/fd-differential.json')
assert fd['Passed']==8 and fd['Failed']==0 and fd['Results']==j(r/'docs/validation/part-270/external269/part-269-differential/fd-differential.json')['Results']
assert m['Artifact']['workflow_run']['id']==38596894388 and m['Artifact']['workflow_run']['head_sha']==head and not m['Artifact']['expired']
assert sha((a/'part-270-differential.zip').read_bytes())==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-270-differential.zip') as z:
 for name in z.namelist():
  if not name.endswith('/'):assert z.read(name)==(a/'part-270-differential'/name).read_bytes()
log=(a/'part-270-ordinary-job.log').read_bytes();digest=j(a/'part-270-ordinary-job.log.digest.json')
assert sha(log)==digest['SHA256'] and b'ad05823719d77c8faee87348ea39513d4e2f99c5' in log
for name in declared:assert ('PASS module '+name).encode() in log
guard={'Status':'PASS','Head':head,'OriginalPassed':5785,'FdPassed':8,'NewIdentifierContracts':11,'AllOld4837RowsExact':not differences,'HistoricalSemanticContractsExact':True,'RawDifferences':differences,'FullReferenceCoverage':False,'ExclusionsUnchanged':6,'Run':38596894388,'Job':115965831492,'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'JobLogSha256':sha(log),'NoSourcesOrExpectedChanges':True}
(a/'part270-original-guard.json').write_bytes((json.dumps(guard,indent=2)+'\n').encode())
print('PASS5785+fd8; all11new native contracts exact; historical raw equality',not differences)
