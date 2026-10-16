import ast,json,hashlib,tempfile,types,os,contextlib
from pathlib import Path as RealPath
ROOT=RealPath(__file__).resolve().parents[2];OUT=RealPath(__file__).resolve().parent
OLD=ROOT/'tools/Erlang.ProcLibRealPeerObservations/collect-original.py';NEW=OUT/'source/tools/Erlang.ProcLibRealPeerObservations/collect-original.py'
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def run(source,mode):
 tree=ast.parse(source.read_text()); observe=next(n for n in ast.walk(tree) if isinstance(n,ast.FunctionDef) and n.name=='observe');save=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='save')
 graph={100:[200],200:[300],300:[]};events=[]
 class Proc:
  def __init__(self,parts):self.parts=parts
  def __truediv__(self,x):return Proc(self.parts+[str(x)])
  def read_text(self):events.append(('children',self.parts[1]));return ' '.join(map(str,graph[int(self.parts[1])]))
  def read_bytes(self):
   if mode=='denied':raise PermissionError('synthetic preserved refusal')
   events.append(('cmdline',self.parts[1]));return b'peer_test@127.0.0.1\x00beam\x00'
  def resolve(self,strict=False):return self
  def is_relative_to(self,x):return mode!='wrong_exe'
  def stat(self):return types.SimpleNamespace(st_uid=1001+(1 if mode=='wrong_uid' else 0))
 def Path(*parts):return Proc(list(map(str,parts))) if parts and str(parts[0])=='/proc' else RealPath(*parts)
 def require(ok,message):
  if not ok:raise ValueError(message)
 # Bind the unchanged nested observe through an AST-extracted enclosing function.
 factory=ast.parse('def factory():\n peer_pid=None\n peer_start=None\n def state():return peer_pid,peer_start\n return observe,state\n').body[0]
 factory.body.insert(2,observe)
 ns={'Path':Path,'json':json,'os':types.SimpleNamespace(getuid=lambda:1001),'require':require,'proc_token':lambda pid:(pid,pid+1000),'env':{'ERLANG_NET_PEER_NAME':'peer_test'},'installed':RealPath('installed'),'parent_pid':[100]}
 exec(compile(ast.fix_missing_locations(ast.Module(body=[save,factory],type_ignores=[])),str(source),'exec'),ns)
 directory=OUT/('control5-'+H(source)[:8]+'-'+mode);directory.mkdir(exist_ok=False)
 with contextlib.nullcontext(directory) as tmp:
  d=RealPath(tmp);ns['d']=d
  # Real collector uses its per-case beam path, retained verbatim in the cmdline seam.
  original=Proc.read_bytes
  def cmd(self):return original(self).replace(b'beam',str(d/'beam').encode())
  Proc.read_bytes=cmd
  observe_fn,state=ns['factory']();error=None
  try:
   observe_fn();first=(d/'peer-owned-process.json').read_bytes() if (d/'peer-owned-process.json').exists() else None
   for _ in range(3):observe_fn()
  except Exception as e:error=type(e).__name__
  saved=json.loads((d/'peer-owned-process.json').read_bytes()) if (d/'peer-owned-process.json').exists() else None
  return {'Mode':mode,'Error':error,'State':state(),'Saved':saved,'Events':events}
results=[]
red=run(OLD,'match');assert red['Error']=='FileExistsError' and red['Saved']['Pid']==200 and red['State'][0]==300;results.append(dict(Profile='original-red',**red))
green=run(NEW,'match');assert green['Error'] is None and green['Saved']['Pid']==200 and green['State']==(200,1200) and green['Events'].count(('cmdline','200'))==1;results.append(dict(Profile='patched-green-repeated-poll',**green))
for mode,error in [('denied','PermissionError'),('wrong_uid','ValueError')]:
 r=run(NEW,mode);assert r['Error']==error and r['Saved'] is None;results.append(dict(Profile='patched-refusal',**r))
r=run(NEW,'wrong_exe');assert r['Error'] is None and r['Saved'] is None;results.append(dict(Profile='patched-no-owned-candidate',**r))
r={'Status':'PASS_LOCAL_AST_EXTRACTED_MULTIMATCH_RED_GREEN_AND_REFUSAL_CONTROLS','OriginalCollectorSha256':H(OLD),'PatchedCollectorSha256':H(NEW),'Results':results,'ProductionCodeExecuted':'Only local AST-extracted observe/save functions with deterministic synthetic procfs; no VM, real /proc, process signaling or network','SdkExecuted':False,'OriginalOtpExecuted':False}
p=OUT/'controls-result.json';p.write_text(json.dumps(r,indent=2)+'\n');print(H(p))
