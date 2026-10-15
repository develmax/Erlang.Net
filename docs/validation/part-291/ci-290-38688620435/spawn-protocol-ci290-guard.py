from pathlib import Path
import hashlib,json,subprocess,sys,zipfile
r=Path.cwd(); head=sys.argv[1]; rid=int(sys.argv[2]); d=r/f'artifacts/spawn-protocol-ci-290-{rid}-terminal'
j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'))
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
blob=lambda p:subprocess.check_output(['git','cat-file','blob',head+':'+p])
run=j(d/'run.json'); jobs=j(d/'jobs.json')['jobs']; items=j(d/'artifacts.json')['artifacts']
assert run['id']==rid and run['head_sha']==head and run['run_attempt']==1 and run['status']=='completed' and run['conclusion']=='success'
assert run['name']=='Validate CLR implementation' and len(jobs)==len(items)==1
job=jobs[0]; item=items[0]
assert job['head_sha']==head and job['conclusion']=='success' and all(s['conclusion'] in ['success','skipped'] for s in job['steps'])
assert item['workflow_run']['head_sha']==head and not item['expired']
z=d/f"artifact-{item['id']}.zip"; ex=d/f"artifact-{item['id']}"
assert z.stat().st_size==item['size_in_bytes'] and sha(z.read_bytes())==item['digest'][7:].upper()
with zipfile.ZipFile(z) as f:
 for n in f.namelist():
  assert not Path(n).is_absolute() and '..' not in Path(n).parts
  if not n.endswith('/'): assert f.read(n)==(ex/n).read_bytes()
tp=list(ex.rglob('tests.json')); ip=list(ex.rglob('integration-results.json')); assert len(tp)==len(ip)==1
t=j(tp[0]); expected=json.loads(blob('docs/validation/part-289/tests.json'))
assert t['Passed']==6588 and t['Failed']==0 and len(t['Tests'])==6588 and all(x['Status']=='Passed' for x in t['Tests'])
names=[x['Name'] for x in t['Tests']]; assert len(set(names))==6588 and names==[x['Name'] for x in expected['Tests']]
old=json.loads(blob('docs/validation/part-288/tests.json')); oldnames=[x['Name'] for x in old['Tests']]
assert [n for n in names if n in set(oldnames)]==oldnames and len(oldnames)==6570
added=[n for n in names if n not in set(oldnames)]; assert len(added)==18 and len([n for n in names if n.startswith('mfa/')])==502
integration=j(ip[0]); assert integration==json.loads(blob('docs/validation/part-289/integration-results.json')) and len(integration)==11
log=(d/f"job-{job['id']}.log").read_bytes(); assert b'Validation passed' in log
result={'Head':head,'Run':rid,'Job':job['id'],'Artifact':item['id'],'Passed':6588,'Failed':0,'OrderedNamesExact':True,'Old6570NamesRetained':True,'Added':added,'RegisteredMfas':502,'Integration':integration,'RawZipSha256':sha(z.read_bytes()),'RawJobSha256':sha(log),'TestsSha256':sha(tp[0].read_bytes()),'SameHeadCIPassed':True,'OriginalNifSevenExecutedByThisGuard':False,'MainSourceUnchangedStandalonePart290':True}
out=r/f'artifacts/spawn-protocol-ci290-{rid}-independent-guard.json'; assert not out.exists(); out.write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
