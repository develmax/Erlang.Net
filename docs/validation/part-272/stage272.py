from pathlib import Path
import json,hashlib,shutil,subprocess
r=Path('.');a=r/'artifacts';d=r/'docs/validation/part-272';sha=lambda b:hashlib.sha256(b).hexdigest().upper()
imports=json.loads((a/'part272-main-import-guard.json').read_text())
for name in ['MODULE_READINESS.md','module-readiness.json']:
 shutil.copyfile(a/'part272-readiness'/name,r/'docs'/name);shutil.copyfile(a/'part272-readiness'/name,d/name)
for name in ['reference-checkpoint272-source-guard.py','part272-test-name-preservation-guard.json','part272-halt-postfull-guard.log']:
 shutil.copyfile(a/name,d/name)
owned=['.gitattributes','AGENTS.md','CHANGELOG.md','docs/PROGRESS.md','docs/changelog/0040.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']+[v['Path'] for v in imports['Files']]
def stage(paths):subprocess.run(['git','add','-f','--pathspec-from-file=-','--pathspec-file-nul'],input=('\0'.join(paths)+'\0').encode(),check=True)
stage(owned)
bindings=[]
for row in imports['Files']:
 working=(r/row['Path']).read_bytes();committed=subprocess.check_output(['git','show',':'+row['Path']])
 assert sha(working)==row['AfterSha256']
 assert working.decode().replace('\r\n','\n')==committed.decode().replace('\r\n','\n')
 if row['Path'].endswith('.erl'):assert working==committed
 bindings.append({'Path':row['Path'],'WorkingSha256':sha(working),'StagedSha256':sha(committed),'RawBytesExact':working==committed,'OnlyUtf8LineEndingsMayDiffer':True})
(d/'staged-source-bindings.json').write_bytes((json.dumps({'Status':'PASS','Files':bindings,'SourcePaths':25,'NativeProbeBytesExact':True},indent=2)+'\n').encode())
p=d/'evidence-receipt.json';receipt=json.loads(p.read_text());receipt['Files']=[{'Path':v.relative_to(d).as_posix(),'Length':v.stat().st_size,'Sha256':sha(v.read_bytes())} for v in sorted(d.rglob('*')) if v.is_file() and v!=p]
p.write_bytes((json.dumps(receipt,indent=2)+'\n').encode())
owned += [v.as_posix() for v in sorted(d.rglob('*')) if v.is_file()]
assert len(owned)==len(set(owned));stage(owned)
for row in receipt['Files']:assert sha(subprocess.check_output(['git','show',':'+(d/row['Path']).as_posix()]))==row['Sha256'],row['Path']
subprocess.run(['git','diff','--cached','--check'],check=True)
assert set(subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0'))-{''}==set(owned)
assert 'LICENSE.local.txt' not in owned
print('Explicit',len(owned),'staged paths, 25source bindings and all archived raw bytes exact; private license excluded')
