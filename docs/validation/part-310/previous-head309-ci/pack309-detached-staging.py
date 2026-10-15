"""Read-only future part310 staging proposal; terminal failures are preserved too."""
from pathlib import Path
import hashlib,json,sys
r=Path.cwd();a=r/'artifacts';kind=sys.argv[1];rid={'ci':38766513916,'beam':38766554804}[kind];d=a/f'pack309-watch-{rid}-terminal'
envelope=json.loads((a/f'pack309-terminal-{rid}-envelope-guard.json').read_bytes());assert envelope['Head']=='e829c53ef4323ea8cdd0948cd77c243efa787abd'
files=[p for p in d.rglob('*') if p.is_file()]
for name in ['pack309-read-two-runs.ps1','pack309-terminal-envelope.py','pack309-detached-staging.py','pack309-verify-terminals.py','root-part309-stage-index-guard.json','root-part309-beam-fixtures-dispatch-attempt.json','root-part309-beam-fixtures-dispatch-result.json']:
 p=a/name;assert p.is_file();files.append(p)
for p in a.glob('pack309-*'):
 if p.is_file() and (p.name.startswith(f'pack309-{kind}-') or p.name.startswith(f'pack309-terminal-{rid}-')) and 'staging-manifest' not in p.name:files.append(p)
if kind=='beam':
 replay=a/'pack309-beam-passive-replay1'
 if replay.exists():
  files.extend(p for p in (replay/'published-review-tools').rglob('*') if p.is_file())
  files.extend(p for p in replay.glob('*.attempt.json'))
  for name in ['load-nif-opcodes/load-nif-opcode-verifier-results.json','metadata-mutations/metadata-mutation-verifier-results.json']:
   p=replay/'evidence'/name
   if p.exists():files.append(p)
rows=[]
for p in sorted(set(files)):
 assert not {'.git','bin','obj','__pycache__','cache'}.intersection(p.parts)
 data=p.read_bytes();rows.append({'SourcePath':p.relative_to(r).as_posix(),'Destination':f'docs/validation/part-310/previous-head309-{kind}/'+p.relative_to(a).as_posix(),'Bytes':len(data),'Sha256':hashlib.sha256(data).hexdigest().upper(),'HashMode':'BinaryExact'})
out=a/f'pack309-{kind}-part310-detached-staging-manifest.json';assert not out.exists();out.write_text(json.dumps(rows,indent=2)+'\n');print(json.dumps({'Files':len(rows),'Conclusion':envelope['Conclusion'],'MainEdited':False,'GitMutated':False}))
