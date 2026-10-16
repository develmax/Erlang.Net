"""Independent passive actual319 two-node group-leader replay; no VM/compiler/native process."""
from pathlib import Path,PurePosixPath
import hashlib,importlib.util,json,re,sys
sys.dont_write_bytecode=True
R=Path.cwd();A=R/'artifacts';s=importlib.util.spec_from_file_location('common',A/'pack324-audit-common.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m);mapping,d,run,job,items,allrows=m.load('distribution');assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
E=d/f'artifact-{items[0]["id"]}';O=E/'guest-connected-exit322-original';j=m.read;sha=m.sha;report=j(O/'report.json');assert report['Status']=='PASS_ACTUAL_ORIGINAL_TWO_NODE_GUEST_CONNECTED_EXIT_OBSERVATION' and report['PrimaryFailure'] is None and report['CleanupFailures']==[] and not report['SdkExecuted'] and not report['SdkCompared'] and report['NativeNetworkingUsed']
planpath='tools/Erlang.GuestConnectedExitObservations/source-plan.json';planraw=m.blobs(mapping['Head'],[planpath])[planpath];plan=json.loads(planraw);assert sha(planraw)==report['SourcePlanSha256']=='F40BA9404BF5BE58BE58108365AB85C6B24E9528DA870355FDFE0126DBD3576B'
paths=['tools/Erlang.GuestConnectedExitObservations/'+x['Path'] for x in plan['Sources']];blobs=m.blobs(mapping['Head'],paths);contracts=A/'pack324-guest-exit-published-passive-contracts';contracts.mkdir(exist_ok=True)
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
diff={}
for key in set(actual)|set(report['ActualAssertions']):
 left,right=actual.get(key),report['ActualAssertions'].get(key)
 if left!=right:
  assert isinstance(left,str) and isinstance(right,str)
  i=next((i for i,(x,y) in enumerate(zip(left,right)) if x!=y),min(len(left),len(right)))
  diff[key]=dict(LocalSha256=sha(left.encode()),RemoteSha256=sha(right.encode()),FirstDifference=i,LocalWindow=left[max(0,i-60):i+160],RemoteWindow=right[max(0,i-60):i+160],LocalLength=len(left),RemoteLength=len(right))
v=dict(Status='STRICT_REMOTE_REPR_COMPARISON_REFUSED_PASSIVE_DIAGNOSIS',Head=mapping['Head'],DifferentFields=diff,AllOtherPublishedSemanticAssertionsExact=all(actual[k]==report['ActualAssertions'][k] for k in actual if k not in diff),PublishedVerifyActualPassed=True,RawOutcomeEtfSha256=sha((O/'client/outcome.etf').read_bytes()),NoNativeVmSdkExecuted=True)
p=A/'pack324-guest-exit-strict-repr-refusal.json';assert not p.exists();p.write_text(json.dumps(v,indent=2)+'\n');print(json.dumps(v))
