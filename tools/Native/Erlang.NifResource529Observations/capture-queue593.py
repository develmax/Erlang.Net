"""Fresh original queue593 observations using an admitted, unchanged family1 native image."""
from pathlib import Path
import sys,os,json,hashlib,shutil,importlib.util
sys.dont_write_bytecode=True
import argparse,subprocess
parser=argparse.ArgumentParser(description=__doc__)
for key in ['repository','expected-head','source-plan-sha256','provider-admission','provider-sha256','output']:
 parser.add_argument('--'+key,required=True)
parser.add_argument('--retained-original',required=True)
parser.add_argument('--retained-completion-sha256',required=True)
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
out=args.output;out.mkdir(parents=True)
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def save(p,d):p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8')
def load(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
helpers=r/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts';inv=load('queue533inventory',helpers/'bounded_inventory.py');owner=load('queue533owner',helpers/'owned.py')
provider=Path(args.provider_admission).resolve();assert sha(provider)==args.provider_sha256.upper();p=json.loads(provider.read_bytes());assert p['SourceCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5';installed=Path(p['InstalledOtpRoot']);manifest=Path(p['ExecutionManifestPath']);assert sha(manifest)==p['ExecutionManifestSha256'];expected={x['Path'].replace('\\','/'):x['Sha256'] for x in json.loads(manifest.read_bytes())};before=inv.inventory(installed);assert before==expected and len(before)==6427;save(out/'provider-before.json',before)
base=Path(args.retained_original).resolve();assert sha(base/'completion.json')==args.retained_completion_sha256.upper();completion=json.loads((base/'completion.json').read_bytes());assert completion['AllSelectedPassed'] and completion['OriginalExecuted'] and not completion['ManagedExecuted'];br=base/'build-family1/build-receipt.json';build=json.loads(br.read_bytes());dso=base/'build-family1/resource529.dll';assert sha(dso)==build['LibrarySha256'] and build['CompileExitCode']==build['LinkExitCode']==0
assert build['Status']=='RESOURCE529_WINDOWS_BUILD_COMPLETE' and build['Variant']=='family1' and build['PinnedCommit']==plan['PinnedCommit']
assert sha(br.parent/'fixture.c')==build['CompiledSourceSha256']==build['NativeSourceSha256']==plan['Variants']['family1']['NativeSourceSha256']
assert sha(br.parent/'fixture.erl')==build['CopiedErlangSourceSha256']==build['ErlangSourceSha256']==plan['Variants']['family1']['ErlangSourceSha256']
timers=list((installed/'lib').glob('stdlib-*/ebin/timer.beam'));assert len(timers)==1
assert sha(timers[0])==expected[timers[0].relative_to(installed).as_posix()]
fixture=tool/'fixtures/resource_queue593.erl';shutil.copyfile(fixture,out/fixture.name)
family=r/'tools/Native/Erlang.NifResource529Observations/fixtures/native_resource_family527.erl';assert sha(family)==build['ErlangSourceSha256'];shutil.copyfile(family,out/family.name)
q=lambda x:json.dumps(str(x).replace('\\','/'))
expr='''W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,
try
 Expected=filename:absname(INSTALLED),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),
 C=[compile:file(F,[return_errors,return_warnings])||F<-["native_resource_family527.erl","resource_queue593.erl"]],W("compile.etf",C),
 true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,C),
 Mods=[native_resource_family527,resource_queue593,timer],
 [code:ensure_loaded(M)||M<-Mods],
 Paths=[{native_resource_family527,filename:absname("native_resource_family527.beam")},{resource_queue593,filename:absname("resource_queue593.beam")},{timer,filename:absname(TIMER)}],
 Owners=fun()->[begin P=filename:absname(code:which(M)),Running=M:module_info(md5),{ok,{M,Running}}=beam_lib:md5(P),{M,P,Running,M:module_info(compile),M:module_info(attributes),M:module_info(exports)} end||{M,P}<-Paths] end,
 Before=Owners(),W("owners-before.etf",Before),
 ok=native_resource_family527:load(LIBRARY,0),V=resource_queue593:run(),
 W("observed.etf",V),ok=file:write_file("observed-deterministic.etf",term_to_binary(V,[deterministic,{minor_version,2}])),
 W("owners-after.etf",Owners()),Before=Owners(),
 true=lists:all(fun({_,{{returned593,_,_,_,_},[{sender_cleanup593,ok},{receiver_cleanup593,ok}]}})->true;(_)->false end,V),
 [consume,abandon]=[K||{K,_}<-V],
 Cleanup=[{M,code:delete(M),code:purge(M),code:is_loaded(M)}||M<-[resource_queue593,native_resource_family527]],W("cleanup.etf",Cleanup),
 true=lists:all(fun({_,true,_,false})->true;(_)->false end,Cleanup),halt(0)
catch Class:Reason:Stack->W("failure.etf",{Class,Reason,Stack}),halt(3) end.'''.replace('INSTALLED',q(installed)).replace('LIBRARY',q(dso.with_suffix(''))).replace('TIMER',q(timers[0]))
(out/'expression.erl').write_text(expr,encoding='utf-8');env=os.environ.copy();env['ERL_CRASH_DUMP']=str(out/'erl_crash.dump');receipt={};errors=[]
try:receipt=owner.owned([str(installed/'bin/erl.exe'),'+S','2:2','+A','1','-noshell','-noinput','-boot','no_dot_erlang','-pa',str(out),'-eval',expr],out,'original',90,env,str(out))
except Exception as e:
 errors.append(repr(e))
 if (out/'original.exit.json').is_file():receipt=json.loads((out/'original.exit.json').read_bytes())
finally:
 try:check_source593()
 except Exception as e:errors.append('Final source closure: '+repr(e))
 after=None
 try:
  after=inv.inventory(installed);save(out/'provider-after.json',after)
  if after!=before:errors.append('Provider bytes changed')
 except Exception as e:errors.append('Provider after: '+repr(e))
 try:
  if sha(dso)!=build['LibrarySha256']:errors.append('Retained native image bytes changed')
 except Exception as e:errors.append('Retained native image after: '+repr(e))
passed=not errors and receipt.get('Settled') and receipt.get('ExitCode')==0 and all((out/n).is_file() for n in ['observed.etf','observed-deterministic.etf','cleanup.etf','owners-before.etf','owners-after.etf'])
inventory=[dict(Path=f.relative_to(out).as_posix(),Bytes=f.stat().st_size,Sha256=sha(f)) for f in sorted(out.rglob('*')) if f.is_file()];save(out/'artifacts.json',inventory)
save(out/'completion.json',dict(Passed=bool(passed),LaunchAttempted=True,OriginalExecuted=bool(receipt.get('Pid')),ManagedExecuted=False,OriginalProviderSha256=sha(provider),NativeBuildReceiptPath=str(br.resolve()),NativeBuildReceiptSha256=sha(br),NativeLibraryPath=str(dso.resolve()),NativeLibrarySha256=sha(dso),RetainedOriginalCompletionSha256=sha(base/'completion.json'),ArtifactInventorySha256=sha(out/'artifacts.json'),SourcePlanSha256=sha(planpath),ExpectedHead=args.expected_head,ObserverSourceSha256=sha(fixture),ProviderUnchanged=after==before,Process=receipt,Errors=errors,WholeContractsClosed=0))
print('Original queue capture passed:',bool(passed),'raw bytes:',(out/'observed.etf').stat().st_size if (out/'observed.etf').exists() else None);raise SystemExit(0 if passed else 1)
