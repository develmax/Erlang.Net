from pathlib import Path
import shutil,json,hashlib
r=Path.cwd();e=r.parent/'pid-order-evidence';o=e/'owned-style-root'; owned=['src/Erlang.Terms/TermOrder.cs','tools/Erlang.Differential/CompiledModuleCases.cs','tools/Erlang.Differential/IdentifierOrderCases.cs']
for d in ['src','tools','tests','benchmarks','examples']:(o/d).mkdir(parents=True,exist_ok=True)
for name in owned:
 t=o/name;t.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(r/name,t)
