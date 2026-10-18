from pathlib import Path
import hashlib, importlib.util, json, os, sys
r=Path.cwd();o=r/'artifacts/binary-integration369'/sys.argv[1];o.mkdir(parents=True,exist_ok=False)
p=r/'artifacts/spawn-priority351-preparation/execute-root.py'
assert hashlib.sha256(p.read_bytes()).hexdigest().upper()=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
s=importlib.util.spec_from_file_location('binary369_owned',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
m.owned(sys.argv[3:],o,'execution',int(sys.argv[2]),env,r)
print(json.dumps(dict(Status='PASS_OWNED_STEP',Directory=str(o))))
