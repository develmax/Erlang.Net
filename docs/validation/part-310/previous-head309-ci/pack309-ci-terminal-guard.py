from pathlib import Path
import base64, hashlib, json, re, subprocess, sys, zipfile
r=Path.cwd();a=r/'artifacts';head='e829c53ef4323ea8cdd0948cd77c243efa787abd';rid=int(sys.argv[1]);kind=sys.argv[2];d=a/f'pack309-watch-{rid}-terminal'
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
 t=j(tp[0]);expected=json.loads(blob('docs/validation/part-309/tests.json'));assert t['Passed']==6637 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
 names=[x['Name'] for x in t['Tests']];assert len(names)==len(set(names))==6637 and names==[x['Name'] for x in expected['Tests']]
 old=json.loads(blob('docs/validation/part-308/tests.json'));oldnames=[x['Name'] for x in old['Tests']];assert [n for n in names if n in set(oldnames)]==oldnames
 assert len(oldnames)==6637 and len([n for n in names if n.startswith('mfa/')])==505
 integration=j(ip[0]);assert integration==json.loads(blob('docs/validation/part-309/integration-results.json')) and len(integration)==11
 assert 'Validation passed' in log
 stage=j(a/'root-part309-stage-index-guard.json');assert stage['Status']=='PASS_EXACT_STAGE_PATHS_EXPLICIT_GIT_ATTRIBUTE_HASH_MODES' and stage['SourcePaths']==8 and stage['CSharpPaths']==3 and stage['ProofsBinaryExact'] and stage['WorkingSourceFreezePreserved']
 assert stage['SourceFreezeSha256']=='EDB97892181DA1CA458A9ADEFCD60163AF255DF416E0388FDB18E74217832659'
 source=json.loads(blob('docs/validation/part-309/root-artifacts/root-part309-final-source-freeze.json'))
 indexed={x['Path']:x for x in stage['Records']}
 for row in source['Files']:
  record=indexed[row['Path']];data=blob(row['Path']);assert h(data)==record['StagedSha256'] and len(data)==record['StagedBytes']
  assert record['WorkingSha256']==row['Sha256'] and record['WorkingBytes']==row['Bytes']
  if row['Path'].endswith('.cs'):assert record['ComparisonMode']=='BinaryExact'
 result.update(PublishedStageSource8AndCSharp3Exact=True,PublishedStageGuardSha256=h((a/'root-part309-stage-index-guard.json').read_bytes()))
 result.update(Status='PASS_EXACT_HEAD309_CI6637_FULL',Passed=6637,Failed=0,OrderedNamesExact=True,Historical6637OrderedNamesExact=True,Integration=integration,RegisteredMfas=505)

else:raise ValueError('Original301 requires explicit migration verifier')

out=a/f'pack309-{kind}-{rid}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
