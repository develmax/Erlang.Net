from pathlib import Path
import hashlib
import json
import shutil
import subprocess

root = Path.cwd().resolve()
artifacts = root / 'artifacts'
base = artifacts / 'root-process-info301-isolated'
candidate = base / 'candidate'
read = lambda path: json.loads(path.read_bytes())
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root).decode().strip()
assert head == 'c2d2cc304d03201dd52592c77d0389062aea0268'
assert read(artifacts / 'root-part300-full-exit.json')['ExitCode'] == 0
assert read(artifacts / 'root-part300-main-native-exit.json')['ExitCode'] == 0
freeze_path = artifacts / 'root-part300-final-source-freeze.json'
freeze = read(freeze_path)
for row in freeze['Files']:
    source = root / row['Path']
    assert digest(source.read_bytes()) == row['Sha256']
    if row['Path'] != 'tests/Erlang.Tests/Program.cs':
        shutil.copyfile(source, candidate / row['Path'])
program = 'tests/Erlang.Tests/Program.cs'
data = (root / program).read_bytes()
needle = b'Test(Erlang.Tests.ProcessInfoDefaultHostNames.Test, Erlang.Tests.ProcessInfoDefaultHostChecks.Run);'
assert data.count(needle) == 1
(candidate / program).write_bytes(data.replace(needle, needle + b'\nTest(Erlang.Tests.ProcessInfoDefaultSubsetHostNames.Test, Erlang.Tests.ProcessInfoDefaultSubsetHostChecks.Run);'))
proposal = artifacts / 'process-info-default-subset-candidate1'
for row in read(proposal / 'source-freeze.json')['Files']:
    if row['Path'].startswith('overlay/'):
        target = candidate / row['Path'][len('overlay/'):]
        assert digest(target.read_bytes()) == row['Sha256']
central = candidate / 'tools/Erlang.Differential/CompiledModuleCases.cs'
data = central.read_text(encoding='utf-8-sig')
assert data.count('.. ProcessInfoDefaultSubsetCases.Modules') == 1
data = data.replace('\n        ,\n        .. TryModuleCases.All', ',\n        .. TryModuleCases.All')
central.write_text(data, encoding='utf-8')
receipt = {'Status': 'EXACT_VALIDATED_PART300_SOURCE_REBASED_FOR_ISOLATED_API', 'ParentHead': head, 'Part300SourceFreezeSha256': digest(freeze_path.read_bytes()), 'SourcePaths': read(base / 'paths8.json'), 'MainChanged': False, 'NewApiExecuted': False, 'OriginalNewCasesExecuted': False}
(base / 'rebased-part300.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(json.dumps(receipt))
