from pathlib import Path
source=Path('artifacts/library443-prepare-package.py').read_text()
assert source.count('artifacts/library443-driver-package1')==1
source=source.replace('artifacts/library443-driver-package1','artifacts/library448-driver-package2')
exec(compile(source,str(Path(__file__).resolve()),'exec'))
