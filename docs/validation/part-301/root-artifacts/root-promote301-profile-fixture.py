from pathlib import Path
import json,hashlib,subprocess,shutil
r=Path.cwd();a=r/'artifacts';iso=a/'root-process-info301-isolated';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());fpath=a/'root-part301-final-source-freeze.json';f=read(fpath)
assert read(a/'root-system-profile301-focus/corrected23-and40-exit.json')['ExitCode']==0
assert len((a/'root-system-profile301-focus/corrected23-and40.log').read_text(encoding='utf-8-sig').splitlines())==63
assert read(iso/'tokens12-before.json')==read(iso/'tokens12-after.json')
assert f['SourcePaths']==13
for row in f['Files']:assert h((r/row['Path']).read_bytes())==row['Sha256']
shutil.copyfile(fpath,a/'root-part301-second-source-freeze.json')
n='tools/Erlang.Differential/SystemProfileCases.cs';proposal=read(a/'system-profile-dead-receiver301-fixture-revision1/source-freeze.json');assert h((r/n).read_bytes())==proposal['ParentSha256'];shutil.copyfile(iso/'candidate'/n,r/n)
rows=[];paths=[x['Path'] for x in f['Files']]+[n]
for name in sorted(paths):
 data=(r/name).read_bytes()
 if name.endswith('.cs'):assert data==(iso/'candidate'/name).read_bytes()
 proc=subprocess.run(['git','show','HEAD:'+name],capture_output=True)
 rows.append(dict(Path=name,Bytes=len(data),Sha256=h(data),PreviousGitSha256=h(proc.stdout) if proc.returncode==0 else None))
f.update(SourcePaths=14,CSharpPaths=12,HistoricalSingleFixtureSourceMigration='erlang-system-profile-dead-receiver',HistoricalFixtureMigrationRuntimeChanged=False,SystemProfileFocusCasesPassed=23,CorrectedDeadReceiverRepetitionsPassed=40,OriginalCasesUnmodifiedReproductionTimedOut=True,DiagnosticSdkNoprocObserved=True,Files=rows)
fpath.write_text(json.dumps(f,indent=2)+'\n',encoding='utf-8')
for name in ['root-guard301-source.py','root-guard301-native.py']:
 p=a/name;t=p.read_text().replace('== 13\n','== 14\n').replace('== 11\n','== 12\n').replace("'SourcePaths': 13","'SourcePaths': 14").replace("'CSharpPaths': 11","'CSharpPaths': 12").replace('tokens11-','tokens12-');p.write_text(t,encoding='utf-8')
for kind in ['ordinary','native']:
 p=a/f'root-dispatch301-{kind}.ps1';t=p.read_text().replace('-ne 13','-ne 14');p.write_text(t,encoding='utf-8')
print(h(fpath.read_bytes()))
