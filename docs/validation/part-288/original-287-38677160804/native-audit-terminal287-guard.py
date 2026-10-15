from pathlib import Path
import json,hashlib,subprocess,zipfile,runpy,sys
R=Path.cwd();A=R/'artifacts';H='58d227ecef29d117fa091ab30b1e0928f969d2fa';PIN='ad05823719d77c8faee87348ea39513d4e2f99c5';sha=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes().decode('utf-8-sig'));blob=lambda p:subprocess.check_output(['git','cat-file','blob',H+':'+p]);etf=runpy.run_path(str(A/'part276-faithful-etf-parser.py'))
rid=int(sys.argv[1]);D=A/f'native-audit-run-{rid}-terminal';run=j(D/'run.json');jobs=j(D/'jobs.json')['jobs'];items=j(D/'artifacts.json')['artifacts'];assert run['id']==rid and run['head_sha']==H and run['status']=='completed' and run['run_attempt']==1 and len(jobs)==len(items)==1;job=jobs[0];item=items[0];assert job['head_sha']==H and item['workflow_run']['head_sha']==H
z=D/f"artifact-{item['id']}.zip";ex=D/f"artifact-{item['id']}";assert z.stat().st_size==item['size_in_bytes'] and sha(z.read_bytes())==item['digest'][7:].upper()
with zipfile.ZipFile(z) as f:
 for n in f.namelist():
  if not n.endswith('/'):assert f.read(n)==(ex/n).read_bytes()
log=(D/f"job-{job['id']}.log").read_bytes();common={'Head':H,'Run':rid,'Job':job['id'],'Artifact':item['id'],'RunConclusion':run['conclusion'],'JobConclusion':job['conclusion'],'RunAttempt':1,'RawZipSha256':sha(z.read_bytes()),'RawJobSha256':sha(log),'NoNewDispatch':True,'MainOrGitEdited':False}
if rid==38677107982:
 assert run['conclusion']==job['conclusion']=='success';tests=next(ex.rglob('tests.json'));t=j(tests);expected=json.loads(blob('docs/validation/part-287/tests.json'));assert t['Passed']==6568 and t['Failed']==0 and len(t['Tests'])==6568 and all(r['Status']=='Passed' for r in t['Tests']);names=[r['Name'] for r in t['Tests']];assert names==[r['Name'] for r in expected['Tests']] and len(set(names))==6568 and len([n for n in names if n.startswith('mfa/')])==501
 integration=j(next(ex.rglob('integration-results.json')));assert all(v in ['Passed','Expected failure','Expected failure recorded','Hello World'] for v in integration.values());assert b'Validation passed' in log
 result=dict(common,Stage='INDEPENDENT_CI2876568_ORDERED_FULL_VERIFIED',Passed=6568,Failed=0,ExactOrderedNames=True,TestsSha256=sha(tests.read_bytes()),Integration=integration)
else:
 payload=rid==38677155447;assert payload or rid==38677160804;prefix='tools/Erlang.DistributionPayloadExitObservations/' if payload else 'tools/Erlang.DistributionMonitorPayloadObservations/';source='distribution_payload_exit_probe.erl' if payload else 'distribution_monitor_payload_observer.erl';reportName='distribution-payload-exit-observations.json' if payload else 'distribution-monitor-payload-observations.json';reports=list(ex.rglob(reportName));assert len(reports)==1,'No observation report: preserve actual compile/source failure separately';root=reports[0].parent;plan=j(root/'source-plan.json');report=j(reports[0]);assert (root/source).read_bytes()==blob(prefix+source) and (root/'source-plan.json').read_bytes()==blob(prefix+'source-plan.json');assert report['Pin']==PIN and not report['SdkExpectedUsed'] and not report['SemanticCompatibilityClaim'];assert report['Planned']==(11 if payload else 3)
 for row in report['Files']:
  b=(root/row['Path']).read_bytes();assert len(b)==row['Length'] and sha(b)==row['Sha256']
 proof=j(root/'source-build-evidence.json');assert proof['SourceCommit']==PIN and proof['SourceTag']=='OTP-29.1.1' and proof['SourceBuildCompleted'] and proof['HarnessCommit']==H and proof['RunId']==str(rid) and proof['RunAttempt']=='1';assert sha((root/'source-build.log').read_bytes())==proof['SourceBuildLogSha256'];assert b'PINNED_BEAM_INSTALL_VERIFIED 29.1.1' in log
 runtime=etf['decode']((root/'runtime.etf').read_bytes());rp=etf['project'](runtime);assert rp[:3]==([50,57],[49,55,46,49],'jit') and bytes(rp[3]).decode()==proof['InstalledOtpRoot'] and len(rp)==7 and rp[4] in [4,8]
 cases=plan['Cases'] if payload else plan['Scenarios'];assert [r['Name'] for r in report['Rows']]==cases
 typed=[]
 for row in report['Rows']:
  directory=root/row['Name'];terms={p.name:etf['decode'](p.read_bytes()) for p in sorted(directory.glob('*.etf'))};stage=etf['project'](terms['stage.etf']) if payload and 'stage.etf' in terms else (directory/'stage').read_text() if (directory/'stage').exists() else None
  if row['Captured']:
   assert row['Process']=={'ExitCode':0,'TimedOut':False,'RawComplete':True} and row['Error'] is None
   if payload:assert stage==('complete',row['Name']) and etf['project'](terms['outer.etf'])==('result','ok')
   else:assert stage=='complete'
  typed.append({'Name':row['Name'],'Process':row['Process'],'Captured':row['Captured'],'Error':row['Error'],'Stage':stage,'Terms':terms})
 allCaptured=report['Captured']==report['Planned'] and all(r['Captured'] for r in report['Rows']);assert (run['conclusion']=='success')==allCaptured
 result=dict(common,Stage='INDEPENDENT_ORIGINAL287_TYPED_CAPTURE_NOT_SDK_PARITY' if allCaptured else 'INDEPENDENT_ORIGINAL287_PARTIAL_TYPED_CAPTURE_FAILURE_PRESERVED',Planned=report['Planned'],Captured=report['Captured'],Pin=PIN,RuntimeTyped=runtime,RuntimeWordsize=rp[4],RuntimeArchitecture=bytes(rp[5]).decode(),RuntimeBuildType=rp[6],TypedScenarios=typed,SdkParityClaim=False,SourceAndPlanByteExactPublished=True)
out=A/f'native-audit-terminal287-{rid}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(str(out),sha(out.read_bytes()))
S=A/f'native-audit-part288-run{rid}-evidence-staging';assert not S.exists();sources=[q for q in sorted(D.rglob('*')) if q.is_file()]+[out,Path(__file__),A/'native-audit-read-286-287-runs.ps1',A/'part276-faithful-etf-parser.py'];records=[]
for q in sources:
 b=q.read_bytes();dest=f'docs/validation/part-288/original-287-{rid}/'+q.relative_to(A).as_posix();p=S/dest;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b);records.append({'SourcePath':q.relative_to(R).as_posix(),'Destination':dest,'Sha256':sha(b),'Bytes':len(b)})
m=S/'manifest.json';m.write_text(json.dumps({'Stage':'DETACHED_ORIGINAL287_RUN_COMPLETE_RAW_TYPED_PROOF','Files':records,'FileCount':len(records),'Bytes':sum(r['Bytes'] for r in records)},indent=2)+'\n');print(str(m),sha(m.read_bytes()))
