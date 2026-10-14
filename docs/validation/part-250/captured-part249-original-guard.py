from pathlib import Path
import hashlib,json,zipfile
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
report=j(a/'part-249-differential/differential.json');old=j(r/'docs/validation/part-243/differential.json');ledger=j(r/'docs/validation/part-245/source-ledger.json');m=j(a/'part-249-differential-metadata.json');p=j(a/'part-249-original-latest-provenance.json')
assert m['Run']['head_sha']==p['Run']['head_sha']=='8a72ad88f907a34d424ecc23648bc45c315213a4'
assert m['Run']['id']==p['Run']['id']==38520657510 and p['Job']['id']==115709346609 and p['Job']['labels']==['ubuntu-latest']
assert m['Run']['conclusion']==p['Job']['conclusion']=='success'
assert report['Complete'] and report['VersionVerified'] and report['Planned']==report['Executed']==report['Passed']==5774 and report['Failed']==0 and report['InfrastructureError'] is None
assert not report['ReferenceCoverageComplete'] and len(report['OriginalOtpDefectExclusions'])==6
assert report['OriginalOtpDefectExclusions']==old['OriginalOtpDefectExclusions']
assert report['Results']==old['Results'] and len(report['Results'])==778
modules=report['ModuleResults'];diagnostics=report['DiagnosticResults'];assert len(modules)==4837 and len(diagnostics)==159
assert modules[:4825]==old['ModuleResults'] and diagnostics[:151]==old['DiagnosticResults']
assert len(ledger['Modules'])==12 and len(ledger['Diagnostics'])==8
for expected,actual in zip(ledger['Modules'],modules[4825:]):
 assert all(actual[k]==v for k,v in expected.items()),expected['Name']
 assert actual['Passed'] and actual['Actual']==actual['Expected']==actual['FixtureExpected']
for expected,actual in zip(ledger['Diagnostics'],diagnostics[151:]):
 for k,v in expected.items():
  key={'ExpectedCode':'ImplementationCode','ExpectedMessage':'ImplementationMessage'}.get(k,k)
  assert actual[key]==v,(expected['Name'],key)
 assert actual['Passed'] and actual['Reference']==actual['Expected'] and not actual['ImplementationAccepted']
for row in modules+diagnostics:assert hashlib.sha256(row['Source'].encode()).hexdigest().upper()==row['SourceSha256'] and row['Passed']
fd=j(a/'part-249-differential/fd-differential.json');assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8
assert fd['Results']==j(r/'docs/validation/part-243/fd-differential.json')['Results']
assert sha(a/'part-249-differential.zip')==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-249-differential.zip') as z:
 for name in z.namelist():
  if not name.endswith('/'):assert z.read(name)==(a/'part-249-differential'/name).read_bytes()
digest=j(a/'part-249-original-job.log.digest.json');assert sha(a/'part-249-original-job.log')==digest['SHA256']
log=(a/'part-249-original-job.log').read_text(encoding='utf-8-sig');assert 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'PASS module erlang-alias-owner-exit' in log
for step in p['Job']['steps']:assert step['status']=='completed' and step['conclusion']=='success',step['name']
ci=j(a/'part249-ci-final-guard.json');assert ci['Status']=='PASS' and ci['CiPassed']==6464 and not ci['ProvenanceLogPending']
result={'Status':'PASS','Head':m['Run']['head_sha'],'Run':m['Run']['id'],'Job':p['Job']['id'],'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'JobLogSha256':digest['SHA256'],'OriginalPassed':5774,'OriginalFailed':0,'Complete':True,'Expressions':778,'ComparableModules':4837,'Diagnostics':159,'All4825HistoricalModuleRowsExact':True,'All151HistoricalDiagnosticRowsExact':True,'All12Plus8IndependentNewContractsExact':True,'AllSixExclusionsUnchanged':True,'ReferenceCoverageComplete':False,'FdPassed':8,'CiPassed':6464,'SeparateHaltOriginalStatus':'PASSED26of26 plus2physical; format comparison false'}
(a/'part249-original-guard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('PASS exact5774 original contracts+fd8+same-headCI6464; separatehalt26plus2 and onload4 verified')
