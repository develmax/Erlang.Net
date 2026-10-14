from pathlib import Path
import json,hashlib,zipfile
art=Path('artifacts'); j=lambda p:json.loads(p.read_text(encoding='utf-8-sig')); sha=lambda b:hashlib.sha256(b).hexdigest().upper()
m=j(art/'part-274-validation-metadata.json'); p=j(art/'part-274-ci-latest-provenance.json'); tests=j(art/'part-274-validation/tests.json'); before=j(art/'part-273-validation/tests.json')
head='b87e7fe1539dc9850a0c24d1b426bc6851f19e5c'
assert m['Run']['head_sha']==p['Run']['head_sha']==head and m['Run']['id']==p['Run']['id']==38636837534 and p['Job']['id']==116102041330
assert m['Run']['conclusion']==p['Job']['conclusion']=='success' and p['Job']['labels']==['windows-latest']
assert all(s['status']=='completed' and s['conclusion']=='success' for s in p['Job']['steps'])
assert tests['Passed']==6527 and tests['Failed']==0 and len(tests['Tests'])==6527 and all(t['Status']=='Passed' for t in tests['Tests'])
assert [t['Name'] for t in tests['Tests']]==[t['Name'] for t in before['Tests']]
assert j(art/'part-274-validation/integration-results.json')==j(art/'part-273-validation/integration-results.json')
assert m['Artifact']['workflow_run']['head_sha']==head and m['Artifact']['workflow_run']['id']==38636837534
archive=art/'part-274-validation.zip'; assert sha(archive.read_bytes())==m['ZipSHA256']==m['Artifact']['digest'][7:].upper()
with zipfile.ZipFile(archive) as z:
    for name in z.namelist():
        if not name.endswith('/'):assert z.read(name)==(art/'part-274-validation'/name).read_bytes()
log=art/'part-274-ci-job.log'; assert sha(log.read_bytes())==j(Path(str(log)+'.digest.json'))['SHA256']
text=log.read_text(encoding='utf-8-sig'); assert '6527/6527 passed' in text and 'Validation passed' in text
result={'Status':'PASS','Head':head,'Run':38636837534,'Job':116102041330,'Artifact':m['Artifact']['id'],'TestsPassed':6527,'TestsFailed':0,'OldOrderedNamesExact':True,'FullIntegrationPass':True,'AllJobStepsSuccess':True,'ZipSha256':sha(archive.read_bytes()),'RawJobSha256':sha(log.read_bytes()),'NativeSemanticsClaim':False}
(art/'part274-ci-independent-guard.json').write_text(json.dumps(result,indent=2)+'\n'); print(json.dumps(result))
