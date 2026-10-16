"""Passive current-head CI typed audit. Never executes downloaded code."""
from pathlib import Path
import importlib.util,json,sys
sys.dont_write_bytecode=True
S=importlib.util.spec_from_file_location('passive',Path(__file__).with_name('passive-common.py'));m=importlib.util.module_from_spec(S);S.loader.exec_module(m)
mp,d,run,job,items,rows=m.load('ci');head='6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d'
assert mp['Head']==run['head_sha']==head and run['id']==38904967552
assert run['path']=='.github/workflows/validate.yml' and run['event']=='push' and run['head_branch']=='main'
assert run['conclusion']==job['conclusion']=='success' and all(s['conclusion'] in ['success','skipped'] for s in job['steps']) and len(items)==1
ex=d/f'artifact-{items[0]["id"]}';tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
paths=['docs/validation/part-331/final-main/tests.json','docs/validation/part-329/final-main/tests.json','docs/validation/part-331/final-main/integration-results.json','docs/validation/part-331/final-main/source-freeze.json','docs/validation/part-331/final-main/full-guard.json']
b=m.blobs(head,paths);t=m.read(tp[0]);expected,prior,integrations,freeze,full=[json.loads(b[p]) for p in paths]
names=[x['Name'] for x in t['Tests']];added=set(full['AddedTestNames'])
assert t['Passed']==len(names)==len(set(names))==6775 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
assert names==[x['Name'] for x in expected['Tests']] and len(added)==28 and prior['Passed']==6747
assert [n for n in names if n not in added]==[x['Name'] for x in prior['Tests']]
assert sum(n.startswith('mfa/') for n in names)==515 and sum(n.startswith('mfa/erlang:') for n in names)==351
assert m.read(ip[0])==integrations and len(integrations)==11
stagepath=m.A/'root-part331-stage-index-guard.json';assert m.sha(stagepath.read_bytes())=='F9A7C4CCBA9543F73BDE7750CDD3E949F12DD912AEE89E0D6F225C7EF51429BA'
stage=m.read(stagepath);assert (stage['StagedPaths'],stage['SourcePaths'],stage['CSharpPaths'])==(979,19,15)
assert stage['SourceFreezeSha256']==m.sha(b[paths[3]])=='559184A858FEDF503164F7D303B9E17DEFAD80292533EE4AB7D9B7C221EBCA5E'
records={x['Path']:x for x in stage['Records']};blobs=m.blobs(head,records)
for name,x in records.items():assert len(blobs[name])==x['StagedBytes'] and m.sha(blobs[name])==x['StagedSha256']
for x in freeze['Files']:
 assert len(blobs[x['Path']])==x['Bytes'] and m.sha(blobs[x['Path']])==x['Sha256']
 if x['Path'].endswith('.cs'):assert records[x['Path']]['ComparisonMode']=='BinaryExact'
assert m.sha(b[paths[4]])=='8914E21E6485FB04504DE9B654E818524FD3C44CD009EC730B1F98E6E2E6C05A'
assert freeze['SourceClosureSha256']=='9E6119DDB243964E57FB9AC2A35B550B03B4B4495DE54AD6505BE6CB5A76FF34'
value=dict(Status='PASS_ACTUAL_PUBLISHED331_CI6775_ELEVEN_INTEGRATIONS_AND979_COMMITTED_BLOBS',Head=head,Run=run['id'],Job=job['id'],Artifact=items[0]['id'],Attempt=1,Passed=6775,Failed=0,Previous329All6747OrderedNamesExactPlusTwentyEight=True,AddedNames=sorted(added),IntegrationResults=11,RegisteredMfas=515,ErlangMfas=351,PublishedStagePathsRehashed=979,PublishedSourcePaths=19,PublishedCSharpRawExact=15,RawZipSha256=m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),RawJobLogSha256=m.sha((d/f'job-{job["id"]}.log').read_bytes()),ApiEnvelopeZipDigestAndExtractedMembersVerified=True,OtherWorkflowPassInferred=False,ReviewerExecutedSdkVmNativeNetworkMainOrGitMutation=False)
p=d/'independent-ci-guard.json';assert not p.exists();p.write_bytes((json.dumps(value,indent=2)+'\n').encode());print(json.dumps(dict(Guard=str(p),Sha256=m.sha(p.read_bytes()))))
