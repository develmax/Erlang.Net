from pathlib import Path
import json,hashlib,zipfile
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));m=j(a/'part-270-validation-metadata.json');t=j(a/'part-270-validation/tests.json');l=j(r/'docs/validation/part-270/tests.json')
assert m['Run']['head_sha']=='f26e1a15320553eb9d725764ec2260f3c262e1da' and m['Run']['conclusion']=='success'
assert len(t['Tests'])==6496 and all(row['Status']=='Passed' for row in t['Tests'])
assert m['Run']['id']==38596889740 and m['Artifact']['workflow_run']['id']==38596889740 and m['Artifact']['workflow_run']['head_sha']==m['Run']['head_sha']
assert t['Passed']==6496 and t['Failed']==0 and [x['Name'] for x in t['Tests']]==[x['Name'] for x in l['Tests']]
assert j(a/'part-270-validation/integration-results.json')==j(r/'docs/validation/part-270/integration-results.json')
assert hashlib.sha256((a/'part-270-validation.zip').read_bytes()).hexdigest().upper()==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-270-validation.zip') as z:
 for n in z.namelist():
  if not n.endswith('/'):assert z.read(n)==(a/'part-270-validation'/n).read_bytes()
(a/'part270-ci-report-guard.json').write_text(json.dumps({'Status':'PASS','CiPassed':6496,'CiFailed':0,'OrderedNamesExact':True,'FullIntegrationPass':True,'Head':m['Run']['head_sha'],'Run':m['Run']['id'],'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'RawJobLogAuditPending':True},indent=2)+'\n');print('PASS CI6496/full/exact names/ZIP; rawlog pending')
