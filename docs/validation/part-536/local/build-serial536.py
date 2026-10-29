from pathlib import Path
import os, sys, json, importlib.util
sys.dont_write_bytecode=True
r=Path.cwd();out=r/'artifacts/resource536/build-serial536'
assert not out.exists()
out.mkdir()
spec=importlib.util.spec_from_file_location('owner536',r/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
environment=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9')
restore=m.owned(['dotnet','restore','tests/Erlang.Tests/Erlang.Tests.csproj','--ignore-failed-sources','--disable-parallel','-m:1','-p:BuildInParallel=false','-p:UseSharedCompilation=false','-v:q'],out,'restore',240,environment,str(r))
assert restore['Settled'] and restore['ExitCode']==0
build=m.owned(['dotnet','build','tests/Erlang.Tests/Erlang.Tests.csproj','-c','Release','--no-restore','-m:1','-p:UseSharedCompilation=false','-p:BuildInParallel=false','-v:q'],out,'build',300,environment,str(r))
assert build['Settled'] and build['ExitCode']==0
print(json.dumps(dict(Restore=restore,Build=build)))
