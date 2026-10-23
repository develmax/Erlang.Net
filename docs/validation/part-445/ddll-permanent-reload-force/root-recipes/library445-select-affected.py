from pathlib import Path
source = Path('artifacts/library444-select-affected.py').read_text()
assert source.count('artifacts/library444-affected-selection1') == 1
source = source.replace('artifacts/library444-affected-selection1', 'artifacts/library445-affected-selection1')
exec(compile(source, str(Path(__file__).resolve()), 'exec'))
