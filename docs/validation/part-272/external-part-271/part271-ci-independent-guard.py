from pathlib import Path
import json,hashlib,zipfile
a=Path('artifacts');j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
m=j(a/'part-271-validation-metadata.json');p=j(a/'part-271-ci-latest-provenance.json');t=j(a/'part-271-validation/tests.json');local=j(Path('docs/validation/part-271/tests.json'))
assert m['Run']['id']==p['Run']['id']==38604603134 and p['Job']['id']==115992143366 and m['Artifact']['id']==11915631341
assert m['Run']['head_sha']==p['Run']['head_sha']==Path('artifacts/part-271-frozen-head.txt').read_text().strip()
assert m['Run']['conclusion']==p['Job']['conclusion']=='success' and p['Job']['labels']==['windows-latest']
assert t['Passed']==len(t['Tests'])==6497 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
assert [x['Name'] for x in t['Tests']]==[x['Name'] for x in local['Tests']]
assert j(a/'part-271-validation/integration-results.json')==j(Path('docs/validation/part-271/integration-results.json'))
assert sha(a/'part-271-validation.zip')==m['ZipSHA256']==m['Artifact']['digest'][7:].upper()
with zipfile.ZipFile(a/'part-271-validation.zip') as z:
 for n in z.namelist():
  if not n.endswith('/'):assert z.read(n)==(a/'part-271-validation'/n).read_bytes()
assert all(x['status']=='completed' and x['conclusion']=='success' for x in p['Job']['steps'])
log=a/'part-271-ci-job.log';assert sha(log)==j(Path(str(log)+'.digest.json'))['SHA256']
text=log.read_text(encoding='utf-8-sig');assert '6497/6497 passed' in text and 'Validation passed' in text
result={'Status':'PASS','Head':m['Run']['head_sha'],'CiPassed':6497,'CiFailed':0,'OrderedNamesExact':True,'FullIntegrationPass':True,'Run':38604603134,'Job':115992143366,'Artifact':11915631341,'ZipSha256':m['ZipSHA256'],'AllJobStepsSuccess':True,'RawJobLogVerificationPending':False,'Prior267AllocationCauseEstablished':False,'JobLogSha256':sha(log)}
(a/'part271-ci-independent-guard.json').write_bytes((json.dumps(result,indent=2)+'\n').encode());print('PASS exact271 CI6497/full/rawlog/source names/ZIP/API')
