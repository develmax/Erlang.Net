from pathlib import Path
import hashlib, importlib.util, json, os, shutil

r = Path(__file__).resolve().parent.parent
out = r / 'artifacts/library443-driver-package1'
assert not out.exists()
out.mkdir()
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def save(p,x): p.write_text(json.dumps(x,indent=2)+'\n')
owned_path = r/'artifacts/spawn-priority351-preparation/execute-root.py'
assert sha(owned_path)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
spec=importlib.util.spec_from_file_location('owned443',owned_path)
owned=importlib.util.module_from_spec(spec);spec.loader.exec_module(owned)
dotnet=Path('C:/Program Files/dotnet/dotnet.exe')
assert sha(dotnet)=='AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135'
sources={str(p.resolve()):sha(p) for p in (r/'src').rglob('*.cs') if not {'bin','obj'}.intersection(p.relative_to(r/'src').parts)}
save(out/'sources-before.json',sources)
command=[str(dotnet),'build',str(r/'src/Erlang.DriverHost/Erlang.DriverHost.csproj'),'--no-restore','--output',str(out/'build'),'-m:1','-nr:false','--nologo','--disable-build-servers','-p:UseSharedCompilation=false','-p:ConcurrentBuild=false','-p:BuildInParallel=false']
owned.owned(command,out,'build-real-child',180,dict(os.environ),str(r))
assert all(sha(p)==h for p,h in sources.items())
old=r/'artifacts/library441-driver-package1/package'
manifest=json.loads((old/'erldrv-host-package.json').read_bytes())
native=json.loads((r/'artifacts/library423-driver-native-bridge1/completion.json').read_bytes())
assert native['CompilePassed'] and native['ProviderBeforeEqual'] and native['ProviderAfterEqual']
for row in native['Sources']: assert sha(row['Path'])==row['Sha256']
package=out/'package';package.mkdir()
for row in manifest['Files']:
    name=row['Path']; assert sha(old/name)==row['Sha256']
    src=old/name if name.startswith('erldrv-host-') else out/'build'/name
    assert src.is_file(),src
    shutil.copyfile(src,package/name)
    row['Sha256']=sha(package/name)
save(package/'erldrv-host-package.json',manifest)
parent=json.loads((r/'artifacts/library441-parent-input1/parent-input.json').read_bytes())
# Retain only unchanged non-managed authorities; bind current sources/binaries explicitly.
inputs={}
for p,h in parent['ExpectedInputs'].items():
    path=Path(p)
    if path.is_relative_to(r/'src') or path.is_relative_to(r/'tests/Erlang.Tests/bin'): continue
    assert sha(path)==h,path
    inputs[p]=h
inputs.update(sources)
for row in manifest['Files']: inputs[str((package/row['Path']).resolve())]=row['Sha256']
for p in [owned_path,dotnet,Path(__file__).resolve(),package/'erldrv-host-package.json']:inputs[str(p)]=sha(p)
parent.update(PackageManifest=str(package/'erldrv-host-package.json'),PackageSha256=sha(package/'erldrv-host-package.json'),ErrnoManifest=str(package/'erldrv-host-errno.json'),ErrnoSha256=sha(package/'erldrv-host-errno.json'),ExpectedInputs=inputs,OutputDirectory=str(out/'observations'))
parent['SystemInfo']['ErtsVersion']='Erlang.Net/runtime-sha256-'+sha(package/'Erlang.Runtime.dll')
parent['SystemInfo']['SchedulerThreads']=3
save(out/'parent-input.json',parent)
save(out/'completion.json',dict(Passed=True,PreparedOnly=True,RuntimeSha256=sha(package/'Erlang.Runtime.dll'),PackageSha256=parent['PackageSha256'],CurrentSourcesUnchanged=True,NativeBridgeReusedWithActualCompileReceipt=True,ParentNormalLanes=3))
print(json.dumps(dict(Passed=True,RuntimeSha256=sha(package/'Erlang.Runtime.dll'),Inputs=len(inputs))))
