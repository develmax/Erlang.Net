"""Prepare manifests for next part307 without touching published docs or index."""
from pathlib import Path
import hashlib,json,sys
r=Path.cwd();a=r/'artifacts';kind=sys.argv[1];rid={'ci':38752928298,'original':38752974930,'native':38752981264}[kind];d=a/f'pack306-watch-{rid}-terminal'
assert json.loads((a/f'pack306-terminal-{rid}-envelope-guard.json').read_bytes())['Head']=='ae4cd999e20e461f7f5917ec346ade31416e2874'
files=[p for p in d.rglob('*') if p.is_file()]
if kind=='ci':files.append(a/'root-part306-stage-index-guard.json')
for p in a.glob('pack306-*'):
 if not p.is_file():continue
 if (kind=='ci' and (p.name.startswith('pack306-ci-') or p.name==f'pack306-terminal-{rid}-envelope-guard.json')) or (kind=='native' and (p.name.startswith(('pack306-native-','pack306-original13-')) or p.name==f'pack306-terminal-{rid}-envelope-guard.json')) or (kind=='original' and (p.name.startswith('pack306-original-') or p.name==f'pack306-terminal-{rid}-envelope-guard.json' or 'history-differences' in p.name)):files.append(p)
if kind=='native':files += [p for p in (a/'pack306-original13-source-contracts').rglob('*') if p.is_file()]
rows=[]
for p in sorted(set(files)):
 assert not {'.git','bin','obj','__pycache__'}.intersection(p.parts) and 'LICENSE.local.txt' not in p.parts
 relative=p.relative_to(a).as_posix();data=p.read_bytes();rows.append({'SourcePath':p.relative_to(r).as_posix(),'Destination':f'docs/validation/part-307/previous-head306-{kind}/{relative}','Bytes':len(data),'Sha256':hashlib.sha256(data).hexdigest().upper()})
out=a/f'pack306-{kind}-detached-staging-manifest.json';assert not out.exists();out.write_text(json.dumps(rows,indent=2)+'\n');print(json.dumps({'Kind':kind,'Files':len(rows),'MainEdited':False,'GitMutated':False}))
