"""Passive ZIP-only extraction, exact API digests and immutable downloaded closure."""

from pathlib import Path,PurePosixPath,PureWindowsPath

import json,hashlib,zipfile,sys

sys.dont_write_bytecode=True

r=Path.cwd();a=r/'artifacts';kind=sys.argv[1];mapping=json.loads((a/'part328-remote-beam310/monitor/mapped-runs.json').read_bytes());selected=[x for x in mapping['Runs'] if x['Kind']==kind];assert len(selected)==1;target=selected[0];rid=target['RunId'];d=a/'part328-remote-beam310'/kind;j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()

assert not (d/'download-manifest.json').exists()

run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts'];assert run['head_sha']==mapping['PublishedHead'] and run['id']==rid and run['status']=='completed' and run['run_attempt']==target['RunAttempt']==1

assert len(jobs)==1

for item in items:

 z=d/f'artifact-{item["id"]}.zip';ex=d/f'artifact-{item["id"]}';assert not ex.exists() and not item['expired'] and item['workflow_run']['head_sha']==mapping['PublishedHead'] and item['workflow_run']['id']==rid and z.stat().st_size==item['size_in_bytes'] and h(z)==item['digest'][7:].upper()

 with zipfile.ZipFile(z) as archive:

  names=archive.namelist();assert len(names)==len(set(names))==len({x.casefold() for x in names})

  for name in names:

   assert not PurePosixPath(name).is_absolute() and not PureWindowsPath(name).is_absolute() and not PureWindowsPath(name).drive and '..' not in PurePosixPath(name).parts and '..' not in PureWindowsPath(name).parts and '\\' not in name

  for name in names:

   if not name.endswith('/'):

    out=ex/name;assert out.resolve().is_relative_to(ex.resolve());out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(archive.read(name))

rows=[{'Path':p.relative_to(d).as_posix(),'Bytes':p.stat().st_size,'Sha256':h(p)} for p in sorted(d.rglob('*')) if p.is_file()]

(d/'download-manifest.json').write_text(json.dumps(rows,indent=2)+'\n')

mp={'Head':mapping['PublishedHead'],'Run':rid,'Job':jobs[0]['id'],'Attempt':1,'Workflow':run['name'],'WorkflowId':run['workflow_id'],'Path':run['path'],'Event':run['event'],'CreatedAt':run['created_at'],'MappedDispatchAttemptTime':target['DispatchAttemptTime'],'MappedDispatchAcceptedTime':target['DispatchAcceptedTime'],'InitialMappingSha256':h(a/'part328-remote-beam310/monitor/mapped-runs.json')}

p=d/'run-map.json';assert not p.exists();p.write_text(json.dumps(mp,indent=2)+'\n')

print(json.dumps({'Kind':kind,'Run':rid,'Job':mp['Job'],'Conclusion':run['conclusion'],'ArtifactIds':[x['id'] for x in items],'RawFiles':len(rows),'PassiveOnly':True}))

