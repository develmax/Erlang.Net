from pathlib import Path
import hashlib
import json
import shutil
import tarfile

root = Path.cwd().resolve()
artifacts = root / 'artifacts'
base = artifacts / 'root-process-info301-isolated'
candidate = base / 'candidate'
assert not base.exists()
candidate.mkdir(parents=True)
with tarfile.open(artifacts / 'root-nif-lifetime300-isolated/published299-source.tar') as archive:
    archive.extractall(candidate, filter='data')
read = lambda path: json.loads(path.read_bytes())
digest = lambda data: hashlib.sha256(data).hexdigest().upper()
freeze_path = artifacts / 'root-part300-final-source-freeze.json'
freeze = read(freeze_path)
for row in freeze['Files']:
    source = root / row['Path']
    assert digest(source.read_bytes()) == row['Sha256']
    target = candidate / row['Path']
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, target)
proposal = artifacts / 'process-info-default-subset-candidate1'
for row in read(proposal / 'source-freeze.json')['Files']:
    source = proposal / row['Path']
    assert digest(source.read_bytes()) == row['Sha256']
replacement = read(proposal / 'source-bindings.json')['ReplacesOneMainFile']
assert digest((candidate / replacement['Path']).read_bytes()) == replacement['Sha256']
paths = []
for source in sorted((proposal / 'overlay').rglob('*.cs')):
    name = source.relative_to(proposal / 'overlay').as_posix()
    target = candidate / name
    target.parent.mkdir(parents=True, exist_ok=True)
    if name != replacement['Path']:
        assert not target.exists()
    shutil.copyfile(source, target)
    paths.append(name)
program = candidate / 'tests/Erlang.Tests/Program.cs'
data = program.read_bytes()
needle = b'Test(Erlang.Tests.ProcessInfoDefaultHostNames.Test, Erlang.Tests.ProcessInfoDefaultHostChecks.Run);'
assert data.count(needle) == 1
program.write_bytes(data.replace(needle, needle + b'\nTest(Erlang.Tests.ProcessInfoDefaultSubsetHostNames.Test, Erlang.Tests.ProcessInfoDefaultSubsetHostChecks.Run);'))
paths.append(program.relative_to(candidate).as_posix())
central = candidate / 'tools/Erlang.Differential/CompiledModuleCases.cs'
data = central.read_text(encoding='utf-8-sig')
needle = '.. NifDeclarationCases.Modules'
assert data.count(needle) == 1
data = data.replace(needle, needle + ', .. ProcessInfoDefaultSubsetCases.Modules')
lines = data.splitlines()
index = next(i for i, line in enumerate(lines) if line.startswith('        , .. TryModuleCases.All'))
lines[index] = lines[index].replace(', ..', ',\n        ..')
central.write_text('\n'.join(lines) + '\n', encoding='utf-8')
paths.append(central.relative_to(candidate).as_posix())
assert len(paths) == 8
(base / 'paths8.json').write_text(json.dumps(paths, indent=2) + '\n', encoding='utf-8')
(base / 'prepared-baseline.json').write_text(json.dumps({
    'Status': 'ISOLATED_SOURCE_PREPARATION_NOT_BUILT',
    'Baseline': 'Frozen intended Part300 Main source; publication and full validation still pending at preparation',
    'Part300SourceFreezeSha256': digest(freeze_path.read_bytes()),
    'SourcePaths': paths,
    'ExpectedMfaDelta': 1,
    'NewCompiledCases': 8,
    'MainChanged': False,
}, indent=2) + '\n', encoding='utf-8')
print('Prepared isolated future process_info/1 source8; no Main mutation or execution')
