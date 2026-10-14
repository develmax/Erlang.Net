from pathlib import Path
import json,hashlib,zipfile
p=Path('artifacts');load=lambda q:json.loads(q.read_text(encoding='utf-8-sig'));sha=lambda q:hashlib.sha256(q.read_bytes()).hexdigest().upper()
m=load(p/'part-257-validation-metadata.json');v=load(p/'part-257-ci-latest-provenance.json');t=load(p/'part-257-validation/tests.json');d=load(p/'part-257-ci-job.log.digest.json')
head='b111d6de5a5d19ec16d0f491c1fa10d406327b73'
assert m['Run']['head_sha']==v['Run']['head_sha']==head and m['Run']['id']==v['Run']['id']==38541910397 and v['Job']['id']==115778448279
assert m['Run']['conclusion']==v['Job']['conclusion']=='failure' and v['Job']['labels']==['windows-latest']
assert len(t['Tests'])==6470 and t['Passed']==6469 and t['Failed']==1
f=[r for r in t['Tests'] if r['Status']!='Passed'];assert len(f)==1 and f[0]['Name']=='erlang/hash-iterative-deep-terms-and-no-binary-payload-copy' and '25128 bytes, budget 4096' in f[0]['Error']
assert sum(r['Status']=='Passed' for r in t['Tests'])==6469
assert sha(p/'part-257-validation.zip')==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
assert m['Artifact']['workflow_run']['id']==38541910397 and m['Artifact']['workflow_run']['head_sha']==head and not m['Artifact']['expired']
with zipfile.ZipFile(p/'part-257-validation.zip') as z:
 for n in z.namelist():
  if not n.endswith('/'):assert z.read(n)==(p/'part-257-validation'/n).read_bytes()
assert sha(p/'part-257-ci-job.log')==d['SHA256'];log=(p/'part-257-ci-job.log').read_text(encoding='utf-8-sig');assert '6469/6470 passed' in log and '25128 bytes, budget 4096' in log
assert not (p/'part-257-validation/integration-results.json').exists()
s=[r for r in v['Job']['steps'] if r['conclusion']=='failure'];assert len(s)==1 and s[0]['name']=='Run ./tools/validate.ps1'
out={'AuditStatus':'PASS','CiStatus':'FAIL','Head':head,'Run':38541910397,'Job':115778448279,'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'LogSha256':d['SHA256'],'Passed':6469,'Failed':1,'FailedRows':f,'FirstFailedStep':s[0],'FullIntegrationCompleted':False,'NoFixtureOrBudgetChanges':True}
(p/'part257-ci-failure-provenance-guard.json').write_text(json.dumps(out,indent=2)+'\n');print('PASS preserved exact-head CI failure integrity:6469/6470, full integration incomplete')
