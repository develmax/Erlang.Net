import runpy,json,collections
from pathlib import Path
ns=runpy.run_path('artifacts/root-formatter-compare.py');obs=json.loads(Path('artifacts/part336-remote-new-review/format/artifact-12063070500/capture/observations.json').read_bytes());root=Path('artifacts/part336-remote-new-review/format/artifact-12063070500/capture');counts=collections.Counter()
for r in obs['Cases']:
 if r['Track']=='report':
  v=ns['decode']((root/('report--'+r['Name'])/'returned.etf').read_bytes());counts[v[1][0][1] if v[0]=='tuple' else v[0]]+=1
print(dict(counts))

