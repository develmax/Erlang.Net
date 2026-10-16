from pathlib import Path
import argparse,hashlib,json,os,subprocess,time
p=argparse.ArgumentParser();p.add_argument('phase',choices=['green-build','green-run','red-build','red-host']);a=p.parse_args();r=Path.cwd();art=r/'artifacts';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
recipe=art/'process-info-async-dist324-managed-proposal1';freeze=recipe/'source-freeze.json';assert h(freeze.read_bytes())=='A9480397A3676C5F38D6F7025413249C561A61DF73BD9D0E06BB8D2CD5B1E9C3'
for e in read(freeze)['Files']:
 raw=(recipe/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256']
green=art/'process-info-async-dist324-focus2';red=art/'process-info-async-dist324-historical-red1';folder=green if a.phase.startswith('green') else red;candidate=folder/'candidate';fp=folder/('formatted-source-freeze.json' if folder==green else 'source-freeze.json');cl=folder/('formatted-source-resource-closure.json' if folder==green else 'source-resource-closure.json')
expected='5DB2781B3B5A67D93235989967DBA02592A88C9BAED5DB9AC8B8F206A6AFB2E9' if folder==green else '6D0D1DA69FD8C11336A63E25AAE27A48400B394831A25268ACAE5E42038ED885';assert h(fp.read_bytes())==expected
closure_sha=read(fp)['SourceClosureSha256'];assert h(cl.read_bytes())==closure_sha
rows=read(cl)['Files'];assert len(rows)==2353

def source_exact():
 assert h(fp.read_bytes())==expected and h(cl.read_bytes())==closure_sha
 for e in rows:
  raw=(candidate/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
source_exact();out=folder/('root-'+a.phase+'-execution');assert not out.exists();out.mkdir();report=out/'actual.json';limit=180 if a.phase.endswith('build') else 120
settings=['--disable-build-servers','-m:1','-nr:false','-p:UseSharedCompilation=false','-p:RestoreDisableParallel=true','--nologo']
if a.phase=='green-build':cmd=['dotnet','build',str(recipe/'ProcessInfoManagedFocus.csproj'),'-p:FocusDifferentialProject='+str(candidate/'tools/Erlang.Differential/Erlang.Differential.csproj'),*settings]
elif a.phase=='red-build':cmd=['dotnet','build',str(candidate/'tests/Erlang.Tests/Erlang.Tests.csproj'),*settings]
elif a.phase=='green-run':cmd=['dotnet',str(recipe/'bin/Debug/net10.0/ProcessInfoManagedFocus.dll'),str(report),str(fp),expected,'all']
else:cmd=['dotnet',str(art/'source-purge-stage315-focus/bin/Debug/net10.0/Focus.dll'),str(candidate/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll'),str(report),'ProcessInfoAsyncDistributionHostChecks:DefaultQuery']
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',DOTNET_CLI_USE_MSBUILD_SERVER='0',MSBUILDDISABLENODEREUSE='1',UseSharedCompilation='false',RestoreDisableParallel='true');start=time.monotonic();proc=subprocess.Popen(cmd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env);expired=False;cleanup={};settled=True
try:stdout,stderr=proc.communicate(timeout=limit)
except subprocess.TimeoutExpired:
 expired=True;settled=False
 try:
  killed=subprocess.run(['taskkill','/PID',str(proc.pid),'/T','/F'],stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=10)
  cleanup.update(TaskkillExitCode=killed.returncode,TaskkillStdout=killed.stdout.decode(errors='replace'),TaskkillStderr=killed.stderr.decode(errors='replace'))
 except subprocess.TimeoutExpired as error:
  cleanup.update(TaskkillTimedOut=True,TaskkillStdout=(error.stdout or b'').decode(errors='replace'),TaskkillStderr=(error.stderr or b'').decode(errors='replace'))
 try:
  stdout,stderr=proc.communicate(timeout=10)
  settled=proc.poll() is not None and cleanup.get('TaskkillExitCode')==0
 except subprocess.TimeoutExpired as error:
  stdout=error.stdout or b'';stderr=error.stderr or b'';cleanup['FinalPipeDrainTimedOut']=True
  proc.kill()
  for pipe in (proc.stdout,proc.stderr):
   if pipe is not None:pipe.close()
  try:proc.wait(timeout=2)
  except subprocess.TimeoutExpired:cleanup['FinalRootWaitTimedOut']=True
 cleanup['OwnedRootExited']=proc.poll() is not None
 cleanup['OwnedTreeTerminationAndPipeDrainConfirmed']=settled
(out/'stdout.log').write_bytes(stdout);(out/'stderr.log').write_bytes(stderr);source_exact();value=dict(Phase=a.phase,ExitCode=proc.returncode,TimedOut=expired,OwnedProcessPid=proc.pid,OwnedProcessSettled=settled,TimeoutCleanupMaximumSeconds=22,TimeoutCleanup=cleanup,OuterDeadlineSeconds=limit,ElapsedSeconds=time.monotonic()-start,Command=cmd,SourceFreezeSha256=expected,SourceClosureSha256=h(cl.read_bytes()),SourceClosureFiles=len(rows),SourcesRawExactBeforeAndAfter=True,RecipeFreezeSha256=h(freeze.read_bytes()),ScriptSha256=h(Path(__file__).read_bytes()),RawReportSha256=h(report.read_bytes()) if report.exists() else None)
if a.phase=='red-host':value['SemanticRedClassificationPendingRootInspection']=True;passed=not expired and settled and proc.returncode!=0 and report.exists()
elif a.phase=='green-run':v=read(report) if report.exists() else {};passed=not expired and settled and proc.returncode==0 and v.get('RecipePassed') is True and v.get('FixturePassed')==6 and v.get('FixtureFailed')==0 and v.get('Executed')==6
else:passed=not expired and settled and proc.returncode==0
value['ProcessEnvelopePassed']=passed;(out/'process.json').write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8');print(stdout.decode(errors='replace')[-2500:]);print(stderr.decode(errors='replace')[-2000:]);print(json.dumps(value));assert passed
