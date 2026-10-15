from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();o=r/'docs/validation/part-290';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
f=read(o/'source-freeze.json');v=read(o/'evidence-receipt.json');assert len(f['Files'])==v['SourcePaths']==30 and not v['MainFullRepeated'] and v['DefaultMfas']==502
metadata=['.gitattributes','AGENTS.md','CHANGELOG.md','docs/COMPATIBILITY.md','docs/DECISIONS.md','docs/ERLANG_COMPATIBILITY.md','docs/NEXT_STEPS.md','docs/PROGRESS.md','docs/changelog/0044.md','docs/dependency-decisions.md','docs/OTP-PORTS.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']
expected=metadata+[x['Path'] for x in f['Files']]+[p.relative_to(r).as_posix() for p in o.rglob('*') if p.is_file()]
assert len(expected)==len(set(expected));assert not any('LICENSE.local' in p or '__pycache__' in p for p in expected)
assert not subprocess.check_output(['git','diff','--name-only','HEAD','--','src','tests','Erlang.Net.slnx','docs/supported-mfas.json']).strip()
pathspec=r/'artifacts/root-part290-stage-pathspec';pathspec.write_bytes(b'\0'.join(p.encode() for p in expected)+b'\0')
subprocess.run(['git','add','--force','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul'],check=True)
actual=set(subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().split('\0')[:-1]);assert actual==set(expected),(actual-set(expected),set(expected)-actual)
for x in v['Files']:
 b=subprocess.check_output(['git','show',':docs/validation/part-290/'+x['Path']]);assert len(b)==x['Bytes'] and h(b)==x['Sha256'],x['Path']
for x in f['Files']:
 b=(r/x['Path']).read_bytes();assert h(b)==x['Sha256'];staged=subprocess.check_output(['git','show',':'+x['Path']])
 if '/native/' in x['Path'] or '/original/' in x['Path']:assert b==staged,x['Path']
 else:assert b.replace(b'\r\n',b'\n')==staged.replace(b'\r\n',b'\n'),x['Path']
assert h((r/'docs/changelog/0043.md').read_bytes())=='1246C10CA856935B004FA7ACB7E177E5E6A9B6F1250E32EBB893BE9739B6F0E1'
assert read(r/'docs/supported-mfas.json')==json.loads(subprocess.check_output(['git','show','HEAD:docs/supported-mfas.json']))
subprocess.run(['git','diff','--cached','--check'],check=True)
print({'StagedPaths':len(expected),'SourcePaths':30,'RawProofFiles':len(v['Files']),'RuntimeChanges':0,'Registry':502,'LicenseExcluded':True})
