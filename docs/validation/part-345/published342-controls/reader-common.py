"""Extra strict subject/artifact binding for prepared342 passive readers."""
from pathlib import Path
import importlib.util,json,hashlib,sys
sys.dont_write_bytecode=True
FOLDER=Path(__file__).resolve().parent
p=FOLDER/'passive-common.py'
EXPECTED='3469D239995C73AD6D4EAB9B099FDF5CB9FC2A06E25F2DB4368207CBC73F2E27'
assert hashlib.sha256(p.read_bytes()).hexdigest().upper()==EXPECTED
s=importlib.util.spec_from_file_location('trusted_root342_capture',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
def load(kind):
 mapping,raw,run,job,items,rows=m.load(kind)
 assert len(items)==1 and items[0]['workflow_run']['id']==run['id']==mapping['Run']
 assert items[0]['name']=={'ci':'validation','ordinary':'differential'}[kind]
 assert run['head_branch']=='main' and items[0]['digest'].startswith('sha256:')
 import zipfile
 ex=raw/f'artifact-{items[0]["id"]}'
 with zipfile.ZipFile(raw/f'artifact-{items[0]["id"]}.zip') as z:assert {p.relative_to(ex).as_posix() for p in ex.rglob('*') if p.is_file()}=={n for n in z.namelist() if not n.endswith('/')}
 return mapping,raw,run,job,items,rows
read=m.read;sha=m.sha;blobs=m.blobs;A=m.A;ROOT=m.ROOT
