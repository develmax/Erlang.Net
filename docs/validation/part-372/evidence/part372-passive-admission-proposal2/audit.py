"""Parent-only batch admission. Reads bytes/receipts; never executes captured VM/BEAM code."""
from pathlib import Path
import argparse,hashlib,json,subprocess,sys,zipfile
HERE=Path(__file__).resolve().parent
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def dump(p,x):p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')

def main():
 p=argparse.ArgumentParser()
 for key in ['download-run','artifact','tools-root','bindings','output']:p.add_argument('--'+key,type=Path,required=True)
 for key in ['expected-head','zip-sha256']:p.add_argument('--'+key,required=True)
 for key in ['run-id','job-id','artifact-id']:p.add_argument('--'+key,type=int,required=True)
 a=p.parse_args();out=a.output.resolve();assert out.is_relative_to(HERE) and not out.exists()
 for row in J(HERE/'source-freeze.json')['Files']:
  q=(HERE/row['Path']).resolve();assert q.is_relative_to(HERE) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
 remote=a.download_run.resolve();artifact=a.artifact.resolve();assert artifact.is_relative_to(remote)
 run=J(remote/'run.json');assert run['id']==a.run_id and run['head_sha']==a.expected_head and run['run_attempt']==1 and run['status']=='completed' and run['conclusion'] in ['success','failure']
 jobs=J(remote/'jobs.json')['jobs'];assert len(jobs)==1 and jobs[0]['id']==a.job_id and jobs[0]['status']=='completed' and jobs[0]['conclusion']==run['conclusion']
 ars=J(remote/'artifacts.json')['artifacts'];assert len(ars)==1 and ars[0]['id']==a.artifact_id and ars[0]['workflow_run']['head_sha']==a.expected_head and not ars[0]['expired']
 archive=remote/('artifact-'+str(a.artifact_id)+'.zip');assert H(archive)==a.zip_sha256.upper()==ars[0]['digest'].split(':',1)[1].upper()
 if (remote/'download-manifest.json').exists():
  for row in J(remote/'download-manifest.json'):
   q=(remote/row['Path']).resolve();assert q.is_relative_to(remote) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
 else:
  downloads=J(remote/'download.json');assert len(downloads)==1
  download=downloads[0]
  assert download['Id']==a.artifact_id and download['ZipSha256']==H(archive) and Path(download['Directory']).resolve()==artifact
 with zipfile.ZipFile(archive) as z:
  files=[x for x in z.infolist() if not x.is_dir()];assert len(files)==len({x.filename for x in files})
  assert {q.relative_to(artifact).as_posix() for q in artifact.rglob('*') if q.is_file()}=={x.filename for x in files}
  for row in files:
   q=(artifact/row.filename).resolve();assert q.is_relative_to(artifact) and q.stat().st_size==row.file_size and H(q)==hashlib.sha256(z.read(row)).hexdigest().upper()
 out.mkdir()
 shared=[sys.executable,'-B',str(HERE/'audit-source-common.py'),'--artifact',str(artifact),'--tools-root',str(a.tools_root.resolve()),'--bindings',str(a.bindings.resolve()),'--expected-head',a.expected_head,'--output',str(out/'source')]
 with (out/'source.stdout').open('w',encoding='utf-8') as stdout,(out/'source.stderr').open('w',encoding='utf-8') as stderr:
  result=subprocess.run(shared,stdout=stdout,stderr=stderr)
 if result.returncode:
  dump(out/'acceptance.json',{'Status':'FAIL_SHARED_DEPENDENCY_NO_COLLECTOR_ADMISSION_ATTEMPTED','SharedExitCode':result.returncode,'DownloadedCodeExecuted':False})
  raise SystemExit(1)
 snapshot=out/'source/producer-snapshot';results=[];accepted=[]
 for cohort in J(HERE/'reviewed-source-packet.json')['Cohorts']:
  name=cohort['Id'];child=HERE/name;childout=child/out.name;assert not childout.exists()
  argv=[sys.executable,'-B',str(child/'audit-provenance.py'),'--artifact',str(artifact),'--run-json',str(remote/'run.json'),'--source-snapshot',str(snapshot),'--output',str(childout),'--tool',str(a.tools_root.resolve()/name),'--artifact-id',str(a.artifact_id),'--job-id',str(a.job_id),'--run-id',str(a.run_id),'--zip-sha256',a.zip_sha256,'--expected-head',a.expected_head,'--manifest-sha256',cohort['ManifestSha256']]
  for stage in ['parse-build','parse-sources','collector-admit']:argv+=['--preflight-receipt',str(artifact/(name+'-preflight')/(stage+'.process.json'))]
  with (out/(name+'.stdout')).open('w',encoding='utf-8') as stdout,(out/(name+'.stderr')).open('w',encoding='utf-8') as stderr:
   result=subprocess.run(argv,stdout=stdout,stderr=stderr)
  row={'Id':name,'ExitCode':result.returncode,'AcceptancePath':str(childout/'acceptance.json') if not result.returncode else None}
  if not result.returncode:
   admission=J(childout/'acceptance.json');assert admission['Values']==cohort['ExpectedValues'] and admission['FullStackProfiles']==cohort['ExpectedProfiles']
   row['AcceptanceSha256']=H(childout/'acceptance.json');accepted.append(admission)
  results.append(row)
 allpass=all(x['ExitCode']==0 for x in results)
 receipt={'Status':'PASS_PASSIVE_PART372_ALL_THREE_COLLECTORS' if allpass and run['conclusion']=='success' else 'PARTIAL_OR_FAILED_PART372_PASSIVE_ADMISSION','Head':a.expected_head,'Run':a.run_id,'ParentRunConclusion':run['conclusion'],'Collectors':results,'SharedSourceAcceptanceSha256':H(out/'source/acceptance.json'),'SourceFreezeSha256':H(HERE/'source-freeze.json'),'DownloadedCodeExecuted':False,'CompatibilityPromoted':False,'ExpectedOutcomesSeeded':False}
 if allpass:
  values=sum(x['Values'] for x in accepted);profiles=sum(x['FullStackProfiles'] for x in accepted)
  controls=sum(len(x['MetadataCorruptionControls']) for x in accepted)
  originals={};preflights={}
  for report in accepted:
   for key,target in [('OriginalNormalProcesses',originals),('PreflightNormalProcesses',preflights)]:
    for process in report[key]:
     if process['Pid'] in target:assert target[process['Pid']]==process
     target[process['Pid']]=process
  assert values==167 and profiles==835 and controls==12 and len(originals)==11 and len(preflights)==9
  receipt.update(Values=values,FullStackProfiles=profiles,TypedObservations=values+profiles,MetadataCorruptionControls=controls,UniqueOriginalNormalProcesses=len(originals),UniquePreflightNormalProcesses=len(preflights),PinnedSourceBlobsAuditedOnce=12008)
 dump(out/'acceptance.json',receipt);print(json.dumps({'Acceptance':str(out/'acceptance.json'),'Sha256':H(out/'acceptance.json'),'Status':receipt['Status']}))
 if not allpass or run['conclusion']!='success':raise SystemExit(1)
if __name__=='__main__':main()
