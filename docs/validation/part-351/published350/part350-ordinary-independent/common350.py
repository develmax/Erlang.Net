"""Use exact reviewed350 transport and enforce archive set/API subject identity."""
from pathlib import Path
import hashlib,json,zipfile,sys,importlib.util
sys.dont_write_bytecode=True
ROOT=Path.cwd();BASE=ROOT/'artifacts/part350-remote-new-review'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
PINS={'passive-common.py':'B77791F5FD4303EE399C85AB09F9D6E44FC5B5B541DC72C48A573AAA585263E4'}
assert sha((BASE/'passive-common.py').read_bytes())==PINS['passive-common.py']
s=importlib.util.spec_from_file_location('transport350',BASE/'passive-common.py');n=importlib.util.module_from_spec(s);s.loader.exec_module(n)
read=n.read;blobs=n.blobs;A=n.A
def load(kind):
 mapping,raw,run,job,items,rows=n.load(kind)
 assert len(items)==1 and items[0]['workflow_run']['id']==run['id']==mapping['Run']
 assert items[0]['name']=={'ci':'validation','ordinary':'differential'}[kind]
 assert run['head_branch']=='main' and items[0]['digest'].startswith('sha256:')
 ex=raw/f'artifact-{items[0]["id"]}'
 with zipfile.ZipFile(raw/f'artifact-{items[0]["id"]}.zip') as z:
  assert {p.relative_to(ex).as_posix() for p in ex.rglob('*') if p.is_file()}=={p for p in z.namelist() if not p.endswith('/')}
 return mapping,raw,run,job,items,rows
