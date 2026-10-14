from pathlib import Path
import hashlib,json,shutil,xml.etree.ElementTree as E
r=Path(__file__).resolve().parents[1];a=r/'artifacts';source=a/'hash-checkpoint-diagnostic-source';d=a/'HashCheckpointAllocationDiagnostic';runtime=d/'instrumented-runtime';runtime.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
ledger=json.loads((a/'hash-checkpoint-diagnostic-proposed-ledger.json').read_bytes())
assert all(sha(r/row['MainPath'])==row['BeforeRawSha256'] and sha(a/row['CandidatePath'])==row['CandidateSha256'] for row in ledger['Changes'])
root=E.Element('Project',Sdk='Microsoft.NET.Sdk');props=E.SubElement(root,'PropertyGroup')
for key,value in {'AssemblyName':'Erlang.Runtime','RootNamespace':'Erlang','EnableDefaultCompileItems':'false','AllowUnsafeBlocks':'true'}.items():E.SubElement(props,key).text=value
group=E.SubElement(root,'ItemGroup');bindings=[]
for p in sorted((r/'src/Erlang.Runtime').glob('*.cs')):
 if p.name in ['ModuleRegistry.cs','ErlangHashRegistrations.cs']:continue
 E.SubElement(group,'Compile',Include=str(p),Link=p.name);bindings.append({'Path':str(p),'Sha256':sha(p)})
for name in ['ModuleRegistry.cs','ErlangHashRegistrations.cs','HashDiagnosticCheckpoints.cs']:
 shutil.copyfile(source/name,runtime/name);E.SubElement(group,'Compile',Include=name)
for name in ['Erlang.Terms','Erlang.Serialization']:
 p=r/'tests/Erlang.Tests/bin/Debug/net10.0'/(name+'.dll');ref=E.SubElement(group,'Reference',Include=name);E.SubElement(ref,'HintPath').text=str(p);bindings.append({'Path':str(p),'Sha256':sha(p)})
E.indent(root);(runtime/'Erlang.Runtime.csproj').write_bytes(E.tostring(root,encoding='utf-8')+b'\n')
tree=E.parse(a/'HashCorpusAllocationDiagnostic/HashCorpusAllocationDiagnostic.csproj');group=tree.getroot().find('ItemGroup')
for item in list(group):
 if item.tag=='Reference' and item.get('Include')=='Erlang.Runtime':group.remove(item)
 if item.tag=='Compile' and item.get('Include')=='HashPrefixDiagnosticMetadata.cs':group.remove(item)
E.SubElement(group,'ProjectReference',Include='instrumented-runtime/Erlang.Runtime.csproj')
E.indent(tree);tree.write(d/'HashCheckpointAllocationDiagnostic.csproj',encoding='utf-8')
for name in ['Program.cs','ErlangHashHostChecks.cs']:shutil.copyfile(source/name,d/name)
for item in tree.getroot().iter('Reference'):
 path=Path(item.findtext('HintPath'));bindings.append({'Path':str(path),'Sha256':sha(path)})
for item in tree.getroot().iter('Compile'):
 path=Path(item.attrib['Include'])
 if path.is_absolute():bindings.append({'Path':str(path),'Sha256':sha(path)})
out={'Status':'APPROVED_SOURCE_PREPARED_BEFORE_TARGET','MainSourceUnchanged':True,'RuntimeMathUnchanged':True,'ActualMainSourceAndDependencyBindings':bindings,'InstrumentedCandidateFiles':[{'Path':p.relative_to(d).as_posix(),'Sha256':sha(p)} for p in sorted(d.rglob('*')) if p.is_file()],'NewCollectorAndJitShapeMayPerturbAllocationContext':True,'NoFullValidationExecuted':True}
(a/'hash-checkpoint-diagnostic-prebuild-freeze.json').write_bytes((json.dumps(out,indent=2)+'\n').encode())
print('Prepared isolated runtime and prefix runner; source/DLL guard exact.')
