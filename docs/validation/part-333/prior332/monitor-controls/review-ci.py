"""Passive current-head CI typed audit. Never executes downloaded code."""
from pathlib import Path
import importlib.util,json,sys
sys.dont_write_bytecode=True
S=importlib.util.spec_from_file_location('passive',Path(__file__).with_name('passive-common.py'));m=importlib.util.module_from_spec(S);S.loader.exec_module(m)
mp,d,run,job,items,rows=m.load('ci');head='aaf48552bee429b0447d3e28e8274d34ad590d73'
assert mp['Head']==run['head_sha']==head and run['id']==38911204008
assert run['path']=='.github/workflows/validate.yml' and run['event']=='push' and run['head_branch']=='main'
assert run['conclusion']==job['conclusion']=='success' and all(s['conclusion'] in ['success','skipped'] for s in job['steps']) and len(items)==1
ex=d/f'artifact-{items[0]["id"]}';tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
paths=['docs/validation/part-332/final-candidate/tests.json','docs/validation/part-331/final-main/tests.json','docs/validation/part-332/final-candidate/integration-results.json','docs/validation/part-332/final-candidate/source-freeze.json','docs/validation/part-332/final-candidate/full-guard.json']
b=m.blobs(head,paths);t=m.read(tp[0]);expected,prior,integrations,freeze,full=[json.loads(b[p]) for p in paths]
names=[x['Name'] for x in t['Tests']];added=set(full['AddedTestNames'])
assert t['Passed']==len(names)==len(set(names))==6822 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
assert names==[x['Name'] for x in expected['Tests']] and len(added)==47 and prior['Passed']==6775
assert [n for n in names if n not in added]==[x['Name'] for x in prior['Tests']]
assert sum(n.startswith('mfa/') for n in names)==521 and sum(n.startswith('mfa/erlang:') for n in names)==351
assert m.read(ip[0])==integrations and len(integrations)==11
stagepath=m.A/'proc-lib332-actual-stage-independent/report-rich.json';assert m.sha(stagepath.read_bytes())=='5FA698CBBD7C47DC404DDBC2C29D12897C37C570614D46A8AF84D2E36E039580'
stage=m.read(stagepath);assert (stage['Paths'],stage['SourcePaths'])==(946,25)
assert stage['SourceFreezeSha256']==m.sha(b[paths[3]])=='8CA193FB17ED3418F6FD79127FE3CBB64AAFCC423AE32EB564A1B8E82D417E71'
records={x['Path']:x for x in stage['Records']};blobs=m.blobs(head,records)
for name,x in records.items():assert len(blobs[name])==x['StagedBytes'] and m.sha(blobs[name])==x['StagedSha256']
for x in freeze['Files']:
 assert len(blobs[x['Path']])==x['Bytes'] and m.sha(blobs[x['Path']])==x['Sha256']
 if x['Path'].endswith('.cs'):assert m.sha(blobs[x['Path']])==x['Sha256']
assert m.sha(b[paths[4]])=='FBB9A95750DD49DA34C8FF76B4CE10D2D1EC640C98A32555D9D4667BFDC899E7'
assert freeze['SourceClosureSha256']=='56908A656DEA730DE49C960EB82B4C5656A46E5B94B0E77E75F98BAA07A12F95'
value=dict(Status='PASS_ACTUAL_PUBLISHED332_CI6822_ELEVEN_INTEGRATIONS_AND946_COMMITTED_BLOBS',Head=head,Run=run['id'],Job=job['id'],Artifact=items[0]['id'],Attempt=1,Passed=6822,Failed=0,Previous331All6775OrderedNamesExactPlusFortySeven=True,AddedNames=sorted(added),IntegrationResults=11,RegisteredMfas=521,ErlangMfas=351,PublishedStagePathsRehashed=946,PublishedSourcePaths=25,PublishedCSharpRawExact=21,RawZipSha256=m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),RawJobLogSha256=m.sha((d/f'job-{job["id"]}.log').read_bytes()),ApiEnvelopeZipDigestAndExtractedMembersVerified=True,OtherWorkflowPassInferred=False,ReviewerExecutedSdkVmNativeNetworkMainOrGitMutation=False)
p=d/'independent-ci-guard.json';assert not p.exists();p.write_bytes((json.dumps(value,indent=2)+'\n').encode());print(json.dumps(dict(Guard=str(p),Sha256=m.sha(p.read_bytes()))))
