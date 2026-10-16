from pathlib import Path
import argparse,hashlib,json,os,re,subprocess,time,traceback
p=argparse.ArgumentParser();p.add_argument('phase',choices=['build','run'])
for name in ('focus-directory','recipe'):p.add_argument('--'+name,type=Path,required=True)
for name in ('expected-freeze','recipe-sha256'):p.add_argument('--'+name,required=True)
p.add_argument('--selector',default='original-retained');a=p.parse_args();r=Path.cwd().resolve();d=a.focus_directory.resolve();recipe=a.recipe.resolve()
h=lambda q:hashlib.sha256(q.read_bytes()).hexdigest().upper();j=lambda q:json.loads(q.read_bytes());write=lambda q,v:q.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
assert d.is_relative_to(r/'artifacts') and recipe.is_relative_to(r/'artifacts') and re.fullmatch('[a-z0-9-]{1,90}',a.selector)
rf=recipe/'source-freeze.json';assert h(rf)==a.recipe_sha256.upper();rec=j(rf)
for x in rec['Files']:
 q=recipe/x['Path'];assert q.stat().st_size==x['Bytes'] and h(q)==x['Sha256']
fp=d/('formatted-source-freeze.json' if (d/'formatted-source-freeze.json').exists() else 'source-freeze.json');assert h(fp)==a.expected_freeze.upper();f=j(fp)
cl=d/('formatted-source-resource-closure.json' if (d/'formatted-source-resource-closure.json').exists() else 'source-resource-closure.json');assert h(cl)==f.get('SourceClosureSha256',f.get('FinalSourceClosureSha256'));rows=j(cl)['Files'];assert len(rows)==f['SourceClosurePaths']
def exact():
 for x in rows:
  q=d/'candidate'/x['Path'];assert q.stat().st_size==x['Bytes'] and h(q)==x['Sha256'],x['Path']
 assert h(fp)==a.expected_freeze.upper() and h(rf)==a.recipe_sha256.upper()
exact();project=d/('recursion-project-'+recipe.name);out=d/('recursion-build-'+recipe.name if a.phase=='build' else 'recursion-'+a.selector);assert not out.exists()
if a.phase=='build':
 assert not project.exists();project.mkdir()
 for x in rec['Files']:
  if x['Path'].endswith(('.cs','.csproj')):(project/x['Path']).write_bytes((recipe/x['Path']).read_bytes())
else:assert project.is_dir()
out.mkdir();report=out/'actual.json';settings=['--disable-build-servers','-m:1','-nr:false','-p:UseSharedCompilation=false','-p:RestoreDisableParallel=true','--nologo'];dll=project/'bin/Debug/net10.0/NonTailRecursionManagedFocus.dll'
cmd=['dotnet','build',str(project/'NonTailRecursionManagedFocus.csproj'),'-p:FocusDifferentialProject='+str(d/'candidate/tools/Erlang.Differential/Erlang.Differential.csproj'),*settings] if a.phase=='build' else ['dotnet',str(dll),str(report),str(fp),h(fp),a.selector]
if a.phase=='run' and a.selector=='original-retained':cmd.append(str(r/'artifacts/undefined-lambda328-oracle1/erlang/no-current-original-fun-environment-arguments-self.erl'))
limit=180 if a.phase=='build' else 120;env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',DOTNET_CLI_USE_MSBUILD_SERVER='0',MSBUILDDISABLENODEREUSE='1',UseSharedCompilation='false',RestoreDisableParallel='true')
binaries={q.name:h(q) for q in dll.parent.glob('*.dll')} if a.phase=='run' else {};write(out/'attempt.json',dict(Command=cmd,DeadlineSeconds=limit,SourceFreezeSha256=h(fp),RecipeSha256=h(rf),RunnerBinaryPins=binaries));start=time.monotonic();proc=None;timed=settled=passed=False;cleanup={};stdout=stderr=b'';failure=None;classification='INFRASTRUCTURE_OR_RECEIPT_FAILURE'
try:
 proc=subprocess.Popen(cmd,cwd=r,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env)
 try:stdout,stderr=proc.communicate(timeout=limit);settled=True
 except subprocess.TimeoutExpired:
  timed=True
  try:cleanup['TaskkillExitCode']=subprocess.run(['taskkill','/PID',str(proc.pid),'/T','/F'],capture_output=True,timeout=10).returncode
  except subprocess.TimeoutExpired:cleanup['TaskkillTimedOut']=True
  try:stdout,stderr=proc.communicate(timeout=10);settled=proc.poll() is not None and cleanup.get('TaskkillExitCode')==0
  except subprocess.TimeoutExpired as e:
   stdout=e.stdout or b'';stderr=e.stderr or b'';cleanup['FinalPipeDrainTimedOut']=True;proc.kill()
   try:proc.wait(timeout=2)
   except subprocess.TimeoutExpired:cleanup['FinalWaitTimedOut']=True
 assert settled and not timed,'Owned process not settled within deadline'
 if a.phase=='build':
  assert proc.returncode==0;classification='BUILD_EXIT_ZERO_NOT_FEATURE_PROOF';passed=True
 elif report.exists():
  v=j(report);assert v['Selector']==a.selector and v['Executed']==v['ExpectedCases']==1
  for x in v['ActualLoadedAssemblies']:
   q=Path(x['Path']);assert q.resolve()==(dll.parent/(x['Name']+'.dll')).resolve() and h(q)==x['Sha256']==binaries[q.name]
   if x['Name']!='NonTailRecursionManagedFocus':assert h(d/'candidate/tools/Erlang.Differential/bin/Debug/net10.0'/q.name)==x['Sha256']
  passed=proc.returncode==0 and v['RecipePassed'] and v['FixturePassed']==1 and v['FixtureFailed']==0
  classification='ACTUAL_ONE_FIXTURE_PASS' if passed else 'ACTUAL_FIXTURE_FAILURE_NOT_EXPECTED_GREEN'
 else:classification='ACTUAL_CHILD_NO_REPORT_NOT_A_PASS'
 exact()
 for n,digest in binaries.items():assert h(dll.parent/n)==digest
except Exception:failure=traceback.format_exc();passed=False
finally:
 (out/'stdout.log').write_bytes(stdout);(out/'stderr.log').write_bytes(stderr)
 try:exact()
 except Exception:failure=(failure or '')+traceback.format_exc();passed=False;classification='SOURCE_CHANGED'
 result=dict(Phase=a.phase,Selector=a.selector,Classification=classification,Passed=passed,Command=cmd,ExitCode=proc.returncode if proc else None,TimedOut=timed,OwnedProcessSettled=settled,OwnedPid=proc.pid if proc else None,DeadlineSeconds=limit,CleanupMaximumSeconds=22,Cleanup=cleanup,ElapsedSeconds=time.monotonic()-start,SourceFreezeSha256=h(fp),SourceClosureSha256=h(cl),SourceClosurePaths=len(rows),RecipeSha256=h(rf),ActualReportSha256=h(report) if report.exists() else None,StandardOutputSha256=h(out/'stdout.log'),StandardErrorSha256=h(out/'stderr.log'),ValidationError=failure,OriginalExecuted=False,MainEdited=False)
 write(out/'process.json',result)
print(json.dumps({k:result[k] for k in ['Phase','Selector','Classification','Passed','ExitCode','OwnedProcessSettled','ElapsedSeconds','ValidationError']}))
if cleanup.get('FinalPipeDrainTimedOut'):os._exit(1)
raise SystemExit(0 if passed else 1)

