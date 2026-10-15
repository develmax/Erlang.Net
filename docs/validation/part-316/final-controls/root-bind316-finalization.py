from pathlib import Path
import json,hashlib
r=Path.cwd().resolve();a=r/'artifacts';d=a/'root-part316-finalization-bound1';out=r/'docs/validation/part-316';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,x:p.write_bytes((json.dumps(x,indent=2)+'\n').encode())
manifest=a/'part316-lean-copy-plan1/static-copy-manifest.json';assert h(manifest.read_bytes())=='846BFA3A7F2567679C511A41D737BE8B9D86A76269B7FBA62A5D3A662A800E44'
rows={}
def add(source,suffix,expected=None):
 source=source.resolve();assert source.is_relative_to(r) and source.is_file();target=(out/suffix).resolve();assert target.is_relative_to(out);raw=source.read_bytes();assert len(raw)<100*1024*1024
 row={'SourcePath':source.relative_to(r).as_posix(),'Destination':target.relative_to(r).as_posix(),'Bytes':len(raw),'Sha256':h(raw),'HashMode':'BinaryExact'}
 if expected:assert row['SourcePath']==expected['SourcePath'] and row['Bytes']==expected['Bytes'] and row['Sha256']==expected['Sha256']
 if row['Destination'] in rows:assert rows[row['Destination']]['SourcePath']==row['SourcePath'] and rows[row['Destination']]['Sha256']==row['Sha256'];return
 rows[row['Destination']]=row
for e in read(manifest):add(r/e['SourcePath'],Path(e['Destination']).relative_to('docs/validation/part-316'),e)
for family,target in [(a/'root-part316-main2','final-main'),(a/'root-part316-main-windows-native1','final-native')]:
 for source in sorted(family.iterdir()):
  if source.is_file():add(source,target+'/'+source.name)
add(a/'tests.json','final-main/tests.json');add(a/'integration-results.json','final-main/integration-results.json')
for name in ['module-readiness.json','module-readiness-scope.json','MODULE_READINESS.md','supported-mfas.json']:add(r/'docs'/name,name if name=='module-readiness.json' else 'readiness/'+name)
for name in ['static-copy-manifest.json','manifest-plan.json','manifest-freeze.json','external-api-artifact-index.json','prepare-manifest.py','README.md']:add(a/'part316-lean-copy-plan1'/name,'copy-plan/'+name)
for name in ['root-finalize316.py','root-bind316-finalization.py','root316-local-gates-and-lean-finalizer-beam310-peer.json','root-part316-main-windows-native1-beam310-independent-guard.json','root-correct316-registry-count2.py','root-correct316-registry-count.py','root-run316-main2-full.ps1']:
 p=a/name
 if p.exists():add(p,'final-controls/'+name)
for name in ['guard316-full.py','guard316-main-native.py','run316-main-native.py','proposal-freeze.json']:add(a/'part316-main-native-proposal2'/name,'final-controls/native-revision2/'+name)
for name in ['english-entry.md','runtime-note.txt','config-scope.json']:add(d/name,'final-controls/'+name)
cfg=read(d/'config-scope.json');cfg.update(ActualReadinessExitCode=read(a/'root-part316-main2/readiness-exit.json')['ExitCode'],EnglishEntrySha256=h((d/'english-entry.md').read_bytes()),VolumeBeforeSha256=h((r/'docs/changelog/0047.md').read_bytes()),ChangelogBeforeSha256=h((r/'CHANGELOG.md').read_bytes()),MetadataBefore={n:h((r/n).read_bytes()) for n in ['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md','docs/OTP-PORTS.md']},Files=[rows[k] for k in sorted(rows)])
assert not (d/'config-finalize.json').exists();assert cfg['ActualReadinessExitCode']==0;write(d/'config-finalize.json',cfg);print(json.dumps({'Copies':len(rows),'TotalBytes':sum(e['Bytes'] for e in rows.values()),'ConfigSha256':h((d/'config-finalize.json').read_bytes())}))
