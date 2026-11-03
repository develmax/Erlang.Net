from pathlib import Path
import os,sys,json,importlib.util
sys.dont_write_bytecode=True
r=Path.cwd();out=r/'artifacts/resource536/restore-diagnostic';out.mkdir(exist_ok=False)
s=importlib.util.spec_from_file_location('owner536',r/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
receipt=m.owned(['dotnet','restore','tests/Erlang.Tests/Erlang.Tests.csproj','--ignore-failed-sources','--disable-parallel','-v:diag'],out,'restore',240,dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9'),str(r))
print(json.dumps(receipt))
