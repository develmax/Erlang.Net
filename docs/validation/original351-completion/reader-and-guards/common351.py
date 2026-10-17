"""Use exact reviewed351 transport and enforce archive set/API subject identity."""
from pathlib import Path
import hashlib,json,zipfile,sys,importlib.util
sys.dont_write_bytecode=True
ROOT=Path.cwd();BASE=ROOT/'artifacts/part351-remote-new-review'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
PINS={'passive-common.py':'BA2160DB60288DDD0E2C4D18B50A3155DE0156E7CB6A1DA68D40F42242C68C32'}
assert sha((BASE/'passive-common.py').read_bytes())==PINS['passive-common.py']
s=importlib.util.spec_from_file_location('transport351',BASE/'passive-common.py');n=importlib.util.module_from_spec(s);s.loader.exec_module(n)
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
