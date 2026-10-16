"""Passive published323 raw replay only; never calls collector.main or a VM."""
from pathlib import Path,PurePosixPath
import hashlib,json,importlib.util,sys,re
sys.dont_write_bytecode=True
R=Path.cwd();A=R/'artifacts'
def imp(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
m=imp('common323cw',A/'part332-distribution-independent/audit-common.py');mapping,d,run,job,items,allrows=m.load('distribution');assert mapping['Head']=='aaf48552bee429b0447d3e28e8274d34ad590d73' and mapping['Run']==38911991552 and job['id']==117009874087 and len(items)==1 and items[0]['id']==m.expected['Artifact']
assert run['conclusion']==job['conclusion']=='success' and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
E=d/('artifact-'+str(items[0]['id']));O=E/'distribution-control-wait323-original';j=m.read;sha=m.sha;report=j(O/'report.json');assert report['Status']=='PASS_ACTUAL_ORIGINAL_THREE_NODE_BUSY_QUERY_AND_REMOTE_KILL' and report['PrimaryFailure'] is None and report['CleanupFailures']==[] and report['SdkExecuted'] is False and report['NativeWireCaptured'] is False and report['TicketTurnEquivalent'] is False
planpath='tools/Erlang.DistributionControlWaitObservations/source-plan.json';raw=m.blobs(mapping['Head'],[planpath])[planpath];plan=json.loads(raw);assert sha(raw)==report['SourcePlanSha256']=='146C1DFA51058C54D8C72DAFEAAB24D82F5684B3EA5C9CB0CE1B35724D070705' and (O/'source-plan.json').read_bytes()==raw
contracts=A/'pack327-control-wait-published-passive-contracts';assert contracts.is_dir();paths=['tools/Erlang.DistributionControlWaitObservations/'+x['Path'] for x in plan['Sources']];blobs=m.blobs(mapping['Head'],paths)
for x in plan['Sources']:
 b=blobs['tools/Erlang.DistributionControlWaitObservations/'+x['Path']];assert len(b)==x['Bytes'] and sha(b)==x['Sha256'] and (O/'sources'/x['Path']).read_bytes()==b;assert (contracts/x['Path']).read_bytes()==b
members=report['Files'];assert len(members)==len({x['Path'] for x in members});assert {p.relative_to(O).as_posix() for p in O.rglob('*') if p.is_file() and p.name!='report.json'}=={x['Path'] for x in members}
for x in members:
 p=O/x['Path'];assert p.stat().st_size==x['Bytes'] and sha(p.read_bytes())==x['Sha256']
assert (O/'source-head.stdout').read_text().strip()==plan['PinnedCommit']==report['PinnedCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and not (O/'source-status.stdout').read_bytes()
for i,x in enumerate(plan['PinnedSources']):assert sha((O/f'pin-{i}.stdout').read_bytes())==x['Sha256']
build=j(O/'source-build-evidence.json');assert build==j(E/'beam-source-build-evidence.json') and build['SourceCommit']==plan['PinnedCommit'] and build['SourceBuildCompleted'] is True and build['HarnessCommit']==mapping['Head'] and int(build['RunId'])==mapping['Run'] and str(build['RunAttempt'])=='1';assert build['SourceBuildLogSha256']==sha((E/'beam-otp-build.log').read_bytes())
compile=j(O/'compile.process.json');assert compile['ExitCode']==0 and compile['TimedOut'] is False and compile['PipesClosed'] and compile['ParentReaped'];args=compile['Arguments'];assert len(args)==7 and args[1:4]==['-Werror','+debug_info','-o'] and PurePosixPath(args[0]).is_relative_to(PurePosixPath(build['InstalledOtpRoot']));assert [PurePosixPath(z).name for z in args[5:]]==['distribution_control_wait323.erl','distribution_control_wait323_driver.erl']
c=imp('published323controlwait',contracts/'collect.py');decoder=imp('published323controldecoder',contracts/'passive_control_etf.py');images={n:hashlib.md5((O/'ebin'/(n+'.beam')).read_bytes()).digest() for n in c.MODULES};assert len(images)==2
assert [x['Mode'] for x in report['ActualCases']]==['send','link'];verified=[]
for actual in report['ActualCases']:
 mode=actual['Mode'];case=O/mode;names=actual['Nodes'];assert set(names)=={'a','b','c'} and len(set(names.values()))==3
 for role in ['a','b','c']:
  prefix=case/role/'process';p=j(prefix.with_suffix('.process.json'));assert p['OwnedPid']==p['OwnedProcessGroup']>0 and p['ExitCode']==0 and p['ParentReaped'] and p['StreamsClosed'] and p['CleanupFailure'] is None and p['StderrBytes']==0 and p['ElapsedSeconds']<45
  assert p['StdoutBytes']==prefix.with_suffix('.stdout').stat().st_size and p['StderrBytes']==prefix.with_suffix('.stderr').stat().st_size
  cmd=p['Arguments'];assert cmd[0]==str(PurePosixPath(build['InstalledOtpRoot'])/'bin/erl') and cmd[cmd.index('-name')+1]==names[role] and cmd[cmd.index('-start_epmd')+1]=='false' and cmd[cmd.index('-pa')+1]==args[4] and cmd[cmd.index('-eval')+1]=='distribution_control_wait323_driver:start().'
  raw=c.decode(case/role/'runtime.etf',decoder);fields=c.seq(raw,count=12);assert PurePosixPath(c.text(fields[5]))==PurePosixPath(build['InstalledOtpRoot']);c.verify_metadata(case/role/'runtime.etf',decoder,role,names,Path(build['InstalledOtpRoot']).resolve(),images,p['OwnedPid'])
  connection=c.seq(c.decode(case/role/'connections.etf',decoder),count=3);assert connection[0]==c.atom(names[role]) and set(c.seq(connection[1],'list'))=={c.atom(v) for k,v in names.items() if k!=role}
  pairs=c.seq(connection[2],'list',count=2);assert {c.seq(x,count=2)[0] for x in pairs}=={c.atom(v) for k,v in names.items() if k!=role}
  for pair in pairs:c.node_of(c.seq(pair,count=2)[1],'port',names[role])
 mapper=j(case/'epmd.process.json');assert mapper['ExitCode']==-15 and mapper['TerminationRequested'] and mapper['ParentReaped'] and mapper['StreamsClosed'] and mapper['CleanupFailure'] is None
 value=c.decode(case/'a/measurement.etf',decoder);proof=c.verify_observation(value,mode,names);assert proof==actual['Proof'];cleanup=c.verify_cleanup(case,decoder,mode,names,value);assert cleanup==actual['ActualCleanup']
 journal=j(case/'journal.json');c.verify_timeline(journal);ph={x['Phase']:x for x in journal};stop=(ph['SIGCONT']['ParentMonotonicNs']-ph['SIGSTOP-confirmed-T']['ParentMonotonicNs'])/1e9;assert 0<stop<20 and (journal[-1]['ParentMonotonicNs']-journal[0]['ParentMonotonicNs'])/1e9<45;last=journal[-1];assert last['Phase']=='session-closed' and last['PrimaryFailure'] is None and last['CleanupFailures']==[] and last['StopIssued'] and last['ResumeIssued']
 row=c.seq(value,count=8);query=c.seq(row[5],count=3);kill=c.seq(row[6],count=4);ids=c.seq(row[2],count=8);assert c.decode(case/'a/query.etf',decoder)==('tuple',(query[1],query[2]));assert c.decode(case/'c/kill-call.etf',decoder)==('tuple',(ids[1],kill[1],ids[5],ids[2],('tuple',(c.atom('return'),c.atom('true')))))
 request=c.seq(c.decode(case/'a/resume-request.etf',decoder),count=5);assert request[4]==('tuple',(c.atom('return'),value));assert c.decode(case/'a/phase-outcome.etf',decoder)==request[4]
 verified.append(dict(Mode=mode,Nodes=names,MetadataRoles=3,ImagesPerRole=2,TargetBusyAndFullOwnerQuery=True,FreshRemoteCExitAndDownKilledBeforeOwnedCont=True,StoppedSeconds=stop,PrimaryAndAllWorkerCleanupVerified=True))
assert len(report['Tools'])==4 and all(PurePosixPath(x['Path']).is_relative_to(PurePosixPath(build['InstalledOtpRoot'])) and x['Bytes']>0 and re.fullmatch('[A-F0-9]{64}',x['Sha256']) for x in report['Tools'])
result=dict(Status='PASS_INDEPENDENT_PUBLISHED331_ACTUAL_THREE_NODE_BUSY_QUERY_REMOTE_KILL_TWO_SCENARIOS',Head=mapping['Head'],Run=mapping['Run'],Job=job['id'],Artifact=items[0]['id'],Attempt=1,SourcePlanSha256=sha(raw) if False else sha((O/'source-plan.json').read_bytes()),CollectorPublishedSourceSha256=sha((contracts/'collect.py').read_bytes()),PinnedSourcesVerified=len(plan['PinnedSources']),CollectorFilesRehashed=len(members),Cases=verified,RawZipSha256=sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),RawJobSha256=sha((d/f'job-{job["id"]}.log').read_bytes()),NativeActualVmCountPerScenario=3,ActualCompiledImageCountPerVm=2,OriginalTargetBusyIsNotSdkPreEnqueueCapacityPhase=True,NativeTicketTurnEquivalenceClaimed=False,NativeWireCaptured=False,SdkLivePeerOrWireEqualityClaimed=False,LinkRetention325ObservedByThisProbe=False,NoReviewerVmNativeSdkBuildNetworkMainGitMutation=True)
output=A/'part332-distribution-independent/control-wait-independent-guard.json';assert not output.exists();output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(dict(Path=output.relative_to(R).as_posix(),Sha256=sha(output.read_bytes()),Status=result['Status'])))
