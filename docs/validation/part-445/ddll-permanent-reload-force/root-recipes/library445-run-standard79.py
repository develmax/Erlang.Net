from pathlib import Path
source = Path('artifacts/library436-run-standard79.py').read_text()
assert source.count('artifacts/library436-standard79-final1') == 1
source = source.replace('artifacts/library436-standard79-final1', 'artifacts/library445-standard79-final1')
exec(compile(source, str(Path(__file__).resolve()), 'exec'))
