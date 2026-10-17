from pathlib import Path
import importlib.util, os, sys

r=Path.cwd()
assert len(sys.argv)==2 and sys.argv[1] in ['restricted','approved']
spec=importlib.util.spec_from_file_location('priority_execution',r/'artifacts/spawn-priority351-preparation/execute-root.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
o=r/('artifacts/spawn-priority351-port-'+sys.argv[1]);o.mkdir(exist_ok=False)
runner=r/'artifacts/spawn-priority351-diagnostic3/bin/Debug/net10.0/Diagnostic.dll'
env=os.environ.copy();env['DOTNET_PROCESSOR_COUNT']='2'
module.owned(['dotnet',str(runner),'erlang-port-native-option-echo-3',str(o/'result.json')],o,'probe',60,env,r/'artifacts/spawn-priority351-full-candidate')
