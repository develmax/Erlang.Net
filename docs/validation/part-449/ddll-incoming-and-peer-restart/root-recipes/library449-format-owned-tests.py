from pathlib import Path
import hashlib,importlib.util,json,os,shutil
r=Path.cwd();out=r/'artifacts/library449-format-owned-tests1';assert not out.exists();out.mkdir()
for folder in ['src','tools','tests','benchmarks','examples']:(out/folder).mkdir()
names=['ConnectedRemoteEpochHostChecks449.cs','ConnectedRemoteEpochHostNames449.cs'];sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest().upper();before={n:sha(r/'tests/Erlang.Tests'/n) for n in names}
for name in names:shutil.copyfile(r/'tests/Erlang.Tests'/name,out/'tests'/name)
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('owned449format',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
m.owned(['C:/Program Files/dotnet/dotnet.exe','tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll','--write',str(out)],out,'format-only-artifact-copies',120,dict(os.environ),str(r))
for name in names:
 dst=r/'tests/Erlang.Tests'/name;assert sha(dst)==before[name];dst.write_bytes((out/'tests'/name).read_bytes())
(out/'completion.json').write_text(json.dumps({'Passed':True,'OnlyOwnedFiles':names,'Before':before,'After':{n:sha(r/'tests/Erlang.Tests'/n) for n in names},'BroadRepositoryWrite':False},indent=2)+'\n')
