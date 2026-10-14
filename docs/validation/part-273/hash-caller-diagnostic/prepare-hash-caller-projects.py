from pathlib import Path
import json,hashlib,shutil,xml.etree.ElementTree as E
r=Path(__file__).resolve().parents[1];a=r/'artifacts';source=a/'hash-caller-checkpoint-source';d=a/'HashCallerAllocationDiagnostic';terms=d/'instrumented-terms';runtime=d/'instrumented-runtime';terms.mkdir(parents=True,exist_ok=True);runtime.mkdir(exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();bindings=[]
proposal=json.loads((a/'hash-caller-checkpoint-proposed-ledger.json').read_bytes())
assert sha(r/'src/Erlang.Terms/Term.cs')==proposal['OriginalTermSourceSha256']
assert sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll')==proposal['ActualMainTestDllSha256']
assert sha(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Terms.dll')==proposal['ActualMainTermsDllSha256']
for name in ['Term.cs','HashDiagnosticCheckpoints.cs']:shutil.copyfile(source/name,terms/name)
root=E.Element('Project',Sdk='Microsoft.NET.Sdk');properties=E.SubElement(root,'PropertyGroup')
for key,value in {'AssemblyName':'Erlang.Terms','RootNamespace':'Erlang','EnableDefaultCompileItems':'false'}.items():E.SubElement(properties,key).text=value
group=E.SubElement(root,'ItemGroup')
for p in sorted((r/'src/Erlang.Terms').glob('*.cs')):
 if p.name=='Term.cs':continue
 E.SubElement(group,'Compile',Include=str(p),Link=p.name);bindings.append({'Path':str(p),'Sha256':sha(p)})
for name in ['Term.cs','HashDiagnosticCheckpoints.cs']:E.SubElement(group,'Compile',Include=name)
E.indent(root);(terms/'Erlang.Terms.csproj').write_bytes(E.tostring(root,encoding='utf-8')+b'\n')
old=a/'HashCheckpointAllocationDiagnostic';tree=E.parse(old/'instrumented-runtime/Erlang.Runtime.csproj');group=tree.getroot().find('ItemGroup')
for item in list(group):
 if item.tag=='Compile' and item.get('Include')=='HashDiagnosticCheckpoints.cs':group.remove(item)
 if item.tag=='Reference' and item.get('Include')=='Erlang.Terms':group.remove(item)
E.SubElement(group,'ProjectReference',Include='../instrumented-terms/Erlang.Terms.csproj')
E.indent(tree);tree.write(runtime/'Erlang.Runtime.csproj',encoding='utf-8')
for name in ['ModuleRegistry.cs','ErlangHashRegistrations.cs']:shutil.copyfile(source/name,runtime/name)
tree=E.parse(old/'HashCheckpointAllocationDiagnostic.csproj');group=tree.getroot().find('ItemGroup')
for item in list(group):
 if item.tag=='Reference' and item.get('Include')=='Erlang.Terms':group.remove(item)
E.SubElement(group,'ProjectReference',Include='instrumented-terms/Erlang.Terms.csproj')
E.indent(tree);tree.write(d/'HashCallerAllocationDiagnostic.csproj',encoding='utf-8')
for name in ['Program.cs','ErlangHashHostChecks.cs']:shutil.copyfile(source/name,d/name)
for project in [runtime/'Erlang.Runtime.csproj',d/'HashCallerAllocationDiagnostic.csproj']:
 for item in E.parse(project).getroot().iter('Compile'):
  p=Path(item.attrib['Include'])
  if p.is_absolute():bindings.append({'Path':str(p),'Sha256':sha(p)})
 for item in E.parse(project).getroot().iter('Reference'):
  p=Path(item.findtext('HintPath'));bindings.append({'Path':str(p),'Sha256':sha(p)})
for p in [r/'src/Erlang.Terms/Term.cs',r/'src/Erlang.Runtime/ModuleRegistry.cs',r/'src/Erlang.Runtime/ErlangHashRegistrations.cs',r/'tests/Erlang.Tests/ErlangHashHostChecks.cs',r/'tests/Erlang.Tests/Program.cs']:bindings.append({'Path':str(p),'Sha256':sha(p)})
ledger={'Status':'SOURCE_ONLY_PROJECTS_FROZEN_AWAITING_HEAVY_GRANT','Graph':['isolated instrumented Terms','isolated instrumented Runtime using those Terms + actual Serialization','isolated prefix Tests using those Terms/Runtime + actual other DLLs'],'Registered':6526,'ExecutedPrefix':60,'OriginalWarmupCalls':1,'OriginalBudget':4096,'HashMathUnchanged':True,'MainModified':False,'BuildExecuted':False,'NativeExecuted':False,'SourceJitPerturbationPossible':True,'ActualMainSourceAndDependencyBindings':bindings,'CandidateFiles':[{'Path':p.relative_to(d).as_posix(),'Sha256':sha(p)} for p in sorted(d.rglob('*')) if p.is_file()]}
(a/'hash-caller-diagnostic-prebuild-freeze.json').write_bytes((json.dumps(ledger,indent=2)+'\n').encode())
print('Prepared isolated Terms→Runtime→prefix projects; no build/run.')
