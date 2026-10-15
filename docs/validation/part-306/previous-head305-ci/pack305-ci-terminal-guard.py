from pathlib import Path
import base64, hashlib, json, re, subprocess, sys, zipfile
r=Path.cwd();a=r/'artifacts';head='0494375635061b4952d40a3c7667234db321a931';rid=int(sys.argv[1]);kind=sys.argv[2];d=a/f'pack305-watch-{rid}-terminal'
j=lambda q:json.loads(q.read_text(encoding='utf-8-sig'));h=lambda b:hashlib.sha256(b).hexdigest().upper();blob=lambda q:subprocess.check_output(['git','cat-file','blob',head+':'+q])
run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts'];assert len(jobs)==len(items)==1
job=jobs[0];item=items[0];assert run['head_sha']==head and run['run_attempt']==1 and run['status']=='completed' and run['conclusion']=='success'
assert run['id']==rid and run['name']=={'ci':'Validate CLR implementation','original':'Pinned OTP differential oracle'}[kind]
assert job['head_sha']==head and job['status']=='completed' and job['conclusion']=='success' and all(s['conclusion'] in ['success','skipped'] for s in job['steps'])
assert item['workflow_run']['head_sha']==head and not item['expired']
z=d/f"artifact-{item['id']}.zip";ex=d/f"artifact-{item['id']}";assert z.stat().st_size==item['size_in_bytes'] and h(z.read_bytes())==item['digest'][7:].upper()
with zipfile.ZipFile(z) as archive:
 for name in archive.namelist():
  assert not Path(name).is_absolute() and '..' not in Path(name).parts
  if not name.endswith('/'):assert archive.read(name)==(ex/name).read_bytes()
manifest=j(d/'download-manifest.json');assert {x.relative_to(d).as_posix() for x in d.rglob('*') if x.is_file() and x.name!='download-manifest.json'}=={x['Path'].replace('\\','/') for x in manifest}
for row in manifest:
 q=d/row['Path'];assert q.stat().st_size==row['Bytes'] and h(q.read_bytes())==row['Sha256']
raw=(d/f"job-{job['id']}.log").read_bytes();log=raw.decode('utf-8-sig')
result={'Head':head,'Run':rid,'Job':job['id'],'Artifact':item['id'],'RawZipSha256':h(z.read_bytes()),'RawJobSha256':h(raw),'Attempt':1,'MainEdited':False,'DispatchOrRerun':False}
if kind=='ci':
 tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
 t=j(tp[0]);expected=json.loads(blob('docs/validation/part-305/tests.json'));assert t['Passed']==6624 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
 names=[x['Name'] for x in t['Tests']];assert len(names)==len(set(names))==6624 and names==[x['Name'] for x in expected['Tests']]
 old=json.loads(blob('docs/validation/part-304/reused303/tests.json'));oldnames=[x['Name'] for x in old['Tests']];assert [n for n in names if n in set(oldnames)]==oldnames
 assert len(oldnames)==6622 and len([n for n in names if n.startswith('mfa/')])==503
 integration=j(ip[0]);assert integration==json.loads(blob('docs/validation/part-305/integration-results.json')) and len(integration)==11
 assert 'Validation passed' in log
 result.update(Status='PASS_EXACT_HEAD305_CI6624_FULL',Passed=6624,Failed=0,OrderedNamesExact=True,Historical6622OrderedNamesExact=True,Integration=integration,RegisteredMfas=503)

else:raise ValueError('Original301 requires explicit migration verifier')

out=a/f'pack305-{kind}-{rid}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
