from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil,socket,struct,subprocess,time,xml.etree.ElementTree as ET
p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--depths',default='0,1,2,8');p.add_argument('--orders',default='incoming,outgoing');a=p.parse_args()
r=Path.cwd();packet=Path(__file__).resolve().parent;out=(r/a.output).resolve();assert out.is_relative_to(r/'artifacts') and not out.exists();out.mkdir()
def sha(f):return hashlib.sha256(Path(f).read_bytes()).hexdigest().upper()
def save(f,v):f.write_text(json.dumps(v,indent=2)+'\n')
def load(n,f):s=importlib.util.spec_from_file_location(n,f);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
ownfile=r/'artifacts/spawn-priority351-preparation/execute-root.py';assert sha(ownfile)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A';owned=load('owned447',ownfile)
invfile=r/'artifacts/library426-trace-low-memory-original-proposal1/bounded_inventory.py';assert sha(invfile)=='502862EB7045D780115D046A02FA56DB11FB67EBC7A1883CA58D06BEC6C7910A';inventory=load('inventory447',invfile)
adfile=r/'artifacts/library376-official-compiler/official-admission.json';ad=json.loads(adfile.read_bytes());assert ad['SourceCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
installed=Path(ad['InstalledOtpRoot']);epmd_candidates=list(installed.glob('erts-*/bin/epmd.exe'));assert len(epmd_candidates)==1;epmd_executable=epmd_candidates[0];execution=Path(ad['ExecutionManifestPath']);assert sha(execution)==ad['ExecutionManifestSha256'];expected={row['Path'].replace('\\','/'):row['Sha256'] for row in json.loads(execution.read_bytes())};assert len(expected)==6427
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1')
for k in list(env):
 if k.startswith('ERL_') or k.startswith('ERLC_') or k.startswith('DDLL_VISIBILITY_'):env.pop(k,None)
env['ERL_FLAGS']='+S 1:1 +A 1';env['ERLC_USE_SERVER']='false'
source=out/'source';source.mkdir();ebin=out/'observer-ebin';ebin.mkdir();drivers=out/'drivers';drivers.mkdir();(out/'stock').mkdir();harness=out/'harness';harness.mkdir()
for f in packet.glob('*.erl'):shutil.copyfile(f,source/f.name)
old=r/'artifacts/library420-erldrv-original1';auth=json.loads((old/'completion.json').read_bytes());assert auth['Passed'] and auth['WholeRows']==228
for src,dst in [(old/'drivers/echo_drv.dll',drivers/'echo_drv.dll'),(old/'stock/erl_ddll.beam',out/'stock/erl_ddll.beam')]:
 assert any(Path(row['Path']).resolve()==src.resolve() and row['Sha256']==sha(src) for row in auth['Files']);shutil.copyfile(src,dst)
parentfile=r/'artifacts/library451-driver-package1/parent-input.json';parent=json.loads(parentfile.read_bytes());inputs=dict(parent['ExpectedInputs'])
for f in [*packet.glob('*'),*source.glob('*'),*drivers.glob('*'),out/'stock/erl_ddll.beam',adfile,execution,ownfile,invfile,parentfile,old/'completion.json']:
 if f.is_file():inputs[str(f.resolve())]=sha(f)
for f,digest in inputs.items():assert sha(f)==digest,f
dotnet=Path('C:/Program Files/dotnet/dotnet.exe');assert sha(dotnet)=='AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135'
runtime=r/'tests/Erlang.Tests/bin/Debug/net10.0';package=Path(parent['PackageManifest']).parent
for n in ['Erlang.Runtime','Erlang.Terms','Erlang.Serialization']:assert sha(runtime/(n+'.dll'))==sha(package/(n+'.dll'))
helpers=json.loads((r/'artifacts/library446-ddll-frontier-proposal3/harness-helper-bindings.json').read_bytes());files=[]
for f in [*(r/row['Path'] for row in helpers),*packet.glob('*.cs')]:
 if any(f==r/row['Path'] for row in helpers):assert any(f==r/row['Path'] and sha(f)==row['Sha256'] for row in helpers)
 dst=harness/f.name;shutil.copyfile(f,dst);files.append(dst);inputs[str(f.resolve())]=sha(f);inputs[str(dst.resolve())]=sha(dst)
root=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(root,'PropertyGroup')
for k,v in {'TargetFramework':'net10.0','LangVersion':'14.0','OutputType':'Exe','AssemblyName':'Erlang.Compiler','Nullable':'enable','ImplicitUsings':'enable','TreatWarningsAsErrors':'true','EnableDefaultCompileItems':'false','UseSharedCompilation':'false'}.items():ET.SubElement(props,k).text=v
items=ET.SubElement(root,'ItemGroup')
for f in files:ET.SubElement(items,'Compile',Include=str(f))
for n in ['Erlang.Runtime','Erlang.Terms','Erlang.Serialization']:
 row=ET.SubElement(items,'Reference',Include=n);ET.SubElement(row,'HintPath').text=str(runtime/(n+'.dll'))
ET.indent(root);project=harness/'Ddll447.csproj';ET.ElementTree(root).write(project,encoding='utf-8',xml_declaration=True);inputs[str(project.resolve())]=sha(project)
scans=[];failures=[];groups=[];epmd=None;epmd_logs=[];epmd_port=None
def scan(phase):
 start=time.monotonic();equal=inventory.inventory(installed)==expected;scans.append({'Phase':phase,'Equal':equal,'Files':6427,'Seconds':time.monotonic()-start});save(out/'provider.json',scans);assert equal
def stage(command,name,seconds=180):
 assert all(sha(f)==digest for f,digest in inputs.items());owned.owned(command,out,name,seconds,env,str(r));assert all(sha(f)==digest for f,digest in inputs.items())
try:
 stage([str(dotnet),'build',str(project),'-c','Release','--nologo','-m:1','-nr:false','-p:UseSharedCompilation=false'],'compile-consumer')
 scan('before')
 stage([str(installed/'bin/erlc.exe'),'+debug_info','+deterministic','-o',str(ebin),*[str(f) for f in source.glob('*.erl')]],'compile-observer')
 for f in ebin.glob('*.beam'):inputs[str(f.resolve())]=sha(f)
 build=harness/'bin/Release/net10.0'
 for f in build.glob('*'):
  if f.is_file():inputs[str(f.resolve())]=sha(f)
 save(out/'inputs-before.json',inputs)
 with socket.socket() as reservation:reservation.bind(('127.0.0.1',0));epmd_port=reservation.getsockname()[1]
 env['ERL_EPMD_PORT']=str(epmd_port)
 epmd_logs=[(out/'epmd.stdout.log').open('xb'),(out/'epmd.stderr.log').open('xb')]
 epmd_command=[str(epmd_executable),'-port',str(epmd_port),'-address','127.0.0.1']
 epmd=subprocess.Popen(epmd_command,cwd=r,env=env,stdout=epmd_logs[0],stderr=epmd_logs[1]);save(out/'epmd.live.json',{'Pid':epmd.pid,'Command':epmd_command})
 deadline=time.monotonic()+10;epmd_ready=False
 while epmd.poll() is None and time.monotonic()<deadline:
  try:
   with socket.create_connection(('127.0.0.1',epmd_port),timeout=.2):epmd_ready=True;break
  except OSError:time.sleep(.05)
 assert epmd_ready,'Private foreground EPMD failed to start'
 def read_exact(stream,n):
  data=b''
  while len(data)<n:
   part=stream.recv(n-len(data));assert part,'EPMD truncated response';data+=part
  return data
 def port_please(name,destination):
  request=b'z'+name.split('@')[0].encode('latin1')
  with socket.create_connection(('127.0.0.1',epmd_port),timeout=5) as stream:
   stream.sendall(struct.pack('>H',len(request))+request);head=read_exact(stream,12)
   assert head[0:2]==bytes([119,0]) and head[5]==0 and struct.unpack('>H',head[6:8])[0]>=6 and struct.unpack('>H',head[8:10])[0]<=6
   tail=read_exact(stream,struct.unpack('>H',head[10:12])[0]+2);extra=read_exact(stream,struct.unpack('>H',tail[-2:])[0]);destination.write_bytes(head+tail+extra)
   return struct.unpack('>H',head[2:4])[0]

 for depth,order in [(d,o) for d in map(int,a.depths.split(',')) for o in a.orders.split(',')]:
  group=out/('depth'+str(depth)+'-'+order);group.mkdir();children=[];group_errors=[]
  try:
   ports=[];nodes={}
   for role in ['primary','peer']:
    ready=group/(role+'.ready');logs=[(group/(role+'.stdout.log')).open('xb'),(group/(role+'.stderr.log')).open('xb')]
    expr='ddll_tcp451:start(["'+ready.as_posix()+'"]).'
    nodes[role]=('ddll451oracle' if role=='primary' else ('ddll451z_peer' if order=='incoming' else 'ddll451a_peer'))+str(depth)+order+'@127.0.0.1'
    command=[str(installed/'bin/erl.exe'),'-noshell','-noinput','-boot','no_dot_erlang','-start_epmd','false','-name',nodes[role],'-setcookie','ddll451_owned_fixture_cookie','-kernel','inet_dist_use_interface','{127,0,0,1}','-pa',str(ebin),'-eval',expr]
    process=subprocess.Popen(command,cwd=r,env=env,stdout=logs[0],stderr=logs[1]);children.append((role,process,logs,time.monotonic()));save(group/(role+'.live.json'),{'Pid':process.pid,'Command':command})
    deadline=time.monotonic()+20
    ready_port=None
    while process.poll() is None and time.monotonic()<deadline:
     if ready.exists():
      value=ready.read_text()
      if value.isdecimal() and 0<int(value)<65536:ready_port=int(value);break
     time.sleep(.05)
    assert ready_port is not None and process.poll() is None,'Original server port preparation failed: '+role
    ports.append(str(ready_port))
   dist_port=port_please(nodes['peer'],group/'peer-epmd-port.bin')
   fresh={k:v for k,v in parent.items() if k!='ExpectedInputs'};fresh.update({'OriginalDirectory':str(out),'DriverDirectory':str(drivers),'OutputDirectory':str(group/'observations'),'ExpectedInputs':inputs})
   config=group/'input.json';save(config,fresh)
   try:stage([str(dotnet),str(build/'Erlang.Compiler.dll'),str(config),*ports,str(depth),str(dist_port),nodes['peer'],nodes['primary'],('ddll451a_managed' if order=='incoming' else 'ddll451z_managed')+str(depth)+order+'@127.0.0.1',str(epmd_port),order],'compare-depth'+str(depth)+'-'+order,180)
   except Exception as error:group_errors.append(repr(error))
  except Exception as error:group_errors.append(repr(error))
  finally:
   for role,process,logs,start in reversed(children):
    forced=False;cleanup_failure=None
    try:
     try:process.wait(timeout=10)
     except subprocess.TimeoutExpired:
      forced=True;process.terminate()
      try:process.wait(timeout=10)
      except subprocess.TimeoutExpired:
       killed=subprocess.run(['taskkill','/PID',str(process.pid),'/T','/F'],capture_output=True,timeout=15);(group/(role+'.kill.stdout.log')).write_bytes(killed.stdout);(group/(role+'.kill.stderr.log')).write_bytes(killed.stderr);process.wait(timeout=10)
    except Exception as error:cleanup_failure=repr(error);group_errors.append(role+' cleanup: '+cleanup_failure)
    finally:
     for log in logs:log.close()
    fatal=group/'observations/fatal-peer-stop.json';fatal_body=json.loads(fatal.read_bytes()) if fatal.exists() else {};expected_kill=role=='peer' and fatal_body.get('Passed') is True and fatal_body.get('OldPid')==process.pid and fatal_body.get('KillRequested') is True and process.returncode!=0
    receipt={'ExpectedScenarioTermination':expected_kill,'ForcedCleanup':forced,'Pid':process.pid,'ExitCode':process.returncode,'ForcedTermination':forced,'Settled':process.poll() is not None,'Seconds':time.monotonic()-start};save(group/(role+'.exit.json'),receipt)
    if forced or (process.returncode!=0 and not expected_kill):group_errors.append(role+' did not exit normally or by its validated scenario kill')
  comparison=group/'observations/comparison.json';body=json.loads(comparison.read_bytes()) if comparison.exists() else None
  good=not group_errors and body is not None and body['Passed'];groups.append({'Depth':depth,'Order':order,'Passed':good,'Errors':group_errors});save(out/'groups.json',groups)
except Exception as error:failures.append(repr(error))
finally:
 if epmd is not None:
  forced=False
  try:
   owned.owned([str(epmd_executable),'-port',str(epmd_port),'-kill'],out,'epmd-stop',15,env,str(r));epmd.wait(timeout=10)
  except Exception as error:
   failures.append('Private EPMD cleanup: '+repr(error))
   if epmd.poll() is None:
    forced=True
    try:epmd.terminate();epmd.wait(timeout=10)
    except Exception as cleanup_error:failures.append('EPMD forced settle: '+repr(cleanup_error))
  finally:
   for log in epmd_logs:log.close()
   save(out/'epmd.exit.json',{'Pid':epmd.pid,'ExitCode':epmd.returncode,'ForcedTermination':forced,'Settled':epmd.poll() is not None})
 try:
  if scans:scan('after')
 except Exception as error:failures.append(repr(error))
stable=all(sha(f)==digest for f,digest in inputs.items());passed=not failures and stable and len(groups)==len(a.depths.split(','))*len(a.orders.split(',')) and all(g['Passed'] for g in groups)
save(out/'completion.json',{'Passed':passed,'Groups':groups,'Failures':failures,'InputsUnchanged':stable,'RuntimeSha256':sha(runtime/'Erlang.Runtime.dll'),'ProviderScans':scans,'Transport':'actual original pending collisions/alive status, runtime data queues and deliberate owned OS peer-tree termination/restart'})
raise SystemExit(0 if passed else 1)
