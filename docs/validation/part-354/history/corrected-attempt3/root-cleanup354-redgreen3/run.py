from pathlib import Path
import hashlib, importlib.util, json, os, shutil, argparse
P=Path(__file__).resolve().parent
R=P.parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def write(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
ap=argparse.ArgumentParser();ap.add_argument('--output',required=True);ap.add_argument('--peer-report',required=True);a=ap.parse_args()
peer=Path(a.peer_report).resolve();assert J(peer)['Status'].startswith('PASS_')
for row in J(P/'source-freeze.json')['Files']:
 q=P/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
o=Path(a.output).resolve();assert o.is_relative_to(R/'artifacts') and not o.exists()
base=R/'artifacts/size-bif354-full-candidate2'
plan_path=R/'artifacts/size-bif354-full2-preparation/composition-plan.json'
assert H(plan_path)=='2E0A920C615F96CE8FE0F4BA91DFE0EDB6DF4853ABB71801E78FF67E8C602A2B'
plan={x['Path']:x for x in J(plan_path)['Files']}
old_rows=J(R/'artifacts/halt-port354-gates-probe2/targets.json')['ActualFullDlls']
assert H(R/'artifacts/halt-port354-gates-probe2/targets.json')=='8B4FDF894EACACE6E853CC99033E9BC16C624116799734D6ABC55F62677607D1'
for x in old_rows:
 assert H(base/x['Path'])==x['Sha256']
control=R/'artifacts/cleanup-stealing354-regression-proposal2'
patch=R/'artifacts/halt-cleanup354-stealing-proposal'
for directory,expected in [(control,'59383C4AEE2909C659B94E77025A1A992EA6A100C0A2571230D5045C5A27F7B4'),(patch,'63754A3D92C96A57CCDB6126D6EB148B589384A6C3C151B957AF1FF6F3B914A0')]:
 assert H(directory/'source-freeze.json')==expected
 for row in J(directory/'source-freeze.json')['Files']:
  q=directory/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
jobs=P/'jobs.py';assert H(jobs)==J(P/'source-freeze.json')['JobsSha256']
spec=importlib.util.spec_from_file_location('cleanup_jobs',jobs);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
helper=R/'artifacts/spawn-priority351-preparation/execute-root.py'
assert H(helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
spec=importlib.util.spec_from_file_location('cleanup_build',helper);b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
o.mkdir();green=o/'green-source';green.mkdir();inputs=[]
for name in ['Directory.Build.props','global.json']:
 shutil.copyfile(base/name,green/name);inputs.append({'Path':name,'Sha256':H(base/name)})
for project in ['Erlang.Runtime','Erlang.Terms','Erlang.Serialization']:
 for f in sorted((base/'src'/project).rglob('*')):
  if not f.is_file() or any(x in ('bin','obj') for x in f.relative_to(base).parts):continue
  rel=f.relative_to(base);q=green/rel;q.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(f,q);inputs.append({'Path':rel.as_posix(),'Sha256':H(f)})
for x in inputs:
 assert x['Sha256']==plan[x['Path']]['Sha256']
target=green/'src/Erlang.Runtime/RuntimeActorDispatcher.cs'
assert H(target)=='0AF57D1F9F65CE153129E6D9718B3F545F7CE83FFE409E6889F51957040C3555'
shutil.copyfile(patch/'source/src/Erlang.Runtime/RuntimeActorDispatcher.cs',target)
write(o/'inputs.json',{'SourceInputs':inputs,'DispatcherSha256':H(target),'PeerReportSha256':H(peer)})
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
dotnet='C:/Program Files/dotnet/dotnet.exe'
b.owned([dotnet,'build',str(green/'src/Erlang.Runtime/Erlang.Runtime.csproj'),'--disable-build-servers','-m:1','-nr:false','--nologo','-v:minimal','--output',str(o/'runtime')],o,'runtime-build',120,env,R)
source=o/'harness';source.mkdir()
for name in ['CleanupStealingHostChecks.cs','CleanupStealingHostNames.cs']:
 shutil.copyfile(control/'source/tests/Erlang.Tests'/name,source/name)
for name in ['Main.cs','Diagnostic.csproj']:shutil.copyfile(P/name,source/name)
b.owned([dotnet,'build',str(source/'Diagnostic.csproj'),'--disable-build-servers','-m:1','-nr:false','--nologo','-v:minimal','--output',str(o/'build')],o,'harness-build',120,env,R)
dll=o/'build/Cleanup354.dll';harness_hash=H(dll);attempts=[]
for label,runtime in [('red',base/'tests/Erlang.Tests/bin/Debug/net10.0'),('green',o/'runtime')]:
 d=o/label;d.mkdir();out=d/'outcome.json'
 bound=[{'Path':str(runtime/name),'Sha256':H(runtime/name)} for name in ['Erlang.Runtime.dll','Erlang.Terms.dll','Erlang.Serialization.dll']]
 receipt=m.invoke([dotnet,str(dll),str(runtime),str(out)],d,60,env,R)
 outcome=J(out) if out.exists() else None
 assert outcome is not None and {Path(x['Path']).name for x in outcome['LoadedRuntime']} >= {'Erlang.Runtime.dll','Erlang.Terms.dll'}
 assert all(any(x['Path'].casefold()==y['Path'].casefold() and x['Sha256']==y['Sha256'] for y in bound) for x in outcome['LoadedRuntime'])
 for x in old_rows:assert H(base/x['Path'])==x['Sha256']
 assert H(dll)==harness_hash and all(H(Path(x['Path']))==x['Sha256'] for x in bound)
 attempts.append({'Label':label,'Inputs':bound,'Outcome':outcome,'Process':receipt});write(o/'attempts.json',attempts)
 assert receipt['Settled'] and not receipt['TimedOut'] and not receipt['Failures'] and not receipt['JobTerminationRequested'] and receipt['RemainingJobProcesses']==0
red,green_result=attempts
assert red['Process']['ExitCode']==1 and red['Outcome']['InnerCount']==1 and red['Outcome']['InnerType']=='System.TimeoutException'
assert 'CleanupStealingHostChecks.cs:line 71' in red['Outcome']['Failure']
assert 'CLEANUP_BLOCKER_ENTERED' in (o/'red/process.stdout.log').read_text() and 'CLEANUP_OBSERVER_ENTERED_BEFORE_RELEASE' not in (o/'red/process.stdout.log').read_text()
assert green_result['Process']['ExitCode']==0 and green_result['Outcome']['Passed']
assert 'CLEANUP_OBSERVER_ENTERED_BEFORE_RELEASE' in (o/'green/process.stdout.log').read_text()
write(o/'completion.json',{'Status':'PASS_DETERMINISTIC_REAL_ACTOR_RED_GREEN_CLEANUP_STEALING','Attempts':attempts,'HarnessSha256':harness_hash,'OriginalOtpExecuted':False,'FullValidationExecuted':False})
