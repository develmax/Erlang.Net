"""Root-only actual native/OTP capture; no managed integration or golden generation."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil,time

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def load(name,path):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def save(path,value):path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
p=argparse.ArgumentParser()
for n in ['repository','output','compiler','toolchain-environment']:p.add_argument('--'+n,type=Path,required=True)
for n in ['compiler-sha256','toolchain-environment-sha256']:p.add_argument('--'+n,required=True)
a=p.parse_args();r=a.repository.resolve();b=Path(__file__).resolve().parent;out=a.output.resolve()
assert os.name=='nt' and not out.exists() and out.is_relative_to(r/'artifacts') and not out.is_relative_to(b)
freeze=json.loads((b/'freeze.json').read_bytes());assert sha(b/'manifest.json')==freeze['ManifestSha256']
for row in json.loads((b/'manifest.json').read_bytes())['Files']:assert sha(b/row['Path'])==row['Sha256'],row['Path']
profile=json.loads((b/'profile.json').read_bytes())
for row in profile['Bindings']:assert sha(r/row['Path'])==row['Sha256'],row['Path']
for row in profile['Sources']:assert sha(b/row['Path'])==row['Sha256'],row['Path']
assert sha(a.compiler)==a.compiler_sha256.upper() and sha(a.toolchain_environment)==a.toolchain_environment_sha256.upper()
owned=load('boundary443_owned',r/profile['Bindings'][0]['Path']);inventory=load('boundary443_inventory',r/profile['BoundedInventory']['Path']);etf=load('boundary443_etf',r/profile['Bindings'][2]['Path'])
assert sha(r/profile['BoundedInventory']['Path'])==profile['BoundedInventory']['Sha256']
admission=r/profile['Bindings'][3]['Path'];ad=json.loads(admission.read_bytes());assert ad['SourceCommit']==profile['Pin'] and ad['ReleaseTag']=='OTP-29.1.1'
installed=Path(ad['InstalledOtpRoot']);execution=Path(ad['ExecutionManifestPath']);assert sha(execution)==ad['ExecutionManifestSha256']
expected={row['Path'].replace('\\','/'):row['Sha256'] for row in json.loads(execution.read_bytes())};assert len(expected)==6427
headers=list(installed.glob('erts-*/include/erl_driver.h'));assert len(headers)==1
stock=profile['ReuseStock'];original=r/stock['Receipt'];assert sha(original)==stock['ReceiptSha256'];receipt=json.loads(original.read_bytes());assert receipt['Passed'] and receipt['WholeRows']==228 and receipt['Pin']==profile['Pin']
stock_beam=r/stock['Path'];assert sha(stock_beam)==stock['Sha256'];matches=[row for row in receipt['Files'] if Path(row['Path']).resolve()==stock_beam.resolve()];assert len(matches)==1 and matches[0]['Sha256']==sha(stock_beam)
out.mkdir();source=out/'source';source.mkdir();drivers=out/'drivers';drivers.mkdir();ebin=out/'observer-ebin';ebin.mkdir();(out/'stock').mkdir()
for f in ['native/ddll_boundary443.c','original/beam_ddll_boundary443.erl']:shutil.copyfile(b/f,source/Path(f).name)
shutil.copyfile(stock_beam,out/'stock/erl_ddll.beam')
inputs={str(f.resolve()):sha(f) for f in [a.compiler,a.toolchain_environment,admission,execution,headers[0],original,stock_beam,b/'manifest.json']}
for row in json.loads((b/'manifest.json').read_bytes())['Files']:inputs[str((b/row['Path']).resolve())]=row['Sha256']
for f in source.iterdir():inputs[str(f.resolve())]=sha(f)
save(out/'inputs-before.json',inputs)
env=dict(os.environ)
for key in list(env):
 if key.startswith('ERL_') or key.startswith('ERLC_') or key.startswith('DDLL_VISIBILITY_'):env.pop(key,None)
env['ERL_FLAGS']='+S 1:1 +A 1';env['ERLC_USE_SERVER']='false'
native_env=dict(env);native_env.update(json.loads(a.toolchain_environment.read_bytes()))
stages={};errors=[];checks=[];provider_checks=[]
def provider_scan(phase):
 started=time.perf_counter();equal=False;failure=None
 try:equal=inventory.inventory(installed)==expected
 except Exception as error:failure=repr(error)
 row={'Phase':phase,'CompleteInventoryAttempted':True,'ExpectedFiles':6427,'Equal':equal,'Failure':failure,'DurationSeconds':time.perf_counter()-started}
 provider_checks.append(row);save(out/'provider-batch-checks.json',provider_checks)
 if not equal:errors.append({'Stage':'provider-'+phase,'Failure':failure or 'Full provider inventory mismatch'})
 return equal
def execute(command,name,stage_env):
 before=all(sha(path)==digest for path,digest in inputs.items());failure=None
 try:
  assert before,'Bound source inputs changed before '+name
  owned.owned(command,out,name,120,stage_env,str(source));stages[name]=True
 except Exception as error:stages[name]=False;failure=repr(error);errors.append({'Stage':name,'Failure':failure})
 finally:
  stable=all(sha(path)==digest for path,digest in inputs.items())
  checks.append({'Stage':name,'InputsBeforeUnchanged':before,'InputsAfterUnchanged':stable,'ProviderScope':'complete-batch-envelope','Failure':failure});save(out/'closure-checks.json',checks)
  if not before or not stable:raise RuntimeError('Actual bound source inputs changed')
captured=0
try:
 assert provider_scan('before'),'Initial complete provider scan failed'
 execute([str(a.compiler),'/nologo','/LD','/MD','/O2','/I'+str(headers[0].parent),'/Fo'+str(drivers)+os.sep,str(source/'ddll_boundary443.c'),'/link','/OUT:'+str(drivers/'ddll_boundary443.dll')],'compile-native',native_env)
 execute([str(installed/'bin/erlc.exe'),'+debug_info','+deterministic','-o',str(ebin),str(source/'beam_ddll_boundary443.erl')],'compile-observer',env)
 if stages.get('compile-native') and stages.get('compile-observer'):
  text=out.as_posix().replace(chr(34),chr(92)+chr(34));path=drivers.as_posix().replace(chr(34),chr(92)+chr(34))
  for function,stage in [('capture_values','capture-values'),('capture_stacks','capture-stacks')]:
   expr='{module,erl_ddll}=code:ensure_loaded(erl_ddll),beam_ddll_boundary443:'+function+'(["'+text+'","'+path+'"]).'
   execute([str(installed/'bin/erl.exe'),'-noshell','-noinput','-boot','no_dot_erlang','-pa',str(ebin),'-eval',expr],stage,env)
 def proper(v):assert isinstance(v,tuple) and len(v)==2 and v[0]=='list',repr(v)[:200];return v[1]
 def no_opaque(v):
  pending=[v]
  while pending:
   x=pending.pop()
   if isinstance(x,tuple):
    assert not x or x[0] not in ('pid','port','reference','new_reference','newer_reference','fun','new_fun','export','export_fun'),repr(x)[:200]
    pending.extend(x)
   elif isinstance(x,list):pending.extend(x)
 def rows(value):
  no_opaque(value);rs=proper(value);assert len(rs)==10
  for name,row in zip(profile['Cases'],rs):
   assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name)
   result=row[1][1];assert result[0]=='tuple'
   if name.startswith('bad_'):
    assert len(result[1])==4 and result[1][:3]==(('atom','exception'),('atom','error'),('atom','badarg'));proper(result[1][3])
   else:assert len(result[1])==2 and result[1][0]==('atom','value')
  return len(rs)
 for stage,file in [('capture-values','values.etf'),('capture-stacks','stacks.etf')]:
  if not stages.get(stage):continue
  try:
   value=etf.decode_exact((out/file).read_bytes())
   if file=='values.etf':captured+=rows(value)
   else:
    profiles=proper(value);assert len(profiles)==5
    for d,row in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],profiles):
     assert row[0]=='tuple' and len(row[1])==3 and row[1][0]==d and row[1][1][0]=='integer';captured+=rows(row[1][2])
  except Exception as error:errors.append({'Stage':'verify-'+file,'Failure':repr(error)})
except Exception as error:
 errors.append({'Stage':'batch','Failure':repr(error)})
finally:
 provider_scan('after')
files=[{'Path':str(f.resolve()),'Bytes':f.stat().st_size,'Sha256':sha(f)} for f in sorted(out.rglob('*')) if f.is_file()]
passed=len(provider_checks)==2 and all(row['Equal'] for row in provider_checks) and not errors and all(stages.get(s) for s in ['compile-native','compile-observer','capture-values','capture-stacks']) and captured==60
save(out/'completion.json',{'Passed':bool(passed),'WholeRows':captured,'ExpectedWholeRows':60,'BoundaryControls':True,'Pin':profile['Pin'],'PinnedOtpCommit':profile['Pin'],'Stages':stages,'Errors':errors,'ClosureChecks':checks,'ProviderBatchChecks':provider_checks,'CompleteProviderScans':len(provider_checks),'ProviderScope':'batch-before-and-after','Files':files,'ExecutionManifestSha256':sha(execution),'RawValuesRewritten':False,'GoldenConstructed':False,'SeparateValuesStacksVms':True,'Original228Rerun':False,'PhysicalAllocationParityClaim':False,'FullDdllCompatibilityClaim':False})
raise SystemExit(0 if passed else 1)
