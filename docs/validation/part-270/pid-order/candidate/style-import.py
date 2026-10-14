from pathlib import Path
import shutil
r=Path.cwd();o=r.parent/'pid-order-evidence/owned-style-root'
for name in ['src/Erlang.Terms/TermOrder.cs','tools/Erlang.Differential/CompiledModuleCases.cs','tools/Erlang.Differential/IdentifierOrderCases.cs']:shutil.copyfile(o/name,r/name)
