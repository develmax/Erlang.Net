from pathlib import Path
import sys,json,subprocess,urllib.request,time,hashlib
r=Path.cwd();o=r/'artifacts/erlang371-original1/ci';o.mkdir(exist_ok=True);w=r/'artifacts/proplists-stack-ci-validation1';branch='validation/erlang-371-conversions';wf='proplist-get-value-original.yml'
head=subprocess.check_output(['git','-C',str(w),'rev-parse','HEAD']).decode().strip()
assert subprocess.check_output(['git','-C',str(w),'branch','--show-current']).decode().strip()==branch
def blob(p):return subprocess.check_output(['git','-C',str(w),'show',head+':'+p])
manifest=blob('tools/Erlang.ProplistFamilyObservations/source-inputs.corrected.json');mh=hashlib.sha256(manifest).hexdigest().upper();assert mh=='9A216480984F46A35178F3606B04D515E2639A71DB1A848578BEEB9C2B0871AF'
c=subprocess.run(['git','credential','fill'],input=b'protocol=https\nhost=github.com\n\n',stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,check=True,timeout=30);passwords=[x[9:] for x in c.stdout.splitlines() if x.startswith(b'password=')];assert len(passwords)==1
headers={'Authorization':'Bearer '+passwords[0].decode(),'Accept':'application/vnd.github+json','User-Agent':'ErlangNet-Proplists22-Original'};api='https://api.github.com/repos/develmax/Erlang.Net'
def get(p):
 with urllib.request.urlopen(urllib.request.Request(api+p,headers=headers),timeout=30) as q:return json.loads(q.read(4*1024*1024))
if sys.argv[1]=='dispatch':
 intent=o/'dispatch-intent.json';assert not intent.exists()
 paths=['.github/workflows/'+wf,'tools/Erlang.ProplistFamilyObservations/ci-gates.py','tools/Erlang.ProplistFamilyObservations/source-inputs.corrected.json','tools/Erlang.ProplistFamilyObservations/profile.json','tools/Erlang.OrdsetsCoreOriginalObservations/source-inputs.corrected.json','tools/Erlang.OrdsetsCoreOriginalObservations/ci-preflight.py']
 v=dict(Head=head,Branch=branch,Workflow=wf,SourceInputsSha256=mh,IssuedUnixSeconds=time.time(),ValidationOnly=True,RuntimeImplementationsPublished=0,Bindings=[dict(Path=p,Bytes=len(blob(p)),Sha256=hashlib.sha256(blob(p)).hexdigest().upper()) for p in paths]);intent.write_text(json.dumps(v,indent=2)+'\n')
 body=json.dumps(dict(ref=branch,inputs=dict(expected_head=head,source_inputs_sha256=mh))).encode()
 with urllib.request.urlopen(urllib.request.Request(api+'/actions/workflows/'+wf+'/dispatches',data=body,headers=headers,method='POST'),timeout=30) as q:assert q.status==204
 (o/'dispatch-response.json').write_text(json.dumps(dict(Status=204,Head=head,Branch=branch),indent=2)+'\n');print(json.dumps(dict(Status='DISPATCHED_ONCE',Head=head,Branch=branch)))
elif sys.argv[1]=='status':
 runs=get('/actions/workflows/'+wf+'/runs?head_sha='+head+'&event=workflow_dispatch&per_page=20')['workflow_runs'];runs=[x for x in runs if x['head_sha']==head and x['head_branch']==branch and x['event']=='workflow_dispatch'];assert len(runs)<=1
 if not runs:print('DISPATCH_NOT_VISIBLE_YET');sys.exit(0)
 x=runs[0];jobs=get('/actions/runs/'+str(x['id'])+'/jobs?per_page=100');assert jobs['total_count']<=1
 (o/('status-'+str(time.time_ns())+'.json')).write_text(json.dumps(dict(Run=x,Jobs=jobs),indent=2)+'\n');print(json.dumps(dict(Head=head,Run=x['id'],Status=x['status'],Conclusion=x['conclusion'],Url=x['html_url'],Steps=[dict(Name=s['name'],Conclusion=s['conclusion']) for j in jobs['jobs'] for s in j['steps'] if s['status']=='in_progress' or s['conclusion']=='failure'])))
else:raise ValueError('dispatch or status required')
