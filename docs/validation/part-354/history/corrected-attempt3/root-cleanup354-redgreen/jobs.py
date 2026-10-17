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
