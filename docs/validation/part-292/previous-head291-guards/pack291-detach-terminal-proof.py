from pathlib import Path
import hashlib,json
r=Path.cwd();a=r/'artifacts';sha=lambda raw:hashlib.sha256(raw).hexdigest().upper();rows=[]
for kind,rid in [('ci',38698195062),('original',38698216243)]:
 guard=a/f'pack291-{kind}-{rid}-independent-guard.json';g=json.loads(guard.read_bytes())
 assert g['Head']=='9c5b8a809b2a6bcd76965cdeae2d9dbd2ec873e2' and g['Failed']==0
 for source in sorted((a/f'pack291-watch-{rid}-terminal').rglob('*')):
  if source.is_file():rows.append((source,Path('docs/validation/part-292')/f'previous-head291-{kind}'/source.relative_to(a)))
 rows.append((guard,Path('docs/validation/part-292')/'previous-head291-guards'/guard.name))
for name in ['pack291-terminal-guard.py','pack291-terminal-guard-initial-literal-only.py','pack291-read-two-runs.ps1','pack291-detach-terminal-proof.py','pack291-watch-shell-launch-failure.json','root-part291-ordinary-dispatch-attempt.json','root-part291-ordinary-dispatch-result.json']:
 source=a/name
 if source.is_file():rows.append((source,Path('docs/validation/part-292')/'previous-head291-guards'/name))
for source in sorted(a.glob('pack291-*-failure*.json')):
 pair=(source,Path('docs/validation/part-292')/'previous-head291-guards'/source.name)
 if pair not in rows:rows.append(pair)
assert len({str(dst) for _,dst in rows})==len(rows)
manifest=[{'SourcePath':src.relative_to(r).as_posix(),'Destination':dst.as_posix(),'Bytes':src.stat().st_size,'Sha256':sha(src.read_bytes())} for src,dst in rows]
out=a/'pack291-detached-staging-manifest.json';assert not out.exists();out.write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8');print(json.dumps({'Files':len(manifest),'Sha256':sha(out.read_bytes())}))
