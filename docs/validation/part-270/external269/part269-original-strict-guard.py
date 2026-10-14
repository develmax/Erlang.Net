from pathlib import Path
import hashlib,json,zipfile
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
report=j(a/'part-269-differential/differential.json');old=j(r/'docs/validation/part-243/differential.json');ledger=j(r/'docs/validation/part-245/source-ledger.json');m=j(a/'part-269-differential-metadata.json');p=j(a/'part-269-ordinary-latest-provenance.json')
assert m['Run']['head_sha']==p['Run']['head_sha']=='783088ff0b6095c1e984e913b385a1cfceae6d7c'
assert m['Run']['id']==p['Run']['id']==38582861072 and p['Job']['id']==115917033075 and p['Job']['labels']==['ubuntu-latest']
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
fd=j(a/'part-269-differential/fd-differential.json');assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8
assert fd['Results']==j(r/'docs/validation/part-243/fd-differential.json')['Results']
assert sha(a/'part-269-differential.zip')==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-269-differential.zip') as z:
 for name in z.namelist():
  if not name.endswith('/'):assert z.read(name)==(a/'part-269-differential'/name).read_bytes()
digest=j(a/'part-269-ordinary-job.log.digest.json');assert sha(a/'part-269-ordinary-job.log')==digest['SHA256']
log=(a/'part-269-ordinary-job.log').read_text(encoding='utf-8-sig');assert 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'PASS module erlang-alias-owner-exit' in log
for step in p['Job']['steps']:assert step['status']=='completed' and step['conclusion']=='success',step['name']
print("Strict raw historical rows exact PASS")
