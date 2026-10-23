from pathlib import Path
source=Path('artifacts/library444-select-affected.py').read_text()
assert source.count('artifacts/library444-affected-selection1')==1
source=source.replace('artifacts/library444-affected-selection1','artifacts/library447-affected-selection1')
needle='write(out/\'selection.json\''
assert source.count(needle)==1
source=source.replace(needle,"names=list(dict.fromkeys(names+[n for n in all_names if any(token in n for token in ['setnode','node-identity','external-identifier','connected-spawn'])]))\n"+needle)
exec(compile(source,str(Path(__file__).resolve()),'exec'))
