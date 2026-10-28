"""Acquire original resource529 workers then independently compare both loaders/profiles."""
import argparse, hashlib, importlib.util, json, os, shutil, sys
from pathlib import Path
sys.dont_write_bytecode = True
ROOT=Path(__file__).resolve().parents[3]
TOOL=Path(__file__).resolve().parent
PIN="ad05823719d77c8faee87348ea39513d4e2f99c5"
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def dependency_sha(row):
 mode=row.get('HashMode','Raw')
 path=ROOT/row['Path']
 if mode not in {'Raw','TextLF'} or mode=='TextLF' and path.suffix not in {'.csproj','.props','.targets','.json'}:
  raise ValueError('Unexpected declared source hash mode')
 data=path.read_bytes()
 if mode=='TextLF':data=data.replace(b'\r\n',b'\n')
 return hashlib.sha256(data).hexdigest().upper()

def save(p,v): p.write_text(json.dumps(v,indent=2)+"\n")
def load(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def es(p):
 s=str(p).replace("\\","/")
 if any(ord(c)<32 or ord(c)>126 for c in s): raise ValueError("ASCII acquisition paths required")
 return json.dumps(s)
def expr(job, spec, libraries, directory, installed, modules):
 # Entire actual result is encoded in both profiles, never normalized.
 fresh="<<>>" if job=="family527" else "element(2,file:read_file(\"native_resource_handoff528.beam\"))"
 first=es(libraries[0].with_suffix(""));second=es(libraries[-1].with_suffix(""))
 marker=directory/'marker529'
 if job=='family527':
  assert spec['Observer']=='resource_observer529' and spec['ObserverArity']==5
  call='resource_observer529:run(family527,'+first+','+second+','+fresh+','+es(marker)+')'
 else:
  assert job in ['fresh_load_after_purge','genuine_upgrade']
  assert spec['Observer']=='resource_takeover_observer529' and spec['ObserverArity']==6
  call='resource_takeover_observer529:run('+job+','+first+','+fresh+','+second+','+es(str(marker)+'-first')+','+es(str(marker)+'-second')+')'
 files=",".join(es(x+".erl") for x in modules)
 return ('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,try '
  +'Expected=filename:absname('+es(installed)+'),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
  +'W("metadata.etf",{Expected,erlang:system_info(version),erlang:system_info(system_architecture)}),'
  +'C=[compile:file(F,[return_errors,return_warnings])||F<-['+files+']],W("compile.etf",C),'
  +'true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,C),'
  +'W("executing-owners.etf",[{M,code:which(M),M:module_info(md5),M:module_info(compile),M:module_info(attributes),M:module_info(exports)}||M<-['+','.join(modules)+']]),'
  +'V='+call+','
  +'W("observed.etf",V),ok=file:write_file("observed-deterministic.etf",term_to_binary(V,[deterministic,{minor_version,2}])),'
  +'Cleanup=[{M,code:delete(M),code:purge(M),code:is_loaded(M)}||M<-['+','.join(modules)+']],W("cleanup.etf",Cleanup),'
  +'true=lists:all(fun({_,_,_,false})->true;(_)->false end,Cleanup),'
  +'case V of {returned529,_}->halt(0);_->halt(78) end'
  +' catch Class:Reason:Stack->W("entry-failure.etf",{Class,Reason,Stack}),halt(77) end.')

def validate_batch(originals, managed, selected, original_only):
 failures=[]
 if set(originals)!=set(selected) or not all(v.get('Succeeded') is True for v in originals.values()):
  failures.append('Selected original protocol batch incomplete')
 expected={name+'-'+route+'-'+profile for name in selected for route in ['ordinary','reviewed'] for profile in ['ordinary','private-owned']}
 if not original_only and (set(managed)!=expected or not all(v.get('Succeeded') is True for v in managed.values())):
  failures.append('Selected managed protocol matrix incomplete')
 if original_only and managed:failures.append('Unexpected managed execution during original-only batch')
 return failures

def main():
 a=argparse.ArgumentParser(description=__doc__)
 for n in ['provider-admission','provider-sha256','plan-sha256','output']:a.add_argument('--'+n,required=True)
 a.add_argument('--execute-resource529',action='store_true');a.add_argument('--original-only',action='store_true')
 a.add_argument('--jobs',nargs='+',choices=['family527','fresh_load_after_purge','genuine_upgrade'],help='Explicit independent protocols; default preserves all three')
 a.add_argument('--dotnet-fx-version',default='10.0.11',help='Exact admitted backend runtime version')
 a.add_argument('--backend');a.add_argument('--backend-sha256');a.add_argument('--backend-manifest');a.add_argument('--backend-manifest-sha256');a.add_argument('--dependency-images',help='Source-bound original/runtime BEAM dependency inventory')
 x=a.parse_args();planpath=TOOL/'source-plan.json'
 if os.name!='nt' or sha(planpath)!=x.plan_sha256.upper():raise RuntimeError('Windows/source-plan admission')
 plan=json.loads(planpath.read_bytes());assert plan['PinnedCommit']==PIN
 assert plan['ProtocolSchemaVersion']==2 and plan['OriginalFirstRequired'] is True
 assert set(plan['Variants'])=={'family1','handoff1','handoff2'}
 assert set(plan['Jobs'])=={'family527','fresh_load_after_purge','genuine_upgrade'}
 names=x.jobs if x.jobs is not None else list(plan['Jobs'])
 if len(names)!=len(set(names)):
  raise RuntimeError('Duplicate selected protocols')
 selected={name:plan['Jobs'][name] for name in names}
 selected_variants={variant for job in selected.values() for variant in job['Variants']}
 assert plan.get('SealState')=='FINAL_SOURCE_BOUND_NOT_EXECUTED' and plan['Dependencies']
 assert plan['BuildScriptSha256']==sha(TOOL/'build-native.py')
 for row in plan['Dependencies']:assert dependency_sha(row)==row['Sha256']
 admission=Path(x.provider_admission).resolve();assert sha(admission)==x.provider_sha256.upper()
 provider=json.loads(admission.read_bytes());assert provider['SourceCommit']==PIN and provider['ReleaseTag']=='OTP-29.1.1'
 installed=Path(provider['InstalledOtpRoot']).resolve();manifest=Path(provider['ExecutionManifestPath']);assert sha(manifest)==provider['ExecutionManifestSha256']
 helpers=ROOT/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts';owned=load('owned529',helpers/'owned.py');inv=load('inv529',helpers/'bounded_inventory.py')
 expected={r['Path'].replace('\\','/'):r['Sha256'] for r in json.loads(manifest.read_bytes())};before=inv.inventory(installed);assert len(expected)==6427 and before==expected
 # All independent prerequisites are checked before any original execution.
 dep=[];backend_inputs=[]
 if not x.original_only:
  assert x.backend and x.backend_sha256 and x.dependency_images and x.backend_manifest and x.backend_manifest_sha256
  backend=Path(x.backend).resolve();assert sha(backend)==x.backend_sha256.upper()
  assert sha(x.backend_manifest)==x.backend_manifest_sha256.upper()
  backend_inputs=json.loads(Path(x.backend_manifest).read_bytes())
  assert any(Path(row['Path']).resolve()==backend and row['Sha256']==sha(backend) for row in backend_inputs)
  for row in backend_inputs:assert sha(row['Path'])==row['Sha256']
  dep=json.loads(Path(x.dependency_images).read_bytes())
  for row in dep:assert sha(row['Path'])==row['Sha256']
 out=Path(x.output).resolve();assert out.is_relative_to(ROOT/'artifacts/nif-resource529') and not out.exists();es(out);out.mkdir(parents=True)
 save(out/'provider-before.json',before);save(out/'selection.json',{'Execute':x.execute_resource529,'OriginalOnly':x.original_only,'PlanSha256':sha(planpath),'ProviderSha256':sha(admission),'DependencyImages':dep,'SelectedProtocols':list(selected),'SelectedVariants':sorted(selected_variants)})
 if not x.execute_resource529:save(out/'completion.json',{'PreparedOnly':True,'OriginalExecuted':False,'ManagedExecuted':False});return 0
 env=dict(os.environ)
 for k in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR']:env.pop(k,None)
 phases={};errors=[];builds={};originals={};managed={}
 def phase(name,command,cwd):
  try:
   d=out/(name+'-process');d.mkdir();local=dict(env);local['ERL_CRASH_DUMP']=str(d/'erl_crash.dump');save(d/'launch.json',{'Command':command,'WorkingDirectory':str(cwd)})
   phases[name]=owned.owned(command,d,name,180,local,str(cwd));return True
  except Exception as e:phases[name]={'Failed':True,'Error':repr(e)};errors.append(name+': '+repr(e));return False
 try:
  for variant in plan['Variants']:
   if variant not in selected_variants:
    continue
   d=out/('build-'+variant)
   ok=phase('build-'+variant,[sys.executable,'-B',str(TOOL/'build-native.py'),'--plan',str(planpath),'--approved-plan-sha256',x.plan_sha256,'--provider-admission',str(admission),'--variant',variant,'--output',str(d)],ROOT)
   if ok:
    try:
     b=json.loads((d/'build-receipt.json').read_bytes());assert b['Status']=='RESOURCE529_WINDOWS_BUILD_COMPLETE' and b['Variant']==variant and b['LibrarySha256']==sha(d/'resource529.dll')
    except Exception as e:ok=False;errors.append(variant+' receipt: '+repr(e))
   builds[variant]=ok
  # Acquire every selected independent original before launching any managed child.
  for name,job in selected.items():
   if not all(builds[v] for v in job['Variants']):
    originals[name]={'Succeeded':False,'SkippedDependentBuildFailure':True}
    continue
   try:
    d=out/('original-'+name);d.mkdir();libs=[out/('build-'+v)/'resource529.dll' for v in job['Variants']]
    for module in job['Modules']:shutil.copyfile(TOOL/'fixtures'/(module+'.erl'),d/(module+'.erl'))
    command=[str(installed/'bin/erl.exe'),'+S','1:1','+A','1','+SDcpu','1','+SDio','2','-noshell','-noinput','-boot','no_dot_erlang','-pa',str(d),'-eval',expr(name,job,libs,d,installed,job['Modules'])]
    ok=phase('original-'+name,command,d)
    required=['observed.etf','observed-deterministic.etf','compile.etf','metadata.etf','executing-owners.etf','cleanup.etf']+[m+'.beam' for m in job['Modules']]
    missing=[s for s in required if not (d/s).is_file()]
    if missing:ok=False;errors.append(name+' missing '+repr(missing))
    inventory=[{'Path':str(f.resolve()),'Sha256':sha(f),'Bytes':f.stat().st_size} for f in sorted(d.iterdir()) if f.is_file()]
    save(d/'artifacts.json',inventory);originals[name]={'Succeeded':ok,'InventorySha256':sha(d/'artifacts.json')}
   except Exception as e:
    errors.append(name+' independent original: '+repr(e));originals[name]={'Succeeded':False,'Error':repr(e)}
  if not x.original_only:
   for name,job in selected.items():
    if not originals.get(name,{}).get('Succeeded'):
     for route in ['ordinary','reviewed']:
      for profile in ['ordinary','private-owned']:
       managed[name+'-'+route+'-'+profile]={'Succeeded':False,'SkippedDependentOriginalFailure':True}
     continue
    try:
     d=out/('original-'+name);libs=[out/('build-'+v)/'resource529.dll' for v in job['Variants']]
     images=[{'Module':m,'Path':str((d/(m+'.beam')).resolve()),'Sha256':sha(d/(m+'.beam'))} for m in job['Modules']]+dep
     ad={'Status':'RESOURCE529_SOURCE_BOUND','ProtocolSchemaVersion':2,'Pin':PIN,'Job':name,'Images':images,'Dependencies':[r['Module'] for r in images], 'Libraries':[{'Path':str(lib.resolve()),'Sha256':sha(lib),'BuildReceiptPath':str((lib.parent/'build-receipt.json').resolve()),'BuildReceiptSha256':sha(lib.parent/'build-receipt.json')} for lib in libs], 'OriginalRawPath':str((d/'observed.etf').resolve()),'OriginalDeterministicPath':str((d/'observed-deterministic.etf').resolve()),'OriginalInventoryPath':str((d/'artifacts.json').resolve()),'OriginalInventorySha256':sha(d/'artifacts.json'),'PlanSha256':sha(planpath),'Inputs':backend_inputs+[{'Path':str((out/('build-'+v)/'build-receipt.json').resolve()),'Sha256':sha(out/('build-'+v)/'build-receipt.json')} for v in job['Variants']]}
    except Exception as e:
     errors.append(name+' managed admission preparation: '+repr(e))
     for route in ['ordinary','reviewed']:
      for profile in ['ordinary','private-owned']:
       managed[name+'-'+route+'-'+profile]={'Succeeded':False,'SkippedDependentPreparationFailure':True,'Error':repr(e)}
     continue
    for route in ['ordinary','reviewed']:
     for profile in ['ordinary','private-owned']:
      n=name+'-'+route+'-'+profile
      try:
       ap=out/(n+'-admission.json');save(ap,ad)
       command=['dotnet','--fx-version',x.dotnet_fx_version,str(backend),'--resource529-child',str(ap),sha(ap),route,profile,str(out/n)]
       ok=phase(n,command,ROOT)
       cp=out/n/'completion.json'
       if not cp.is_file():ok=False;errors.append(n+' missing completion')
       else:
        result=json.loads(cp.read_bytes())
        if not all(result.get(k) is True for k in ['Captured','RawWholeEqual','DeterministicWholeEqual','PhysicalCleanupPassed','AllFiniteChecksPassed']):
         ok=False;errors.append(n+' actual completion rejected')
        if result.get('NormalizationPerformed') is not False:ok=False;errors.append(n+' normalization refused')
       managed[n]={'Succeeded':ok,'CompletionPath':str(cp),'CompletionSha256':sha(cp) if cp.is_file() else None}
      except Exception as e:
       errors.append(n+' independent managed: '+repr(e));managed[n]={'Succeeded':False,'Error':repr(e)}

 finally:
  for dependency530 in plan['Dependencies']:
   try:assert dependency_sha(dependency530)==dependency530['Sha256']
   except Exception as e:errors.append('Source closure changed: '+repr(e))
  for input529 in backend_inputs:
   try:assert sha(input529['Path'])==input529['Sha256']
   except Exception as e:errors.append('Backend closure changed: '+repr(e))
  try:
   after=inv.inventory(installed);save(out/'provider-after.json',after)
   if after!=before:errors.append('Provider closure changed')
  except Exception as e:errors.append('Provider final inventory: '+repr(e))
  errors.extend(validate_batch(originals,managed,selected,x.original_only))
  save(out/'completion.json',{'ProtocolSchemaVersion':2,'OriginalFirst':True,'NormalizationPerformed':False,'SelectedOriginalCount':len(selected),'SelectedManagedCount':0 if x.original_only else 4*len(selected),'SelectedProtocols':list(selected),'SelectedVariants':sorted(selected_variants),'Phases':phases,'Originals':originals,'Managed':managed,'AllSelectedPassed':not errors,'Errors':errors,'OriginalGoldenState':'ACQUIRED_ONLY_WHERE_SUCCEEDED','OriginalExecuted':any(n.startswith('original-') for n in phases),'ManagedExecuted':any(n in phases for n in managed),'WholeContractsClosed':0,'FullCompatibilityClaimed':False})
 return 1 if errors else 0
if __name__=='__main__':sys.exit(main())
