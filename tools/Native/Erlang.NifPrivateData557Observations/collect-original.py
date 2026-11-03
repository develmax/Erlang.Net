"""Fresh original acquisition using an approved genuine existing DLL; no native build."""
import argparse,json,os,shutil,sys
from pathlib import Path
from evidence import *
ap=argparse.ArgumentParser()
for name in ['repository','output','official-admission','library','build-receipt']:ap.add_argument('--'+name,type=Path,required=True)
for name in ['source-plan-sha256','approved-official-admission-sha256','approved-library-sha256','approved-build-receipt-sha256']:ap.add_argument('--'+name,required=True)
a=ap.parse_args();repo=a.repository.resolve();here=Path(__file__).resolve().parent;out=fresh_output(repo,a.output)
errors=[];executed=False;process=None;before=None;sealed=None;installed=None;inventory=None;source_rows=[]
try:
    require(os.name=='nt','This genuine prebuilt MSVC DLL profile is Windows-only')
    paths=package(repo,here,a.source_plan_sha256)
    official=verified(a.official_admission,a.approved_official_admission_sha256);ad=json.loads(official.read_bytes())
    require(ad['Provider']=='official-release' and ad['SourceCommit']==PIN and ad['ReleaseTag']=='OTP-29.1.1' and ad['EveryExtractedFileMatchesArchive'] is True and ad['ExecutionFileCount']==6427,'Official provider profile differs')
    manifest=verified(ad['ExecutionManifestPath'],ad['ExecutionManifestSha256']);installed=Path(ad['InstalledOtpRoot']).resolve()
    expected={r['Path'].replace('\\','/'):r['Sha256'] for r in json.loads(manifest.read_bytes())};require(len(expected)==6427,'Provider inventory count differs')
    inventory=load('inventory557v2',repo/INVENTORY);owned=load('owned557v2',repo/OWNED)
    before=inventory.inventory(installed);save(out/'provider-before.json',before);require(before==expected,'Official provider inventory differs before execution')
    library=verified(a.library,a.approved_library_sha256);receipt=verified(a.build_receipt,a.approved_build_receipt_sha256);build=json.loads(receipt.read_bytes())
    require(build['Status']=='LOAD_PRIVATE557_GENUINE_WINDOWS_BUILD' and build['Pin']==PIN and build['LibrarySha256']==sha(library),'Build/library admission differs')
    require(library.parent==receipt.parent and library.name=='load_private557.dll','Wrong genuine fixture DLL')
    fixture=verified(library.parent/'fixture.c',build['FixtureSha256']);require(sha(fixture)==sha(here/'fixtures/load_private557.c'),'Frozen C fixture differs')
    headers_plan=verified(build['HeaderRecipePath'],build['HeaderRecipeSha256']);headers=json.loads(headers_plan.read_bytes())['Headers']
    for h in headers:
        verified(installed/'usr/include'/h['OutputName'],h['Sha256']);verified(library.parent/h['OutputName'],h['Sha256'])
    original=out/'original';original.mkdir()
    for n in ['load_private557.erl','load_private_observer557.erl']:shutil.copyfile(here/'fixtures'/n,original/n)
    paths.extend([official,manifest,library,receipt,headers_plan,fixture,*[library.parent/h['OutputName'] for h in headers]])
    paths.extend(p for p in library.parent.rglob('*') if p.is_file())
    paths.extend(original/n for n in ['load_private557.erl','load_private_observer557.erl'])
    sealed=state_inputs(paths);save(out/'sealed-inputs.json',sealed)
    source_rows=state_inputs([fixture,original/'load_private557.erl',original/'load_private_observer557.erl'])
    stem=library.with_suffix('').as_posix();require(all(ord(c)<128 for c in stem),'Fixture path requires explicit nonASCII source protocol')
    expression=('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
      'try Expected=filename:absname(os:getenv("LOAD_PRIVATE557_ROOT")),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
      'W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(system_architecture)}),'
      'C=[compile:file(F,[return_errors,return_warnings])||F<-["load_private557.erl","load_private_observer557.erl"]],W("compile.etf",C),'
      'true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,C),'
      'Loaded=[begin {ok,B}=file:read_file(atom_to_list(M)++".beam"),{module,M}=erlang:load_module(M,B),{M,B} end||M<-[load_private557,load_private_observer557]],W("loaded-images.etf",Loaded),'
      'T=load_private_observer557:observe('+json.dumps(stem)+',probe),W("observed.etf",T),'
      'ok=file:write_file("observed-deterministic.etf",term_to_binary(T,[deterministic,{minor_version,2}])),'
      'W("executing-owners.etf",[{M,code:which(M),M:module_info(md5),M:module_info(compile),M:module_info(attributes),M:module_info(exports)}||M<-[load_private557,load_private_observer557]]),'
      '{returned557,_}=T,D=code:delete(load_private557),P=code:purge(load_private557),L=code:is_loaded(load_private557),W("cleanup.etf",{D,P,L}),false=L,halt(0)'
      ' catch Class:Reason:Stack->W("entry-failure.etf",{Class,Reason,Stack}),halt(77) end.')
    save(out/'original-command.json',{'Command':[str(installed/'bin/erl.exe'),'-noshell','-eval',expression],'WorkingDirectory':str(original),'CrashDump':str(out/'original2.crash.dump')})
    env=dict(os.environ)
    for key in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR']:env.pop(key,None)
    env.update(LOAD_PRIVATE557_ROOT=str(installed),ERL_CRASH_DUMP=str(out/'original2.crash.dump'))
    executed=True
    process=owned.owned([str(installed/'bin/erl.exe'),'-noshell','-eval',expression],out,'original2',120,env,str(original))
    require(process_ok(process),'Original physical process did not settle successfully')
    for name in ['load_private557.beam','load_private_observer557.beam','metadata.etf','loaded-images.etf','executing-owners.etf','compile.etf','observed.etf','observed-deterministic.etf','cleanup.etf']:require((original/name).is_file(),'Missing original output: '+name)
except Exception as exc:errors.append(error('original',exc))
finally:
    if process is None and (out/'original2.exit.json').is_file():process=json.loads((out/'original2.exit.json').read_bytes())
    if sealed is not None:
        try:verify_rows(sealed)
        except Exception as exc:errors.append(error('sealed-inputs-after',exc))
    if inventory is not None and installed is not None:
        try:
            after=inventory.inventory(installed);save(out/'provider-after.json',after)
            require(before is not None and after==before,'Provider changed during acquisition')
        except Exception as exc:errors.append(error('provider-after',exc))
# Success completion is emitted only after execution, input and provider closure checks.
try:
    rows=files(out,('completion.json','output-inventory.json'));save(out/'output-inventory.json',rows)
except Exception as exc:errors.append(error('output-inventory',exc))
attempted=executed
executed=isinstance(process,dict) and type(process.get('Pid')) is int and process['Pid']>0
passed=executed and process_ok(process) and not errors
completion={'SubjectKind':'GENUINE_LOAD_PRIVATE557','Status':'LOAD_PRIVATE557_ORIGINAL_V2','Pin':PIN,'Passed':passed,'OriginalExecuted':executed,'OriginalAttempted':attempted,'Process':process,'Errors':errors,'Sources':source_rows,'WholeContractsClosed':0,'ManagedExecuted':False,'RawEtfRewritten':False,'NormalizationPerformed':False,'CrashDumpRetainedIfProduced':'original2.crash.dump'}
for field,name in [('ObservedSha256','original/observed.etf'),('DeterministicSha256','original/observed-deterministic.etf'),('ProviderBeforeSha256','provider-before.json'),('ProviderAfterSha256','provider-after.json')]:
    if (out/name).is_file():completion[field]=sha(out/name)
if passed:
    completion['NativeLibrary']=row(library);completion['BuildReceipt']=row(receipt);completion['OfficialAdmission']=row(official);completion['ProviderExecutionManifest']=row(manifest)
completion['CompiledImages']=[{'Module':n,'Path':'original/'+n+'.beam','Bytes':(out/'original'/ (n+'.beam')).stat().st_size,'Sha256':sha(out/'original'/(n+'.beam'))} for n in ['load_private557','load_private_observer557'] if (out/'original'/(n+'.beam')).is_file()]
for field,name in [('InputInventory','sealed-inputs.json'),('OutputInventory','output-inventory.json')]:
    if (out/name).is_file():completion[field]={'Path':name,'Sha256':sha(out/name)}
save(out/'completion.json',completion);print(json.dumps({'Passed':passed,'CompletionSha256':sha(out/'completion.json'),'Output':str(out)}));sys.exit(0 if passed else 1)
