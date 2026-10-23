"""Root-only real native/OTP preparations and independently settled isolated profiles."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,os,shutil,time

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def save(path,value):path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def load(name,path):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
p=argparse.ArgumentParser()
for n in ['repository','output','compiler','toolchain-environment']:p.add_argument('--'+n,type=Path,required=True)
for n in ['compiler-sha256','toolchain-environment-sha256']:p.add_argument('--'+n,required=True)
a=p.parse_args();r=a.repository.resolve();b=Path(__file__).resolve().parent;out=a.output.resolve()
assert os.name=='nt' and not out.exists() and out.is_relative_to(r/'artifacts') and not out.is_relative_to(b)
assert sha(b/'capture-manifest.json')==json.loads((b/'capture-freeze.json').read_bytes())['ManifestSha256']
for row in json.loads((b/'capture-manifest.json').read_bytes())['Files']:assert sha(b/row['Path'])==row['Sha256']
profile=json.loads((b/'profile.json').read_bytes())
for row in profile['Bindings']:assert sha(r/row['Path'])==row['Sha256']
for row in profile['Sources']:assert sha(b/row['Path'])==row['Sha256']
assert sha(a.compiler)==a.compiler_sha256.upper() and sha(a.toolchain_environment)==a.toolchain_environment_sha256.upper()
owned=load('final445_owned',r/profile['Bindings'][0]['Path']);etf=load('final445_etf',r/profile['Bindings'][2]['Path'])
assert sha(r/profile['BoundedInventory']['Path'])==profile['BoundedInventory']['Sha256'];inventory=load('final445_inventory',r/profile['BoundedInventory']['Path'])
admission=r/profile['Bindings'][3]['Path'];ad=json.loads(admission.read_bytes());assert ad['SourceCommit']==profile['Pin'] and ad['ReleaseTag']=='OTP-29.1.1'
installed=Path(ad['InstalledOtpRoot']);execution=Path(ad['ExecutionManifestPath']);assert sha(execution)==ad['ExecutionManifestSha256'];expected={row['Path'].replace('\\','/'):row['Sha256'] for row in json.loads(execution.read_bytes())};assert len(expected)==6427
old=r/profile['ReuseStock']['Receipt'];assert sha(old)==profile['ReuseStock']['ReceiptSha256'];receipt=json.loads(old.read_bytes());assert receipt['Passed'] and receipt['WholeRows']==228 and receipt['Pin']==profile['Pin']
stock=r/profile['ReuseStock']['Path'];assert sha(stock)==profile['ReuseStock']['Sha256']
for info in [profile['ReuseStock'],*profile['Fixtures'].values()]:
 f=r/info['Path'];assert sha(f)==info['Sha256'];rows=[row for row in receipt['Files'] if Path(row['Path']).resolve()==f.resolve()];assert len(rows)==1 and rows[0]['Sha256']==sha(f)
headers=list(installed.glob('erts-*/include/erl_driver.h'));assert len(headers)==1
out.mkdir();source=out/'source';source.mkdir();drivers=out/'drivers';drivers.mkdir();ebin=out/'observer-ebin';ebin.mkdir();(out/'stock').mkdir()
for n in ['native/permanent_drv445.c','original/beam_ddll_final_modes445.erl']:shutil.copyfile(b/n,source/Path(n).name)
shutil.copyfile(stock,out/'stock/erl_ddll.beam')
shutil.copyfile(r/profile['Fixtures']['echo_drv']['Path'],drivers/'echo_drv.dll')
for kind,fixture in [('wrong','wrongname_drv'),('initfail','initfail_drv')]:
 (drivers/kind).mkdir();shutil.copyfile(r/profile['Fixtures'][fixture]['Path'],drivers/kind/'echo_drv.dll')
(drivers/'missing').mkdir()
inputs={str(f.resolve()):sha(f) for f in [a.compiler,a.toolchain_environment,admission,execution,headers[0],old,stock,b/'capture-manifest.json',b/'capture-freeze.json']}
for row in json.loads((b/'capture-manifest.json').read_bytes())['Files']:inputs[str((b/row['Path']).resolve())]=row['Sha256']
for info in profile['Fixtures'].values():inputs[str((r/info['Path']).resolve())]=info['Sha256']
for f in [*source.iterdir(),*drivers.rglob('*.dll'),out/'stock/erl_ddll.beam']:inputs[str(f.resolve())]=sha(f)
save(out/'inputs-before.json',inputs)
env=dict(os.environ)
for key in list(env):
 if key.startswith('ERL_') or key.startswith('ERLC_') or key.startswith('DDLL_VISIBILITY_'):env.pop(key,None)
env['ERL_FLAGS']='+S 1:1 +A 1';env['ERLC_USE_SERVER']='false';native_env=dict(env);native_env.update(json.loads(a.toolchain_environment.read_bytes()))
stages={};errors=[];checks=[];provider=[]
def scan(phase):
 start=time.perf_counter();equal=False;failure=None
 try:equal=inventory.inventory(installed)==expected
 except Exception as error:failure=repr(error)
 provider.append({'Phase':phase,'ExpectedFiles':6427,'CompleteScan':True,'Equal':equal,'Failure':failure,'DurationSeconds':time.perf_counter()-start});save(out/'provider-batch-checks.json',provider)
 if not equal:errors.append({'Stage':'provider-'+phase,'Failure':failure or 'Full provider mismatch'})
 return equal
def execute(command,name,stage_env):
 before=all(sha(path)==digest for path,digest in inputs.items());failure=None
 try:
  assert before,'Bound inputs changed before '+name
  owned.owned(command,out,name,120,stage_env,str(source));stages[name]=True
 except Exception as error:stages[name]=False;failure=repr(error);errors.append({'Stage':name,'Failure':failure})
 finally:
  after=all(sha(path)==digest for path,digest in inputs.items());checks.append({'Stage':name,'InputsBefore':before,'InputsAfter':after,'Failure':failure});save(out/'source-checks.json',checks)
  if not before or not after:raise RuntimeError('Bound inputs changed')
def proper(value):assert isinstance(value,tuple) and len(value)==2 and value[0]=='list';return value[1]
def no_opaque(value):
 pending=[value]
 while pending:
  item=pending.pop()
  if isinstance(item,tuple):
   assert not item or item[0] not in ('pid','port','reference','export_fun','new_fun'),repr(item)[:200]
   pending.extend(item)
  elif isinstance(item,list):pending.extend(item)
def verify(path,group,own):
 value=etf.decode_exact(path.read_bytes());assert value[0]=='tuple' and len(value[1])==3
 key=('integer',group['Term']) if isinstance(group['Term'],int) else ('atom',group['Term'])
 assert value[1][0]==key and value[1][1][0]=='integer';rows=proper(value[1][2]);assert len(rows)==10
 for name,row in zip(profile['Cases'],rows):
  assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name)
  if own:
   events=proper(row[1][1]);assert events and events[0][0]=='tuple' and events[0][1][0]==('atom','observer')
  else:
   result=row[1][1];assert result[0]=='tuple' and len(result[1])==2 and result[1][0]==('atom','value'),(name,repr(result)[:600])
 if not own:no_opaque(value)
 return value[1][:2],len(rows)
captured=0
try:
 assert scan('before')
 execute([str(a.compiler),'/nologo','/LD','/MD','/O2','/I'+str(headers[0].parent),'/Fo'+str(drivers)+os.sep,str(source/'permanent_drv445.c'),'/link','/OUT:'+str(drivers/'permanent_drv445.dll')],'compile-permanent-native',native_env)
 execute([str(installed/'bin/erlc.exe'),'+debug_info','+deterministic','-o',str(ebin),str(source/'beam_ddll_final_modes445.erl')],'compile-observer',env)
 if stages.get('compile-permanent-native') and stages.get('compile-observer'):
  for f in [drivers/'permanent_drv445.dll',ebin/'beam_ddll_final_modes445.beam']:inputs[str(f.resolve())]=sha(f)
  save(out/'inputs-after-preparation.json',inputs)
  for group in profile['Groups']:
   group_out=out/group['Name'];group_out.mkdir()
   escaped=lambda path:path.as_posix().replace(chr(34),chr(92)+chr(34))
   term=str(group['Term']);expr='{module,erl_ddll}=code:ensure_loaded(erl_ddll),beam_ddll_final_modes445:write_profile(["'+escaped(group_out)+'","'+escaped(drivers)+'",'+term+']).'
   execute([str(installed/'bin/erl.exe'),'-noshell','-noinput','-boot','no_dot_erlang','-pa',str(ebin),'-eval',expr],'capture-'+group['Name'],env)
  for group in profile['Groups']:
   if not stages.get('capture-'+group['Name']):continue
   try:
    header,n=verify(out/group['Name']/'main.etf',group,False);other,m=verify(out/group['Name']/'own.etf',group,True);assert header==other and n==m;captured+=n
   except Exception as error:errors.append({'Stage':'verify-'+group['Name'],'Failure':repr(error)})
except Exception as error:errors.append({'Stage':'batch','Failure':repr(error)})
finally:scan('after')
files=[{'Path':str(f.resolve()),'Bytes':f.stat().st_size,'Sha256':sha(f)} for f in sorted(out.rglob('*')) if f.is_file()]
passed=not errors and len(provider)==2 and all(row['Equal'] for row in provider) and captured==60 and all(stages.get('capture-'+g['Name']) for g in profile['Groups'])
save(out/'completion.json',{'Passed':bool(passed),'WholeRows':captured,'ExpectedWholeRows':60,'FinalModes445':True,'GroupCount':6,'FreshPhysicalVmPerGroup':True,'FullOwnRawPreserved':True,'NativeFixturesCompiled':1,'StockNativeFixturesReused':3,'Stages':stages,'Errors':errors,'ProviderBatchChecks':provider,'SourceChecks':checks,'Pin':profile['Pin'],'Files':files,'Original228Rerun':False,'GoldenConstructed':False,'CompatibilityClaim':False})
raise SystemExit(0 if passed else 1)
