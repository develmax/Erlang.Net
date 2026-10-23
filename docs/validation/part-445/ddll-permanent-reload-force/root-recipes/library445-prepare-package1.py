from pathlib import Path
source = Path('artifacts/library443-prepare-package.py').read_text()
assert source.count('artifacts/library443-driver-package1') == 1
source = source.replace('artifacts/library443-driver-package1', 'artifacts/library445-driver-package1')
exec(compile(source, str(Path(__file__).resolve()), 'exec'))
