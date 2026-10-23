from pathlib import Path
import hashlib,json,shutil
r=Path.cwd();old=r/'artifacts/library446-ddll-frontier-proposal2';new=r/'artifacts/library446-ddll-frontier-proposal3'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
save=lambda p,v:p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
assert not new.exists() and sha(old/'manifest.json')=='62D8C1DBC5710898280A402E47DF9CDE5181F94DE63D9E6386825FBF1F265E9B'
for row in json.loads((old/'manifest.json').read_bytes())['Files']:assert sha(old/row['Path'])==row['Sha256']
shutil.copytree(old,new)
worker=new/'original/beam_ddll_frontier446.erl';before=sha(worker);text=worker.read_text()
oldline=' A!stop,B!stop,DownA=join_helper(A),DownB=join_helper(B),'
assert text.count(oldline)==1
text=text.replace(oldline,' Owners=two_owner_snapshot(A,B),\n'+oldline)
assert text.count('true->{DownA,DownB,port_driver_events(Port,R)};')==1
text=text.replace('true->{DownA,DownB,port_driver_events(Port,R)};','true->{Owners,DownA,DownB,port_driver_events(Port,R)};')
assert text.count('{DownA,DownB,State,Count,Data,wait(R)}')==1
text=text.replace('{DownA,DownB,State,Count,Data,wait(R)}','{Owners,DownA,DownB,State,Count,Data,wait(R)}')
marker='open_named(Name)->';assert text.count(marker)==1
extra='''two_owner_snapshot(A,B)->
 Raw=info(processes),2=length(Raw),1=owner_count(A,Raw),1=owner_count(B,Raw),
 [{case Owner of A->first;B->second;_->unexpected_owner end,N}||{Owner,N}<-Raw].
owner_count(_,[])->0;
owner_count(P,[{P,N}|T])->N+owner_count(P,T);
owner_count(P,[_|T])->owner_count(P,T).
'''
text=text.replace(marker,extra+marker);worker.write_text(text,encoding='utf-8')
profile=json.loads((new/'profile.json').read_bytes());rows=[row for row in profile['Sources'] if row['Path']=='original/beam_ddll_frontier446.erl'];assert len(rows)==1 and rows[0]['Sha256']==before;rows[0]['Sha256']=sha(worker);save(new/'profile.json',profile)
save(new/'root-pre-execution-review.json',dict(Executed=False,ProductChanged=False,WorkerBefore=before,WorkerAfter=sha(worker),Delta='Before either stop, assert genuine two distinct loaded owners each count1, retain actual source role/count list in both main results. Same six cases, consumer bytes unchanged.',Reason='Prevent accepting a two-owner case that never established two actual loaded references.',NoGoldenOrComparisonRewrite=True))
capture=json.loads((new/'capture-manifest.json').read_bytes())
for row in capture['Files']:row.update(Bytes=(new/row['Path']).stat().st_size,Sha256=sha(new/row['Path']))
save(new/'capture-manifest.json',capture);save(new/'capture-freeze.json',dict(ManifestSha256=sha(new/'capture-manifest.json'),Immutable=True,RootReviewedPreExecutionSetupFence=True))
with (new/'README.md').open('a',encoding='utf-8') as f:f.write('\nRoot successor3: before either helper stop request, query genuine DDLL processes and require exactly two distinct owners with one loaded reference each; include actual ordered source role/count observations in both main rows. This tightens setup evidence for the same two planned cases; all consumer bytes and product source stay unchanged. No original execution occurred before this review.\n')
files=[dict(Path=p.relative_to(new).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted(new.rglob('*')) if p.is_file() and p.name not in {'manifest.json','freeze.json'}]
save(new/'manifest.json',dict(Files=files));save(new/'freeze.json',dict(ManifestSha256=sha(new/'manifest.json'),Immutable=True,PayloadFiles=len(files),RootReviewedPreExecutionSetupFence=True))
print(json.dumps(dict(Passed=True,CaptureManifest=sha(new/'capture-manifest.json'),Manifest=sha(new/'manifest.json'),Worker=sha(worker))))
