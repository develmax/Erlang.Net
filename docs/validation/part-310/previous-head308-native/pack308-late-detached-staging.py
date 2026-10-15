"""Prepare manifests for late part310 without touching published docs or index."""
from pathlib import Path
import hashlib,json,sys
r=Path.cwd();a=r/'artifacts';kind=sys.argv[1];rid={'ci':38763479716,'original':38763659694,'native':38763668519}[kind];d=a/f'pack308-watch-{rid}-terminal'
assert json.loads((a/f'pack308-terminal-{rid}-envelope-guard.json').read_bytes())['Head']=='e1d8a999faac113d98ebf2d4471f80ef30daf84a'
files=[p for p in d.rglob('*') if p.is_file()]
if kind=='ci':files.append(a/'root-part308-stage-index-guard.json')
if kind=='ci':
 for name in ['root-dispatch308-native-initial-hash-precondition.ps1','root-bind308-native-dispatch.py','root-dispatch308-native.ps1','root-dispatch308-ordinary.ps1','root-part308-native-dispatch-attempt.json','root-part308-native-dispatch-result.json','root-part308-ordinary-dispatch-attempt.json','root-part308-ordinary-dispatch-result.json']:
  p=a/name;assert p.is_file();files.append(p)
 for name in ['pack308-read-three-runs.ps1','pack308-terminal-envelope.py','pack308-verify-terminals.py','pack308-detached-staging.py']:
  files.append(a/name)
for p in a.glob('pack308-*'):
 if not p.is_file():continue
 if (kind=='ci' and (p.name.startswith('pack308-ci-') or p.name==f'pack308-terminal-{rid}-envelope-guard.json')) or (kind=='native' and (p.name.startswith(('pack308-native-','pack308-original13-')) or p.name==f'pack308-terminal-{rid}-envelope-guard.json')) or (kind=='original' and (p.name.startswith('pack308-original-') or p.name==f'pack308-terminal-{rid}-envelope-guard.json' or 'historical-differences' in p.name)):files.append(p)
if kind=='native':files += [p for p in (a/'pack308-original13-source-contracts').rglob('*') if p.is_file()]
rows=[]
for p in sorted(set(files)):
 assert not {'.git','bin','obj','__pycache__'}.intersection(p.parts) and 'LICENSE.local.txt' not in p.parts
 relative=p.relative_to(a).as_posix();data=p.read_bytes();rows.append({'SourcePath':p.relative_to(r).as_posix(),'Destination':f'docs/validation/part-310/previous-head308-{kind}/{relative}','Bytes':len(data),'Sha256':hashlib.sha256(data).hexdigest().upper()})
out=a/f'pack308-{kind}-part310-detached-staging-manifest.json';assert not out.exists();out.write_text(json.dumps(rows,indent=2)+'\n');print(json.dumps({'Kind':kind,'Files':len(rows),'MainEdited':False,'GitMutated':False}))
