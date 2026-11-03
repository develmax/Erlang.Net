from pathlib import Path
import os, sys, json, importlib.util
sys.dont_write_bytecode = True
r = Path.cwd()
out = r / 'artifacts/resource536' / sys.argv[1]
assert not out.exists()
out.mkdir()
spec = importlib.util.spec_from_file_location('owner535', r / 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
cmd = ['dotnet', 'build', 'tests/Erlang.Tests/Erlang.Tests.csproj', '-c', 'Release', '--no-restore', '-m:1', '-p:UseSharedCompilation=false', '-p:BuildInParallel=false', '-v:q']
receipt = m.owned(cmd, out, 'build', 240, dict(os.environ, DOTNET_PROCESSOR_COUNT='1', DOTNET_GCConserveMemory='9'), str(r))
print(json.dumps(receipt))
assert receipt['Settled'] and receipt['ExitCode'] == 0
