from pathlib import Path,PurePosixPath,PureWindowsPath
import json,hashlib,zipfile
r=Path.cwd();d=r/'artifacts/part335-remote-new-review/ci';mapping=r/'artifacts/part335-remote-new-review/monitor/mapped-runs.json';j=lambda p:json.loads(p.read_bytes());h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert not (d/'run-map.json').exists()
mp=j(mapping);selected=[x for x in mp['Runs'] if x['Kind']=='ci'];assert len(selected)==1;target=selected[0]
run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts'];assert run['id']==target['RunId']==38936881027 and run['head_sha']==mp['PublishedHead']=='0f357f55d016936fe7df7cf38408c3009887d382' and run['status']=='completed' and run['run_attempt']==target['RunAttempt']==1 and run['event']=='push' and run['path']==target['Path']=='.github/workflows/validate.yml'
assert len(jobs)==len(items)==1 and jobs[0]['run_id']==run['id'] and jobs[0]['head_sha']==run['head_sha'] and jobs[0]['run_attempt']==1 and jobs[0]['status']=='completed'
rows=j(d/'download-manifest.json');assert len(rows)==len({x['Path'] for x in rows}) and {p.relative_to(d).as_posix() for p in d.rglob('*') if p.is_file() and p.name!='download-manifest.json'}=={x['Path'] for x in rows}
for row in rows:
 p=d/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256']
for item in items:
 z=d/f'artifact-{item["id"]}.zip';ex=d/f'artifact-{item["id"]}';assert not item['expired'] and item['workflow_run']['head_sha']==run['head_sha'] and item['workflow_run']['id']==run['id'] and z.stat().st_size==item['size_in_bytes'] and h(z)==item['digest'][7:].upper()
 with zipfile.ZipFile(z) as archive:
  names=archive.namelist();assert len(names)==len(set(names))==len({x.casefold() for x in names})
  for name in names:
   assert not PurePosixPath(name).is_absolute() and not PureWindowsPath(name).is_absolute() and not PureWindowsPath(name).drive and '..' not in PurePosixPath(name).parts and '..' not in PureWindowsPath(name).parts and '\\' not in name
   if not name.endswith('/'):assert archive.read(name)==(ex/name).read_bytes()
value={'Head':mp['PublishedHead'],'Run':run['id'],'Job':jobs[0]['id'],'Attempt':1,'Workflow':run['name'],'WorkflowId':run['workflow_id'],'Path':run['path'],'Event':run['event'],'CreatedAt':run['created_at'],'MappedDispatchAttemptTime':target.get('DispatchAttemptTime'),'MappedDispatchAcceptedTime':target.get('DispatchAcceptedTime'),'InitialMappingSha256':h(mapping)}
(d/'run-map.json').write_bytes((json.dumps(value,indent=2)+'\n').encode())
print(json.dumps(dict(Status='MISSING_CI_RUN_MAP_FINISHED_EXISTING_RAW_BYTES_ALL_REVALIDATED',Run=run['id'],Job=jobs[0]['id'],RawRows=len(rows),NetworkExecuted=False,ArchiveExtractedAgain=False,ExistingRawFilesRewritten=False,RunMapSha256=h(d/'run-map.json'))))
