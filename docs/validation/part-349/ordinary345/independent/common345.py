"""Subject-only adaptation of pinned, previously reviewed local passive readers."""
from pathlib import Path
import hashlib,json,zipfile,sys
sys.dont_write_bytecode=True
ROOT=Path.cwd();BASE=ROOT/'artifacts/part343-remote-new-review'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
PINS={'passive-common.py':'73B290BD8502B8F62A23DA616F1544BD07C8C8C70F12A1EEA7F3CAFBE88E0044','reader-common.py':'1CC727455181020ECF28BD4B1366E78CEF7C5A3A8B18B168887C0528E0392E2D','review-ordinary.py':'279693F106FD76083EB2EC0058D4A834EE00D34A1B00871E091C6443E6E836B0'}
for name,digest in PINS.items():assert sha((BASE/name).read_bytes())==digest,name
source=(BASE/'passive-common.py').read_text();old="'part343-remote-new-review'";assert source.count(old)==2
# Both occurrences are the same load() subject folder (mapping and raw data).
derived=source.replace(old,"'part345-remote-new-review'")
ns={'__file__':str(BASE/'passive-common.py')};exec(compile(derived,'subject-only-passive-common345','exec'),ns)
read=ns['read'];blobs=ns['blobs'];A=ns['A']

def load(kind):
 mapping,raw,run,job,items,rows=ns['load'](kind)
 assert len(items)==1 and items[0]['workflow_run']['id']==run['id']==mapping['Run']
 assert items[0]['name']=={'ci':'validation','ordinary':'differential'}[kind]
 assert run['head_branch']=='main' and items[0]['digest'].startswith('sha256:')
 ex=raw/f'artifact-{items[0]["id"]}'
 with zipfile.ZipFile(raw/f'artifact-{items[0]["id"]}.zip') as z:
  assert {p.relative_to(ex).as_posix() for p in ex.rglob('*') if p.is_file()}=={n for n in z.namelist() if not n.endswith('/')}
 return mapping,raw,run,job,items,rows
