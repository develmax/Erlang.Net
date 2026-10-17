from pathlib import Path
import shutil

runner = Path('artifacts/style354-owned.py').read_bytes()
for before, after in (
    (b"view = root / 'artifacts/size-bif354-owned-style-view'", b"view = root / 'artifacts/size-bif354-owned-style-view3'"),
    (b"output = root / 'artifacts/root-size354-owned-style-actual'", b"output = root / 'artifacts/root-size354-owned-style3-actual'"),
):
    assert runner.count(before) == 1
    runner = runner.replace(before, after)
target = Path('artifacts/style354-owned3.py')
assert not target.exists()
target.write_bytes(runner)
project = Path('artifacts/Style354TokenAudit3')
project.mkdir()
shutil.copyfile('artifacts/Style354TokenAudit/Audit.csproj', project / 'Audit.csproj')
source = Path('artifacts/Style354TokenAudit/Program.cs').read_bytes()
for before, after in (
    (b'artifacts/size-bif354-owned-style-view', b'artifacts/size-bif354-owned-style-view3'),
    (b'artifacts/size-bif354-permanent-test-proposal2/style-inputs.json', b'artifacts/size-bif354-permanent-test-proposal3/style-inputs.json'),
):
    assert source.count(before) == 1
    source = source.replace(before, after)
(project / 'Program.cs').write_bytes(source)
