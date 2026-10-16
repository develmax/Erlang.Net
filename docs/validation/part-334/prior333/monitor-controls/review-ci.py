"""Passive current-head CI typed audit. Never executes downloaded code."""
from pathlib import Path
import importlib.util,json,sys
sys.dont_write_bytecode=True
S=importlib.util.spec_from_file_location('passive',Path(__file__).with_name('passive-common.py'));m=importlib.util.module_from_spec(S);S.loader.exec_module(m)
mp,d,run,job,items,rows=m.load('ci');head='e9725c4b2eef68014aeb70afd0abcf9f646065fd'
assert mp['Head']==run['head_sha']==head and run['id']==38920570561
assert run['path']=='.github/workflows/validate.yml' and run['event']=='push' and run['head_branch']=='main'
assert run['conclusion']==job['conclusion']=='success' and all(s['conclusion'] in ['success','skipped'] for s in job['steps']) and len(items)==1
ex=d/f'artifact-{items[0]["id"]}';tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
paths=['docs/validation/part-333/final-candidate/tests.json','docs/validation/part-332/final-candidate/tests.json','docs/validation/part-333/final-candidate/integration-results.json','docs/validation/part-333/final-candidate/source-freeze.json','docs/validation/part-333/final-candidate/full-guard.json']
b=m.blobs(head,paths);t=m.read(tp[0]);expected,prior,integrations,freeze,full=[json.loads(b[p]) for p in paths]
names=[x['Name'] for x in t['Tests']];added=set(full['AddedTestNames'])
assert t['Passed']==len(names)==len(set(names))==6871 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
assert names==[x['Name'] for x in expected['Tests']] and len(added)==49 and prior['Passed']==6822
assert [n for n in names if n not in added]==[x['Name'] for x in prior['Tests']]
assert sum(n.startswith('mfa/') for n in names)==528 and sum(n.startswith('mfa/erlang:') for n in names)==351
assert m.read(ip[0])==integrations and len(integrations)==11
stagepath=m.A/'proc-lib333-actual-stage-independent/report.json';assert m.sha(stagepath.read_bytes())=='3FF02F66120FDFC4966B54E52DC669C14871950DA8627EF55430B62F87D83B9F'
stage=m.read(stagepath);assert (stage['Paths'],stage['SourcePaths'])==(947,18)
assert stage['SourceFreezeSha256']==m.sha(b[paths[3]])=='AA5249A3F81F74F825626EAAF1DACB1E44564105B6BD0589DB5A0DD9CA4046C4'
records={x['Path']:x for x in stage['Records']};blobs=m.blobs(head,records)
for name,x in records.items():assert len(blobs[name])==x['StagedBytes'] and m.sha(blobs[name])==x['StagedSha256']
for x in freeze['Files']:
 assert len(blobs[x['Path']])==x['Bytes'] and m.sha(blobs[x['Path']])==x['Sha256']
 if x['Path'].endswith('.cs'):assert m.sha(blobs[x['Path']])==x['Sha256']
assert m.sha(b[paths[4]])=='368E328BD251F219270018D37E2FAE4681460A83808B9F741C78B34DBD6C5E72'
assert freeze['SourceClosureSha256']=='BBECB7B32C2807BB7DC74F06AD500C5517283468864F0203FE35C54F084DC2CE'
value=dict(Status='PASS_ACTUAL_PUBLISHED333_CI6871_ELEVEN_INTEGRATIONS_AND947_COMMITTED_BLOBS',Head=head,Run=run['id'],Job=job['id'],Artifact=items[0]['id'],Attempt=1,Passed=6871,Failed=0,Previous332All6822OrderedNamesExactPlusFortyNine=True,AddedNames=sorted(added),IntegrationResults=11,RegisteredMfas=528,ErlangMfas=351,PublishedStagePathsRehashed=947,PublishedSourcePaths=18,PublishedCSharpRawExact=14,RawZipSha256=m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),RawJobLogSha256=m.sha((d/f'job-{job["id"]}.log').read_bytes()),ApiEnvelopeZipDigestAndExtractedMembersVerified=True,OtherWorkflowPassInferred=False,ReviewerExecutedSdkVmNativeNetworkMainOrGitMutation=False)
p=d/'independent-ci-guard.json';assert not p.exists();p.write_bytes((json.dumps(value,indent=2)+'\n').encode());print(json.dumps(dict(Guard=str(p),Sha256=m.sha(p.read_bytes()))))
