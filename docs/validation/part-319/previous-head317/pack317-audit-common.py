import sys
sys.dont_write_bytecode=True
"""Detached byte verification and exact-head bulk Git reads; no native or source execution."""
from pathlib import Path,PurePosixPath,PureWindowsPath
import hashlib,json,subprocess,zipfile
ROOT=Path.cwd();A=ROOT/'artifacts';read=lambda p:json.loads(Path(p).read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def blobs(head,paths):
 paths=list(dict.fromkeys(paths));assert all('\n' not in p and '\r' not in p and '\0' not in p for p in paths)
 process=subprocess.run(['git','cat-file','--batch'],cwd=ROOT,input=''.join(head+':'+p+'\n' for p in paths).encode(),capture_output=True);assert process.returncode==0,process.stderr.decode(errors='replace');raw=process.stdout;offset=0;result={}
 for path in paths:
  end=raw.index(b'\n',offset);header=raw[offset:end].split();assert len(header)==3 and header[1]==b'blob',(path,header);size=int(header[2]);start=end+1;data=raw[start:start+size];assert len(data)==size and raw[start+size:start+size+1]==b'\n';offset=start+size+1;result[path]=data
 assert offset==len(raw);return result
def load(kind):
 mapping=read(A/f'pack317-{kind}-run-map.json');head=mapping['Head'];rid=mapping['Run'];d=A/f'pack317-watch-{rid}-terminal';run=read(d/'run.json');jobs=read(d/'jobs.json')['jobs'];items=read(d/'artifacts.json')['artifacts']
 assert mapping['Attempt']==run['run_attempt']==1 and run['head_sha']==head and run['id']==rid and run['status']=='completed' and run['name']==mapping['Workflow']
 assert mapping['Workflow']=={'ci':'Validate CLR implementation','native':'Pinned OTP pending and bounded native backend observations','ordinary':'Pinned OTP differential oracle','reallocation':'Pinned OTP additional native reallocation comparison'}[kind]
 assert len(jobs)==1 and jobs[0]['id']==mapping['Job'] and jobs[0]['head_sha']==head and jobs[0]['run_id']==rid and jobs[0]['run_attempt']==1 and jobs[0]['status']=='completed'
 rows=read(d/'download-manifest.json');assert len(rows)==len({x['Path'].replace('\\','/') for x in rows})
 assert {p.relative_to(d).as_posix() for p in d.rglob('*') if p.is_file() and p.name!='download-manifest.json'}=={x['Path'].replace('\\','/') for x in rows}
 for row in rows:
  p=d/row['Path'];assert p.stat().st_size==row['Bytes'] and sha(p.read_bytes())==row['Sha256']
 for item in items:
  assert not item['expired'] and item['workflow_run']['head_sha']==head;zp=d/f'artifact-{item["id"]}.zip';ex=d/f'artifact-{item["id"]}';assert zp.stat().st_size==item['size_in_bytes'] and sha(zp.read_bytes())==item['digest'][7:].upper()
  with zipfile.ZipFile(zp) as z:
   names=z.namelist();assert len(names)==len(set(names))
   for name in names:
    assert not PurePosixPath(name).is_absolute() and not PureWindowsPath(name).is_absolute() and '..' not in PurePosixPath(name).parts and '..' not in PureWindowsPath(name).parts
    if not name.endswith('/'):assert z.read(name)==(ex/name).read_bytes()
 return mapping,d,run,jobs[0],items,rows
