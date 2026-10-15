from pathlib import Path
import subprocess,hashlib,json
r=Path.cwd();p='tools/Erlang.BeamFixtures/verify-metadata-mutations.py'
w=(r/p).read_bytes();g=subprocess.check_output(['git','show','HEAD:'+p])
print(json.dumps({'Path':p,'WorkingSha':hashlib.sha256(w).hexdigest().upper(),'GitSha':hashlib.sha256(g).hexdigest().upper(),'RawExact':w==g,'LfExact':w.replace(b'\r\n',b'\n')==g.replace(b'\r\n',b'\n')}))
