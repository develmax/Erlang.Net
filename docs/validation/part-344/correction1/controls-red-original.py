"""Deterministic local procfs-discovery controls; no VM, subprocess, signals, network or downloaded code."""
from pathlib import Path
import ast,json,hashlib,tempfile,types
root=Path(__file__).resolve().parents[2]
source=root/'tools/Erlang.ProcLibRealPeerObservations/collect-original.py'
assert hashlib.sha256(source.read_bytes()).hexdigest().upper()=='3451F256A7FFF8580DBF295C4D6FFE04710A61D063994C3648DA38B0E2035286'
tree=ast.parse(source.read_bytes())
proc=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='proc_token')
observe=next(n for n in ast.walk(tree) if isinstance(n,ast.FunctionDef) and n.name=='observe')
module=ast.parse('''def factory():
 peer_pid=None
 peer_start=None
 def state(): return peer_pid,peer_start
 return observe,state
''')
module.body[0].body.insert(2,observe)
module.body.insert(0,proc);ast.fix_missing_locations(module)
results=[]
def require(value,why):
 if not value:raise ValueError(why)
d=Path(__file__).parent/'uncreated-synthetic-procfs-root'
for label,stage,error,expected in [('cmdline-disappeared','cmdline',ProcessLookupError,'live'),('stat-disappeared','stat',ProcessLookupError,'live'),('exe-disappeared','exe',ProcessLookupError,'live'),('uid-disappeared','uid',ProcessLookupError,'live'),('children-disappeared','children',ProcessLookupError,'none'),('cmdline-missing','cmdline',FileNotFoundError,'live'),('cmdline-permission','cmdline',PermissionError,'raise'),('uid-mismatch','uid',None,'reject'),('exe-mismatch','exe',None,'live')]:
 saved=[]
 class FakeProcPath:
  def __init__(self,*parts):self.parts=tuple(map(str,parts))
  def __truediv__(self,other):return FakeProcPath(*self.parts,str(other))
  @property
  def pid(self):return int(self.parts[1])
  def fail(self,kind):
   if stage==kind and ((kind=='children' and self.pid==100) or (kind!='children' and self.pid==11)) and error:raise error(3 if error is ProcessLookupError else 2,'controlled transient procfs operation')
  def read_text(self):
   if self.parts[-1]=='children':
    self.fail('children')
    if self.pid==100:return '11 22'
    raise ProcessLookupError(3,'exited descendant')
   assert self.parts[-1]=='stat';self.fail('stat');return f'{self.pid} (controlled process) '+ ' '.join(['S','100']+['0']*17+['88'])
  def read_bytes(self):
   assert self.parts[-1]=='cmdline';self.fail('cmdline');return b'beam\x00peer_control@127.0.0.1\x00'+str(d/'beam').encode()+b'\x00'
  def resolve(self,strict):
   self.fail('exe');return types.SimpleNamespace(is_relative_to=lambda _:not(stage=='exe' and self.pid==11 and error is None))
  def stat(self):
   self.fail('uid');return types.SimpleNamespace(st_uid=777 if stage=='uid' and self.pid==11 and error is None else 1000)
 context=dict(Path=FakeProcPath,require=require,os=types.SimpleNamespace(getuid=lambda:1000),env={'ERLANG_NET_PEER_NAME':'peer_control'},d=d,installed=object(),parent_pid=[100],save=lambda p,v:saved.append(v))
 exec(compile(module,'reviewed_local_collector_functions','exec'),context)
 actual,state=context['factory']()
 raised=None
 try:actual()
 except (PermissionError,ValueError) as exc:raised=exc
 if expected=='raise':assert isinstance(raised,PermissionError)
 elif expected=='reject':assert isinstance(raised,ValueError) and str(raised)=='Peer owner UID'
 else:
  assert raised is None
  if expected=='live':assert state()==(22,88) and saved==[dict(Pid=22,StartTicks=88,OriginOwnedPid=100)]
  else:assert state()==(None,None) and saved==[]
 results.append(dict(Name=label,Passed=True,Expected=expected))
# proc_token itself must classify both disappearance errno families, not generic access failure.
for error in (FileNotFoundError,ProcessLookupError,PermissionError):
 class ErrorPath:
  def __init__(self,*args):pass
  def read_text(self):raise error(3,'controlled stat read')
 context['Path']=ErrorPath
 if error is PermissionError:
  try:context['proc_token'](11)
  except PermissionError:pass
  else:raise AssertionError('permission swallowed')
 else:assert context['proc_token'](11) is None
 results.append(dict(Name='proc-token-'+error.__name__,Passed=True))
report=dict(Status='PASS_LOCAL_PROCFS_OBSERVATION_COMPONENT_CONTROLS',SourceSha256=hashlib.sha256(source.read_bytes()).hexdigest().upper(),Checks=len(results),Results=results,Scope='Only AST-extracted local reviewed proc_token/observe functions with deterministic synthetic procfs seams. No original runtime, lifecycle success or native interoperability claim.',SubprocessesStarted=0,SignalsSent=0,NetworkUsed=False,OriginalExecuted=False,SdkExecuted=False,DownloadedCodeExecuted=False)
output=Path(__file__).with_name('controls2-result.json');assert not output.exists();output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
