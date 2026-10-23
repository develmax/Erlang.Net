from pathlib import Path
source=Path('artifacts/library444-select-affected.py').read_text()
source=source.replace('artifacts/library444-affected-selection1','artifacts/library448-affected-selection1')
source=source.replace("names=json.loads((r/'artifacts/library443-affected-selection1/selection.json').read_text())['ExactCases']","names=json.loads((r/'artifacts/library447-affected-selection1/selection.json').read_text())['ExactCases']")
source=source.replace('len(names)==len(set(names))==195','len(names)==len(set(names))==205')
needle="write(out/'selection.json'";assert source.count(needle)==1
source=source.replace(needle,"names=list(dict.fromkeys(names+[n for n in all_names if any(t in n.lower() for t in ['distribution','connected','send','alias','seq-trace','sequence-trace','async-dist','setnode','node-identity'])]))\n"+needle)
source=source.replace('Retained443Cases=195','Retained447Cases=205')
exec(compile(source,str(Path(__file__).resolve()),'exec'))
