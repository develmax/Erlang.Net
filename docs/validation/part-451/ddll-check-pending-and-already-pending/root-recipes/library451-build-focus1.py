from pathlib import Path
import hashlib,importlib.util,json,os
r=Path.cwd();out=r/'artifacts/library451-build-focus1';assert not out.exists();out.mkdir()
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('owned449build',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1');exe='C:/Program Files/dotnet/dotnet.exe'
m.owned([exe,'build','tests/Erlang.Tests/Erlang.Tests.csproj','--no-restore','-m:1','-nr:false','--nologo','--disable-build-servers','-p:UseSharedCompilation=false','-p:ConcurrentBuild=false','-p:BuildInParallel=false'],out,'build',180,env,str(r))
m.owned([exe,'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll','--case','runtime/distribution451/concurrent-candidates-adopt-one-pending-generation','--report',str(out/'focus.json')],out,'focus',60,env,str(r))
(out/'completion.json').write_text(json.dumps({'Passed':True,'RuntimeSha256':hashlib.sha256((r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()).hexdigest().upper()},indent=2)+'\n')
