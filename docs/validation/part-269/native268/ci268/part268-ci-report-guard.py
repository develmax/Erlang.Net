from pathlib import Path
import json,hashlib,zipfile
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));m=j(a/'part-268-validation-metadata.json');t=j(a/'part-268-validation/tests.json');l=j(r/'docs/validation/part-267/tests.json')
assert m['Run']['head_sha']=='3a7ab4a6ac3adeee2242545a25ad2ace27efa8e7' and m['Run']['conclusion']=='success'
assert len(t['Tests'])==6484 and all(row['Status']=='Passed' for row in t['Tests'])
assert m['Run']['id']==38570581183 and m['Artifact']['workflow_run']['id']==38570581183 and m['Artifact']['workflow_run']['head_sha']==m['Run']['head_sha']
assert t['Passed']==6484 and t['Failed']==0 and [x['Name'] for x in t['Tests']]==[x['Name'] for x in l['Tests']]
assert j(a/'part-268-validation/integration-results.json')==j(r/'docs/validation/part-267/integration-results.json')
assert hashlib.sha256((a/'part-268-validation.zip').read_bytes()).hexdigest().upper()==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-268-validation.zip') as z:
 for n in z.namelist():
  if not n.endswith('/'):assert z.read(n)==(a/'part-268-validation'/n).read_bytes()
(a/'part268-ci-report-guard.json').write_text(json.dumps({'Status':'PASS','CiPassed':6484,'CiFailed':0,'OrderedNamesExact':True,'FullIntegrationPass':True,'Head':m['Run']['head_sha'],'Run':m['Run']['id'],'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'RawJobLogAuditPending':True},indent=2)+'\n');print('PASS CI6484/full/exact names/ZIP; rawlog pending')
