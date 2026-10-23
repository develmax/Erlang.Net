from pathlib import Path
r=Path.cwd()
s=(r/'artifacts/library444-select-affected.py').read_text().replace('library444-affected-selection1','library449-affected-selection1').replace('library443-affected-selection1','library448-affected-selection1').replace('195','587').replace('Retained443Cases','Retained448Cases')
s=s.replace("write(out/'selection.json'", "names=list(dict.fromkeys(names+[n for n in all_names if n.startswith('runtime/distribution449/')]))\nwrite(out/'selection.json'")
p=r/'artifacts/library449-select-affected.py';assert not p.exists();p.write_text(s)
s=(r/'artifacts/library448-run-standard79.py').read_text().replace('library448','library449');p=r/'artifacts/library449-run-standard79.py';assert not p.exists();p.write_text(s)
print('Gates prepared')
