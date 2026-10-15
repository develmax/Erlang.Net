"""Read-only detached terminal proof, including failed/cancelled outcomes."""
from pathlib import Path
import hashlib,json,sys,zipfile
r=Path.cwd();a=r/'artifacts';rid=int(sys.argv[1]);d=a/f'pack309-watch-{rid}-terminal';head='e829c53ef4323ea8cdd0948cd77c243efa787abd'
j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper()
run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts'];manifest=j(d/'download-manifest.json')
names={38766513916:'Validate CLR implementation',38766554804:'Pinned OTP BEAM fixtures'}
assert run['id']==rid and run['name']==names[rid] and run['head_sha']==head and run['run_attempt']==1 and run['status']=='completed'
assert all(x['head_sha']==head and x['run_id']==rid and x['status']=='completed' and x['run_attempt']==1 for x in jobs)
assert {p.relative_to(d).as_posix() for p in d.rglob('*') if p.is_file() and p.name!='download-manifest.json'}=={x['Path'].replace('\\','/') for x in manifest}
for row in manifest:
 p=d/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p.read_bytes())==row['Sha256']
for item in items:
 assert not item['expired'] and item['workflow_run']['head_sha']==head
 z=d/f'artifact-{item["id"]}.zip';base=d/f'artifact-{item["id"]}'
 assert z.stat().st_size==item['size_in_bytes'] and h(z.read_bytes())==item['digest'][7:].upper()
 with zipfile.ZipFile(z) as archive:
  for name in archive.namelist():
   assert not Path(name).is_absolute() and '..' not in Path(name).parts
   if not name.endswith('/'):assert archive.read(name)==(base/name).read_bytes()
result={'Status':'AUTHENTICATED_EXACT_HEAD_TERMINAL_BYTES_VERIFIED','Head':head,'Run':rid,'Attempt':1,'Workflow':run['name'],'Conclusion':run['conclusion'],'Jobs':[{'Id':x['id'],'Conclusion':x['conclusion'],'FailedSteps':[s['name'] for s in x['steps'] if s['conclusion']=='failure'],'LogSha256':h((d/f'job-{x["id"]}.log').read_bytes())} for x in jobs],'Artifacts':[{'Id':x['id'],'ApiDigest':x['digest'],'Sha256':h((d/f'artifact-{x["id"]}.zip').read_bytes())} for x in items],'RawFilesVerified':len(manifest),'MainEdited':False,'PostExecuted':False,'NativeExecuted':False}
out=a/f'pack309-terminal-{rid}-envelope-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
