"""Root-only bounded Windows port diagnostics, no candidate build/mutation."""
from pathlib import Path
import argparse,ctypes,hashlib,importlib.util,json,os,subprocess,time
from ctypes import wintypes as W
R=Path(__file__).resolve().parents[2];P=Path(__file__).parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def write(p,v):p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
class BasicLimits(ctypes.Structure):
 _fields_=[('ProcessTime',ctypes.c_longlong),('JobTime',ctypes.c_longlong),('Flags',W.DWORD),('MinWorking',ctypes.c_size_t),('MaxWorking',ctypes.c_size_t),('ActiveLimit',W.DWORD),('Affinity',ctypes.c_size_t),('Priority',W.DWORD),('Scheduling',W.DWORD)]
class IoCounters(ctypes.Structure):
 _fields_=[(n,ctypes.c_ulonglong) for n in ['ReadOps','WriteOps','OtherOps','ReadBytes','WriteBytes','OtherBytes']]
class ExtendedLimits(ctypes.Structure):
 _fields_=[('Basic',BasicLimits),('Io',IoCounters),('ProcessMemory',ctypes.c_size_t),('JobMemory',ctypes.c_size_t),('PeakProcess',ctypes.c_size_t),('PeakJob',ctypes.c_size_t)]
class Accounting(ctypes.Structure):
 _fields_=[(n,ctypes.c_longlong) for n in ['User','Kernel','PeriodUser','PeriodKernel']]+[(n,W.DWORD) for n in ['PageFaults','Processes','Active','Terminated']]
def checked(value):
 if not value:raise ctypes.WinError(ctypes.get_last_error())
 return value
def job_api():
 k=ctypes.WinDLL('kernel32',use_last_error=True)
 for n,args,ret in [('CreateJobObjectW',[ctypes.c_void_p,W.LPCWSTR],W.HANDLE),('SetInformationJobObject',[W.HANDLE,ctypes.c_int,ctypes.c_void_p,W.DWORD],W.BOOL),('AssignProcessToJobObject',[W.HANDLE,W.HANDLE],W.BOOL),('QueryInformationJobObject',[W.HANDLE,ctypes.c_int,ctypes.c_void_p,W.DWORD,ctypes.c_void_p],W.BOOL),('TerminateJobObject',[W.HANDLE,W.UINT],W.BOOL),('CloseHandle',[W.HANDLE],W.BOOL)]:
  f=getattr(k,n);f.argtypes=args;f.restype=ret
 return k
def invoke(command,directory,limit,environment,cwd):
 k=job_api();job=None;proc=None;started=time.monotonic();failures=[];timed=False;forced=False;remaining=None;assigned=False
 def active():
  state=Accounting();checked(k.QueryInformationJobObject(job,1,ctypes.byref(state),ctypes.sizeof(state),None))
  return int(state.Active)
 def attempt(action):
  try:return action()
  except Exception as error:failures.append(repr(error));return None
 with (directory/'process.stdout.log').open('xb') as stdout,(directory/'process.stderr.log').open('xb') as stderr:
  try:
   job=checked(k.CreateJobObjectW(None,None));info=ExtendedLimits();info.Basic.Flags=0x2000
   checked(k.SetInformationJobObject(job,9,ctypes.byref(info),ctypes.sizeof(info)))
   proc=subprocess.Popen(command,cwd=cwd,env=environment,stdin=subprocess.PIPE,stdout=stdout,stderr=stderr)
   write(directory/'process.live.json',{'Pid':proc.pid,'StartedUnixSeconds':time.time(),'Command':command,'InvocationReleased':False})
   checked(k.AssignProcessToJobObject(job,W.HANDLE(proc._handle)));assigned=True
   proc.stdin.write(b'RUN\n');proc.stdin.flush();proc.stdin.close()
   write(directory/'process.admitted.json',{'Pid':proc.pid,'InvocationReleased':True,'KillOnCloseJobAssigned':True})
   try:proc.wait(timeout=limit)
   except subprocess.TimeoutExpired:timed=True
  except Exception as error:failures.append(repr(error))
  finally:
   if proc is not None:
    if assigned:
     remaining=attempt(active)
     end=time.monotonic()+1
     while proc.poll() is not None and remaining and time.monotonic()<end:
      time.sleep(.05);remaining=attempt(active)
     if remaining is None or remaining>0:
      forced=True;attempt(lambda:checked(k.TerminateJobObject(job,1)))
     attempt(lambda:proc.wait(timeout=15));end=time.monotonic()+15
     while True:
      remaining=attempt(active)
      if remaining in (0,None) or time.monotonic()>=end:break
      time.sleep(.05)
     if remaining!=0:failures.append('Owned job settlement not proven')
    elif proc.poll() is None:
     attempt(proc.kill);attempt(lambda:proc.wait(timeout=15))
   if job is not None:attempt(lambda:checked(k.CloseHandle(job)))
 receipt={'Pid':None if proc is None else proc.pid,'ElapsedSeconds':time.monotonic()-started,'TimedOut':timed,'ExitCode':None if proc is None else proc.poll(),'Settled':proc is not None and proc.poll() is not None,'Failures':failures,'KillOnCloseJobAssigned':assigned,'JobTerminationRequested':forced,'RemainingJobProcesses':remaining,'StdoutSha256':H(directory/'process.stdout.log'),'StderrSha256':H(directory/'process.stderr.log')}
 write(directory/'process.exit.json',receipt)
 return receipt
ap=argparse.ArgumentParser();ap.add_argument('--approved-freeze-sha256',required=True);ap.add_argument('--peer-report',required=True);ap.add_argument('--output',required=True);ap.add_argument('--dotnet',default='dotnet');a=ap.parse_args()
assert os.name=='nt','Windows job ownership required; no unowned fallback'
f=P/'source-freeze.json';assert H(f)==a.approved_freeze_sha256.upper()
for row in J(f)['Files']:
 q=P/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
peer=Path(a.peer_report).resolve();assert J(peer)['Status'].startswith('PASS_')
t=J(P/'targets.json');c=Path(t['Candidate']).resolve();assert c.is_relative_to(R/'artifacts')
terminal=R/'artifacts/size-bif354-full-actual2/full.exit.json';full=J(terminal);assert full['Settled'] and not full['TimedOut'] and full['ExitCode']==1 and not full['Failures']
assert H(R/'artifacts/size-bif354-full-actual2/inputs.json')==t['FullInputsSha256']
def verify():
 for row in t['ActualFullDlls']+t['ActualProbeFiles']:
  p=c/row['Path'];assert p.stat().st_size==row['Bytes'] and H(p)==row['Sha256']
verify();helper=R/'artifacts/spawn-priority351-preparation/execute-root.py';assert H(helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
spec=importlib.util.spec_from_file_location('halt_owned_build',helper);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
o=Path(a.output).resolve();assert o.is_relative_to(R/'artifacts') and not o.exists();o.mkdir()
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
write(o/'inputs.json',{'SourceFreezeSha256':H(f),'PeerReportSha256':H(peer),'FullExitSha256':H(terminal),'ActualFullDlls':t['ActualFullDlls'],'Targets':t['Names'],'CanonicalRunSeconds':15,'ParentSeconds':45,'OriginalMethodsModified':False})
m.owned([a.dotnet,'build',str(P/'source/Diagnostic.csproj'),'--disable-build-servers','-m:1','-nr:false','--nologo','-v:minimal','--output',str(o/'build')],o,'build',120,env,R)
dll=o/'build/HaltPort354Diagnostic.dll';harness=H(dll);write(o/'built-harness.json',{'Path':str(dll),'Sha256':harness});attempts=[]
for i,name in enumerate(t['Names']):
 d=o/('target-'+str(i).zfill(2));d.mkdir();out=d/'outcome.json';verify()
 receipt=invoke([a.dotnet,str(dll),str(c/'tests/Erlang.Tests/bin/Debug/net10.0'),name,str(out)],d,30 if name=='Run' else 45,env,c)
 outcome=J(out) if out.is_file() else None
 if outcome is not None:
  assert outcome['Target']==name and outcome['InputAssembliesUnchanged']
  assert len(outcome['BeforeAssemblies'])==19
  for row in outcome['BeforeAssemblies']+outcome['LoadedAssemblies']:
   assert any(str((c/b['Path']).resolve()).casefold()==row['Path'].casefold() and b['Sha256']==row['Sha256'] for b in t['ActualFullDlls'])
 verify();assert H(dll)==harness
 attempts.append({'Target':name,'Outcome':outcome,'Process':receipt,'ObservedSuccess':outcome is not None and outcome['Passed'] and receipt['ExitCode']==0 and not receipt['TimedOut'] and not receipt['Failures'] and not receipt['JobTerminationRequested'] and receipt['RemainingJobProcesses']==0})
 write(o/'attempts.json',attempts)
 if not receipt['Settled'] or receipt['RemainingJobProcesses'] not in (0,None) or receipt['Failures']:raise RuntimeError('Ownership failure retained; no next child launched')
write(o/'completion.json',{'Status':'DIAGNOSTIC_SEVEN_ATTEMPTS_RECORDED_NOT_FULL_OR_ORIGINAL_ACCEPTANCE','Attempts':attempts,'ObservedSuccesses':sum(x['ObservedSuccess'] for x in attempts),'FullValidationExecuted':False,'OriginalOtpExecuted':False,'OriginalAssertionsAndTimeoutsUnchanged':True,'CanonicalRunSeconds':15,'HarnessDllSha256':harness})
