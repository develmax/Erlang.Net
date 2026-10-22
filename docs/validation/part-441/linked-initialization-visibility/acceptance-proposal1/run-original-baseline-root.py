"""Root-only fresh original36 replay against new native bytes; exact old worker BEAM."""
from pathlib import Path
import argparse,hashlib,json,importlib.util,os,shutil

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def load(name,path):
 spec=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
def save(path,value):path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
p=argparse.ArgumentParser()
for name in ['repository','native','baseline-original','admission','output']:p.add_argument('--'+name,type=Path,required=True)
a=p.parse_args();root=a.repository.resolve();packet=Path(__file__).resolve().parent;out=a.output.resolve();native=a.native.resolve();baseline=a.baseline_original.resolve()
assert not out.exists() and out.is_relative_to(root/'artifacts') and not out.is_relative_to(packet)
for row in json.loads((packet/'manifest.json').read_bytes())['Files']:assert sha(packet/row['Path'])==row['Sha256']
for path,digest in json.loads((packet/'baseline-bindings.json').read_bytes()).items():assert sha(root/path)==digest,path
old=root/'artifacts/library435-ddll-init-prefix-proposal4';profile=json.loads((old/'profile.json').read_bytes())
owned=load('vis438_original_owned',root/profile['Bindings'][0]['Path']);inventory=load('vis438_original_inventory',root/profile['BoundedInventory']['Path']);etf=load('vis438_original_etf',root/profile['Bindings'][2]['Path'])
ad=json.loads(a.admission.read_bytes());assert ad['SourceCommit']==profile['Pin'] and ad['ReleaseTag']=='OTP-29.1.1'
installed=Path(ad['InstalledOtpRoot']);manifest=Path(ad['ExecutionManifestPath']);assert sha(manifest)==ad['ExecutionManifestSha256']
expected={row['Path'].replace('\\','/'):row['Sha256'] for row in json.loads(manifest.read_bytes())};assert len(expected)==6427 and inventory.inventory(installed)==expected
before_receipt=json.loads((baseline/'completion.json').read_bytes());assert before_receipt['Passed'] and before_receipt['WholeRows']==36
for row in before_receipt['Files']:assert sha(row['Path'])==row['Sha256'],row['Path']
new_native=json.loads((native/'completion.json').read_bytes());assert new_native['Passed'] and new_native['NativeOnly'] and new_native['NativeFixtures']==4
for row in new_native['Files']:assert sha(row['Path'])==row['Sha256'],row['Path']
out.mkdir();(out/'drivers').mkdir();(out/'stock').mkdir();(out/'observer-ebin').mkdir()
for module in ['beam_ddll_init_prefix435.beam']:
 shutil.copyfile(baseline/'observer-ebin'/module,out/'observer-ebin'/module)
shutil.copyfile(baseline/'stock/erl_ddll.beam',out/'stock/erl_ddll.beam')
for fixture in profile['NativeFixtures']:shutil.copyfile(native/'drivers'/(fixture['Name']+'.dll'),out/'drivers'/(fixture['Name']+'.dll'))
env=dict(os.environ)
for key in list(env):
 if key.startswith('ERL_') or key.startswith('ERLC_') or key.startswith('DDLL_VISIBILITY_'):env.pop(key,None)
env['ERL_FLAGS']='+S 1:1 +A 1'
text=out.as_posix().replace(chr(34),chr(92)+chr(34));drivers=(out/'drivers').as_posix().replace(chr(34),chr(92)+chr(34))
expression='{module,erl_ddll}=code:ensure_loaded(erl_ddll), beam_ddll_init_prefix435:capture(["'+text+'","'+drivers+'"]).'
errors=[];captured=0;settled=None;before=inventory.inventory(installed)==expected
try:
 settled=owned.owned([str(installed/'bin/erl.exe'),'-noshell','-noinput','-boot','no_dot_erlang','-pa',str(out/'observer-ebin'),'-eval',expression],out,'original-new-native36',120,env,str(out))
 def proper(x):assert x[0]=='list' and len(x)==2,x;return x[1]
 def atom(s):return ('atom',s)
 def tup(*v):return ('tuple',tuple(v))
 fail=tup(atom('error'),atom('driver_init_failed'));loaded=tup(atom('ok'),atom('loaded'));yes=atom('true')
 wants={'plain_init_failure':tup(fail,yes,yes),'repeated_plain_init_failure':tup(fail,fail,yes,yes),'nested_failure':tup(loaded,yes,yes),'healthy_nested':tup(loaded,yes,yes)}
 def rows(value):
  values=proper(value);assert len(values)==len(profile['Cases']) and [row[1][0][1] for row in values]==profile['Cases']
  for name,row in zip(profile['Cases'],values):
   result=row[1][1]
   if name in wants:assert result==tup(atom('value'),wants[name]),(name,repr(result))
   else:assert result[0]=='tuple' and len(result[1])==4 and result[1][:3]==(atom('exception'),atom('error'),atom('badarg'));proper(result[1][3])
  return len(values)
 captured=rows(etf.decode_exact((out/'values.etf').read_bytes()));profiles=proper(etf.decode_exact((out/'stacks.etf').read_bytes()));assert len(profiles)==5
 depths=[atom('default'),('integer',0),('integer',1),('integer',2),('integer',8)]
 for i,row in enumerate(profiles):assert row[0]=='tuple' and len(row[1])==3 and row[1][0]==depths[i] and row[1][1][0]=='integer';captured+=rows(row[1][2])
 assert captured==profile['WholeRows']
except Exception as error:errors.append(repr(error))
after=inventory.inventory(installed)==expected
files=[{'Path':str(file.resolve()),'Bytes':file.stat().st_size,'Sha256':sha(file)} for file in sorted(out.rglob('*')) if file.is_file()]
passed=not errors and before and after and settled is not None and captured==36
save(out/'completion.json',{'Passed':passed,'WholeRows':captured,'NestedInitFailurePrefix':True,'PinnedOtpCommit':profile['Pin'],'Files':files,'Errors':errors,'ProviderBefore':before,'ProviderAfter':after,'ReusedExactWorkerBeam':True,'OriginalCompilerRun':False,'FreshNativeBytes':True,'HeldConcurrencyOracleClaim':False,'PriorReceiptRewritten':False})
raise SystemExit(0 if passed else 1)
