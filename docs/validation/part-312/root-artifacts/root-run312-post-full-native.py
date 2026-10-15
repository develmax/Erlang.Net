from pathlib import Path
import hashlib,json,os,runpy,subprocess,time
r=Path.cwd();a=r/'artifacts';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert read(a/'root-part312-revision2/full-exit.json')['ExitCode']==0 and read(a/'tests.json')['Passed']==6640
freeze=a/'root-part312-revision2/source-freeze.json';f=read(freeze);assert f['SourcePaths']==38 and f['CSharpPaths']==35
closure=a/'root-part312-revision2/source-resource-closure.json';assert sha(closure.read_bytes())==f['FinalSourceClosureSha256']
source_rows=read(closure)['Files'];assert len(source_rows)==2171
def check_sources():
 for row in source_rows:
  b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
check_sources()
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';assert sha(plan.read_bytes())==f['ReviewedSourcePlanSha256'] and len(read(plan)['Files'])==1038
out=a/'root-part312-main-native';assert not out.exists();out.mkdir()
fixture=a/'spawn-protocol-nif-290-38688662194-terminal/artifact-11951535100/nif-fixture-build/fixture.dll'
assert sha(fixture.read_bytes())=='235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F'
tool=r/'tools/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
runtime=tool.with_name('Erlang.Runtime.dll');assert runtime.read_bytes()==(r/'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()
binding=[{'Path':str(p.relative_to(r)).replace('\\','/'),'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())} for p in (fixture,tool,runtime)]
environment=os.environ.copy();environment['DOTNET_PROCESSOR_COUNT']='2'
command=['dotnet',str(tool),str(fixture),sha(fixture.read_bytes())]
(out/'attempt.json').write_text(json.dumps({'ActualMainAfterFull':True,'Command':command,'Bindings':binding},indent=2)+'\n',encoding='utf-8')
start=time.time()
try:
 p=subprocess.run(command,cwd=r,env=environment,capture_output=True,timeout=120);stdout,stderr,code,timed=p.stdout,p.stderr,p.returncode,False
except subprocess.TimeoutExpired as failure:
 stdout,stderr,code,timed=failure.stdout or b'',failure.stderr or b'',None,True
(out/'native.stdout').write_bytes(stdout);(out/'native.stderr').write_bytes(stderr)
process={'ExitCode':code,'TimedOut':timed,'ElapsedSeconds':time.time()-start,'ActualMainAfterFull':True,'NativeExecuted':True,'Bindings':binding}
(out/'process.json').write_text(json.dumps(process,indent=2)+'\n',encoding='utf-8')
assert not timed and code==0
for row in binding:assert sha((r/row['Path']).read_bytes())==row['Sha256']
check_sources()
reader=r/'tools/Erlang.NifPendingVarargsObservations/reviewed_sdk_stdout.py'
parsed=runpy.run_path(str(reader))['parse_sdk_stdout'](stdout,stderr,require_abi=False)
assert parsed['GeneratedCSharpSha256']=='6E28E8B093F042C22ACAFD2C0213A55C7DCEFC2D162B486AC8443FC7C4E5F77A'
result={'Status':'ACTUAL_MAIN_POST_FULL_WINDOWS_NATIVE_SEVEN_ORDERED_LINES_AND_THREE_ALLOCATION_DOMAIN_ROLES_PASS','Process':process,'StdoutSha256':sha(stdout),'StderrBytes':len(stderr),'ReaderSha256':sha(reader.read_bytes()),'Parsed':parsed,'SourceHashesExact':True,'SourceFreezeSha256':sha(freeze.read_bytes()),'SourceClosurePathsVerifiedBeforeAndAfter':2171,'ReviewedSourcePlanSha256':sha(plan.read_bytes()),'NativeTrustedWindowsProfilePassed':True,'ActualGeneratedCSharpRoutePassed':True,'LinuxAbiExecuted':False,'OriginalExecuted':False,'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False}
(out/'guard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
