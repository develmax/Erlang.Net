"""Root-owned Linux only: current source/bridge -> genuine pthread original -> four independent children."""
import argparse, hashlib, importlib.util, json, os, re, shutil, subprocess, sys
from pathlib import Path
sys.dont_write_bytecode=True
TOOL=Path(__file__).resolve().parent
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def save(p,v):Path(p).write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def need(v,m):
 if not v:raise RuntimeError(m)
def load(p,n):
 spec=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m

def main():
 p=argparse.ArgumentParser(description=__doc__)
 for key in ['repository','otp-source','build-receipt','bridge-receipt','managed-host','output']:p.add_argument('--'+key,type=Path,required=True)
 for key in ['admission-sha256','bridge-receipt-sha256','host-sha256','runtime-version','expected-head']:p.add_argument('--'+key,required=True)
 p.add_argument('--execute',action='store_true');a=p.parse_args();repo=a.repository.resolve();out=a.output.resolve();a.managed_host=a.managed_host.resolve();a.bridge_receipt=a.bridge_receipt.resolve()
 need(sys.platform.startswith('linux') and os.uname().machine=='x86_64' and out.is_relative_to(repo/'artifacts') and not out.exists(),'Fresh Linux x64 repository artifacts required')
 need(subprocess.check_output(['git','-C',str(repo),'rev-parse','HEAD'],text=True).strip()==a.expected_head,'Wrong exact subject head')
 need(sha(a.bridge_receipt)==a.bridge_receipt_sha256.upper() and sha(a.managed_host)==a.host_sha256.upper(),'Reviewed bridge/host inputs differ')
 proof=json.loads(a.bridge_receipt.read_bytes());need(proof['Status'] in ['VERSION651_LINUX_BRIDGE_BUILD','PASS_OWNED_LINUX_V4_BRIDGE_BUILD'],'Actual built bridge receipt required')
 bridge=Path(proof['BridgePath']);need(sha(bridge)==proof['BridgeSha256'],'Bridge byte mismatch')
 for row in proof['Sources']:need(sha(row['Path'])==row['Sha256'],'Bridge source drift')
 expected=[repo/'tools/Native/Erlang.NifNativeBackend/native'/name for name in ['nif_bridge_api.h','nif_snprintf791.h']]
 bound=proof['ProjectHeaderInputs'];need(len(bound)==len(expected) and {Path(row['Path']).resolve():row['Sha256'] for row in bound}=={x.resolve():sha(x) for x in expected},'Real current project-header closure required')
 inputs=[*TOOL.glob('*.py'),*TOOL.glob('*.md'),*sorted((TOOL/'fixtures').glob('*')),a.build_receipt,a.bridge_receipt,*[Path(row['Path']) for row in proof['Sources']],*expected,repo/'Directory.Build.props']
 inputs += [x for x in (repo/'src/Runtime/Erlang.Runtime.Core/Native/Nif').rglob('*') if x.is_file() and x.suffix in ['.cs','.c','.h']]
 inputs += list((repo/'src/Runtime/Erlang.Runtime.Core/Primitives').glob('TermDisplayWriter*.cs'))+list((repo/'src/Runtime/Erlang.Runtime.Core/Primitives').glob('PrintfTerm*.cs'))
 inputs += list((repo/'tools/Native/Erlang.NifNativeBackend').glob('*.cs'))
 inputs += [repo/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py',repo/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/passive_etf.py']
 managed=[x for x in a.managed_host.parent.rglob('*') if x.is_file() and x.suffix in ['.dll','.so','.json','.pdb']]
 dotnet=shutil.which('dotnet');need(dotnet,'Existing dotnet required');need(sha(a.managed_host.parent/'Erlang.NifBridge.so')==sha(bridge),'Ordinary host bridge copy differs');inputs += managed+[bridge,Path(dotnet)];sealed=[dict(Path=str(x.resolve()),Sha256=sha(x)) for x in sorted(set(inputs))]
 out.mkdir(parents=True);save(out/'sealed-inputs.json',sealed);save(out/'launch-plan.json',dict(Execute=a.execute,FullCompatibilityClaimed=False,FullTlsCompatibilityClaimed=False))
 if not a.execute:print(json.dumps(dict(PreparedOnly=True)));return 0
 provider=load(repo/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py','mutex813_provider');a.official_admission=None
 installed,erl,before,after=provider.provider(a,repo);save(out/'provider-before.json',before)
 env=dict(os.environ,MAP_FAMILY504_RUNTIME_ROOT=str(installed),ERL_CRASH_DUMP='/dev/null')
 for key in ['LD_PRELOAD','LD_AUDIT','LD_LIBRARY_PATH','ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERLC_EMULATOR','ERL_COMPILER_OPTIONS','GITHUB_TOKEN','GH_TOKEN']:env.pop(key,None)
 errors=[];stages={};completed=[]
 def stage(name,command,directory,prepared=False):
  print(json.dumps(dict(Stage=name,State='starting')),flush=True)
  try:
   if not prepared:directory.mkdir(parents=True)
   stages[name]=provider.execute(repo,[str(x) for x in command],directory,env);return True
  except Exception as error:errors.append(name+': '+repr(error));return False
 try:
  headers=out/'headers';headers.mkdir();recipe=json.loads((repo/'tools/Native/Erlang.NifFixtureHost/native/source-plan.json').read_bytes())
  for row in recipe['Headers']:
   if row['OutputName'].endswith('.in'):continue
   source=installed/'usr/include'/row['OutputName'];need(sha(source)==row['Sha256'],'Pinned SDK header differs');shutil.copyfile(source,headers/row['OutputName'])
  shutil.copyfile(installed/'usr/include/erl_int_sizes_config.h',headers/'erl_int_sizes_config.h')
  sizes=(headers/'erl_int_sizes_config.h').read_text()
  for name,value in dict(SIZEOF_CHAR=1,SIZEOF_SHORT=2,SIZEOF_INT=4,SIZEOF_LONG=8,SIZEOF_LONG_LONG=8,SIZEOF_VOID_P=8).items():need(re.search(r'^#define\s+'+name+r'\s+'+str(value)+r'\s*$',sizes,re.M),'Wrong LP64 '+name)
  gcc,nm=[shutil.which(name) for name in ['gcc','nm']];need(gcc and nm,'Existing gcc/nm required');native=out/'native';native.mkdir();library=native/'mutex795.so'
  ok=stage('compile',[gcc,'-shared','-fPIC','-std=c11','-O2','-Wall','-Wextra','-Werror','-pthread','-I',headers,TOOL/'fixtures/mutex795.c','-o',library],native/'compile')
  if not ok:raise RuntimeError('Dependent NIF/original/managed stages skipped after failed native preparation')
  need(stage('exports',[nm,'-D','--defined-only','--format=posix',library],native/'exports'),'Native export inspection failed')
  exports={line.split()[0] for line in (native/'exports/original.stdout').read_text().splitlines() if line.split()};need(exports=={'nif_init'},'Wrong NIF exports')
  build=native/'build-receipt.json';save(build,dict(Status='SOURCE_BOUND_LINUX_MUTEX795_NATIVE_BUILD',Pin=PIN,SourceSha256=sha(TOOL/'fixtures/mutex795.c'),LibraryPath=str(library),LibrarySha256=sha(library),Headers=[dict(Path=str(x),Sha256=sha(x)) for x in headers.iterdir()],Tools=[dict(Path=x,Sha256=sha(x)) for x in [gcc,nm]],Stages=stages,NativeExecuted=False))
  original=out/'original';original.mkdir()
  for name in ['mutex795.erl','mutex795_driver.erl']:shutil.copyfile(TOOL/'fixtures'/name,original/name)
  def q(x):return json.dumps(str(x))
  expression=('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,try '
    'Expected=filename:absname(os:getenv("MAP_FAMILY504_RUNTIME_ROOT")),Expected=filename:absname(code:root_dir()),'
    'C=[compile:file(F,[return_errors,return_warnings])||F<-["mutex795.erl","mutex795_driver.erl"]],W("compile.etf",C),'
    'true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,C),'
    'T=mutex795_driver:observe('+q(library.with_suffix(''))+',probe),W("observed.etf",T),'
    'ok=file:write_file("observed-deterministic.etf",term_to_binary(T,[deterministic,{minor_version,2}])),'
    '{loaded,V}=T,8=tuple_size(V),W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(system_architecture)}),'
    'true=erlang:delete_module(mutex795),true=erlang:purge_module(mutex795),false=erlang:module_loaded(mutex795),'
    'W("cleanup.etf",false),halt(0) catch Cl:Re:St->W("entry-failure.etf",{Cl,Re,St}),halt(77) end.')
  need(stage('original',[erl,'+S','1:1','+A','1','+SDcpu','1','+SDio','2','-noshell','-noinput','-boot','no_dot_erlang','-pa',original,'-eval',expression],original,True),'Genuine original acquisition failed')
  decoder=load(repo/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/passive_etf.py','mutex813_passive');v=decoder.decode_exact((original/'observed.etf').read_bytes());need(v[0]=='tuple' and v[1][0]==('atom','loaded') and len(v[1][1][1])==8,'Complete original8-int outcome missing')
  child_inputs=[*sealed,dict(Path=str(build),Sha256=sha(build)),*[dict(Path=str(x),Sha256=sha(x)) for x in sorted(original.glob('*')) if x.is_file()]]
  admission=out/'admission.json';save(admission,dict(Status='SOURCE_BOUND_LINUX_MUTEX795_WORKER',Pin=PIN,NativeLibraryPath=str(library),NativeLibrarySha256=sha(library),BuildReceiptPath=str(build),BuildReceiptSha256=sha(build),BridgePath=str(bridge),BridgeSha256=sha(bridge),BridgeReceiptPath=str(a.bridge_receipt.resolve()),BridgeReceiptSha256=sha(a.bridge_receipt),ModuleBeam=str(original/'mutex795.beam'),ObserverBeam=str(original/'mutex795_driver.beam'),OriginalRawPath=str(original/'observed.etf'),OriginalDeterministicPath=str(original/'observed-deterministic.etf'),Inputs=child_inputs))
  dotnet=shutil.which('dotnet');need(dotnet,'Existing dotnet required')
  for profile in ['ordinary','private-owned']:
   for route in ['ordinary','reviewed']:
    name=profile+'-'+route;directory=out/name;actual=directory/'actual';good=stage(name,[dotnet,'exec','--fx-version',a.runtime_version,a.managed_host,'--nif-linux-mutex795-child',admission,sha(admission),route,profile,actual],directory)
    try:
     result=json.loads((actual/'completion.json').read_bytes());need(good and result['Passed'] and result['FirstWholeEqual'] and result['SecondWholeEqual'] and result['ActualVmAuthorityNegatives'] and result['RealNativeHolderRetirementFence'] and result['NativeBaselinesRestored'],'Actual managed result/fence failed');need(result['AdmissionSha256']==sha(admission) and result['Route']==route and result['OwnedProfile']==(profile=='private-owned') and result['NormalizationPerformed'] is False,'Wrong route/profile/admission');completed.append(name)
    except Exception as error:errors.append(name+'-validation: '+repr(error))
 except Exception as error:errors.append('preparation: '+repr(error))
 finally:
  try:
   after_value=after();save(out/'provider-after.json',after_value);need(after_value==before['Inventory'],'Original provider changed')
   for row in sealed:need(sha(row['Path'])==row['Sha256'],'Sealed source/runtime/bridge changed')
  except Exception as error:errors.append('final-guards: '+repr(error))
  if len(completed)!=4:errors.append('Managed routes completed '+str(len(completed))+'/4')
  save(out/'completion.json',dict(Passed=not errors,Completed=completed,Expected=4,Errors=errors,Stages=stages,FullCompatibilityClaimed=False,FullTlsCompatibilityClaimed=False))
 return 1 if errors else 0
if __name__=='__main__':raise SystemExit(main())
