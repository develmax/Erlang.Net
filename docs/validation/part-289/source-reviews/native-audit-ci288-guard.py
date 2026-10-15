from pathlib import Path
import hashlib,json,subprocess,sys,zipfile
R=Path.cwd(); A=R/'artifacts'; H=sys.argv[1]; rid=int(sys.argv[2]); assert len(H)==40 and all(c in '0123456789abcdef' for c in H)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
j=lambda p:json.loads(p.read_bytes().decode('utf-8-sig'))
blob=lambda h,p:subprocess.check_output(['git','cat-file','blob',h+':'+p])
D=A/f'native-audit-ci288-{rid}-terminal'
run=j(D/'run.json'); jobs=j(D/'jobs.json')['jobs']; items=j(D/'artifacts.json')['artifacts']
assert run['id']==rid and run['head_sha']==H and run['status']=='completed' and run['run_attempt']==1
assert run['name']=='Validate CLR implementation' and len(jobs)==len(items)==1
job=jobs[0]; item=items[0]
assert job['head_sha']==H and item['workflow_run']['head_sha']==H
assert run['conclusion']==job['conclusion']=='success', 'Actual CI failed; raw terminal evidence retained, no success claim'
assert all(s['conclusion'] in ['success','skipped'] for s in job['steps'])
z=D/f"artifact-{item['id']}.zip"; ex=D/f"artifact-{item['id']}"
assert z.stat().st_size==item['size_in_bytes'] and sha(z.read_bytes())==item['digest'][7:].upper()
with zipfile.ZipFile(z) as f:
 for n in f.namelist():
  if not n.endswith('/'):
   assert not Path(n).is_absolute() and '..' not in Path(n).parts
   assert f.read(n)==(ex/n).read_bytes()
tests=list(ex.rglob('tests.json')); assert len(tests)==1
p=tests[0]; t=j(p); expected=json.loads(blob(H,'docs/validation/part-288/tests.json'))
assert t['Passed']==6570 and t['Failed']==0 and len(t['Tests'])==6570 and all(r['Status']=='Passed' for r in t['Tests'])
names=[r['Name'] for r in t['Tests']]; assert names==[r['Name'] for r in expected['Tests']] and len(set(names))==6570
assert len([n for n in names if n.startswith('mfa/')])==501
added=['runtime/distribution-capacity-wake-retire-and-epoch','runtime/distribution-original-payload14-direct-contract-regressions']
assert all(names.count(n)==1 for n in added)
baseline=json.loads(blob('58d227ecef29d117fa091ab30b1e0928f969d2fa','docs/validation/part-287/tests.json'))
assert [n for n in names if n not in added]==[r['Name'] for r in baseline['Tests']], 'Old6568 ordered names changed'
ips=list(ex.rglob('integration-results.json')); assert len(ips)==1
integration=j(ips[0]); expectedIntegration={'LocalTool':'Passed','PackagedReadiness':'Passed','PackageConsumer':'Passed','DisabledPreprocessing':'Expected failure','Incremental':'Passed','OracleUnavailableReport':'Expected failure recorded','Build':'Passed','HybridOutput':'Hello World','Readiness':'Passed','Tests':'Passed','CleanRebuild':'Passed'}
assert integration==expectedIntegration
log=(D/f"job-{job['id']}.log").read_bytes(); assert b'Validation passed' in log
result={'Stage':'INDEPENDENT_CI2886570_ORDERED_FULL_VERIFIED','Head':H,'Run':rid,'Job':job['id'],'Artifact':item['id'],'RunConclusion':run['conclusion'],'JobConclusion':job['conclusion'],'RunAttempt':1,'Passed':6570,'Failed':0,'ExactOrderedNames':True,'Old6568OrderedNamesRetained':True,'AddedGroups':added,'MfaCount':501,'RawZipSha256':sha(z.read_bytes()),'RawJobSha256':sha(log),'TestsSha256':sha(p.read_bytes()),'Integration':integration,'ValidationPassedMarker':True,'NoNewDispatch':True,'MainOrGitEdited':False}
out=A/f'native-audit-ci288-{rid}-independent-guard.json'; assert not out.exists(); out.write_text(json.dumps(result,indent=2)+'\n')
S=A/f'native-audit-part289-ci288-{rid}-evidence-staging'; assert not S.exists()
sources=[q for q in sorted(D.rglob('*')) if q.is_file()]+[out,Path(__file__),A/'native-audit-read-ci288.ps1']; records=[]
for q in sources:
 b=q.read_bytes(); dest=f'docs/validation/part-289/ci-288-{rid}/'+q.relative_to(A).as_posix(); d=S/dest; d.parent.mkdir(parents=True,exist_ok=True); d.write_bytes(b); records.append({'SourcePath':q.relative_to(R).as_posix(),'Destination':dest,'Sha256':sha(b),'Bytes':len(b)})
m=S/'manifest.json'; m.write_text(json.dumps({'Stage':'DETACHED_EXACT_HEAD_CI288_FULL_RAW_PROOF','Head':H,'Files':records,'FileCount':len(records),'Bytes':sum(r['Bytes'] for r in records)},indent=2)+'\n')
print(str(out),sha(out.read_bytes())); print(str(m),sha(m.read_bytes()))