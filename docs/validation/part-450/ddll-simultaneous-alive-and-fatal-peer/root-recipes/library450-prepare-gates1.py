from pathlib import Path
r=Path.cwd()
s=(r/'artifacts/library449-select-affected.py').read_text().replace('library449-affected-selection1','library450-affected-selection1').replace('library448-affected-selection1','library449-affected-selection1').replace('==587','==588').replace('Cases=587','Cases=588').replace('Retained448','Retained449').replace('runtime/distribution449/','runtime/distribution450/')
(r/'artifacts/library450-select-affected.py').write_text(s)
s=(r/'artifacts/library449-gates1.py').read_text().replace('library449','library450');(r/'artifacts/library450-gates1.py').write_text(s)
s=(r/'artifacts/library449-run-standard79.py').read_text().replace('library449','library450');(r/'artifacts/library450-run-standard79.py').write_text(s)
s=(r/'artifacts/library449-source-authority.py').read_text().replace('library449','library450');(r/'artifacts/library450-source-authority.py').write_text(s)
s=(r/'artifacts/library449-style-readiness.py').read_text().replace('library449','library450').replace('CurrentCatalog8825NotFull','CurrentCatalog8826NotFull').replace('39945517467','39951441544');(r/'artifacts/library450-style-readiness.py').write_text(s)
print('450 gates prepared')
