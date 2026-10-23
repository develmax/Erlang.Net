"""Root-only build/run of genuine DDLL native host consumer; new package required."""
from pathlib import Path
import argparse,hashlib,json,importlib.util,os,shutil,time,xml.etree.ElementTree as ET

def h(f):return hashlib.sha256(Path(f).read_bytes()).hexdigest().upper()
def save(f,x):f.write_text(json.dumps(x,indent=2)+'\n')
def load(n,p):s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
p=argparse.ArgumentParser()
for n in ['repository','fresh-parent-input','runtime-directory','original','output','dotnet']:p.add_argument('--'+n,required=True,type=Path)
p.add_argument('--dotnet-sha256',required=True);a=p.parse_args();r=a.repository.resolve();packet=Path(__file__).resolve().parent;out=a.output.resolve();runtime=a.runtime_directory.resolve();original=a.original.resolve()
assert not out.exists() and out.is_relative_to(r/'artifacts') and not out.is_relative_to(packet)
assert h(a.dotnet)==a.dotnet_sha256.upper()
for row in json.loads((packet/'manifest.json').read_text())['Files']:assert h(packet/row['Path'])==row['Sha256']
parent=json.loads(a.fresh_parent_input.read_text());inputs=dict(parent['ExpectedInputs'])
for path,digest in json.loads((packet/'source-bindings.json').read_bytes()).items():
 assert h(r/path)==digest,path;inputs[str((r/path).resolve())]=digest
for path,digest in inputs.items():assert h(path)==digest,path
package=Path(parent['PackageManifest']);assert h(package)==parent['PackageSha256'];pm=json.loads(package.read_text());assert pm['Protocol']==1
for row in pm['Files']:
 f=package.parent/row['Path'];assert f.resolve().is_relative_to(package.parent) and h(f)==row['Sha256'];inputs[str(f.resolve())]=h(f)
for name in ['Erlang.Runtime.dll','Erlang.Terms.dll','Erlang.Serialization.dll']:assert h(runtime/name)==h(package.parent/name)
assert parent['SystemInfo']['ErtsVersion']=='Erlang.Net/runtime-sha256-'+h(runtime/'Erlang.Runtime.dll')
receipt=json.loads((original/'completion.json').read_text());assert receipt['Passed'] and receipt['WholeRows']==60 and receipt['FinalModes445']
for row in receipt['Files']:
 f=Path(row['Path']);assert f.resolve().is_relative_to(original) and f.stat().st_size==row['Bytes'] and h(f)==row['Sha256'];inputs[str(f.resolve())]=h(f)
helpers=json.loads((packet/'harness-helper-bindings.json').read_text())
for row in helpers:assert h(r/row['Path'])==row['Sha256'];inputs[str((r/row['Path']).resolve())]=row['Sha256']
owned_path=r/'artifacts/spawn-priority351-preparation/execute-root.py';assert h(owned_path)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A';owned=load('ddll443_owned',owned_path)
inventory_path=r/'artifacts/library426-trace-low-memory-original-proposal1/bounded_inventory.py';assert h(inventory_path)=='502862EB7045D780115D046A02FA56DB11FB67EBC7A1883CA58D06BEC6C7910A';inventory=load('ddll443_inventory',inventory_path)
admission=r/'artifacts/library376-official-compiler/official-admission.json';ad=json.loads(admission.read_text());installed=Path(ad['InstalledOtpRoot']);manifest=Path(ad['ExecutionManifestPath']);assert h(manifest)==ad['ExecutionManifestSha256'];expected={row['Path']:row['Sha256'] for row in json.loads(manifest.read_text())}
assert len(expected)==6427
for f in [a.dotnet,a.fresh_parent_input,package,original/'completion.json',owned_path,inventory_path,admission,manifest]:inputs[str(f.resolve())]=h(f)
for row in json.loads((packet/'manifest.json').read_text())['Files']:inputs[str((packet/row['Path']).resolve())]=row['Sha256']
out.mkdir();src=out/'harness';src.mkdir();files=[]
for f in [*(r/row['Path'] for row in helpers),*sorted((packet/'harness').glob('*.cs'))]:
 dst=src/f.name;assert not dst.exists();shutil.copyfile(f,dst);files.append(dst);inputs[str(dst.resolve())]=h(dst)
root=ET.Element('Project',Sdk='Microsoft.NET.Sdk');props=ET.SubElement(root,'PropertyGroup')
for k,v in {'TargetFramework':'net10.0','LangVersion':'14.0','OutputType':'Exe','AssemblyName':'Erlang.Compiler','Nullable':'enable','ImplicitUsings':'enable','TreatWarningsAsErrors':'true','EnableDefaultCompileItems':'false','UseSharedCompilation':'false'}.items():ET.SubElement(props,k).text=v
items=ET.SubElement(root,'ItemGroup')
for f in files:ET.SubElement(items,'Compile',Include=str(f))
for n in ['Erlang.Runtime','Erlang.Terms','Erlang.Serialization']:
 row=ET.SubElement(items,'Reference',Include=n);ET.SubElement(row,'HintPath').text=str(runtime/(n+'.dll'))
ET.indent(root);project=src/'DdllFinalModes445.csproj';ET.ElementTree(root).write(project,encoding='utf-8',xml_declaration=True);inputs[str(project.resolve())]=h(project)
fresh={k:v for k,v in parent.items() if k!='ExpectedInputs'};fresh.update({'DriverDirectory':str(original/'drivers'),'OriginalDirectory':str(original),'OutputDirectory':str(out/'observations')});fresh['ExpectedInputs']=dict(inputs)
config=out/'fresh-input.json';save(config,fresh);inputs[str(config.resolve())]=h(config);save(out/'inputs-before.json',inputs)
stages={};errors=[];checks=[];provider_checks=[]
def provider_scan(phase):
 started=time.perf_counter();equal=False;failure=None
 try:equal=inventory.inventory(installed)==expected
 except Exception as error:failure=repr(error)
 provider_checks.append({'Phase':phase,'CompleteInventoryAttempted':True,'ExpectedFiles':6427,'Equal':equal,'Failure':failure,'DurationSeconds':time.perf_counter()-started})
 save(out/'provider-batch-checks.json',provider_checks)
 if not equal:errors.append({'Stage':'provider-'+phase,'Failure':failure or 'Full provider inventory mismatch'})
 return equal
def execute(command,name,seconds):
 before=all(h(path)==digest for path,digest in inputs.items());failure=None
 try:
  assert before,'Bound source inputs changed before '+name
  owned.owned(command,out,name,seconds,dict(os.environ),str(src));stages[name]=True
 except Exception as error:stages[name]=False;failure=repr(error);errors.append({'Stage':name,'Failure':failure})
 finally:
  stable=all(h(path)==digest for path,digest in inputs.items())
  checks.append({'Stage':name,'InputsBeforeUnchanged':before,'InputsAfterUnchanged':stable,'ProviderScope':'complete-batch-envelope','Failure':failure});save(out/'closure-checks.json',checks)
  if not before or not stable:raise RuntimeError('Actual bound source inputs changed')
observed=out/'observations/completion.json';body=None
try:
 assert provider_scan('before'),'Initial complete provider scan failed'
 execute([str(a.dotnet),'build',str(project),'-c','Release','--nologo','-m:1','-nr:false','-p:UseSharedCompilation=false'],'compile-consumer',180)
 if stages.get('compile-consumer'):
  build=src/'bin/Release/net10.0';assert h(build/'Erlang.Runtime.dll')==h(runtime/'Erlang.Runtime.dll');execute([str(a.dotnet),str(build/'Erlang.Compiler.dll'),str(config)],'actual-full-native-ddll',300)
 observed=out/'observations/completion.json';body=json.loads(observed.read_text()) if observed.exists() else None
except Exception as error:
 errors.append({'Stage':'batch','Failure':repr(error)})
finally:
 provider_scan('after')
passed=len(provider_checks)==2 and all(row['Equal'] for row in provider_checks) and not errors and stages.get('actual-full-native-ddll') and body is not None and body['Passed']
save(out/'completion.json',{'Passed':bool(passed),'Stages':stages,'Errors':errors,'ActualReport':str(observed),'SourceChecks':checks,'ProviderChecks':provider_checks,'CompleteProviderScans':len(provider_checks),'ProviderScope':'batch-before-and-after','OriginalVmRerun':False,'RuntimeSha256':h(runtime/'Erlang.Runtime.dll'),'PackageSha256':parent['PackageSha256']})
raise SystemExit(0 if passed else 1)
