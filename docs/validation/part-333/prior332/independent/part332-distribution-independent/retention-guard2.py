"""Passive published325 evidence replay only. Never invokes collector.main, a VM, or SDK."""
from pathlib import Path,PurePosixPath
import hashlib,json,importlib.util,sys,re
sys.dont_write_bytecode=True
R=Path.cwd();A=R/'artifacts'
def imp(name,path):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
m=imp('common325retention',A/'part332-distribution-independent/audit-common.py');mapping,d,run,job,items,allrows=m.load('distribution');j=m.read;sha=m.sha
assert mapping['Head']=='aaf48552bee429b0447d3e28e8274d34ad590d73' and mapping['Run']==38911991552 and job['id']==117009874087 and len(items)==1 and items[0]['id']==m.expected['Artifact']
assert run['conclusion']==job['conclusion']=='success' and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
E=d/('artifact-'+str(items[0]['id']));O=E/'connected-link-ownership325-original';report=j(O/'report.json')
assert report['Status']=='PASS_ACTUAL_ORIGINAL_THREE_NODE_RETAINED_LINK_AFTER_REMOTE_KILL' and report['PrimaryFailure'] is None and report['CleanupFailures']==[]
assert report['SdkExecuted'] is False and report['NativeWireCaptured'] is False and report['TicketTurnEquivalent'] is False
planpath='tools/Erlang.DistributionLinkRetentionObservations/source-plan.json';raw=m.blobs(mapping['Head'],[planpath])[planpath];plan=json.loads(raw)
assert sha(raw)==report['SourcePlanSha256']=='A62852C3F0148AD1381F62CD470BE858C03B9502613C1761E88718E4A5CAC323' and (O/'source-plan.json').read_bytes()==raw
assert plan['OriginalNodesPerScenario']==3 and plan['Scenarios']==['normal','async'] and len(plan['Sources'])==9 and len(plan['PinnedSources'])==14
contracts=A/'pack327-retention-published-passive-contracts';assert contracts.is_dir()
paths=['tools/Erlang.DistributionLinkRetentionObservations/'+x['Path'] for x in plan['Sources']];blobs=m.blobs(mapping['Head'],paths)
for x in plan['Sources']:
 b=blobs['tools/Erlang.DistributionLinkRetentionObservations/'+x['Path']];assert len(b)==x['Bytes'] and sha(b)==x['Sha256'] and (O/'sources'/x['Path']).read_bytes()==b;assert (contracts/x['Path']).read_bytes()==b
members=report['Files'];assert len(members)==len({x['Path'] for x in members})==145
assert {p.relative_to(O).as_posix() for p in O.rglob('*') if p.is_file() and p.name!='report.json'}=={x['Path'] for x in members}
for x in members:
 p=O/x['Path'];assert p.stat().st_size==x['Bytes'] and sha(p.read_bytes())==x['Sha256']
assert (O/'source-head.stdout').read_text().strip()==plan['PinnedCommit']==report['PinnedCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and not(O/'source-status.stdout').read_bytes()
for index,x in enumerate(plan['PinnedSources']):assert sha((O/f'pin-{index}.stdout').read_bytes())==x['Sha256']
build=j(O/'source-build-evidence.json');assert build==j(E/'beam-source-build-evidence.json') and build['SourceCommit']==plan['PinnedCommit'] and build['SourceBuildCompleted'] is True and build['HarnessCommit']==mapping['Head'] and int(build['RunId'])==mapping['Run'] and str(build['RunAttempt'])=='1'
assert build['SourceBuildLogSha256']==sha((E/'beam-otp-build.log').read_bytes())
c=imp('published325retention',contracts/'collect.py');decoder=imp('published325retentionetf',contracts/'passive_control_etf.py')
compile=j(O/'compile.process.json');args=compile['Arguments'];assert compile['ExitCode']==0 and compile['TimedOut'] is False and compile['PipesClosed'] and compile['ParentReaped']
assert len(args)==8 and args[:4]==[str(PurePosixPath(build['InstalledOtpRoot'])/'bin/erlc'),'-Werror','+debug_info','-o']
original_output=PurePosixPath(args[4]).parent;assert PurePosixPath(args[4])==original_output/'ebin'
assert args[5:]==[str(original_output/'sources'/(name+'.erl')) for name in c.MODULES]
images={name:hashlib.md5((O/'ebin'/(name+'.beam')).read_bytes()).digest() for name in c.MODULES};image_rows=[dict(Module=name,Bytes=(O/'ebin'/(name+'.beam')).stat().st_size,Sha256=sha((O/'ebin'/(name+'.beam')).read_bytes()),FullImageMd5=images[name].hex().upper()) for name in c.MODULES]
verified=[];assert [x['Mode'] for x in report['ActualCases']]==['normal','async']
for actual in report['ActualCases']:
 mode=actual['Mode'];names=actual['Nodes'];case=O/mode;nonce=actual['ActualReceiverExit']['Nonce'];assert re.fullmatch('[0-9a-f]{32}',nonce) and len(set(names.values()))==3
 for role in ['a','b','c']:
  prefix=case/role/'process';process=j(prefix.with_suffix('.process.json'))
  assert process['ExitCode']==0 and process['ParentReaped'] and process['StreamsClosed'] and process['CleanupFailure'] is None and process['StderrBytes']==0 and process['ElapsedSeconds']<45
  assert process['StdoutBytes']==prefix.with_suffix('.stdout').stat().st_size and process['StderrBytes']==prefix.with_suffix('.stderr').stat().st_size
  argv=process['Arguments'];assert argv[0]==str(PurePosixPath(build['InstalledOtpRoot'])/'bin/erl') and argv[argv.index('-name')+1]==names[role] and argv[argv.index('-start_epmd')+1]=='false' and argv[argv.index('-pa')+1]==args[4] and argv[argv.index('-eval')+1]=='distribution_link_retention325_driver:start().'
  metadata=c.seq(c.decode(case/role/'runtime.etf',decoder),count=12);assert PurePosixPath(c.text(metadata[5]))==PurePosixPath(build['InstalledOtpRoot'])
  c.verify_metadata(case/role/'runtime.etf',decoder,role,names,Path(build['InstalledOtpRoot']).resolve(),images,process['OwnedPid'])
  connection=c.seq(c.decode(case/role/'connections.etf',decoder),count=3);assert connection[0]==c.atom(names[role]) and set(c.seq(connection[1],'list'))=={c.atom(v) for k,v in names.items() if k!=role}
  pairs=c.seq(connection[2],'list',count=2);assert {c.seq(x,count=2)[0] for x in pairs}=={c.atom(v) for k,v in names.items() if k!=role}
  for pair in pairs:c.node_of(c.seq(pair,count=2)[1],'port',names[role])
 epmd=j(case/'epmd.process.json');assert epmd['ExitCode']==-15 and epmd['TerminationRequested'] and epmd['ParentReaped'] and epmd['StreamsClosed'] and epmd['CleanupFailure'] is None
 value=c.decode(case/'a/measurement.etf',decoder);proof=c.verify_observation(value,mode,names);assert proof==actual['Proof']
 assert c.verify_receiver(case,decoder,nonce,value)==actual['ActualReceiverExit'] and c.verify_cleanup(case,decoder,mode,names,value)==actual['ActualCleanup']
 journal=j(case/'journal.json');c.verify_timeline(journal);ph={x['Phase']:x for x in journal};stop=(ph['SIGCONT']['ParentMonotonicNs']-ph['SIGSTOP-confirmed-T']['ParentMonotonicNs'])/1e9
 assert 0<stop<20 and (journal[-1]['ParentMonotonicNs']-journal[0]['ParentMonotonicNs'])/1e9<45
 last=journal[-1];assert last['Phase']=='session-closed' and last['PrimaryFailure'] is None and last['CleanupFailures']==[] and last['StopIssued'] and last['ResumeIssued']
 row=c.seq(value,count=8);ids=c.seq(row[2],count=8);query=c.seq(row[5],count=3);kill=c.seq(row[6],count=4)
 assert c.decode(case/'a/query.etf',decoder)==('tuple',(query[1],query[2]))
 assert c.decode(case/'c/kill-call.etf',decoder)==('tuple',(ids[1],kill[1],ids[5],ids[2],('tuple',(c.atom('return'),c.atom('true')))))
 request=c.seq(c.decode(case/'a/resume-request.etf',decoder),count=5);assert request[4]==('tuple',(c.atom('return'),value)) and c.decode(case/'a/phase-outcome.etf',decoder)==request[4]
 assert ph['proof-query-and-killed-before-resume']['RequestSha256']==sha((case/'a/resume-request.etf').read_bytes())
 verified.append(dict(Mode=mode,Nodes=names,Nonce=nonce,CompiledImagesPerRole=3,ActualVmRoles=3,FullOwnerQuery=True,NormalTargetBusy=mode=='normal',AsyncReturnedBeforeDrainWithoutTargetBusy=mode=='async',IndependentCPublicKillAndFreshDownKilledBeforeCont=True,ReceiverNonceLinkedExitKilledAfterCont=True,StoppedSeconds=stop,AllWorkerAndVmCleanupSettled=True))
assert len(report['Tools'])==4 and all(PurePosixPath(x['Path']).is_relative_to(PurePosixPath(build['InstalledOtpRoot'])) and x['Bytes']>0 and re.fullmatch('[A-F0-9]{64}',x['Sha256']) for x in report['Tools'])
result=dict(Status='PASS_INDEPENDENT_PUBLISHED331_ACTUAL_ORIGINAL_NORMAL_AND_ASYNC_RETAINED_LINK_AFTER_REMOTE_KILL',Passed=True,Head=mapping['Head'],Run=mapping['Run'],Job=job['id'],Artifact=items[0]['id'],Attempt=1,SourcePlanSha256=sha((O/'source-plan.json').read_bytes()),PinnedSourcesVerified=14,CollectorSourceFilesVerified=9,CollectorFilesRehashed=len(members),WholeRemoteCaptureRowsRehashed=len(allrows),Cases=verified,CompiledImages=image_rows,RawZipSha256=sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),RawJobSha256=sha((d/f'job-{job["id"]}.log').read_bytes()),ActualOriginalRetentionObserved=True,SdkLivePeerOrWirePhaseEqualityClaimed=False,InheritedGlSendLinkExitControlWaitScopesNotReplayedByThisGuard=True,Original331OrdinaryCasesExecutedByThisGuard=False,ReviewerExecutedVmNativeSdkBuildNetworkOrMainGitMutation=False)
output=A/'part332-distribution-independent/retention-independent-guard.json';assert not output.exists();output.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n');print(json.dumps(dict(Path=output.relative_to(R).as_posix(),Sha256=sha(output.read_bytes()),Status=result['Status'])))
