"""Compare both queue593 native loader routes, preserving independent failures and full raw streams."""
from pathlib import Path
import sys,os,json,hashlib,importlib.util
sys.dont_write_bytecode=True
import argparse,subprocess
parser=argparse.ArgumentParser(description=__doc__)
for key in ['repository','expected-head','source-plan-sha256','provider-admission','provider-sha256','output']:
 parser.add_argument('--'+key,required=True)
parser.add_argument('--original',required=True)
parser.add_argument('--approved-original-completion-sha256',required=True)
parser.add_argument('--backend',required=True)
parser.add_argument('--backend-sha256',required=True)
parser.add_argument('--execute-queue593',action='store_true')
args=parser.parse_args();r=Path(args.repository).resolve();tool=r/'tools/Native/Erlang.NifResource529Observations'
assert Path(__file__).resolve().parent==tool and os.name=='nt'
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=r,text=True).strip()==args.expected_head
planpath=tool/'source-plan.json';assert hashlib.sha256(planpath.read_bytes()).hexdigest().upper()==args.source_plan_sha256.upper()
plan=json.loads(planpath.read_bytes());assert plan['PinnedCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and plan['SealState']=='FINAL_SOURCE_BOUND_NOT_EXECUTED'
assert plan['OptionalProtocols']['queue593']['Observer']=='resource_queue593'
assert plan['OptionalProtocols']['queue593']['StockModules']==['timer']
def check_source593():
 assert hashlib.sha256(planpath.read_bytes()).hexdigest().upper()==args.source_plan_sha256.upper()
 for entry in plan['Dependencies']:
  p=r/entry['Path'];data=p.read_bytes();assert entry['HashMode'] in ['Raw','TextLF']
  if entry['HashMode']=='TextLF':data=data.replace(b'\r\n',b'\n')
  assert hashlib.sha256(data).hexdigest().upper()==entry['Sha256'],entry['Path']
check_source593()
args.output=Path(args.output).resolve();assert args.output.is_relative_to(r/'artifacts') and not args.output.exists()
if not args.execute_queue593:
 print('Reviewed queue593 inputs admitted; no process executed');raise SystemExit(0)

a=args.output;a.mkdir(parents=True);original=Path(args.original).resolve();backend=Path(args.backend).resolve();sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper();assert backend.name=='Erlang.NifNativeBackend.dll' and sha(backend)==args.backend_sha256.upper();assert sha(original/'completion.json')==args.approved_original_completion_sha256.upper()
def save(p,d):p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8')
def row(p):p=Path(p).resolve();return dict(Path=str(p),Bytes=p.stat().st_size,Sha256=sha(p))
def load(n,p):s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
helper=load('queue533imports',r/'tools/Native/Erlang.NifResource529Observations/replay-family.py');owner=load('queue533managedowner',r/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py')
completion=json.loads((original/'completion.json').read_bytes());assert completion['Passed'] and completion['OriginalExecuted'] and completion['ProviderUnchanged'];assert sha(original/'artifacts.json')==completion['ArtifactInventorySha256'];inventory=json.loads((original/'artifacts.json').read_bytes());original_rows=[]
assert sha(original/'resource_queue593.erl')==sha(tool/'fixtures/resource_queue593.erl')
assert sha(original/'native_resource_family527.erl')==plan['Variants']['family1']['ErlangSourceSha256']
for x in inventory:
 p=original/x['Path'];assert sha(p)==x['Sha256'];original_rows.append(row(p))
receipt=Path(completion['NativeBuildReceiptPath']);library=Path(completion['NativeLibraryPath']);assert sha(receipt)==completion['NativeBuildReceiptSha256'] and sha(library)==completion['NativeLibrarySha256']
build=json.loads(receipt.read_bytes());assert build['NativeSourceSha256']==plan['Variants']['family1']['NativeSourceSha256'] and build['ErlangSourceSha256']==plan['Variants']['family1']['ErlangSourceSha256']
assert build['Status']=='RESOURCE529_WINDOWS_BUILD_COMPLETE' and build['Variant']=='family1' and build['PinnedCommit']==plan['PinnedCommit']
assert build['CompileExitCode']==build['LinkExitCode']==0
assert sha(receipt.parent/'fixture.c')==build['CompiledSourceSha256']==build['NativeSourceSha256']
assert sha(receipt.parent/'fixture.erl')==build['CopiedErlangSourceSha256']==build['ErlangSourceSha256']
provider=Path(args.provider_admission).resolve();assert sha(provider)==args.provider_sha256.upper()==completion['OriginalProviderSha256'];pd=json.loads(provider.read_bytes());installed=Path(pd['InstalledOtpRoot']);assert sha(Path(pd['ExecutionManifestPath']))==pd['ExecutionManifestSha256'];expected={x['Path'].replace('\\','/'):x['Sha256'] for x in json.loads(Path(pd['ExecutionManifestPath']).read_bytes())}
timer=list((installed/'lib').glob('stdlib-*/ebin/timer.beam'));assert len(timer)==1 and sha(timer[0])==expected[timer[0].relative_to(installed).as_posix()]
images=[dict(Module=m,Path=str((original/(m+'.beam')).resolve()),Sha256=sha(original/(m+'.beam'))) for m in ['native_resource_family527','resource_queue593']]+[dict(Module='timer',Path=str(timer[0].resolve()),Sha256=sha(timer[0]))]
imports={x['Module']:helper.imports(x['Path']) for x in images};dependencies=sorted({x['Module'] for x in images}|{i['Module'] for rows in imports.values() for i in rows});sources=set()
for root in [r/'src',r/'tools/Native/Erlang.NifNativeBackend']:
 sources.update(p for p in root.rglob('*') if p.is_file() and p.suffix in {'.cs','.csproj','.props','.targets','.json'} and not {'bin','obj','.git','__pycache__'}.intersection(p.parts))
sources.update(r/n for n in ['Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json'] if (r/n).is_file());source_rows=[row(p) for p in sorted(sources)];binary_rows=[row(p) for p in sorted(backend.parent.rglob('*')) if p.is_file()];results=[];env=os.environ.copy();env['DOTNET_PROCESSOR_COUNT']='2'
for route in ['ordinary','reviewed']:
 process={};result=None;route_errors=[];out=a/route
 try:
  check_source593()
  for frozen in original_rows+source_rows+binary_rows:
   assert sha(frozen['Path'])==frozen['Sha256'],frozen['Path']
  out=a/route;assert not out.exists();out.mkdir();save(out/'original-inventory.json',original_rows);save(out/'source-inventory.json',source_rows);save(out/'backend-inventory.json',binary_rows);save(out/'imports.json',imports)
  input_rows=original_rows+source_rows+binary_rows+[row(p) for p in [planpath,original/'completion.json',original/'artifacts.json',receipt,provider,Path(pd['ExecutionManifestPath']),Path(__file__).resolve(),out/'original-inventory.json',out/'source-inventory.json',out/'backend-inventory.json',out/'imports.json',*timer]]
  inputs={x['Path']:x for x in input_rows};ad=dict(Status='RESOURCE_QUEUE593_SOURCE_BOUND',ProtocolSchemaVersion=2,Pin=pd['SourceCommit'],Job='queue593',Images=images,Dependencies=dependencies,Libraries=[dict(Path=str(library),Sha256=sha(library),BuildReceiptPath=str(receipt),BuildReceiptSha256=sha(receipt))],OriginalRawPath=str(original/'observed.etf'),OriginalDeterministicPath=str(original/'observed-deterministic.etf'),OriginalInventoryPath=str(out/'original-inventory.json'),OriginalInventorySha256=sha(out/'original-inventory.json'),PlanSha256=sha(out/'source-inventory.json'),Inputs=list(inputs.values()));save(out/'admission.json',ad)
  cmd=['dotnet','exec','--fx-version','10.0.11',str(backend),'--resource-queue593-child',str(out/'admission.json'),sha(out/'admission.json'),route,'private-owned',str(out/'managed')];process={}
  try:process=owner.owned(cmd,out,'child',180,env,str(r))
  except Exception as e:process=json.loads((out/'child.exit.json').read_bytes()) if (out/'child.exit.json').exists() else dict(Error=repr(e))
  changed=[]
  for frozen in inputs.values():
   try:
    if sha(frozen['Path'])!=frozen['Sha256']:changed.append(frozen['Path'])
   except Exception as error:
    changed.append(frozen['Path']);route_errors.append('Input after '+frozen['Path']+': '+repr(error))
  result=dict(Route=route,ExitCode=process.get('ExitCode'),Settled=process.get('Settled'),InputsUnchanged=not changed,Changed=changed,Errors=route_errors,Process=process)
  print(route,result['ExitCode'],'settled',result['Settled'],flush=True)
 except Exception as error:
  route_errors.append(repr(error))
  result=dict(Route=route,ExitCode=process.get('ExitCode'),Settled=process.get('Settled',False),InputsUnchanged=False,Errors=route_errors,Process=process)
  print(route,'preparation/postcheck failed:',repr(error),flush=True)
 results.append(result)
 try:
  if out.exists():save(out/'result.json',result)
 except Exception as error:route_errors.append('Result receipt: '+repr(error))
closure_errors=[]
try:check_source593()
except Exception as error:closure_errors.append(repr(error))
save(a/'routes.json',results);save(a/'closure-errors.json',closure_errors)
raise SystemExit(0 if not closure_errors and len(results)==2 and all(x['ExitCode']==0 and x['Settled'] and x['InputsUnchanged'] and not x.get('Errors') for x in results) else 1)
