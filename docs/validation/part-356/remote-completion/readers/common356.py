"""Passive356 transport/source helpers. No network, SDK or downloaded-code import."""
from pathlib import Path,PurePosixPath
import hashlib,json,re,subprocess,zipfile
R=Path(__file__).resolve().parents[2]
HEAD='4416a1df0657c08c28f560ab2e96f38c4adabac3'
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
MANIFEST='E400B3393DFA539B65141EB4E36D77411180898D2A9FE7D4F692DE17715C02AD'
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
D=lambda b:hashlib.sha256(b).hexdigest().upper()
J=lambda p:json.loads(Path(p).read_bytes())
def W(p,v):
    with Path(p).open('xb') as f:f.write((json.dumps(v,indent=2)+'\n').encode())
def blobs(paths,head=HEAD,repository=R):
    paths=list(dict.fromkeys(paths));assert all(x and '\n' not in x and '\r' not in x and '\x00' not in x and not x.startswith('/') and '..' not in PurePosixPath(x).parts for x in paths)
    p=subprocess.run(['git','-C',str(repository),'cat-file','--batch'],input=''.join(head+':'+x+'\n' for x in paths).encode(),stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,check=True,timeout=120)
    out={};offset=0
    for name in paths:
        end=p.stdout.index(b'\n',offset);header=p.stdout[offset:end].split();assert len(header)==3 and header[1]==b'blob',name
        size=int(header[2]);start=end+1;out[name]=p.stdout[start:start+size];assert len(out[name])==size and p.stdout[start+size:start+size+1]==b'\n';offset=start+size+1
    assert offset==len(p.stdout);return out
def transport(kind):
    runid,jobid,artifactid,workflow,event,artifactname,folder={'ci':(39013848401,117341772810,12093887110,'validate.yml','push','validation','part356-remote-ci'),'original':(39013935267,117342020079,12094585437,'nif-entry-original.yml','workflow_dispatch','nif-entry-original-eleven','part356-remote-original')}[kind]
    c=R/'artifacts'/folder/('capture-'+str(artifactid));run=J(c/'run.json');jobs=J(c/'jobs.json');artifacts=J(c/'artifacts.json');mapping=J(c/'run-map.json')
    assert mapping==dict(Head=HEAD,Run=runid,Attempt=1,Job=jobid,Kind=kind)
    assert run['id']==runid and run['head_sha']==HEAD and run['head_branch']=='main' and run['status']=='completed' and run['conclusion']=='success' and run['run_attempt']==1 and run['path']=='.github/workflows/'+workflow and run['event']==event
    assert jobs['total_count']==len(jobs['jobs'])==1;job=jobs['jobs'][0]
    assert job['id']==jobid and job['run_id']==runid and job['head_sha']==HEAD and job['run_attempt']==1 and job['status']=='completed' and job['conclusion']=='success' and all(x['conclusion'] in ('success','skipped') for x in job['steps'])
    assert artifacts['total_count']==len(artifacts['artifacts'])==1;a=artifacts['artifacts'][0]
    assert a['id']==artifactid and a['name']==artifactname and a['expired'] is False and a['workflow_run']['head_sha']==HEAD and a['workflow_run']['id']==runid
    rows=J(c/'download-manifest.json');assert len(rows)==len({x['Path'] for x in rows})
    assert {q.relative_to(c).as_posix() for q in c.rglob('*') if q.is_file() and q.name!='download-manifest.json'}=={x['Path'] for x in rows}
    for row in rows:
        q=(c/row['Path']).resolve();assert q.is_relative_to(c.resolve()) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
    z=c/('artifact-'+str(artifactid)+'.zip');e=c/('artifact-'+str(artifactid));assert z.stat().st_size==a['size_in_bytes'] and 'sha256:'+H(z).lower()==a['digest']
    with zipfile.ZipFile(z) as archive:
        infos=archive.infolist();assert len(infos)==len({x.filename for x in infos}) and sum(x.file_size for x in infos)<=1024**3
        assert {q.relative_to(e).as_posix() for q in e.rglob('*') if q.is_file()}=={x.filename for x in infos if not x.is_dir()}
        for item in infos:
            n=item.filename.rstrip('/');assert n and not n.startswith('/') and ':' not in n and '\\' not in n and '..' not in PurePosixPath(n).parts and ((item.external_attr>>16)&0o170000)!=0o120000
            if not item.is_dir():assert (e/n).read_bytes()==archive.read(item)
    log=(c/('job-'+str(jobid)+'.log')).read_text(encoding='utf-8-sig');assert 'origin +'+HEAD+':refs/remotes/origin/main' in log
    return c,e,run,job,a,log
def normal(p):
    v=J(p);assert v['ExitCode']==0 and v['ParentReaped'] and v['StreamsClosed'] and v['CleanupFailure'] is None and not v['TerminationRequested'] and not v['RemainingGroupKillRequested']
    assert v['OwnedPid']==v['OwnedProcessGroup']>0 and v['ElapsedSeconds']>=0
    prefix=str(p).removesuffix('.process.json')
    for suffix,key in [('.stdout','StdoutBytes'),('.stderr','StderrBytes')]:assert Path(prefix+suffix).stat().st_size==v[key]
    return v
