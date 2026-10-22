"""Root-only GET capture of the exact terminal queue368 run; never dispatches or executes downloaded content."""
from pathlib import Path, PurePosixPath
import argparse,hashlib,json,subprocess,urllib.request,urllib.error,urllib.parse,zipfile

R=Path(__file__).resolve().parents[2]
HEAD='53823b168e672fc946e65f6dd53c0ea2f9c94611'
API='https://api.github.com/repos/develmax/Erlang.Net'
KINDS={'original': (39084998283, 'proplist-get-value-original.yml', 'workflow_dispatch', 'original-queue8')}
H=lambda b:hashlib.sha256(b).hexdigest().upper()
def write(p,v):
    with p.open('xb') as f:f.write((json.dumps(v,indent=2)+'\n').encode())
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,*args):return None
class HttpsOnly(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,req,fp,code,msg,headers,url):
        u=urllib.parse.urlsplit(url);assert u.scheme=='https' and u.hostname and not u.username and not u.password
        return super().redirect_request(req,fp,code,msg,headers,url)
def main():
    p=argparse.ArgumentParser();p.add_argument('--kind',choices=KINDS,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    runid,workflow,event,artifactname=KINDS[a.kind]
    # Read exact published Git objects before credential access; never hash the worktree representation.
    def blob(path):return subprocess.check_output(['git','-C',str(R),'show',HEAD+':'+path],stderr=subprocess.DEVNULL,timeout=30)
    bindings=(Path(__file__).parent/'source-bindings.json').read_bytes();assert H(bindings)=='F77A3C91130C06F371BF38E64EA3E93DD364FA06608DDD614529E9FA6F7FB26D';config=json.loads(bindings)
    for row in config['Workflows']:
        data=blob(row['Path']);assert len(data)==row['Bytes'] and H(data)==row['Sha256']
    assert config['Head']==HEAD and config['Runs']=={k:v[0] for k,v in KINDS.items()}
    output=a.output.resolve();assert output.is_relative_to(R/'artifacts') and not output.exists()
    credential=subprocess.run(['git','-C',str(R),'credential','fill'],input=b'protocol=https\nhost=github.com\n\n',stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,timeout=30,check=True)
    passwords=[x[9:] for x in credential.stdout.splitlines() if x.startswith(b'password=')];assert len(passwords)==1
    headers={'Authorization':'Bearer '+passwords[0].decode(),'User-Agent':'ErlangNet-Root-Proplists22-Capture','Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28'}
    def fetch(path,limit=8*1024*1024):
        with urllib.request.build_opener(NoRedirect()).open(urllib.request.Request(API+path,headers=headers),timeout=60) as response:
            assert response.status==200;data=response.read(limit+1);assert len(data)<=limit;return data
    def redirected(path,limit):
        try:response=urllib.request.build_opener(NoRedirect()).open(urllib.request.Request(API+path,headers=headers),timeout=60)
        except urllib.error.HTTPError as e:
            assert e.code==302;url=e.headers['Location'];u=urllib.parse.urlsplit(url);assert u.scheme=='https' and u.hostname and not u.username and not u.password
            response=urllib.request.build_opener(HttpsOnly()).open(urllib.request.Request(url,headers={'User-Agent':'ErlangNet-Root-Proplists22-Capture'}),timeout=60)
        with response:
            assert response.status==200;data=response.read(limit+1);assert 0<len(data)<=limit;return data
    runraw=fetch('/actions/runs/'+str(runid));run=json.loads(runraw)
    assert run['id']==runid and run['head_sha']==HEAD and run['head_branch']=='validation/binary-369-closure' and run['run_attempt']==1 and run['path']=='.github/workflows/'+workflow and run['event']==event and run['status']=='completed' and run['conclusion'] in ('success','failure','cancelled','timed_out','action_required','neutral','skipped','stale','startup_failure')
    jobsraw=fetch('/actions/runs/'+str(runid)+'/jobs?per_page=100');jobs=json.loads(jobsraw);assert jobs['total_count']==len(jobs['jobs'])==1
    job=jobs['jobs'][0];assert job['run_id']==runid and job['head_sha']==HEAD and job['run_attempt']==1 and job['status']=='completed' and job['conclusion']==run['conclusion']
    artifactsraw=fetch('/actions/runs/'+str(runid)+'/artifacts?per_page=100');artifacts=json.loads(artifactsraw);assert artifacts['total_count']==len(artifacts['artifacts']) and artifacts['total_count']<=1
    if run['conclusion']=='success':assert artifacts['total_count']==1
    artifact=artifacts['artifacts'][0] if artifacts['artifacts'] else None
    if artifact is not None:assert artifact['name']==artifactname and artifact['expired'] is False and artifact['workflow_run']['id']==runid and artifact['workflow_run']['head_sha']==HEAD
    output.mkdir();destination=output/('run-'+str(runid));destination.mkdir()
    for name,raw in [('run.json',runraw),('jobs.json',jobsraw),('artifacts.json',artifactsraw)]:
        with (destination/name).open('xb') as f:f.write(raw)
    write(destination/'run-map.json',dict(Head=HEAD,Run=runid,Attempt=1,Job=job['id'],Kind=a.kind))
    data=b''
    if artifact is not None:
        data=redirected('/actions/artifacts/'+str(artifact['id'])+'/zip',512*1024*1024)
        assert len(data)==artifact['size_in_bytes'] and 'sha256:'+H(data).lower()==artifact['digest']
        archivepath=destination/('artifact-'+str(artifact['id'])+'.zip');archivepath.write_bytes(data)
        target=destination/('artifact-'+str(artifact['id']));target.mkdir()
        with zipfile.ZipFile(archivepath) as archive:
            infos=archive.infolist();assert len(infos)==len({x.filename for x in infos}) and len(infos)<=100000 and sum(x.file_size for x in infos)<=1024**3
            folded=set()
            for item in infos:
                name=item.filename;normalized=name.rstrip('/');parts=PurePosixPath(normalized).parts
                assert name and '\x00' not in name and not name.startswith('/') and ':' not in name and '\\' not in name and all(x not in ('','.','..') for x in normalized.split('/')) and item.file_size<=256*1024*1024
                assert ((item.external_attr>>16)&0o170000) in (0,0o100000,0o040000)
                assert normalized.casefold() not in folded;folded.add(normalized.casefold())
                q=(target/normalized).resolve();assert q.is_relative_to(target.resolve())
                if item.is_dir():q.mkdir(parents=True,exist_ok=True)
                else:
                    q.parent.mkdir(parents=True,exist_ok=True)
                    with q.open('xb') as f:f.write(archive.read(item))
    log=redirected('/actions/jobs/'+str(job['id'])+'/logs',64*1024*1024)
    (destination/('job-'+str(job['id'])+'.log')).write_bytes(log)
    summary=dict(Output=str(destination),Run=runid,Job=job['id'],Artifact=artifact['id'] if artifact is not None else None,Head=HEAD,Conclusion=run['conclusion'],ZipSha256=H(data) if data else None,ZipBytes=len(data),TerminalRunAcquired=True,ValidationAcceptanceClaimed=False,LogBytes=len(log),ContentExecuted=False,HelperSha256=H(Path(__file__).read_bytes()),SourceBindingsSha256=H(bindings))
    write(destination/'capture-summary.json',summary)
    rows=[dict(Path=q.relative_to(destination).as_posix(),Bytes=q.stat().st_size,Sha256=H(q.read_bytes())) for q in sorted(destination.rglob('*')) if q.is_file()]
    write(destination/'download-manifest.json',rows)
    print(json.dumps(summary))
if __name__=='__main__':
    try:main()
    except Exception as e:raise SystemExit('Exact terminal proplists22 capture refused: '+type(e).__name__+'; private transport details suppressed') from None
