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
cmd = [sys.executable, 'artifacts/resource536/style536.py', 'style-final536']
receipt = m.owned(cmd, out, 'build', 240, dict(os.environ, DOTNET_PROCESSOR_COUNT='1', DOTNET_GCConserveMemory='9'), str(r))
print(json.dumps(receipt))
assert receipt['Settled'] and receipt['ExitCode'] == 0
