from pathlib import Path
source=Path('artifacts/library447-probe4/run-root.py').read_text()
assert source.count('library447-driver-package1')==1
source=source.replace('library447-driver-package1','library448-driver-package2')
# Resolve the unchanged sealed observer sources, not the wrapper folder.
source=source.replace('packet=Path(__file__).resolve().parent','packet=r/\'artifacts/library447-probe4\'')
exec(compile(source,str(Path(__file__).resolve()),'exec'))
