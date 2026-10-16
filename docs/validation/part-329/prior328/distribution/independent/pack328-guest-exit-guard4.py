"""Independent passive actual319 two-node group-leader replay; no VM/compiler/native process."""
from pathlib import Path,PurePosixPath
import hashlib,importlib.util,json,re,sys
sys.dont_write_bytecode=True
R=Path.cwd();A=R/'artifacts';s=importlib.util.spec_from_file_location('common',A/'pack328-audit-common.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m);mapping,d,run,job,items,allrows=m.load('distribution');assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
E=d/f'artifact-{items[0]["id"]}';O=E/'guest-connected-exit322-original';j=m.read;sha=m.sha;report=j(O/'report.json');assert report['Status']=='PASS_ACTUAL_ORIGINAL_TWO_NODE_GUEST_CONNECTED_EXIT_OBSERVATION' and report['PrimaryFailure'] is None and report['CleanupFailures']==[] and not report['SdkExecuted'] and not report['SdkCompared'] and report['NativeNetworkingUsed']
planpath='tools/Erlang.GuestConnectedExitObservations/source-plan.json';planraw=m.blobs(mapping['Head'],[planpath])[planpath];plan=json.loads(planraw);assert sha(planraw)==report['SourcePlanSha256']=='F40BA9404BF5BE58BE58108365AB85C6B24E9528DA870355FDFE0126DBD3576B'
paths=['tools/Erlang.GuestConnectedExitObservations/'+x['Path'] for x in plan['Sources']];blobs=m.blobs(mapping['Head'],paths);contracts=A/'pack327-guest-exit-published-passive-contracts';assert contracts.is_dir()
for row in plan['Sources']:
 name=row['Path'];b=blobs['tools/Erlang.GuestConnectedExitObservations/'+name];assert len(b)==row['Bytes'] and sha(b)==row['Sha256'] and (O/'sources'/name).read_bytes()==b;assert (contracts/name).read_bytes()==b
assert (O/'source-plan.json').read_bytes()==planraw
members=report['Files'];assert len({x['Path'] for x in members})==len(members)
assert {p.relative_to(O).as_posix() for p in O.rglob('*') if p.is_file() and p.name!='report.json'}=={x['Path'] for x in members}
for row in members:
 p=O/row['Path'];assert p.stat().st_size==row['Bytes'] and sha(p.read_bytes())==row['Sha256']
assert (O/'source-head.stdout').read_text().strip()==plan['PinnedCommit']==report['PinnedCommit'] and (O/'source-status.stdout').read_bytes()==b''
for index,row in enumerate(plan['PinnedSources']):assert sha((O/f'pin-{index}.stdout').read_bytes())==row['Sha256']
build=j(O/'source-build-evidence.json');assert build==j(E/'beam-source-build-evidence.json') and build['SourceCommit']==plan['PinnedCommit'] and build['SourceBuildCompleted'] is True and build['HarnessCommit']==mapping['Head'] and int(build['RunId'])==mapping['Run'] and build['RunAttempt']=='1'
assert build['SourceBuildLogSha256']==sha((O/'source-build.log').read_bytes())==sha((E/'beam-otp-build.log').read_bytes())
compile=j(O/'compile.process.json');assert compile['ExitCode']==0 and not compile['TimedOut'] and compile['PipesClosed'] and compile['ParentReaped'];args=compile['Arguments'];assert args[1:4]==['-Werror','+debug_info','-o'] and PurePosixPath(args[0]).is_relative_to(PurePosixPath(build['InstalledOtpRoot']))
for role in ['server','client','epmd']:
 prefix=O/(role+'/process' if role!='epmd' else 'epmd');p=j(prefix.with_suffix('.process.json'));assert p['OwnedPid']==p['OwnedProcessGroup']>0 and p['ParentReaped'] and p['StreamsClosed'] and p['CleanupFailure'] is None
 assert p['StdoutBytes']==prefix.with_suffix('.stdout').stat().st_size and p['StderrBytes']==prefix.with_suffix('.stderr').stat().st_size
 if role!='epmd':assert p['ExitCode']==0 and p['StderrBytes']==0
 else:assert p['ExitCode']==-15 and p['TerminationRequested']

def imp(name,path):
 s=importlib.util.spec_from_file_location(name,path);v=importlib.util.module_from_spec(s);s.loader.exec_module(v);return v
collector=imp('passive_exact_published319_collector',contracts/'collect.py');decoder=imp('passive_exact_published319_decoder',contracts/'passive_etf.py');names={}
for role in ['server','client']:
 raw=(O/role/'runtime.etf').read_bytes();value,offset=decoder.decode_one(raw);assert offset==len(raw);fields=collector.seq(value,count=10);names[role]=fields[1][1];assert PurePosixPath(collector.text(fields[4]))==PurePosixPath(build['InstalledOtpRoot'])
assert len(compile['Arguments'])==8 and [PurePosixPath(x).name for x in compile['Arguments'][5:]]==['guest_connected_exit322.erl','guest_connected_exit322_driver.erl','guest_connected_bang322.erl']
assert len(report['Tools'])==4 and {PurePosixPath(x['Path']).name for x in report['Tools']}=={'erl','erlc','epmd','beam.smp'}
for tool in report['Tools']:assert PurePosixPath(tool['Path']).is_relative_to(PurePosixPath(build['InstalledOtpRoot'])) and tool['Bytes']>0 and re.fullmatch('[A-F0-9]{64}',tool['Sha256'])
image_md5=tuple(('binary',hashlib.md5((O/'ebin'/(name+'.beam')).read_bytes()).digest()) for name in ['guest_connected_exit322','guest_connected_exit322_driver','guest_connected_bang322'])
for role in ['server','client']:
 value,used=decoder.decode_one((O/role/'runtime.etf').read_bytes());fields=collector.seq(value,count=10);assert fields[0]==collector.atom('guest_connected_exit322_runtime') and fields[5:8]==image_md5 and collector.text(fields[2])=='29' and collector.text(fields[3])=='29.1.1' and collector.text(fields[8]) and collector.text(fields[9])
 process=j(O/role/'process.process.json');args=process['Arguments'];assert PurePosixPath(args[0]).is_relative_to(PurePosixPath(build['InstalledOtpRoot'])) and args[args.index('-name')+1]==names[role] and args[args.index('-start_epmd')+1]=='false' and args[args.index('-pa')+1]==compile['Arguments'][4]
 assert args[args.index('-eval')+1]==('guest_connected_exit322_driver:server(),halt(0).' if role=='server' else 'guest_connected_exit322_driver:client().')
actual=collector.verify_actual(O,decoder,Path(build['InstalledOtpRoot']).resolve(),names)
assert set(actual)==set(report['ActualAssertions'])
displays={'ActualRawOutcome','AllEightRawCaseRows'}
assert all(actual[k]==report['ActualAssertions'][k] for k in actual if k not in displays)
different=sorted(k for k in actual if actual[k]!=report['ActualAssertions'][k]);assert set(different).issubset(displays)
refusal=A/'pack328-guest-exit-strict-repr-refusal.json'
if different:
 assert not refusal.exists()
 refusal.write_text(json.dumps({'Status':'REFUSED_STRICT_REMOTE_PYTHON_REPR_EQUALITY_DISPLAY_ONLY','Head':mapping['Head'],'DifferentFields':different,'PublishedVerifyActualPassed':True,'AllOtherPublishedSemanticAssertionsExact':True,'RawOutcomeEtfSha256':sha((O/'client/outcome.etf').read_bytes()),'RawNormalization':False},indent=2)+'\n')
assert len(actual['OrderedOriginalContracts'])==len(set(actual['OrderedOriginalContracts']))==8 and actual['ExactTokenComparisonBeforeAnyReceive'] and actual['PublicExitHasNoRemoteToken'] and actual['DuplicatePriorityAcceptedOnce'] and actual['UnknownAndImproperNoRemoteExit'] and actual['KillFreshDownKilledWhileRemoteTraps']
assert report['NativeControlWireBytesCaptured'] is False and report['PriorityDeliveryOrderingObserved'] is False and actual['PressureOrSchedulerPhaseObserved'] is False

old=j(E/'distribution-signal-observations/distribution-signal-observations.json');assert old['Captured']==old['Planned']==5 and all(x['Captured'] and x['Error'] is None and x['Process']['ExitCode']==0 and not x['Process']['TimedOut'] and x['Process']['RawComplete'] for x in old['Rows']) and old['SdkExpectedUsed'] is False and old['SemanticCompatibilityClaim'] is False
result={'Status':'PASS_INDEPENDENT_EXACT_HEAD328_ACTUAL_ORIGINAL_TWO_NODE_GUEST_EXPLICIT_EXIT_EIGHT_CONTRACTS','Head':mapping['Head'],'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'RawZipSha256':sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'RawJobSha256':sha((d/f'job-{job["id"]}.log').read_bytes()),'CollectorPublishedSourceSha256':sha((contracts/'collect.py').read_bytes()),'SourcePlanSha256':sha(planraw),'PinnedSourceFilesVerified':len(plan['PinnedSources']),'RawCollectorFilesRehashed':len(members),'ActualOriginalNodes':2,'ActualNodeNames':names,'BothNodesCompiledProbeAndDriverMd5BindActualCapturedBeam':True,'ActualCompilerExitZeroAndOwnedProcessesReaped':True,'OriginalContracts':actual['OrderedOriginalContracts'],'EightRawCasesWithFullTokenBeforeAfterAndRemoteExitHistoryVerified':True,'RawOutcomeAndWorkerServerCleanupRetained':True,'PublishedTypedVerifierIndependentlyReplayed':True,'ExistingFiveInputControllerCapturesComplete':True,'ExistingFiveInputControllerSemanticCompatibilityClaim':False,'New322EightExplicitExitProbeExecuted':True,'SdkExecutedByThisCollector':False,'SdkLivePeerComparisonClaimed':False,'NativeWireByteEqualityClaimed':False,'NoVmNativeSdkCompilerOrGitMutationByReviewer':True,'ToolsScope':'Remote collector captured launcher/emulator identities; this passive replay rehashes actual captured BEAM/source/raw files, not an exported complete OTP runtime binary tree.'}
images=[]
for name in ['guest_connected_exit322','guest_connected_exit322_driver','guest_connected_bang322']:
 beam=O/'ebin'/(name+'.beam');source=O/'sources'/(name+'.erl');images.append({'Module':name,'BeamPath':beam.relative_to(R).as_posix(),'BeamSha256':sha(beam.read_bytes()),'BeamBytes':beam.stat().st_size,'BeamMd5':hashlib.md5(beam.read_bytes()).hexdigest().upper(),'SourceSha256':sha(source.read_bytes())})
result.update(ActualCompiledImages=images,MinimalBang322ActuallyCompiledAndLoadedBothNodes=True,SdkPreparedOrInvokedMinimalBeam=False,ActualBothNodeMetadataFields=10)
result.update(StrictRemotePythonReprEqualityPassed=(actual==report['ActualAssertions']),StrictReprRefusalSha256=sha(refusal.read_bytes()) if different else None,RawOutcomeEtfSha256=sha((O/'client/outcome.etf').read_bytes()),RawEtfAndAllReportFileHashesExact=True,DisplayOnlyFieldsDifferentInThisReplay=sorted(k for k in actual if actual[k]!=report['ActualAssertions'][k]),NoRawNormalization=True,SemanticAssertionsComparedExactly=True)
result.update(FreshReplayNoAssumedDisplayDifferenceSet=True,StrictRefusalCreatedOnlyIfActualDifference=bool(different))
p=A/'pack328-guest-exit-independent-guard.json';assert not p.exists();p.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({'Path':p.relative_to(R).as_posix(),'Sha256':sha(p.read_bytes()),'Status':result['Status']}))
