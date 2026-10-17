from pathlib import Path
import hashlib
import importlib.util
import json
import os

root = Path.cwd().resolve()
view = root / 'artifacts/size-bif354-owned-style-view3'
output = root / 'artifacts/root-size354-owned-style3-actual'
assert view.is_dir() and not output.exists()
helper = root / 'artifacts/spawn-priority351-preparation/execute-root.py'
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
assert sha(helper) == 'C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
spec = importlib.util.spec_from_file_location('owned354style', helper)
owned = importlib.util.module_from_spec(spec)
spec.loader.exec_module(owned)
tool = root / 'artifacts/size-bif354-focus-candidate10/tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll'
assert tool.is_file()
inputs = {str(path): sha(path) for path in tool.parent.iterdir() if path.is_file()}
sources = {str(path.relative_to(view)): sha(path) for path in view.rglob('*.cs')}
assert len(sources) == 50
output.mkdir()
owned.write(output / 'inputs.json', {'ToolFiles': inputs, 'Sources': sources, 'Scope': str(view)})
env = dict(os.environ, DOTNET_PROCESSOR_COUNT='2', MSBUILDDISABLENODEREUSE='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')
for mode in ('write', 'check'):
    owned.owned(['C:/Program Files/dotnet/dotnet.exe', str(tool), '--' + mode, str(view)], output, mode, 90, env, root)
for path, digest in inputs.items():
    assert sha(Path(path)) == digest
after = {str(path.relative_to(view)): sha(path) for path in view.rglob('*.cs')}
assert set(after) == set(sources)
owned.write(output / 'completion.json', {'Status': 'ACTUAL_OWNED_STYLE_WRITE_AND_CHECK_PASS', 'Before': sources, 'After': after, 'Changed': [path for path in sources if sources[path] != after[path]], 'MainChanged': False, 'FullExecuted': False})
print(len([path for path in sources if sources[path] != after[path]]))
