from pathlib import Path
import subprocess,json,hashlib,sys,time,urllib.request
r=Path.cwd();w=r/'artifacts/ordsets372-original-validation';o=r/'artifacts/ordsets372/ci';o.mkdir(parents=True,exist_ok=True);head='76c034724be1b21da8c66ada0ec164307386cfcd';branch='validation/ordsets-erlang-372';wf='proplist-get-value-original.yml';assert subprocess.check_output(['git','-C',str(w),'rev-parse','HEAD']).decode().strip()==head
keys={'ordsets_manifest_sha256':'Erlang.Ordsets372OriginalObservations','numeric_manifest_sha256':'Erlang.Numeric372OriginalObservations','usort_manifest_sha256':'Erlang.OrdsetsUsort372OriginalObservations'};inputs={'expected_head':head}
for key,folder in keys.items():
 b=subprocess.check_output(['git','-C',str(w),'show',head+':tools/'+folder+'/source-inputs.corrected.json']);assert b==(w/'tools'/folder/'source-inputs.corrected.json').read_bytes();inputs[key]=hashlib.sha256(b).hexdigest().upper()
c=subprocess.run(['git','credential','fill'],input=b'protocol=https\nhost=github.com\n\n',stdout=subprocess.PIPE,stderr=subprocess.DEVNULL,check=True,timeout=30);secret=[x[9:] for x in c.stdout.splitlines() if x.startswith(b'password=')];assert len(secret)==1
headers={'Authorization':'Bearer '+secret[0].decode(),'Accept':'application/vnd.github+json','User-Agent':'ErlangNet-Ordsets372'};api='https://api.github.com/repos/develmax/Erlang.Net'
def get(path):
 with urllib.request.urlopen(urllib.request.Request(api+path,headers=headers),timeout=40) as z:return json.load(z)
if sys.argv[1]=='dispatch':
 intent=o/'dispatch-intent.json';assert not intent.exists();intent.write_bytes((json.dumps(dict(Head=head,Branch=branch,Workflow=wf,Inputs=inputs,Unix=time.time()),indent=2)+'\n').encode());data=json.dumps(dict(ref=branch,inputs=inputs)).encode()
 with urllib.request.urlopen(urllib.request.Request(api+'/actions/workflows/'+wf+'/dispatches',data=data,headers=headers,method='POST'),timeout=40) as z:assert z.status==204
 (o/'dispatch-accepted.json').write_bytes((json.dumps(dict(Head=head,Status=204),indent=2)+'\n').encode());print(json.dumps(dict(Status='DISPATCHED_ONCE',Head=head,Values=167,StackProfiles=835)))
elif sys.argv[1]=='status':
 runs=get('/actions/workflows/'+wf+'/runs?head_sha='+head+'&event=workflow_dispatch&per_page=20')['workflow_runs'];runs=[x for x in runs if x['head_sha']==head and x['head_branch']==branch];assert len(runs)<=1
 if not runs:print('NOT_VISIBLE_YET');sys.exit(0)
 run=runs[0];jobs=get('/actions/runs/'+str(run['id'])+'/jobs?per_page=100');assert jobs['total_count']<=1;(o/('status-'+str(time.time_ns())+'.json')).write_bytes((json.dumps(dict(Run=run,Jobs=jobs),indent=2)+'\n').encode());print(json.dumps(dict(Id=run['id'],Head=head,Status=run['status'],Conclusion=run['conclusion'],Steps=[dict(Name=s['name'],Conclusion=s['conclusion']) for j in jobs['jobs'] for s in j['steps'] if s['status']=='in_progress' or s['conclusion']=='failure'],Url=run['html_url'])))
else:raise ValueError('dispatch/status')
