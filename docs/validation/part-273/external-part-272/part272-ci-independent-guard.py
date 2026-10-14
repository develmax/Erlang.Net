from pathlib import Path
import json,hashlib,zipfile
a=Path('artifacts');j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
m=j(a/'part-272-validation-metadata.json');p=j(a/'part-272-ci-latest-provenance.json');t=j(a/'part-272-validation/tests.json');local=j(Path('docs/validation/part-272/tests.json'))
assert m['Run']['id']==p['Run']['id']==38610247436 and p['Job']['id']==116011416500 and m['Artifact']['id']==11917283079
assert m['Run']['head_sha']==p['Run']['head_sha']==Path('artifacts/part-272-frozen-head.txt').read_text().strip()
assert m['Run']['conclusion']==p['Job']['conclusion']=='success' and p['Job']['labels']==['windows-latest']
assert t['Passed']==len(t['Tests'])==6515 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
assert [x['Name'] for x in t['Tests']]==[x['Name'] for x in local['Tests']]
assert j(a/'part-272-validation/integration-results.json')==j(Path('docs/validation/part-272/integration-results.json'))
assert sha(a/'part-272-validation.zip')==m['ZipSHA256']==m['Artifact']['digest'][7:].upper()
with zipfile.ZipFile(a/'part-272-validation.zip') as z:
 for n in z.namelist():
  if not n.endswith('/'):assert z.read(n)==(a/'part-272-validation'/n).read_bytes()
assert all(x['status']=='completed' and x['conclusion']=='success' for x in p['Job']['steps'])
log=a/'part-272-ci-job.log';assert sha(log)==j(Path(str(log)+'.digest.json'))['SHA256']
text=log.read_text(encoding='utf-8-sig');assert '6515/6515 passed' in text and 'Validation passed' in text
result={'Status':'PASS','Head':m['Run']['head_sha'],'CiPassed':6515,'CiFailed':0,'OrderedNamesExact':True,'FullIntegrationPass':True,'Run':38610247436,'Job':116011416500,'Artifact':11917283079,'ZipSha256':m['ZipSHA256'],'AllJobStepsSuccess':True,'RawJobLogVerificationPending':False,'Prior267AllocationCauseEstablished':False,'JobLogSha256':sha(log)}
(a/'part272-ci-independent-guard.json').write_bytes((json.dumps(result,indent=2)+'\n').encode());print('PASS exact272 CI6515/full/rawlog/source names/ZIP/API')
