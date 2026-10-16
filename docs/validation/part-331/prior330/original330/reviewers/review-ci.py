"""Passive current-head CI typed audit. Never executes downloaded code."""
from pathlib import Path
import importlib.util,json,sys
sys.dont_write_bytecode=True
S=importlib.util.spec_from_file_location('passive',Path(__file__).with_name('passive-common.py'));m=importlib.util.module_from_spec(S);S.loader.exec_module(m)
mp,d,run,job,items,rows=m.load('ci');head='ed2785a000ae8b1c520707df3224c2807c959bc3'
assert mp['Head']==run['head_sha']==head and run['id']==38900781337
assert run['path']=='.github/workflows/validate.yml' and run['event']=='push' and run['head_branch']=='main'
assert run['conclusion']==job['conclusion']=='success' and all(s['conclusion'] in ['success','skipped'] for s in job['steps']) and len(items)==1
ex=d/f'artifact-{items[0]["id"]}';tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
paths=['docs/validation/part-329/final-main/tests.json','docs/validation/part-328/final-main/tests.json','docs/validation/part-329/final-main/integration-results.json','docs/validation/part-329/final-main/source-freeze.json','docs/validation/part-329/final-main/full-guard.json']
b=m.blobs(head,paths);t=m.read(tp[0]);expected,prior,integrations,freeze,full=[json.loads(b[p]) for p in paths]
names=[x['Name'] for x in t['Tests']];added=set(full['AddedTestNames'])
assert t['Passed']==len(names)==len(set(names))==6747 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
assert names==[x['Name'] for x in expected['Tests']] and len(added)==9 and prior['Passed']==6738
assert [n for n in names if n not in added]==[x['Name'] for x in prior['Tests']]
assert sum(n.startswith('mfa/') for n in names)==509 and sum(n.startswith('mfa/erlang:') for n in names)==351
assert m.read(ip[0])==integrations and len(integrations)==11
stagepath=m.A/'part330-independent-cleanup-review/report.json';assert m.sha(stagepath.read_bytes())=='A94F737627E8B43AFC20F6FE5974B5962CBE7B3CB4031B1DC86DCDC498C2E472'
stage=m.read(stagepath);assert stage['StagedPaths']==41
records={x['Path']:x for x in stage['StageFiles']};committed=m.blobs(head,records)
for name,x in records.items():assert len(committed[name])==x['Bytes'] and m.sha(committed[name])==x['Sha256']
assert all(not p.startswith(('src/','tests/')) for p in records)
value=dict(Status='PASS_ACTUAL_PUBLISHED330_CI6747_ELEVEN_INTEGRATIONS_AND41_COMMITTED_BLOBS',Head=head,Run=run['id'],Job=job['id'],Artifact=items[0]['id'],Attempt=1,Passed=6747,Failed=0,Previous328All6738OrderedNamesExactPlusNine=True,AddedNames=sorted(added),IntegrationResults=11,RegisteredMfas=509,ErlangMfas=351,PublishedStagePathsRehashed=41,MainProductionOrTestCodeChanged=False,RawZipSha256=m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),RawJobLogSha256=m.sha((d/f'job-{job["id"]}.log').read_bytes()),ApiEnvelopeZipDigestAndExtractedMembersVerified=True,OtherWorkflowPassInferred=False,ReviewerExecutedSdkVmNativeNetworkMainOrGitMutation=False)
p=Path(__file__).with_name('independent-ci-guard.json');assert not p.exists();p.write_bytes((json.dumps(value,indent=2)+'\n').encode());print(json.dumps(dict(Guard=str(p),Sha256=m.sha(p.read_bytes()))))
