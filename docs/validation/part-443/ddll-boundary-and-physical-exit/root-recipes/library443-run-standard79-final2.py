from pathlib import Path
source=Path('artifacts/library436-run-standard79.py').read_text()
assert source.count('artifacts/library436-standard79-final1')==1
source=source.replace('artifacts/library436-standard79-final1','artifacts/library443-standard79-final2')
exec(compile(source,'artifacts/library436-run-standard79.py','exec'))
