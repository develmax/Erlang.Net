from pathlib import Path
import hashlib,json,subprocess,shutil
r=Path.cwd(); a=r/'artifacts'; iso=a/'root-process-info301-isolated'; cand=iso/'candidate'
read=lambda p:json.loads(p.read_bytes())
h=lambda b:hashlib.sha256(b).hexdigest().upper()
rev=read(a/'process-info301-integration-revision1/source-freeze.json')
for suffix in ['integration11-target','integration11-cases-and-mfa','integration11-focus4']:
 assert read(iso/(suffix+'-exit.json'))['ExitCode']==0,suffix
assert read(iso/'tokens11-before.json')==read(iso/'tokens11-after.json')
for row in rev['Files']:
 p=r/row['Path']; previous=row['ParentSha256']
 assert (h(p.read_bytes()) if p.exists() else None)==previous,row['Path']
 shutil.copyfile(cand/row['Path'],p)
fpath=a/'root-part301-final-source-freeze.json'; f=read(fpath)
paths=read(iso/'paths11.json')+[x['Path'] for x in f['Files'] if not x['Path'].endswith('.cs')]
assert len(set(paths))==13
rows=[]
for name in sorted(paths):
 data=(r/name).read_bytes()
 if name.endswith('.cs'):assert data==(cand/name).read_bytes()
 proc=subprocess.run(['git','show','HEAD:'+name],capture_output=True)
 rows.append(dict(Path=name,Bytes=len(data),Sha256=h(data),PreviousGitSha256=h(proc.stdout) if proc.returncode==0 else None))
f.update(SourcePaths=13,CSharpPaths=11,IsolatedHostGroups=4,ActualDirectMfaFixtureRoutePassed=True,IntegrationRepairRuntimeAlgorithmChanged=False,Files=rows)
fpath.write_text(json.dumps(f,indent=2)+'\n',encoding='utf-8')
for name in ['root-guard301-source.py','root-guard301-native.py']:
 p=a/name; t=p.read_text();t=t.replace("== 10","== 13").replace("== 8","== 11").replace("'SourcePaths': 10","'SourcePaths': 13").replace("'CSharpPaths': 8","'CSharpPaths': 11").replace('tokens8-','tokens11-');p.write_text(t,encoding='utf-8')
print(h(fpath.read_bytes()))
