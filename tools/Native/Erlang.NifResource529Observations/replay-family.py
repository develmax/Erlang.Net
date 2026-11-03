"""Reuse admitted original family527 with an explicit final source root and loader."""
import argparse,hashlib,importlib.util,json,os,sys
from pathlib import Path
sys.dont_write_bytecode=True
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def save(path,value):path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def admitted(path,digest):
 p=Path(path).resolve();assert p.is_file() and sha(p)==digest.upper(),str(p);return p
def row(path):
 p=Path(path).resolve();return {'Path':str(p),'Sha256':sha(p),'Bytes':p.stat().st_size}
def inventory(directory):return [row(p) for p in sorted(directory.rglob('*')) if p.is_file()]
def load(name,path):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

def imports(path):
 data=Path(path).read_bytes();assert data[:4]==b'FOR1' and data[8:12]==b'BEAM'
 assert int.from_bytes(data[4:8],'big')+8==len(data)
 chunks={};pos=12
 while pos<len(data):
  key=data[pos:pos+4].decode('ascii');size=int.from_bytes(data[pos+4:pos+8],'big');assert key not in chunks
  chunks[key]=data[pos+8:pos+8+size];assert len(chunks[key])==size;pos+=8+(size+3)//4*4
 atoms_data=chunks['AtU8'];count=int.from_bytes(atoms_data[:4],'big',signed=True);assert 0<abs(count)<100000;pos=4
 def compact(depth=0):
  nonlocal pos
  assert depth<8;x=atoms_data[pos];pos+=1;assert x&7==0
  if not x&8:return x>>4
  if not x&16:
   value=((x>>5)<<8)|atoms_data[pos];pos+=1;return value
  length=(x>>5)+2
  if x>>5==7:length=compact(depth+1)+9
  assert 0<length<=8;value=int.from_bytes(atoms_data[pos:pos+length],'big');pos+=length;return value
 atoms=[None]
 for unused in range(abs(count)):
  if count<0:size=compact()
  else:size=atoms_data[pos];pos+=1
  value=atoms_data[pos:pos+size];assert len(value)==size;atoms.append(value.decode('utf-8'));pos+=size
 assert pos==len(atoms_data)
 table=chunks['ImpT'];n=int.from_bytes(table[:4],'big');assert len(table)==4+12*n
 return [{'Module':atoms[int.from_bytes(table[i:i+4],'big')],'Function':atoms[int.from_bytes(table[i+4:i+8],'big')],'Arity':int.from_bytes(table[i+8:i+12],'big')} for i in range(4,len(table),12)]

def main():
 parser=argparse.ArgumentParser(description=__doc__)
 for key in ['backend','backend-sha256','output']:parser.add_argument('--'+key,required=True)
 parser.add_argument('--original',default=str(ROOT/'artifacts/nif-resource529/original-protocols530'))
 parser.add_argument('--provider-admission',default=str(ROOT/'artifacts/library376-official-compiler/official-admission.json'))
 parser.add_argument('--repository',required=True)
 parser.add_argument('--approved-original-completion-sha256',required=True)
 parser.add_argument('--route',choices=['ordinary','reviewed'],default='ordinary')
 parser.add_argument('--execute-private-family533',action='store_true')
 x=parser.parse_args();assert os.name=='nt'
 repository=Path(x.repository).resolve();assert (repository/'src').is_dir() and (repository/'tools/Native/Erlang.NifNativeBackend/Resource529Driver.cs').is_file()
 backend=admitted(x.backend,x.backend_sha256);assert backend.name=='Erlang.NifNativeBackend.dll'
 original=Path(x.original).resolve();assert original.is_relative_to(ROOT/'artifacts/nif-resource529')
 admitted(original/'completion.json',x.approved_original_completion_sha256)
 completion=json.loads((original/'completion.json').read_bytes());selection=json.loads((original/'selection.json').read_bytes())
 assert completion['AllSelectedPassed'] is True and completion['NormalizationPerformed'] is False
 assert completion['Originals']['family527']['Succeeded'] is True
 phase=completion['Phases']['original-family527'];assert phase['Settled'] and phase['ExitCode']==0 and not phase['TimedOut'] and not phase['Failures']
 captured=original/'original-family527';invpath=captured/'artifacts.json';admitted(invpath,completion['Originals']['family527']['InventorySha256'])
 original_inventory=json.loads(invpath.read_bytes())
 for item in original_inventory:admitted(item['Path'],item['Sha256'])
 required=['observed.etf','observed-deterministic.etf','cleanup.etf','compile.etf','metadata.etf','executing-owners.etf','native_resource_family527.beam','resource_observer529.beam']
 assert all(any(Path(i['Path'])==captured/name for i in original_inventory) for name in required)
 build=original/'build-family1';receipt=build/'build-receipt.json';built=json.loads(receipt.read_bytes());planpath=build/'source-plan.json'
 admitted(planpath,selection['PlanSha256']);assert built['PlanSha256']==selection['PlanSha256']
 assert built['Status']=='RESOURCE529_WINDOWS_BUILD_COMPLETE' and built['Variant']=='family1' and built['PinnedCommit']==PIN
 library=admitted(build/'resource529.dll',built['LibrarySha256'])
 assert sha(build/'fixture.c')==built['CompiledSourceSha256']==built['NativeSourceSha256']
 assert sha(build/'fixture.erl')==built['CopiedErlangSourceSha256']==built['ErlangSourceSha256']
 provider_path=admitted(x.provider_admission,selection['ProviderSha256']);provider=json.loads(provider_path.read_bytes())
 assert provider['SourceCommit']==PIN and provider['ReleaseTag']=='OTP-29.1.1'
 provider_manifest=admitted(provider['ExecutionManifestPath'],provider['ExecutionManifestSha256']);installed=Path(provider['InstalledOtpRoot']).resolve()
 expected={i['Path'].replace('\\','/'):i['Sha256'] for i in json.loads(provider_manifest.read_bytes())};assert len(expected)==6427
 before=json.loads((original/'provider-before.json').read_bytes());after=json.loads((original/'provider-after.json').read_bytes());assert before==after==expected
 # Historical receipts remain readable. Fresh combined observations require
 # independently inventoried fixture and executing provider-stock evidence.
 fixture_modules=['native_resource_family527','resource_observer529']
 fresh_modules=['magic_alias_observer576','resource_binary_observer577']
 present=[(captured/(module+'.beam')).is_file() for module in fresh_modules]
 assert all(present) or not any(present),'Incomplete combined observer images'
 if all(present):
  fixture_modules+=fresh_modules
  required_fresh=['stock-images.json','stock-executing-owners.etf']+[module+'.beam' for module in fresh_modules]
  assert all(any(Path(item['Path'])==captured/name for item in original_inventory) for name in required_fresh)
 images=[{'Module':module,'Path':str((captured/(module+'.beam')).resolve()),'Sha256':sha(captured/(module+'.beam'))} for module in fixture_modules]
 stocks=[]
 if (captured/'stock-images.json').is_file():
  stocks=json.loads((captured/'stock-images.json').read_bytes())
  expected_stock={'timer','erts_debug','gb_trees'} if all(present) else {'timer'}
  assert {item['Module'] for item in stocks}==expected_stock and len(stocks)==len(expected_stock)
  for item in stocks:
   source=(installed/item['ProviderRelativePath']).resolve()
   assert source.is_relative_to(installed) and source==Path(item['ProviderPath']).resolve()
   assert sha(source)==expected[item['ProviderRelativePath']]==item['Sha256']
   copy=admitted(item['Path'],item['Sha256'])
   assert copy.is_relative_to(captured/'stock-images')
   assert any(Path(entry['Path'])==copy and entry['Sha256']==item['Sha256'] for entry in original_inventory)
   images.append({key:item[key] for key in ['Module','Path','Sha256']})
 else:
  assert not all(present),'Fresh combined observer requires stock execution evidence'
  candidates=list((installed/'lib').glob('stdlib-*/ebin/timer.beam'));assert len(candidates)==1
  timer=candidates[0].resolve();admitted(timer,expected[timer.relative_to(installed).as_posix()])
  images.append({'Module':'timer','Path':str(timer),'Sha256':sha(timer)})

 import_rows={i['Module']:imports(i['Path']) for i in images}
 dependencies=sorted({i['Module'] for i in images}|{j['Module'] for imp in import_rows.values() for j in imp})
 out=Path(x.output).resolve();assert (out.is_relative_to(ROOT/'artifacts/batch533/resource-family-entry') or out.is_relative_to(repository/'artifacts/resource529')) and not out.exists();out.mkdir(parents=True)
 binary_closure=inventory(backend.parent);save(out/'backend-inventory.json',binary_closure)
 assert any(Path(i['Path'])==backend and i['Sha256']==x.backend_sha256.upper() for i in binary_closure)
 sources=set()
 for base in [repository/'src',repository/'tools/Native/Erlang.NifNativeBackend',ROOT/'tools/Native/Erlang.NifResource529Observations']:
  sources.update(p for p in base.rglob('*') if p.is_file() and p.suffix in {'.cs','.csproj','.c','.h','.py','.erl','.json','.props','.targets'} and not {'bin','obj','.git','__pycache__'}.intersection(p.parts))
 sources.update(repository/name for name in ['Directory.Build.props','Directory.Build.targets','Directory.Packages.props','global.json'] if (repository/name).is_file())
 sources.add(Path(__file__).resolve());source_closure=[row(p) for p in sorted(sources)];save(out/'source-inventory.json',source_closure)
 save(out/'imports.json',{'ActualImages':import_rows,'DeclaredSelection':dependencies,'NativeDefaultBindings':['erlang native BIFs','binary:referenced_byte_size/1'],'UnloadedBranches':'timer other API dependencies and observer oldhandoff branch remain unavailable unless already default-bound; accidental unavailable calls remain undef'})
 additional=[original/'completion.json',original/'selection.json',original/'provider-before.json',original/'provider-after.json',receipt,planpath,invpath,provider_path,provider_manifest,out/'backend-inventory.json',out/'source-inventory.json',out/'imports.json']
 inputs={i['Path']:i for i in binary_closure+source_closure+[row(p) for p in additional]+[row(Path(i['Path'])) for i in original_inventory]+[row(Path(item['Path'])) for item in images]}
 admission={'Status':'RESOURCE529_SOURCE_BOUND','ProtocolSchemaVersion':2,'Pin':PIN,'Job':'family527','Images':images,'Dependencies':dependencies,'Libraries':[{'Path':str(library),'Sha256':sha(library),'BuildReceiptPath':str(receipt),'BuildReceiptSha256':sha(receipt)}],'OriginalRawPath':str(captured/'observed.etf'),'OriginalDeterministicPath':str(captured/'observed-deterministic.etf'),'OriginalInventoryPath':str(invpath),'OriginalInventorySha256':sha(invpath),'PlanSha256':sha(planpath),'Inputs':list(inputs.values())}
 ap=out/'admission.json';save(ap,admission)
 command=['dotnet','--fx-version','10.0.11',str(backend),'--resource529-child',str(ap),sha(ap),x.route,'private-owned',str(out/'managed')]
 save(out/'launch.json',{'Command':command,'FreshOriginalExecuted':False,'NormalizationPerformed':False,'OnlySelectedJob':'family527/'+x.route+'-loader/private-owned','CurrentSourceRoot':str(repository)})
 if not x.execute_private_family533:
  save(out/'completion.json',{'Prepared':True,'ManagedExecuted':False,'OriginalRerun':False,'AdmissionSha256':sha(ap)});print(json.dumps({'Admission':str(ap),'Sha256':sha(ap),'PreparedOnly':True}));return 0
 helper=ROOT/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts';owned=load('owned533',helper/'owned.py');bounded=load('inventory533',helper/'bounded_inventory.py')
 failures=[];process=None
 try:
  assert bounded.inventory(installed)==expected
  env=dict(os.environ)
  for name in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR']:env.pop(name,None)
  env['ERL_CRASH_DUMP']=str(out/'erl_crash.dump')
  process=owned.owned(command,out,'family-private-managed',180,env,str(repository))
  result=json.loads((out/'managed/completion.json').read_bytes())
  assert all(result.get(k) is True for k in ['Captured','RawWholeEqual','DeterministicWholeEqual','PhysicalCleanupPassed','AllFiniteChecksPassed'])
  assert result.get('NormalizationPerformed') is False
 except Exception as error:failures.append(repr(error))
 finally:
  try:assert inventory(backend.parent)==binary_closure
  except Exception as error:failures.append('Backend closure changed: '+repr(error))
  for item in inputs.values():
   try:admitted(item['Path'],item['Sha256'])
   except Exception as error:failures.append('Bound input changed: '+repr(error))
  try:assert bounded.inventory(installed)==expected
  except Exception as error:failures.append('Provider changed: '+repr(error))
  save(out/'completion.json',{'ManagedExecuted':True,'OriginalRerun':False,'OnlySelectedJob':'family527/'+x.route+'-loader/private-owned','CurrentSourceRoot':str(repository),'Process':process,'Errors':failures,'Passed':not failures,'FullCompatibilityClaimed':False,'WholeContractsClosed':0,'NormalizationPerformed':False,'AdmissionSha256':sha(ap)})
 return 1 if failures else 0
if __name__=='__main__':sys.exit(main())
